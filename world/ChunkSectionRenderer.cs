using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class ChunkSectionRenderer {
    public ChunkSection section;

    public BlockVAO vao;
    public BlockVAO watervao;

    public bool hasTranslucentBlocks;

    public Shader shader;
    public int uMVP;

    public bool isEmpty = false;

    public readonly GL GL;

    // we cheated GC! there is only one list preallocated
    // we need 16x16x16 blocks, each block has max. 24 vertices
    // for indices we need the full 36
    public static readonly List<BlockVertex> chunkVertices = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * 24);
    public static readonly List<ushort> chunkIndices = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * 36);
    public static readonly Dictionary<int, int> neighbours = new(27);

    static object meshingLock = new();

    static Vector3D<int>[][] offsetTable = [
        // west
        [
            new(-1, 0, -1), new(-1, 1, 0), new(-1, 1, -1),
            new(-1, 0, 1), new(-1, 1, 0), new(-1, 1, 1),
            new(-1, 0, -1), new(-1, -1, 0), new(-1, -1, -1),
            new(-1, 0, 1), new(-1, -1, 0), new(-1, -1, 1)
        ],
        // east
        [
            new(1, 0, 1), new(1, 1, 0), new(1, 1, 1),
            new(1, 0, -1), new(1, 1, 0), new(1, 1, -1),
            new(1, 0, 1), new(1, -1, 0), new(1, -1, 1),
            new(1, 0, -1), new(1, -1, 0), new(1, -1, -1),
        ],
        // south
        [
            new(1, 0, -1), new(0, 1, -1), new(1, 1, -1),
            new(-1, 0, -1), new(0, 1, -1), new(-1, 1, -1),
            new(1, 0, -1), new(0, -1, -1), new(1, -1, -1),
            new(-1, 0, -1), new(0, -1, -1), new(-1, -1, -1)
        ],
        // north
        [
            new(-1, 0, 1), new(0, 1, 1), new(-1, 1, 1),
            new(1, 0, 1), new(0, 1, 1), new(1, 1, 1),
            new(-1, 0, 1), new(0, -1, 1), new(-1, -1, 1),
            new(1, 0, 1), new(0, -1, 1), new(1, -1, 1),
        ],
        // down
        [
            new(0, -1, 1), new(-1, -1, 0), new(-1, -1, 1),
            new(0, -1, 1), new(1, -1, 0), new(1, -1, 1),
            new(0, -1, -1), new(-1, -1, 0), new(-1, -1, -1),
            new(0, -1, -1), new(1, -1, 0), new(1, -1, -1),
        ],
        // up
        [
            new(0, 1, -1), new(-1, 1, 0), new(-1, 1, -1),
            new(0, 1, -1), new(1, 1, 0), new(1, 1, -1),
            new(0, 1, 1), new(-1, 1, 0), new(-1, 1, 1),
            new(0, 1, 1), new(1, 1, 0), new(1, 1, 1),
        ]
    ];

    public ChunkSectionRenderer(ChunkSection section) {
        this.section = section;
        shader = section.chunk.world.renderer.shader;
        uMVP = shader.getUniformLocation("uMVP");
    }

    /// <summary>
    /// TODO store the number of blocks in the chunksection and only allocate the vertex list up to that length
    /// </summary>
    public void meshChunk() {
        var sw = new Stopwatch();
        sw.Start();
        vao = new BlockVAO();
        watervao = new BlockVAO();
        // first we render everything which is NOT translucent
        int opaqueCount;
        int translucentCount;
        lock (meshingLock) {
            constructVertices(i => i != 0 && !Blocks.isTranslucent(i), i => !Blocks.isSolid(i));
            vao.bind();
            var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
            var finalIndices = CollectionsMarshal.AsSpan(chunkIndices);
            vao.upload(finalVertices, finalIndices);
            opaqueCount = chunkIndices.Count;
        }

        lock (meshingLock) {
            // then we render everything which is translucent (water for now)
            constructVertices(Blocks.isTranslucent, i => !Blocks.isTranslucent(i) && !Blocks.isSolid(i));
            if (chunkIndices.Count > 0) {
                watervao.bind();
                var tFinalVertices = CollectionsMarshal.AsSpan(chunkVertices);
                var tFinalIndices = CollectionsMarshal.AsSpan(chunkIndices);
                watervao.upload(tFinalVertices, tFinalIndices);
                hasTranslucentBlocks = true;
                //world.sortedTransparentChunks.Add(this);
            }
            else {
                hasTranslucentBlocks = false;
                //world.sortedTransparentChunks.Remove(this);
            }
            translucentCount = chunkIndices.Count;
        }

        if (opaqueCount == 0 && translucentCount == 0) {
            isEmpty = true;
        }
        else {
            isEmpty = false;
        }
        Console.Out.WriteLine($"Meshing: {sw.Elapsed.TotalMicroseconds}us");
        sw.Stop();
    }

    public ushort toVertex(float f) {
        return (ushort)(f / 16f * ushort.MaxValue);
    }



    public bool isVisible(BoundingFrustum frustum) {
        return frustum.Contains(new BoundingBox(section.box.min.toVec3(), section.box.max.toVec3())) != ContainmentType.Disjoint;
    }

    public void drawChunk(PlayerCamera camera) {
        drawOpaque(camera);
        drawTransparent(camera);
        //Game.instance.metrics.renderedChunks += 1;
        //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    public void drawOpaque(PlayerCamera camera) {
        if (!isEmpty && isVisible(camera.frustum)) {
            vao.bind();
            //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

            uint renderedVerts = vao.render();
            Game.instance.metrics.renderedVerts += (int)renderedVerts;
            Game.instance.metrics.renderedChunks += 1;
        }
    }

    public void drawTransparent(PlayerCamera camera) {
        if (hasTranslucentBlocks && !isEmpty && isVisible(camera.frustum)) {
            watervao.bind();
            uint renderedTransparentVerts = watervao.render();
            Game.instance.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    // if neighbourTest returns true for adjacent block, render, if it returns false, don't
    private void constructVertices(Func<int, bool> whichBlocks, Func<int, bool> neighbourTest) {
        // clear arrays before starting
        chunkVertices.Clear();
        chunkIndices.Clear();
        neighbours.Clear();

        ushort i = 0;
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int y = 0; y < Chunk.CHUNKSIZE; y++) {
                for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                    if (whichBlocks(section.getBlockInChunk(x, y, z))) {
                        var wpos = section.world.toWorldPos(section.chunkX, section.chunkY, section.chunkZ, x, y, z);
                        int wx = wpos.X;
                        int wy = wpos.Y;
                        int wz = wpos.Z;
                        //Console.Out.WriteLine(section.getBlockInChunk(x, y, z));
                        //Console.Out.WriteLine(section.world.getBlock(wx, wy, wz));

                        Block b = Blocks.get(section.world.getBlock(wx, wy, wz));

                        // calculate texcoords
                        var westCoords = b.uvs[0];
                        var west = Block.texCoords(westCoords);
                        var westMax = Block.texCoords(westCoords.u + 1, westCoords.v + 1);
                        var westU = west.X;
                        var westV = west.Y;
                        var westMaxU = westMax.X;
                        var westMaxV = westMax.Y;

                        var eastCoords = b.uvs[1];
                        var east = Block.texCoords(eastCoords);
                        var eastMax = Block.texCoords(eastCoords.u + 1, eastCoords.v + 1);
                        var eastU = east.X;
                        var eastV = east.Y;
                        var eastMaxU = eastMax.X;
                        var eastMaxV = eastMax.Y;

                        var southCoords = b.uvs[2];
                        var south = Block.texCoords(southCoords);
                        var southMax = Block.texCoords(southCoords.u + 1, southCoords.v + 1);
                        var southU = south.X;
                        var southV = south.Y;
                        var southMaxU = southMax.X;
                        var southMaxV = southMax.Y;

                        var northCoords = b.uvs[3];
                        var north = Block.texCoords(northCoords);
                        var northMax = Block.texCoords(northCoords.u + 1, northCoords.v + 1);
                        var northU = north.X;
                        var northV = north.Y;
                        var northMaxU = northMax.X;
                        var northMaxV = northMax.Y;

                        var bottomCoords = b.uvs[4];
                        var bottom = Block.texCoords(bottomCoords);
                        var bottomMax = Block.texCoords(bottomCoords.u + 1, bottomCoords.v + 1);
                        var bottomU = bottom.X;
                        var bottomV = bottom.Y;
                        var bottomMaxU = bottomMax.X;
                        var bottomMaxV = bottomMax.Y;

                        var topCoords = b.uvs[5];
                        var top = Block.texCoords(topCoords);
                        var topMax = Block.texCoords(topCoords.u + 1, topCoords.v + 1);
                        var topU = top.X;
                        var topV = top.Y;
                        var topMaxU = topMax.X;
                        var topMaxV = topMax.Y;

                        float offset = 0.0004f;

                        float xmin = wx;
                        float ymin = wy;
                        float zmin = wz;
                        float xmax = wx + 1f;
                        float ymax = wy + 1f;
                        float zmax = wz + 1f;

                        Func<int, bool> test = bl => bl != -1 && Blocks.isSolid(bl);
                        // calculate AO for all 8 vertices
                        // this is garbage but we'll deal with it later
                        // bottom
                        var w = section.world;
                        // cache blocks
                        for (int cx = wx - 1; cx <= wx + 1; cx++) {
                            for (int cy = wy - 1; cy <= wy + 1; cy++) {
                                for (int cz = wz - 1; cz <= wz + 1; cz++) {
                                    neighbours[new Vector3D<int>(cx, cy, cz).GetHashCode()] = w.getBlockUnsafe(cx, cy, cz);
                                }
                            }
                        }

                        // helper function to get blocks from cache
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        int getBlockFromCache(int x, int y, int z) {
                            return neighbours[new Vector3D<int>(x, y, z).GetHashCode()];
                        }

                        var aoXminZminYmin = calculateAO(test, getBlockFromCache(wx - 1, wy - 1, wz),
                            getBlockFromCache(wx, wy - 1, wz - 1),
                            getBlockFromCache(wx - 1, wy - 1, wz - 1));
                        var aoXmaxZminYmin = calculateAO(test, getBlockFromCache(wx + 1, wy - 1, wz),
                            getBlockFromCache(wx, wy - 1, wz - 1),
                            getBlockFromCache(wx + 1, wy - 1, wz - 1));
                        var aoXminZmaxYmin = calculateAO(test, getBlockFromCache(wx - 1, wy - 1, wz),
                            getBlockFromCache(wx, wy - 1, wz + 1),
                            getBlockFromCache(wx - 1, wy - 1, wz + 1));
                        var aoXmaxZmaxYmin = calculateAO(test, getBlockFromCache(wx + 1, wy - 1, wz),
                            getBlockFromCache(wx, wy - 1, wz + 1),
                            getBlockFromCache(wx + 1, wy - 1, wz + 1));

                        // top
                        var aoXminZminYmax = calculateAO(test, getBlockFromCache(wx - 1, wy + 1, wz),
                            getBlockFromCache(wx, wy + 1, wz - 1),
                            getBlockFromCache(wx - 1, wy + 1, wz - 1));
                        var aoXmaxZminYmax = calculateAO(test, getBlockFromCache(wx + 1, wy + 1, wz),
                            getBlockFromCache(wx, wy + 1, wz - 1),
                            getBlockFromCache(wx + 1, wy + 1, wz - 1));
                        var aoXminZmaxYmax = calculateAO(test, getBlockFromCache(wx - 1, wy + 1, wz),
                            getBlockFromCache(wx, wy + 1, wz + 1),
                            getBlockFromCache(wx - 1, wy + 1, wz + 1));
                        var aoXmaxZmaxYmax = calculateAO(test, getBlockFromCache(wx + 1, wy + 1, wz),
                            getBlockFromCache(wx, wy + 1, wz + 1),
                            getBlockFromCache(wx + 1, wy + 1, wz + 1));

                        // west
                        var west1 = calculateAO(test, getBlockFromCache(wx - 1, wy, wz + 1),
                            getBlockFromCache(wx - 1, wy + 1, wz),
                            getBlockFromCache(wx - 1, wy + 1, wz + 1));
                        var west2 = calculateAO(test, getBlockFromCache(wx - 1, wy, wz + 1),
                            getBlockFromCache(wx - 1, wy - 1, wz),
                            getBlockFromCache(wx - 1, wy - 1, wz + 1));
                        var west3 = calculateAO(test, getBlockFromCache(wx - 1, wy, wz - 1),
                            getBlockFromCache(wx - 1, wy - 1, wz),
                            getBlockFromCache(wx - 1, wy - 1, wz - 1));
                        var west4 = calculateAO(test, getBlockFromCache(wx - 1, wy, wz - 1),
                            getBlockFromCache(wx - 1, wy + 1, wz),
                            getBlockFromCache(wx - 1, wy + 1, wz - 1));

                        // east
                        var east1 = calculateAO(test, getBlockFromCache(wx + 1, wy, wz - 1),
                            getBlockFromCache(wx + 1, wy + 1, wz),
                            getBlockFromCache(wx + 1, wy + 1, wz - 1));
                        var east2 = calculateAO(test, getBlockFromCache(wx + 1, wy, wz - 1),
                            getBlockFromCache(wx + 1, wy - 1, wz),
                            getBlockFromCache(wx + 1, wy - 1, wz - 1));
                        var east3 = calculateAO(test, getBlockFromCache(wx + 1, wy, wz + 1),
                            getBlockFromCache(wx + 1, wy - 1, wz),
                            getBlockFromCache(wx + 1, wy - 1, wz + 1));
                        var east4 = calculateAO(test, getBlockFromCache(wx + 1, wy, wz + 1),
                            getBlockFromCache(wx + 1, wy + 1, wz),
                            getBlockFromCache(wx + 1, wy + 1, wz + 1));

                        // south
                        var south1 = calculateAO(test, getBlockFromCache(wx - 1, wy, wz - 1),
                            getBlockFromCache(wx, wy + 1, wz - 1),
                            getBlockFromCache(wx - 1, wy + 1, wz - 1));
                        var south2 = calculateAO(test, getBlockFromCache(wx - 1, wy, wz - 1),
                            getBlockFromCache(wx, wy - 1, wz - 1),
                            getBlockFromCache(wx - 1, wy - 1, wz - 1));
                        var south3 = calculateAO(test, getBlockFromCache(wx + 1, wy, wz - 1),
                            getBlockFromCache(wx, wy - 1, wz - 1),
                            getBlockFromCache(wx + 1, wy - 1, wz - 1));
                        var south4 = calculateAO(test, getBlockFromCache(wx + 1, wy, wz - 1),
                            getBlockFromCache(wx, wy + 1, wz - 1),
                            getBlockFromCache(wx + 1, wy + 1, wz - 1));

                        // north
                        var north1 = calculateAO(test, getBlockFromCache(wx + 1, wy, wz + 1),
                            getBlockFromCache(wx, wy + 1, wz + 1),
                            getBlockFromCache(wx + 1, wy + 1, wz + 1));
                        var north2 = calculateAO(test, getBlockFromCache(wx + 1, wy, wz + 1),
                            getBlockFromCache(wx, wy - 1, wz + 1),
                            getBlockFromCache(wx + 1, wy - 1, wz + 1));
                        var north3 = calculateAO(test, getBlockFromCache(wx - 1, wy, wz + 1),
                            getBlockFromCache(wx, wy - 1, wz + 1),
                            getBlockFromCache(wx - 1, wy - 1, wz + 1));
                        var north4 = calculateAO(test, getBlockFromCache(wx - 1, wy, wz + 1),
                            getBlockFromCache(wx, wy + 1, wz + 1),
                            getBlockFromCache(wx - 1, wy + 1, wz + 1));

                        var nb = getBlockFromCache(wx - 1, wy, wz);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data1 = Block.packData((byte)RawDirection.WEST, west1);
                            var data2 = Block.packData((byte)RawDirection.WEST, west2);
                            var data3 = Block.packData((byte)RawDirection.WEST, west3);
                            var data4 = Block.packData((byte)RawDirection.WEST, west4);
                            BlockVertex[] verticesWest = [
                                // west
                                new BlockVertex(xmin, ymax, zmax, westU, westV, data1),
                                new BlockVertex(xmin, ymin, zmax, westU, westMaxV, data2),
                                new BlockVertex(xmin, ymin, zmin, westMaxU, westMaxV, data3),
                                new BlockVertex(xmin, ymax, zmin, westMaxU, westV, data4),
                            ];
                            chunkVertices.AddRange(verticesWest);
                            ushort[] indices = [
                                i,
                                (ushort)(i + 1),
                                (ushort)(i + 2),
                                (ushort)(i + 0),
                                (ushort)(i + 2),
                                (ushort)(i + 3)
                            ];
                            chunkIndices.AddRange(indices);
                            i += 4;
                        }
                        nb = getBlockFromCache(wx + 1, wy, wz);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data1 = Block.packData((byte)RawDirection.EAST, east1);
                            var data2 = Block.packData((byte)RawDirection.EAST, east2);
                            var data3 = Block.packData((byte)RawDirection.EAST, east3);
                            var data4 = Block.packData((byte)RawDirection.EAST, east4);
                            BlockVertex[] verticesEast = [
                                // east
                                new BlockVertex(xmax, ymax, zmin, eastU, eastV, data1),
                                new BlockVertex(xmax, ymin, zmin, eastU, eastMaxV, data2),
                                new BlockVertex(xmax, ymin, zmax, eastMaxU, eastMaxV, data3),
                                new BlockVertex(xmax, ymax, zmax, eastMaxU, eastV, data4),
                            ];
                            chunkVertices.AddRange(verticesEast);
                            ushort[] indices = [
                                i,
                                (ushort)(i + 1),
                                (ushort)(i + 2),
                                (ushort)(i + 0),
                                (ushort)(i + 2),
                                (ushort)(i + 3)
                            ];
                            chunkIndices.AddRange(indices);
                            i += 4;
                        }
                        nb = getBlockFromCache(wx, wy, wz - 1);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data1 = Block.packData((byte)RawDirection.SOUTH, south1);
                            var data2 = Block.packData((byte)RawDirection.SOUTH, south2);
                            var data3 = Block.packData((byte)RawDirection.SOUTH, south3);
                            var data4 = Block.packData((byte)RawDirection.SOUTH, south4);
                            BlockVertex[] verticesSouth = [
                                // south
                                new BlockVertex(xmin, ymax, zmin, southU, southV, data1),
                                new BlockVertex(xmin, ymin, zmin, southU, southMaxV, data2),
                                new BlockVertex(xmax, ymin, zmin, southMaxU, southMaxV, data3),
                                new BlockVertex(xmax, ymax, zmin, southMaxU, southV, data4),
                            ];
                            chunkVertices.AddRange(verticesSouth);
                            ushort[] indices = [
                                i,
                                (ushort)(i + 1),
                                (ushort)(i + 2),
                                (ushort)(i + 0),
                                (ushort)(i + 2),
                                (ushort)(i + 3)
                            ];
                            chunkIndices.AddRange(indices);
                            i += 4;
                        }
                        nb = getBlockFromCache(wx, wy, wz + 1);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data1 = Block.packData((byte)RawDirection.NORTH, north1);
                            var data2 = Block.packData((byte)RawDirection.NORTH, north2);
                            var data3 = Block.packData((byte)RawDirection.NORTH, north3);
                            var data4 = Block.packData((byte)RawDirection.NORTH, north4);
                            BlockVertex[] verticesNorth = [
                                // north
                                new BlockVertex(xmax, ymax, zmax, northU, northV, data1),
                                new BlockVertex(xmax, ymin, zmax, northU, northMaxV, data2),
                                new BlockVertex(xmin, ymin, zmax, northMaxU, northMaxV, data3),
                                new BlockVertex(xmin, ymax, zmax, northMaxU, northV, data4),
                            ];
                            chunkVertices.AddRange(verticesNorth);
                            ushort[] indices = [
                                i,
                                (ushort)(i + 1),
                                (ushort)(i + 2),
                                (ushort)(i + 0),
                                (ushort)(i + 2),
                                (ushort)(i + 3)
                            ];
                            chunkIndices.AddRange(indices);
                            i += 4;
                        }
                        nb = getBlockFromCache(wx, wy - 1, wz);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data1 = Block.packData((byte)RawDirection.DOWN, aoXminZminYmin);
                            var data2 = Block.packData((byte)RawDirection.DOWN, aoXminZmaxYmin);
                            var data3 = Block.packData((byte)RawDirection.DOWN, aoXmaxZmaxYmin);
                            var data4 = Block.packData((byte)RawDirection.DOWN, aoXmaxZminYmin);
                            BlockVertex[] verticesBottom = [
                                // bottom
                                new BlockVertex(xmin, ymin, zmin, bottomU, bottomV, data1),
                                new BlockVertex(xmin, ymin, zmax, bottomU, bottomMaxV, data2),
                                new BlockVertex(xmax, ymin, zmax, bottomMaxU, bottomMaxV, data3),
                                new BlockVertex(xmax, ymin, zmin, bottomMaxU, bottomV, data4),
                            ];
                            chunkVertices.AddRange(verticesBottom);
                            ushort[] indices = [
                                i,
                                (ushort)(i + 1),
                                (ushort)(i + 2),
                                (ushort)(i + 0),
                                (ushort)(i + 2),
                                (ushort)(i + 3)
                            ];
                            chunkIndices.AddRange(indices);
                            i += 4;
                        }
                        nb = getBlockFromCache(wx, wy + 1, wz);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data1 = Block.packData((byte)RawDirection.UP, aoXminZmaxYmax);
                            var data2 = Block.packData((byte)RawDirection.UP, aoXminZminYmax);
                            var data3 = Block.packData((byte)RawDirection.UP, aoXmaxZminYmax);
                            var data4 = Block.packData((byte)RawDirection.UP, aoXmaxZmaxYmax);
                            BlockVertex[] verticesTop = [
                                // top
                                new BlockVertex(xmin, ymax, zmax, topU, topV, data1),
                                new BlockVertex(xmin, ymax, zmin, topU, topMaxV, data2),
                                new BlockVertex(xmax, ymax, zmin, topMaxU, topMaxV, data3),
                                new BlockVertex(xmax, ymax, zmax, topMaxU, topV, data4),
                            ];
                            chunkVertices.AddRange(verticesTop);
                            ushort[] indices = [
                                i,
                                (ushort)(i + 1),
                                (ushort)(i + 2),
                                (ushort)(i + 0),
                                (ushort)(i + 2),
                                (ushort)(i + 3)
                            ];
                            chunkIndices.AddRange(indices);
                            i += 4;
                        }
                    }
                }
            }
        }
    }

    private byte calculateAO(Func<int, bool> test, int side1, int side2, int corner) {
        if (!Settings.instance.AO) {
            return 0;
        }
        if (test(side1) && test(side2)) {
            return 3;
        }
        return (byte)((byte)(test(side1) ? 1 : 0) + (test(side2) ? 1 : 0) + (test(corner) ? 1 : 0));
    }

    /*private Vector3D<int>[] getNeighbours(RawDirection side) {
        return offsetTable[(int)side];
    }*/
}