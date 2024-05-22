using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class ChunkSectionRenderer {
    public ChunkSection section;


    // we need it here because completely full chunks are also empty of any rendering
    public bool isEmpty;

    public VAO vao;
    public VAO watervao;

    public bool hasTranslucentBlocks;
    public bool hasOnlySolid;

    public Shader shader;
    public int uMVP;

    public readonly GL GL;

    public static readonly Func<int, bool> AOtest = bl => bl != -1 && Blocks.isSolid(bl);

    // we cheated GC! there is only one list preallocated
    // we need 16x16x16 blocks, each block has max. 24 vertices
    // for indices we need the full 36

    // actually we don't need a list, regular arrays will do because it's only a few megs of space and it's shared
    // in the future when we want multithreaded meshing, we can just allocate like 4-8 of them and it will still be in the ballpark of 10MB
    public static List<BlockVertex> chunkVertices = new(1024);
    public static List<ushort> chunkIndices = new(1024);
    // YZX again
    public static ushort[] neighbours = new ushort[Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX];
    public static BlockData[] neighbourSections = new BlockData[27];

    static object meshingLock = new();

    private static int[] offsetTable = [
        // west

        -1, 0, 1, -1, 1, 0, -1, 1, 1,
        -1, 0, 1, -1, -1, 0, -1, -1, 1,
        -1, 0, -1, -1, -1, 0, -1, -1, -1,
        -1, 0, -1, -1, 1, 0, -1, 1, -1,

        // east

        1, 0, -1, 1, 1, 0, 1, 1, -1,
        1, 0, -1, 1, -1, 0, 1, -1, -1,
        1, 0, 1, 1, -1, 0, 1, -1, 1,
        1, 0, 1, 1, 1, 0, 1, 1, 1,

        // south

        -1, 0, -1, 0, 1, -1, -1, 1, -1,
        -1, 0, -1, 0, -1, -1, -1, -1, -1,
        1, 0, -1, 0, -1, -1, 1, -1, -1,
        1, 0, -1, 0, 1, -1, 1, 1, -1,

        // north

        1, 0, 1, 0, 1, 1, 1, 1, 1,
        1, 0, 1, 0, -1, 1, 1, -1, 1,
        -1, 0, 1, 0, -1, 1, -1, -1, 1,
        -1, 0, 1, 0, 1, 1, -1, 1, 1,


        // down

        0, -1, -1, 1, -1, 0, 1, -1, -1,
        0, -1, 1, 1, -1, 0, 1, -1, 1,
        0, -1, 1, -1, -1, 0, -1, -1, 1,
        0, -1, -1, -1, -1, 0, -1, -1, -1,


        // up
        0, 1, 1, -1, 1, 0, -1, 1, 1,
        0, 1, -1, -1, 1, 0, -1, 1, -1,
        0, 1, -1, 1, 1, 0, 1, 1, -1,
        0, 1, 1, 1, 1, 0, 1, 1, 1,
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
        return Blocks.notSolid(b);
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

                // if chunk is full, don't mesh either
                if (hasOnlySolid) {
                    isEmpty = true;
                    return;
                }

                /*if (World.glob) {
                    MeasureProfiler.StartCollectingData();
                }*/
                //Console.Out.WriteLine($"PartMeshing0.7: {sw.Elapsed.TotalMicroseconds}us");
                constructVertices(&opaqueBlocks, &Blocks.notSolid);
                /*if (World.glob) {
                    MeasureProfiler.SaveData();
                }*/
                //Console.Out.WriteLine($"PartMeshing1: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
                if (chunkIndices.Count > 0) {
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
                else {
                    isEmpty = true;
                }
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
                        isEmpty = true;
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

    [SkipLocalsInit]
    private void setupNeighbours() {
        //var sw = new Stopwatch();
        //sw.Start();

        hasTranslucentBlocks = false;
        hasOnlySolid = true;
        //Console.Out.WriteLine($"vert1: {sw.Elapsed.TotalMicroseconds}us");

        // cache blocks
        // we need a 18x18 area
        // we load the 16x16 from the section itself then get the world for the rest
        // if the chunk section is an EmptyBlockData, don't bother
        // it will always be ArrayBlockData so we can access directly without those pesky BOUNDS CHECKS
        var blockData = (ArrayBlockData)section.blocks;
        ref var blockArray = ref MemoryMarshal.GetArrayDataReference(blockData.blocks);
        ref var neighboursArray = ref MemoryMarshal.GetArrayDataReference(neighbours);
        var world = section.world;
        for (int y = 0; y < Chunk.CHUNKSIZE; y++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    var bl = accessRef(ref blockArray, y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
                    if (Blocks.isTranslucent(bl)) {
                        hasTranslucentBlocks = true;
                    }
                    if (Blocks.notSolid(bl)) {
                        hasOnlySolid = false;
                    }
                    accessRef(ref neighboursArray, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1), bl);
                }
            }
        }

        //Console.Out.WriteLine($"vert2: {sw.Elapsed.TotalMicroseconds}us");

        // if chunk is empty, nothing to do, don't need to check neighbours
        // btw this shouldn't fucking happen because we checked it but we check it anyway so our program doesn't crash if the chunk representation is changed

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort getBlockFromSection(ChunkSection section, int x, int y, int z) {
            if (y is < 0 or >= World.WORLDHEIGHT) {
                return 0;
            }
            var world = section.world;
            var blockPos = world.getPosInChunkSection(x, y, z);
            var sectionPos = world.getChunkSectionPos(x, y, z);
            return access(neighbourSections, (sectionPos.y - section.chunkY + 1) * 9 + (sectionPos.z - section.chunkZ + 1) * 3 + (sectionPos.x - section.chunkX + 1))[blockPos.X, blockPos.Y, blockPos.Z];
        }

        // setup neighbouring sections
        var coord = section.chunkCoord;
        ref var neighbourSectionsArray = ref MemoryMarshal.GetArrayDataReference(neighbourSections);
        for (int y = -1; y <= 1; y++) {
            for (int z = -1; z <= 1; z++) {
                for (int x = -1; x <= 1; x++) {
                    world.getChunkSectionMaybe(new ChunkSectionCoord(coord.x + x, coord.y + y, coord.z + z), out var sec);
                    accessRef(ref neighbourSectionsArray, (y + 1) * 9 + (z + 1) * 3 + x + 1, sec != null ? sec.blocks : new EmptyBlockData());
                }
            }
        }

        for (int y = -1; y < Chunk.CHUNKSIZE + 1; y++) {
            for (int z = -1; z < Chunk.CHUNKSIZE + 1; z++) {
                for (int x = -1; x < Chunk.CHUNKSIZE + 1; x++) {
                    // if inside the chunk, skip
                    if (x is >= 0 and < Chunk.CHUNKSIZE &&
                        z is >= 0 and < Chunk.CHUNKSIZE &&
                        y is >= 0 and < Chunk.CHUNKSIZE) {
                        // skip this entire loop
                        x = Chunk.CHUNKSIZE - 1;
                        continue;
                    }
                    var wpos = world.toWorldPos(section.chunkX, section.chunkY, section.chunkZ, x, y, z);
                    accessRef(ref neighboursArray, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1), getBlockFromSection(section, wpos.X, wpos.Y, wpos.Z));
                }
            }
        }
        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");
    }

    // if neighbourTest returns true for adjacent block, render, if it returns false, don't
    //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
    [SkipLocalsInit]
    unsafe private void constructVertices(delegate*<int, bool> whichBlocks, delegate*<int, bool> neighbourTest) {
        //var sw = new Stopwatch();
        //sw.Start();
        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");

        // clear arrays before starting
        chunkVertices.Clear();
        chunkIndices.Clear();

        ushort i = 0;

        var AOenabled = Settings.instance.AO;
        //ushort cv = 0;
        //ushort ci = 0;

        // helper function to get blocks from cache
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort getBlockFromCache(int x, int y, int z) {
            return access(neighbours, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort getBlockFromCacheUnsafeVec(ref ushort arrayBase, Vector3D<int> vec) {
            return accessRef(ref arrayBase, (vec.Y + 1) * Chunk.CHUNKSIZEEXSQ + (vec.Z + 1) * Chunk.CHUNKSIZEEX + (vec.X + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort getBlockFromCacheUnsafe(ref ushort arrayBase, int x, int y, int z) {
            return accessRef(ref arrayBase, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1));
        }

        Span<BlockVertex> tempVertices = stackalloc BlockVertex[4];
        Span<ushort> tempIndices = stackalloc ushort[6];

        ref var neighboursArray = ref MemoryMarshal.GetArrayDataReference(neighbours);
        ref var offsetArray = ref MemoryMarshal.GetArrayDataReference(offsetTable);

        for (int y = 0; y < Chunk.CHUNKSIZE; y++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    var bl = getBlockFromCacheUnsafe(ref neighboursArray, x, y, z);
                    if (whichBlocks(bl)) {
                        //var pos = new Vector3D<int>(x, y, z);

                        // unrolled world.toWorldPos
                        float wx = section.chunkX * Chunk.CHUNKSIZE + x;
                        float wy = section.chunkY * Chunk.CHUNKSIZE + y;
                        float wz = section.chunkZ * Chunk.CHUNKSIZE + z;

                        Block b = Blocks.get(bl);
                        var faces = b.model.faces;
                        ref var facesRef = ref MemoryMarshal.GetArrayDataReference(faces);
                        Face face;

                        // calculate texcoords
                        Vector2D<float> tex;
                        Vector2D<float> texMax;

                        //float offset = 0.0004f;

                        ushort d0;
                        ushort d1;
                        ushort d2;
                        ushort d3;
                        ushort d4;
                        ushort d5;


                        // calculate AO for all 8 vertices
                        // this is garbage but we'll deal with it later
                        // bottom

                        byte ao1 = 0;
                        byte ao2 = 0;
                        byte ao3 = 0;
                        byte ao4 = 0;

                        ushort data1;
                        ushort data2;
                        ushort data3;
                        ushort data4;

                        int dirIdx = 0;

                        // setup neighbour data
                        d0 = getBlockFromCacheUnsafe(ref neighboursArray, x - 1, y, z);
                        d1 = getBlockFromCacheUnsafe(ref neighboursArray, x + 1, y, z);
                        d2 = getBlockFromCacheUnsafe(ref neighboursArray, x, y, z - 1);
                        d3 = getBlockFromCacheUnsafe(ref neighboursArray, x, y, z + 1);
                        d4 = getBlockFromCacheUnsafe(ref neighboursArray, x, y - 1, z);
                        d5 = getBlockFromCacheUnsafe(ref neighboursArray, x, y + 1, z);

                        ushort nb;

                        // if full block, don't even bother going through the faces

                        for (int d = 0; d < faces.Length; d++) {
                            face = accessRef(ref facesRef, d);
                            var dir = face.direction;
                            bool test;
                            if (dir == RawDirection.NONE) {
                                // if it's not a diagonal face, don't even bother checking neighbour because we have to render it anyway
                                test = true;
                            }
                            else {
                                dirIdx = (int)dir;
                                // THE SWITCH
                                switch (dir) {

                                    case RawDirection.WEST:
                                        nb = d0;
                                        break;
                                    case RawDirection.EAST:
                                        nb = d1;
                                        break;
                                    case RawDirection.SOUTH:
                                        nb = d2;
                                        break;
                                    case RawDirection.NORTH:
                                        nb = d3;
                                        break;
                                    case RawDirection.DOWN:
                                        nb = d4;
                                        break;
                                    case RawDirection.UP:
                                        nb = d5;
                                        break;
                                    case RawDirection.NONE:
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                test = neighbourTest(nb) || face.nonFullFace && !Blocks.isTranslucent(nb);
                            }
                            // either neighbour test passes, or neighbour is not air + face is not full
                            if (test) {
                                if (AOenabled && !face.noAO) {
                                    if (dir != RawDirection.NONE) {
                                        int ox;
                                        int oy;
                                        int oz;

                                        int xb;
                                        int yb;
                                        int zb;

                                        for (int j = 0; j < 4; j++) {
                                            getOffset(ref offsetArray, dirIdx, j, 0, out xb, out yb, out zb);
                                            xb = x + xb;
                                            yb = y + yb;
                                            zb = z + zb;
                                            ox = getBlockFromCacheUnsafe(ref neighboursArray, xb, yb, zb);

                                            getOffset(ref offsetArray, dirIdx, j, 1, out xb, out yb, out zb);
                                            xb = x + xb;
                                            yb = y + yb;
                                            zb = z + zb;
                                            oy = getBlockFromCacheUnsafe(ref neighboursArray, xb, yb, zb);

                                            getOffset(ref offsetArray, dirIdx, j, 2, out xb, out yb, out zb);
                                            xb = x + xb;
                                            yb = y + yb;
                                            zb = z + zb;
                                            oz = getBlockFromCacheUnsafe(ref neighboursArray, xb, yb, zb);

                                            switch (j) {
                                                case 0:
                                                    ao1 = calculateAOFixed(ox, oy, oz);
                                                    break;
                                                case 1:
                                                    ao2 = calculateAOFixed(ox, oy, oz);
                                                    break;
                                                case 2:
                                                    ao3 = calculateAOFixed(ox, oy, oz);
                                                    break;
                                                case 3:
                                                    ao4 = calculateAOFixed(ox, oy, oz);
                                                    break;

                                            }
                                        }
                                    }
                                }
                                tex = Block.texCoords(faces[d].min);
                                texMax = Block.texCoords(faces[d].max);

                                data1 = Block.packData((byte)dir, ao1);
                                data2 = Block.packData((byte)dir, ao2);
                                data3 = Block.packData((byte)dir, ao3);
                                data4 = Block.packData((byte)dir, ao4);


                                // add vertices

                                tempVertices[0] = new BlockVertex(wx + accessRef(ref facesRef, d).x1, wy + accessRef(ref facesRef, d).y1, wz + accessRef(ref facesRef, d).z1, tex.X, tex.Y, data1);
                                tempVertices[1] = new BlockVertex(wx + accessRef(ref facesRef, d).x2, wy + accessRef(ref facesRef, d).y2, wz + accessRef(ref facesRef, d).z2, tex.X, texMax.Y, data2);
                                tempVertices[2] = new BlockVertex(wx + accessRef(ref facesRef, d).x3, wy + accessRef(ref facesRef, d).y3, wz + accessRef(ref facesRef, d).z3, texMax.X, texMax.Y, data3);
                                tempVertices[3] = new BlockVertex(wx + accessRef(ref facesRef, d).x4, wy + accessRef(ref facesRef, d).y4, wz + accessRef(ref facesRef, d).z4, texMax.X, tex.Y, data4);
                                chunkVertices.AddRange(tempVertices);
                                //cv += 4;

                                tempIndices[0] = i;
                                tempIndices[1] = (ushort)(i + 1);
                                tempIndices[2] = (ushort)(i + 2);
                                tempIndices[3] = i;
                                tempIndices[4] = (ushort)(i + 2);
                                tempIndices[5] = (ushort)(i + 3);
                                chunkIndices.AddRange(tempIndices);
                                i += 4;
                                //ci += 6;
                            }
                        }
                    }
                }
            }
        }
        //Console.Out.WriteLine($"vert4: {sw.Elapsed.TotalMicroseconds}us");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void addAO(int x, int y, int z, int x1, int y1, int z1, out int x2, out int y2, out int z2) {
        x2 = x + x1;
        y2 = y + y1;
        z2 = z + z1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3D<int> addAO(int x, int y, int z, int x1, int y1, int z1) {
        return new(x + x1, y + y1, z + z1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void getOffset(ref int arr, int dir, int idx, int vert, out int x, out int y, out int z) {
        // array has 6 directions, 4 indices which each contain 3 AOs of 3 ints each
        // 36 = 3 * 3 * 4
        // 9 = 3 * 3
        var index = (dir * 36) + idx * (9) + vert * 3;
        x = Unsafe.Add(ref arr, index);
        y = Unsafe.Add(ref arr, index + 1);
        z = Unsafe.Add(ref arr, index + 2);
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
    unsafe public static int toInt(bool b) {
        return *(byte*)&b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T access<T>(T[] arr, int index) {
        ref T arrayRef = ref MemoryMarshal.GetArrayDataReference(arr);
        return Unsafe.Add(ref arrayRef, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void access<T>(T[] arr, int index, T value) {
        ref T arrayRef = ref MemoryMarshal.GetArrayDataReference(arr);
        Unsafe.Add(ref arrayRef, index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T accessRef<T>(ref T arr, int index) {
        return Unsafe.Add(ref arr, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void accessRef<T>(ref T arr, int index, T value) {
        Unsafe.Add(ref arr, index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void setRange<T>(T[] arr, int index, T[] values) {
        ref T arrayRef = ref MemoryMarshal.GetArrayDataReference(arr);
        for (int i = 0; i < values.Length; i++) {
            Unsafe.Add(ref arrayRef, index + i) = values[i];
        }
    }

/*private Vector3D<int>[] getNeighbours(RawDirection side) {
    return offsetTable[(int)side];
}*/
}