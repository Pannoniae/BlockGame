using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Profiler.Api;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class ChunkSectionRenderer {
    public ChunkSection section;

    public SharedBlockVAO vao;
    public SharedBlockVAO watervao;

    public bool hasTranslucentBlocks;

    public Shader shader;
    public int uMVP;

    public readonly GL GL;

    public static readonly Func<int, bool> AOtest = bl => bl != -1 && Blocks.isSolid(bl);

    // we cheated GC! there is only one list preallocated
    // we need 16x16x16 blocks, each block has max. 24 vertices
    // for indices we need the full 36
    public static readonly List<BlockVertex> chunkVertices = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * 24);
    public static readonly List<ushort> chunkIndices = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * 36);
    // YZX again
    public static readonly ushort[] neighbours = new ushort[Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX];

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
        //var sw = new Stopwatch();
        //sw.Start();
        vao = new SharedBlockVAO();
        watervao = new SharedBlockVAO();

        // if the section is empty, nothing to do
        if (section.isEmpty) {
            return;
        }

        //Console.Out.WriteLine($"PartMeshing0.5: {sw.Elapsed.TotalMicroseconds}us");
        // first we render everything which is NOT translucent
        lock (meshingLock) {
            /*if (World.glob) {
                MeasureProfiler.StartCollectingData();
            }*/
            constructVertices(i => i != 0 && !Blocks.isTranslucent(i), i => !Blocks.isSolid(i));
            /*if (World.glob) {
                MeasureProfiler.SaveData();
            }*/
            //Console.Out.WriteLine($"PartMeshing1: {sw.Elapsed.TotalMicroseconds}us");
            vao.bind();
            var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
            var finalIndices = CollectionsMarshal.AsSpan(chunkIndices);
            vao.upload(finalVertices, finalIndices);
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
        }
        //Console.Out.WriteLine($"Meshing: {sw.Elapsed.TotalMicroseconds}us");
        //sw.Stop();
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
        if (!section.isEmpty && isVisible(camera.frustum)) {
            vao.bind();
            //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

            uint renderedVerts = vao.render();
            Game.instance.metrics.renderedVerts += (int)renderedVerts;
            Game.instance.metrics.renderedChunks += 1;
        }
    }

    public void drawTransparent(PlayerCamera camera) {
        if (hasTranslucentBlocks && !section.isEmpty && isVisible(camera.frustum)) {
            watervao.bind();
            uint renderedTransparentVerts = watervao.render();
            Game.instance.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }
    // if neighbourTest returns true for adjacent block, render, if it returns false, don't
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void constructVertices(Func<int, bool> whichBlocks, Func<int, bool> neighbourTest) {

        // clear arrays before starting
        chunkVertices.Clear();
        chunkIndices.Clear();

        ushort i = 0;
        // cache blocks
        // we need a 18x18 area
        // we load the 16x16 from the section itself then get the world for the rest
        // if the chunk section is an EmptyBlockData, don't bother
        for (int y = 0; y < Chunk.CHUNKSIZE; y++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    var bl = section.getBlockInChunk(x, y, z);
                    neighbours[(y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1)] = bl;
                }
            }
        }

        // if chunk is empty, nothing to do, don't need to check neighbours
        // btw this shouldn't fucking happen because we checked it but we check it anyway so our program doesn't crash if the chunk representation is changed


        for (int y = -1; y < Chunk.CHUNKSIZE + 1; y++) {
            for (int z = -1; z < Chunk.CHUNKSIZE + 1; z++) {
                for (int x = -1; x < Chunk.CHUNKSIZE + 1; x++) {
                    // if inside the chunk, skip
                    if (x is >= 0 and < Chunk.CHUNKSIZE &&
                        y is >= 0 and < Chunk.CHUNKSIZE &&
                        z is >= 0 and < Chunk.CHUNKSIZE) {
                        continue;
                    }

                    var wpos = section.world.toWorldPos(section.chunkX, section.chunkY, section.chunkZ, x, y, z);
                    neighbours[(y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1)] = section.world.getBlock(wpos.X, wpos.Y, wpos.Z);
                }
            }
        }

        // helper function to get blocks from cache
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort getBlockFromCache(int x, int y, int z) {
            return neighbours[(y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe static ushort getBlockFromCacheUnsafe(ushort* arrayBase, int x, int y, int z) {
            return arrayBase[(y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1)];
        }

        for (int y = 0; y < Chunk.CHUNKSIZE; y++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    var bl = getBlockFromCache(x, y, z);
                    if (whichBlocks(bl)) {
                        var wpos = section.world.toWorldPos(section.chunkX, section.chunkY, section.chunkZ, x, y, z);
                        int wx = wpos.X;
                        int wy = wpos.Y;
                        int wz = wpos.Z;

                        Block b = Blocks.get(bl);

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


                        // calculate AO for all 8 vertices
                        // this is garbage but we'll deal with it later
                        // bottom

                        byte aoXminZminYmin;
                        byte aoXmaxZminYmin;
                        byte aoXminZmaxYmin;
                        byte aoXmaxZmaxYmin;
                        byte aoXminZminYmax;
                        byte aoXmaxZminYmax;
                        byte aoXminZmaxYmax;
                        byte aoXmaxZmaxYmax;
                        byte west1;
                        byte west2;
                        byte west3;
                        byte west4;
                        byte east1;
                        byte east2;
                        byte east3;
                        byte east4;
                        byte south1;
                        byte south2;
                        byte south3;
                        byte south4;
                        byte north1;
                        byte north2;
                        byte north3;
                        byte north4;
                        if (Settings.instance.AO) {
                            aoXminZminYmin = calculateAOFixed(getBlockFromCache(x - 1, y - 1, z),
                                getBlockFromCache(x, y - 1, z - 1),
                                getBlockFromCache(x - 1, y - 1, z - 1));
                            aoXmaxZminYmin = calculateAOFixed(getBlockFromCache(x + 1, y - 1, z),
                                getBlockFromCache(x, y - 1, z - 1),
                                getBlockFromCache(x + 1, y - 1, z - 1));
                            aoXminZmaxYmin = calculateAOFixed(getBlockFromCache(x - 1, y - 1, z),
                                getBlockFromCache(x, y - 1, z + 1),
                                getBlockFromCache(x - 1, y - 1, z + 1));
                            aoXmaxZmaxYmin = calculateAOFixed(getBlockFromCache(x + 1, y - 1, z),
                                getBlockFromCache(x, y - 1, z + 1),
                                getBlockFromCache(x + 1, y - 1, z + 1));

                            // top
                            aoXminZminYmax = calculateAOFixed(getBlockFromCache(x - 1, y + 1, z),
                                getBlockFromCache(x, y + 1, z - 1),
                                getBlockFromCache(x - 1, y + 1, z - 1));
                            aoXmaxZminYmax = calculateAOFixed(getBlockFromCache(x + 1, y + 1, z),
                                getBlockFromCache(x, y + 1, z - 1),
                                getBlockFromCache(x + 1, y + 1, z - 1));
                            aoXminZmaxYmax = calculateAOFixed(getBlockFromCache(x - 1, y + 1, z),
                                getBlockFromCache(x, y + 1, z + 1),
                                getBlockFromCache(x - 1, y + 1, z + 1));
                            aoXmaxZmaxYmax = calculateAOFixed(getBlockFromCache(x + 1, y + 1, z),
                                getBlockFromCache(x, y + 1, z + 1),
                                getBlockFromCache(x + 1, y + 1, z + 1));

                            // west
                            west1 = calculateAOFixed(getBlockFromCache(x - 1, y, z + 1),
                                getBlockFromCache(x - 1, y + 1, z),
                                getBlockFromCache(x - 1, y + 1, z + 1));
                            west2 = calculateAOFixed(getBlockFromCache(x - 1, y, z + 1),
                                getBlockFromCache(x - 1, y - 1, z),
                                getBlockFromCache(x - 1, y - 1, z + 1));
                            west3 = calculateAOFixed(getBlockFromCache(x - 1, y, z - 1),
                                getBlockFromCache(x - 1, y - 1, z),
                                getBlockFromCache(x - 1, y - 1, z - 1));
                            west4 = calculateAOFixed(getBlockFromCache(x - 1, y, z - 1),
                                getBlockFromCache(x - 1, y + 1, z),
                                getBlockFromCache(x - 1, y + 1, z - 1));

                            // east
                            east1 = calculateAOFixed(getBlockFromCache(x + 1, y, z - 1),
                                getBlockFromCache(x + 1, y + 1, z),
                                getBlockFromCache(x + 1, y + 1, z - 1));
                            east2 = calculateAOFixed(getBlockFromCache(x + 1, y, z - 1),
                                getBlockFromCache(x + 1, y - 1, z),
                                getBlockFromCache(x + 1, y - 1, z - 1));
                            east3 = calculateAOFixed(getBlockFromCache(x + 1, y, z + 1),
                                getBlockFromCache(x + 1, y - 1, z),
                                getBlockFromCache(x + 1, y - 1, z + 1));
                            east4 = calculateAOFixed(getBlockFromCache(x + 1, y, z + 1),
                                getBlockFromCache(x + 1, y + 1, z),
                                getBlockFromCache(x + 1, y + 1, z + 1));

                            // south
                            south1 = calculateAOFixed(getBlockFromCache(x - 1, y, z - 1),
                                getBlockFromCache(x, y + 1, z - 1),
                                getBlockFromCache(x - 1, y + 1, z - 1));
                            south2 = calculateAOFixed(getBlockFromCache(x - 1, y, z - 1),
                                getBlockFromCache(x, y - 1, z - 1),
                                getBlockFromCache(x - 1, y - 1, z - 1));
                            south3 = calculateAOFixed(getBlockFromCache(x + 1, y, z - 1),
                                getBlockFromCache(x, y - 1, z - 1),
                                getBlockFromCache(x + 1, y - 1, z - 1));
                            south4 = calculateAOFixed(getBlockFromCache(x + 1, y, z - 1),
                                getBlockFromCache(x, y + 1, z - 1),
                                getBlockFromCache(x + 1, y + 1, z - 1));

                            // north
                            north1 = calculateAOFixed(getBlockFromCache(x + 1, y, z + 1),
                                getBlockFromCache(x, y + 1, z + 1),
                                getBlockFromCache(x + 1, y + 1, z + 1));
                            north2 = calculateAOFixed(getBlockFromCache(x + 1, y, z + 1),
                                getBlockFromCache(x, y - 1, z + 1),
                                getBlockFromCache(x + 1, y - 1, z + 1));
                            north3 = calculateAOFixed(getBlockFromCache(x - 1, y, z + 1),
                                getBlockFromCache(x, y - 1, z + 1),
                                getBlockFromCache(x - 1, y - 1, z + 1));
                            north4 = calculateAOFixed(getBlockFromCache(x - 1, y, z + 1),
                                getBlockFromCache(x, y + 1, z + 1),
                                getBlockFromCache(x - 1, y + 1, z + 1));
                        }
                        else {
                            aoXminZminYmin = 0;
                            aoXmaxZminYmin = 0;
                            aoXminZmaxYmin = 0;
                            aoXmaxZmaxYmin = 0;
                            aoXminZminYmax = 0;
                            aoXmaxZminYmax = 0;
                            aoXminZmaxYmax = 0;
                            aoXmaxZmaxYmax = 0;
                            west1 = 0;
                            west2 = 0;
                            west3 = 0;
                            west4 = 0;
                            east1 = 0;
                            east2 = 0;
                            east3 = 0;
                            east4 = 0;
                            south1 = 0;
                            south2 = 0;
                            south3 = 0;
                            south4 = 0;
                            north1 = 0;
                            north2 = 0;
                            north3 = 0;
                            north4 = 0;
                        }

                        var nb = getBlockFromCache(x - 1, y, z);
                        if (neighbourTest(nb)) {
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
                        nb = getBlockFromCache(x + 1, y, z);
                        if (neighbourTest(nb)) {
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
                        nb = getBlockFromCache(x, y, z - 1);
                        if (neighbourTest(nb)) {
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
                        nb = getBlockFromCache(x, y, z + 1);
                        if (neighbourTest(nb)) {
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
                        // if below world, don't include in mesh
                        // this prevents meshing exactly nothing under the world
                        nb = getBlockFromCache(x, y - 1, z);
                        if (wy - 1 >= 0 && neighbourTest(nb)) {
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
                        nb = getBlockFromCache(x, y + 1, z);
                        if (neighbourTest(nb)) {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static private byte calculateAO(int side1, int side2, int corner) {
        if (!Settings.instance.AO) {
            return 0;
        }
        if (AOtest(side1) && AOtest(side2)) {
            return 3;
        }
        return (byte)(toInt(AOtest(side1)) + toInt(AOtest(side2)) + toInt(AOtest(corner)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static private byte calculateAOFixed(int side1, int side2, int corner) {
        var test1 = Blocks.isSolid(side1);
        var test2 = Blocks.isSolid(side2);
        var testCorner = Blocks.isSolid(corner);
        if (test1 && test2) {
            return 3;
        }
        return (byte)(toInt(test1) + toInt(test2) + toInt(testCorner));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    unsafe static private int toInt(bool b) {
        return *(byte*)&b;
    }

    /*private Vector3D<int>[] getNeighbours(RawDirection side) {
        return offsetTable[(int)side];
    }*/
}