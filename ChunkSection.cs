using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class ChunkSection {

    public Chunk chunk;

    public int chunkX;
    public int chunkY;
    public int chunkZ;

    public int worldX => chunkX * CHUNKSIZE;
    public int worldY => chunkY * CHUNKSIZE;
    public int worldZ => chunkZ * CHUNKSIZE;
    public Vector3D<int> worldPos => new(worldX, worldY, worldZ);
    public Vector3D<int> centrePos => new(worldX + 8, worldY + 8, worldZ + 8);

    public bool isEmpty = false;

    public BlockVAO vao;
    public BlockVAO watervao;

    public bool hasTranslucentBlocks;

    public Shader shader;
    public int uMVP;
    public AABB box;


    public readonly GL GL;

    private World world;
    public const int CHUNKSIZE = 16;

    public ChunkSection(World world, Chunk chunk, Shader shader, int xpos, int ypos, int zpos) {
        this.chunk = chunk;
        chunkX = xpos;
        chunkY = ypos;
        chunkZ = zpos;
        this.world = world;
        this.shader = shader;
        GL = Game.instance.GL;
        box = new AABB(new Vector3D<double>(chunkX * 16, chunkY * 16, chunkZ * 16), new Vector3D<double>(chunkX * 16 + 16, chunkY * 16 + 16, chunkZ * 16 + 16));

        uMVP = shader.getUniformLocation("uMVP");
    }

    public bool isVisible(BoundingFrustum frustum) {
        return frustum.Contains(new BoundingBox(box.min.toVec3(), box.max.toVec3())) != ContainmentType.Disjoint;
    }

    /// <summary>
    /// TODO store the number of blocks in the chunksection and only allocate the vertex list up to that length
    /// </summary>
    public void meshChunk() {
        vao = new BlockVAO();
        watervao = new BlockVAO();
        // first we render everything which is NOT translucent
        constructVertices(i => i != 0 && !Blocks.isTranslucent(i), i => !Blocks.isSolid(i), out var chunkVertices, out var chunkIndices, true);
        // then we render everything which is translucent (water for now)
        constructVertices(Blocks.isTranslucent, i => !Blocks.isTranslucent(i) && !Blocks.isSolid(i), out var tChunkVertices, out var tChunkIndices, false);
        vao.bind();
        var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
        var finalIndices = CollectionsMarshal.AsSpan(chunkIndices);
        vao.upload(finalVertices, finalIndices);

        if (tChunkIndices.Count > 0) {
            watervao.bind();
            var tFinalVertices = CollectionsMarshal.AsSpan(tChunkVertices);
            var tFinalIndices = CollectionsMarshal.AsSpan(tChunkIndices);
            watervao.upload(tFinalVertices, tFinalIndices);
            hasTranslucentBlocks = true;
            //world.sortedTransparentChunks.Add(this);
        }
        else {
            hasTranslucentBlocks = false;
            //world.sortedTransparentChunks.Remove(this);
        }

        if (chunkIndices.Count == 0 && tChunkIndices.Count == 0) {
            isEmpty = true;
        }
        else {
            isEmpty = false;
        }
    }

    public ushort toVertex(float f) {
        return (ushort)(f / 16f * ushort.MaxValue);
    }

    // if neighbourTest returns true for adjacent block, render, if it returns false, don't
    private void constructVertices(Func<int, bool> whichBlocks, Func<int, bool> neighbourTest, out List<BlockVertex> chunkVertices, out List<ushort> chunkIndices, bool full) {
        // at most, we need chunksize^3 blocks times 8 vertices
        if (full) {
            chunkVertices = new List<BlockVertex>(CHUNKSIZE * CHUNKSIZE * CHUNKSIZE * 4);
            chunkIndices = new List<ushort>(CHUNKSIZE * CHUNKSIZE * CHUNKSIZE * 6);
        }
        // small array, we don't need that much pre-allocation
        else {
            chunkVertices = new List<BlockVertex>(16);
            chunkIndices = new List<ushort>(16);
        }
        ushort i = 0;
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int y = 0; y < CHUNKSIZE; y++) {
                for (int z = 0; z < CHUNKSIZE; z++) {
                    if (whichBlocks(getBlockInChunk(x, y, z))) {
                        var wpos = world.toWorldPos(chunkX, chunkY, chunkZ, x, y, z);
                        int wx = wpos.X;
                        int wy = wpos.Y;
                        int wz = wpos.Z;


                        Block b = Blocks.get(world.getBlock(wx, wy, wz));

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


                        float xmin = wx;
                        float ymin = wy;
                        float zmin = wz;
                        float xmax = wx + 1f;
                        float ymax = wy + 1f;
                        float zmax = wz + 1f;

                        var nb = world.getBlockUnsafe(wx - 1, wy, wz);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data = packData((byte)RawDirection.WEST);
                            BlockVertex[] verticesWest = [
                                // west
                                new BlockVertex(xmin, ymax, zmax, westU, westV, data),
                                new BlockVertex(xmin, ymin, zmax, westU, westMaxV, data),
                                new BlockVertex(xmin, ymin, zmin, westMaxU, westMaxV, data),
                                new BlockVertex(xmin, ymax, zmin, westMaxU, westV, data),
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
                        nb = world.getBlockUnsafe(wx + 1, wy, wz);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data = packData((byte)RawDirection.EAST);
                            BlockVertex[] verticesEast = [
                                // east
                                new BlockVertex(xmax, ymax, zmin, eastU, eastV, data),
                                new BlockVertex(xmax, ymin, zmin, eastU, eastMaxV, data),
                                new BlockVertex(xmax, ymin, zmax, eastMaxU, eastMaxV, data),
                                new BlockVertex(xmax, ymax, zmax, eastMaxU, eastV, data),
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
                        nb = world.getBlockUnsafe(wx, wy, wz - 1);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data = packData((byte)RawDirection.SOUTH);
                            BlockVertex[] verticesSouth = [
                                // south
                                new BlockVertex(xmin, ymax, zmin, southU, southV, data),
                                new BlockVertex(xmin, ymin, zmin, southU, southMaxV, data),
                                new BlockVertex(xmax, ymin, zmin, southMaxU, southMaxV, data),
                                new BlockVertex(xmax, ymax, zmin, southMaxU, southV, data),
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
                        nb = world.getBlockUnsafe(wx, wy, wz + 1);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data = packData((byte)RawDirection.NORTH);
                            BlockVertex[] verticesNorth = [
                                // north
                                new BlockVertex(xmax, ymax, zmax, northU, northV, data),
                                new BlockVertex(xmax, ymin, zmax, northU, northMaxV, data),
                                new BlockVertex(xmin, ymin, zmax, northMaxU, northMaxV, data),
                                new BlockVertex(xmin, ymax, zmax, northMaxU, northV, data),
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
                        nb = world.getBlockUnsafe(wx, wy - 1, wz);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data = packData((byte)RawDirection.DOWN);
                            BlockVertex[] verticesBottom = [
                                // bottom
                                new BlockVertex(xmin, ymin, zmin, bottomU, bottomV, data),
                                new BlockVertex(xmin, ymin, zmax, bottomU, bottomMaxV, data),
                                new BlockVertex(xmax, ymin, zmax, bottomMaxU, bottomMaxV, data),
                                new BlockVertex(xmax, ymin, zmin, bottomMaxU, bottomV, data),
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
                        nb = world.getBlockUnsafe(wx, wy + 1, wz);
                        if (nb != -1 && neighbourTest(nb)) {
                            var data = packData((byte)RawDirection.UP);
                            BlockVertex[] verticesTop = [
                                // top
                                new BlockVertex(xmin, ymax, zmax, topU, topV, data),
                                new BlockVertex(xmin, ymax, zmin, topU, topMaxV, data),
                                new BlockVertex(xmax, ymax, zmin, topMaxU, topMaxV, data),
                                new BlockVertex(xmax, ymax, zmax, topMaxU, topV, data),
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

    private int getBlockInChunk(int x, int y, int z) {
        return chunk.block[x, y + chunkY * CHUNKSIZE, z];
    }

    // this will pack the data into the uint
    public ushort packData(byte direction) {
        return direction;
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

    public void tick(int x, int y, int z) {
        var block = getBlockInChunk(x, y, z);
        if (block == Blocks.DIRT.id) {
            if (chunk.world.inWorld(x, y + 1, z) && getBlockInChunk(x, y + 1, z) == 0) {
                chunk.block[x, y + chunkY * CHUNKSIZE, z] = Blocks.GRASS.id;
                meshChunk();
            }
        }
    }
}