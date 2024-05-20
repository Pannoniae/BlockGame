using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class ChunkSectionRenderer {
    public ChunkSection section;

    public VAO vao;
    public VAO watervao;

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

    private static bool opaqueBlocks(int b) {
        return b != 0 && !Blocks.isTranslucent(b);
    }

    private static bool notSolid(int b) {
        return !Blocks.isSolid(b);
    }

    private static bool isSolid(int b) {
        return Blocks.isSolid(b);
    }

    private static bool notAir(int b) {
        return b != 0;
    }

    private static bool notTranslucent(int b) {
        return !Blocks.isTranslucent(b) && !Blocks.isSolid(b);
    }

    /// <summary>
    /// TODO store the number of blocks in the chunksection and only allocate the vertex list up to that length
    /// </summary>
    public void meshChunk() {
        //var sw = new Stopwatch();
        //sw.Start();
        if (section.world.renderer.fastChunkSwitch) {
            vao = new VerySharedBlockVAO(section.world.renderer.chunkVAO);
            watervao = new VerySharedBlockVAO(section.world.renderer.chunkVAO);
        }
        else {
            vao = new SharedBlockVAO();
            watervao = new SharedBlockVAO();
        }

        // if the section is empty, nothing to do
        if (section.isEmpty) {
            return;
        }

        unsafe {
            //Console.Out.WriteLine($"PartMeshing0.5: {sw.Elapsed.TotalMicroseconds}us");
            // first we render everything which is NOT translucent
            lock (meshingLock) {
                setupNeighbours();
                /*if (World.glob) {
                    MeasureProfiler.StartCollectingData();
                }*/
                constructVertices(&opaqueBlocks, &notSolid);
                /*if (World.glob) {
                    MeasureProfiler.SaveData();
                }*/
                //Console.Out.WriteLine($"PartMeshing1: {sw.Elapsed.TotalMicroseconds}us");
                if (section.world.renderer.fastChunkSwitch) {
                    (vao as VerySharedBlockVAO).bindVAO();
                }
                else {
                    vao.bind();
                }
                var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
                var finalIndices = CollectionsMarshal.AsSpan(chunkIndices);
                vao.upload(finalVertices, finalIndices);
            }
            lock (meshingLock) {
                if (hasTranslucentBlocks) {
                    // then we render everything which is translucent (water for now)
                    constructVertices(&Blocks.isTranslucent, &notTranslucent);
                    if (chunkIndices.Count > 0) {
                        if (section.world.renderer.fastChunkSwitch) {
                            (watervao as VerySharedBlockVAO).bindVAO();
                        }
                        else {
                            watervao.bind();
                        }
                        var tFinalVertices = CollectionsMarshal.AsSpan(chunkVertices);
                        var tFinalIndices = CollectionsMarshal.AsSpan(chunkIndices);
                        watervao.upload(tFinalVertices, tFinalIndices);
                        //world.sortedTransparentChunks.Add(this);
                    }
                    else {
                        //world.sortedTransparentChunks.Remove(this);
                    }
                }
            }
        }
        //Console.Out.WriteLine($"Meshing: {sw.Elapsed.TotalMicroseconds}us");
        //sw.Stop();
    }

    public ushort toVertex(float f) {
        return (ushort)(f / 16f * ushort.MaxValue);
    }



    public bool isVisible(BoundingFrustum frustum) {
        return frustum.Contains(section.bbbox) != ContainmentType.Disjoint;
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

    private void setupNeighbours() {

        hasTranslucentBlocks = false;
        //Console.Out.WriteLine($"vert1: {sw.Elapsed.TotalMicroseconds}us");

        // cache blocks
        // we need a 18x18 area
        // we load the 16x16 from the section itself then get the world for the rest
        // if the chunk section is an EmptyBlockData, don't bother
        for (int y = 0; y < Chunk.CHUNKSIZE; y++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    var bl = section.getBlockInChunk(x, y, z);
                    if (Blocks.isTranslucent(bl)) {
                        hasTranslucentBlocks = true;
                    }
                    access(neighbours, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1), bl);
                }
            }
        }
        //Console.Out.WriteLine($"vert2: {sw.Elapsed.TotalMicroseconds}us");

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
                    access(neighbours, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1), section.world.getBlock(wpos.X, wpos.Y, wpos.Z));
                }
            }
        }
    }

    // if neighbourTest returns true for adjacent block, render, if it returns false, don't
    //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
    unsafe private void constructVertices(delegate*<int, bool> whichBlocks, delegate*<int, bool> neighbourTest) {
        //var sw = new Stopwatch();
        //sw.Start();
        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");

        // clear arrays before starting
        chunkVertices.Clear();
        chunkIndices.Clear();

        ushort i = 0;

        // helper function to get blocks from cache
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort getBlockFromCache(int x, int y, int z) {
            return access(neighbours, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort getBlockFromCacheUnsafe(ushort* arrayBase, int x, int y, int z) {
            return arrayBase[(y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1)];
        }

        Span<BlockVertex> tempVertices = stackalloc BlockVertex[4];
        Span<ushort> tempIndices = stackalloc ushort[6];

        for (int y = 0; y < Chunk.CHUNKSIZE; y++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    var bl = getBlockFromCache(x, y, z);
                    if (whichBlocks(bl)) {
                        var pos = new Vector3D<int>(x, y, z);
                        var wpos = section.world.toWorldPos(section.chunkX, section.chunkY, section.chunkZ, x, y, z);
                        int wx = wpos.X;
                        int wy = wpos.Y;
                        int wz = wpos.Z;

                        Block b = Blocks.get(bl);
                        var faces = b.model.faces;
                        Face face;

                        // calculate texcoords

                        UVPair texCoords;
                        UVPair texCoordsMax;
                        Vector2D<float> tex;
                        Vector2D<float> texMax;
                        float u;
                        float v;
                        float maxU;
                        float maxV;

                        float offset = 0.0004f;

                        float x1;
                        float y1;
                        float z1;
                        float x2;
                        float y2;
                        float z2;
                        float x3;
                        float y3;
                        float z3;
                        float x4;
                        float y4;
                        float z4;


                        // calculate AO for all 8 vertices
                        // this is garbage but we'll deal with it later
                        // bottom

                        byte ao1;
                        byte ao2;
                        byte ao3;
                        byte ao4;

                        ushort data1;
                        ushort data2;
                        ushort data3;
                        ushort data4;

                        ushort nb;

                        for (int d = 0; d < faces.Length; d++) {
                            face = faces[d];
                            var dir = face.direction;
                            bool test;
                            if (dir == RawDirection.NONE) {
                                // if it's not a diagonal face, don't even bother checking neighbour because we have to render it anyway
                                test = true;
                            }
                            else {
                                var nbPos = pos + Direction.getDirection(dir);
                                nb = getBlockFromCache(nbPos.X, nbPos.Y, nbPos.Z);
                                test = neighbourTest(nb) || (face.nonFullFace && isSolid(nb));
                            }
                            // either neighbour test passes, or neighbour is not air + face is not full
                            if (test) {
                                if (!Settings.instance.AO || face.noAO) {
                                    ao1 = 0;
                                    ao2 = 0;
                                    ao3 = 0;
                                    ao4 = 0;
                                }
                                else {
                                    switch (dir) {
                                        case RawDirection.NONE:
                                            ao1 = 0;
                                            ao2 = 0;
                                            ao3 = 0;
                                            ao4 = 0;
                                            break;
                                        case RawDirection.WEST:
                                            // west
                                            ao1 = calculateAOFixed(getBlockFromCache(x - 1, y, z + 1),
                                                getBlockFromCache(x - 1, y + 1, z),
                                                getBlockFromCache(x - 1, y + 1, z + 1));
                                            ao2 = calculateAOFixed(getBlockFromCache(x - 1, y, z + 1),
                                                getBlockFromCache(x - 1, y - 1, z),
                                                getBlockFromCache(x - 1, y - 1, z + 1));
                                            ao3 = calculateAOFixed(getBlockFromCache(x - 1, y, z - 1),
                                                getBlockFromCache(x - 1, y - 1, z),
                                                getBlockFromCache(x - 1, y - 1, z - 1));
                                            ao4 = calculateAOFixed(getBlockFromCache(x - 1, y, z - 1),
                                                getBlockFromCache(x - 1, y + 1, z),
                                                getBlockFromCache(x - 1, y + 1, z - 1));
                                            break;
                                        case RawDirection.EAST:
                                            // east
                                            ao1 = calculateAOFixed(getBlockFromCache(x + 1, y, z - 1),
                                                getBlockFromCache(x + 1, y + 1, z),
                                                getBlockFromCache(x + 1, y + 1, z - 1));
                                            ao2 = calculateAOFixed(getBlockFromCache(x + 1, y, z - 1),
                                                getBlockFromCache(x + 1, y - 1, z),
                                                getBlockFromCache(x + 1, y - 1, z - 1));
                                            ao3 = calculateAOFixed(getBlockFromCache(x + 1, y, z + 1),
                                                getBlockFromCache(x + 1, y - 1, z),
                                                getBlockFromCache(x + 1, y - 1, z + 1));
                                            ao4 = calculateAOFixed(getBlockFromCache(x + 1, y, z + 1),
                                                getBlockFromCache(x + 1, y + 1, z),
                                                getBlockFromCache(x + 1, y + 1, z + 1));
                                            break;
                                        case RawDirection.SOUTH:
                                            // south
                                            ao1 = calculateAOFixed(getBlockFromCache(x - 1, y, z - 1),
                                                getBlockFromCache(x, y + 1, z - 1),
                                                getBlockFromCache(x - 1, y + 1, z - 1));
                                            ao2 = calculateAOFixed(getBlockFromCache(x - 1, y, z - 1),
                                                getBlockFromCache(x, y - 1, z - 1),
                                                getBlockFromCache(x - 1, y - 1, z - 1));
                                            ao3 = calculateAOFixed(getBlockFromCache(x + 1, y, z - 1),
                                                getBlockFromCache(x, y - 1, z - 1),
                                                getBlockFromCache(x + 1, y - 1, z - 1));
                                            ao4 = calculateAOFixed(getBlockFromCache(x + 1, y, z - 1),
                                                getBlockFromCache(x, y + 1, z - 1),
                                                getBlockFromCache(x + 1, y + 1, z - 1));
                                            break;
                                        case RawDirection.NORTH:
                                            // north
                                            ao1 = calculateAOFixed(getBlockFromCache(x + 1, y, z + 1),
                                                getBlockFromCache(x, y + 1, z + 1),
                                                getBlockFromCache(x + 1, y + 1, z + 1));
                                            ao2 = calculateAOFixed(getBlockFromCache(x + 1, y, z + 1),
                                                getBlockFromCache(x, y - 1, z + 1),
                                                getBlockFromCache(x + 1, y - 1, z + 1));
                                            ao3 = calculateAOFixed(getBlockFromCache(x - 1, y, z + 1),
                                                getBlockFromCache(x, y - 1, z + 1),
                                                getBlockFromCache(x - 1, y - 1, z + 1));
                                            ao4 = calculateAOFixed(getBlockFromCache(x - 1, y, z + 1),
                                                getBlockFromCache(x, y + 1, z + 1),
                                                getBlockFromCache(x - 1, y + 1, z + 1));
                                            break;
                                        case RawDirection.DOWN:
                                            // bottom
                                            ao1 = calculateAOFixed(getBlockFromCache(x - 1, y - 1, z),
                                                getBlockFromCache(x, y - 1, z - 1),
                                                getBlockFromCache(x - 1, y - 1, z - 1));
                                            ao2 = calculateAOFixed(getBlockFromCache(x + 1, y - 1, z),
                                                getBlockFromCache(x, y - 1, z + 1),
                                                getBlockFromCache(x + 1, y - 1, z + 1));
                                            ao3 = calculateAOFixed(getBlockFromCache(x + 1, y - 1, z),
                                                getBlockFromCache(x, y - 1, z - 1),
                                                getBlockFromCache(x + 1, y - 1, z - 1));
                                            ao4 = calculateAOFixed(getBlockFromCache(x - 1, y - 1, z),
                                                getBlockFromCache(x, y - 1, z + 1),
                                                getBlockFromCache(x - 1, y - 1, z + 1));
                                            break;
                                        case RawDirection.UP:
                                            // top
                                            ao1 = calculateAOFixed(getBlockFromCache(x - 1, y + 1, z),
                                                getBlockFromCache(x, y + 1, z + 1),
                                                getBlockFromCache(x - 1, y + 1, z + 1));
                                            ao2 = calculateAOFixed(getBlockFromCache(x - 1, y + 1, z),
                                                getBlockFromCache(x, y + 1, z - 1),
                                                getBlockFromCache(x - 1, y + 1, z - 1));
                                            ao3 = calculateAOFixed(getBlockFromCache(x + 1, y + 1, z),
                                                getBlockFromCache(x, y + 1, z - 1),
                                                getBlockFromCache(x + 1, y + 1, z - 1));
                                            ao4 = calculateAOFixed(getBlockFromCache(x + 1, y + 1, z),
                                                getBlockFromCache(x, y + 1, z + 1),
                                                getBlockFromCache(x + 1, y + 1, z + 1));
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                }
                                texCoords = faces[d].min;
                                texCoordsMax = faces[d].max;
                                tex = Block.texCoords(texCoords);
                                texMax = Block.texCoords(texCoordsMax);
                                u = tex.X;
                                v = tex.Y;
                                maxU = texMax.X;
                                maxV = texMax.Y;

                                x1 = wx + faces[d].x1;
                                y1 = wy + faces[d].y1;
                                z1 = wz + faces[d].z1;
                                x2 = wx + faces[d].x2;
                                y2 = wy + faces[d].y2;
                                z2 = wz + faces[d].z2;
                                x3 = wx + faces[d].x3;
                                y3 = wy + faces[d].y3;
                                z3 = wz + faces[d].z3;
                                x4 = wx + faces[d].x4;
                                y4 = wy + faces[d].y4;
                                z4 = wz + faces[d].z4;

                                data1 = Block.packData((byte)dir, ao1);
                                data2 = Block.packData((byte)dir, ao2);
                                data3 = Block.packData((byte)dir, ao3);
                                data4 = Block.packData((byte)dir, ao4);


                                // add vertices

                                tempVertices[0] = new BlockVertex(x1, y1, z1, u, v, data1);
                                tempVertices[1] = new BlockVertex(x2, y2, z2, u, maxV, data2);
                                tempVertices[2] = new BlockVertex(x3, y3, z3, maxU, maxV, data3);
                                tempVertices[3] = new BlockVertex(x4, y4, z4, maxU, v, data4);
                                chunkVertices.AddRange(tempVertices);

                                tempIndices[0] = i;
                                tempIndices[1] = (ushort)(i + 1);
                                tempIndices[2] = (ushort)(i + 2);
                                tempIndices[3] = (ushort)(i + 0);
                                tempIndices[4] = (ushort)(i + 2);
                                tempIndices[5] = (ushort)(i + 3);
                                chunkIndices.AddRange(tempIndices);
                                i += 4;
                            }
                        }
                    }
                }
            }
        }
        //Console.Out.WriteLine($"vert4: {sw.Elapsed.TotalMicroseconds}us");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte calculateAO(int side1, int side2, int corner) {
        if (!Settings.instance.AO) {
            return 0;
        }
        if (AOtest(side1) && AOtest(side2)) {
            return 3;
        }
        return (byte)(toInt(AOtest(side1)) + toInt(AOtest(side2)) + toInt(AOtest(corner)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte calculateAOFixed(int side1, int side2, int corner) {
        var test1 = Blocks.isSolid(side1);
        var test2 = Blocks.isSolid(side2);
        var testCorner = Blocks.isSolid(corner);
        if (test1 && test2) {
            return 3;
        }
        return (byte)(toInt(test1) + toInt(test2) + toInt(testCorner));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    unsafe private static int toInt(bool b) {
        return *(byte*)&b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T access<T>(T[] arr, int index) {
        ref T tableRef = ref MemoryMarshal.GetArrayDataReference(arr);
        return Unsafe.Add(ref tableRef, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void access<T>(T[] arr, int index, T value) {
        ref T tableRef = ref MemoryMarshal.GetArrayDataReference(arr);
        Unsafe.Add(ref tableRef, index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void setRange<T>(T[] arr, int index, T[] values) {
        ref T tableRef = ref MemoryMarshal.GetArrayDataReference(arr);
        for (int i = 0; i < values.Length; i++) {
            Unsafe.Add(ref tableRef, index + i) = values[i];
        }
    }

/*private Vector3D<int>[] getNeighbours(RawDirection side) {
    return offsetTable[(int)side];
}*/
}