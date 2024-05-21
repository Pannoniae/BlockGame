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

    // actually we don't need a list, regular arrays will do because it's only a few megs of space and it's shared
    // in the future when we want multithreaded meshing, we can just allocate like 4-8 of them and it will still be in the ballpark of 10MB
    public static readonly BlockVertex[] chunkVertices = new BlockVertex[Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * 24];
    public static readonly ushort[] chunkIndices = new ushort[Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * 36];
    // YZX again
    public static readonly ushort[] neighbours = new ushort[Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX];

    static object meshingLock = new();

    private static readonly int[] offsetTable = [
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
        var sw = new Stopwatch();
        sw.Start();
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
                Console.Out.WriteLine($"PartMeshing0.7: {sw.Elapsed.TotalMicroseconds}us");
                constructVertices(&opaqueBlocks, &notSolid);
                /*if (World.glob) {
                    MeasureProfiler.SaveData();
                }*/
                Console.Out.WriteLine($"PartMeshing1: {sw.Elapsed.TotalMicroseconds}us");
                if (chunkIndices.Length > 0) {
                    if (section.world.renderer.fastChunkSwitch) {
                        (vao as VerySharedBlockVAO).bindVAO();
                    }
                    else {
                        vao.bind();
                    }
                    vao.upload(chunkVertices, chunkIndices);
                }
            }
            lock (meshingLock) {
                if (hasTranslucentBlocks) {
                    // then we render everything which is translucent (water for now)
                    constructVertices(&Blocks.isTranslucent, &notTranslucent);
                    if (chunkIndices.Length > 0) {
                        if (section.world.renderer.fastChunkSwitch) {
                            (watervao as VerySharedBlockVAO).bindVAO();
                        }
                        else {
                            watervao.bind();
                        }
                        watervao.upload(chunkVertices, chunkIndices);
                        //world.sortedTransparentChunks.Add(this);
                    }
                    else {
                        //world.sortedTransparentChunks.Remove(this);
                    }
                }
            }
        }
        Console.Out.WriteLine($"Meshing: {sw.Elapsed.TotalMicroseconds}us");
        sw.Stop();
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
        //var sw = new Stopwatch();
        //sw.Start();

        hasTranslucentBlocks = false;
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
                    accessRef(ref neighboursArray, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1), bl);
                }
            }
        }

        // setup neighbouring chunks

        //Console.Out.WriteLine($"vert2: {sw.Elapsed.TotalMicroseconds}us");

        // if chunk is empty, nothing to do, don't need to check neighbours
        // btw this shouldn't fucking happen because we checked it but we check it anyway so our program doesn't crash if the chunk representation is changed

        for (int y = -1; y < Chunk.CHUNKSIZE + 1; y++) {
            for (int z = -1; z < Chunk.CHUNKSIZE + 1; z++) {
                for (int x = -1; x < Chunk.CHUNKSIZE + 1; x++) {
                    // if inside the chunk, skip
                    /*if (x is >= 0 and < Chunk.CHUNKSIZE &&
                        y is >= 0 and < Chunk.CHUNKSIZE &&
                        z is >= 0 and < Chunk.CHUNKSIZE) {
                        continue;
                    }*/
                    // skip-ahead
                    if (x is >= 0 and < Chunk.CHUNKSIZE) {
                        x = Chunk.CHUNKSIZE - 1;
                        continue;
                    }
                    if (y is >= 0 and < Chunk.CHUNKSIZE) {
                        y = Chunk.CHUNKSIZE - 1;
                        continue;
                    }
                    if (z is >= 0 and < Chunk.CHUNKSIZE) {
                        z = Chunk.CHUNKSIZE - 1;
                        continue;
                    }

                    var wpos = world.toWorldPos(section.chunkX, section.chunkY, section.chunkZ, x, y, z);
                    accessRef(ref neighboursArray, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1), world.getBlock(wpos.X, wpos.Y, wpos.Z));
                }
            }
        }
        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");
    }

    // if neighbourTest returns true for adjacent block, render, if it returns false, don't
    //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
    unsafe private void constructVertices(delegate*<int, bool> whichBlocks, delegate*<int, bool> neighbourTest) {
        //var sw = new Stopwatch();
        //sw.Start();
        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");

        // clear arrays before starting
        Array.Clear(chunkVertices);
        Array.Clear(chunkIndices);

        ushort i = 0;
        ushort cv = 0;
        ushort ci = 0;

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

                        //float offset = 0.0004f;

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
                            var dirIdx = (int)dir;
                            bool test;
                            if (dir == RawDirection.NONE) {
                                // if it's not a diagonal face, don't even bother checking neighbour because we have to render it anyway
                                test = true;
                            }
                            else {
                                var dirV = Direction.getDirection(dir);
                                var nbPosX = x + dirV.X;
                                var nbPosY = y + dirV.Y;
                                var nbPosZ = z + dirV.Z;
                                nb = getBlockFromCacheUnsafe(ref neighboursArray, nbPosX, nbPosY, nbPosZ);
                                test = neighbourTest(nb) || (face.nonFullFace && isSolid(nb));
                            }
                            // either neighbour test passes, or neighbour is not air + face is not full
                            if (test) {
                                if (true || !Settings.instance.AO || face.noAO) {
                                    ao1 = 0;
                                    ao2 = 0;
                                    ao3 = 0;
                                    ao4 = 0;
                                }
                                else {
                                    if (dir == RawDirection.NONE) {
                                        ao1 = 0;
                                        ao2 = 0;
                                        ao3 = 0;
                                        ao4 = 0;
                                    }
                                    else {
                                        Vector3D<int> offset;
                                        int ox;
                                        int oy;
                                        int oz;

                                        Vector3D<int> side;

                                        for (int j = 0; j < 4; j++) {
                                            offset = getOffset(ref offsetArray, dirIdx, j, 0);
                                            side = addAO(x, y, z, offset.X, offset.Y, offset.Z);
                                            ox = getBlockFromCacheUnsafe(ref neighboursArray, side.X, side.Y, side.Z);

                                            offset = getOffset(ref offsetArray, dirIdx, j, 1);
                                            side = addAO(x, y, z, offset.X, offset.Y, offset.Z);
                                            oy = getBlockFromCacheUnsafe(ref neighboursArray, side.X, side.Y, side.Z);

                                            offset = getOffset(ref offsetArray, dirIdx, j, 2);
                                            side = addAO(x, y, z, offset.X, offset.Y, offset.Z);
                                            oz = getBlockFromCacheUnsafe(ref neighboursArray, side.X, side.Y, side.Z);
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
                                chunkVertices.AddRange(cv, tempVertices);
                                cv += 4;

                                tempIndices[0] = i;
                                tempIndices[1] = (ushort)(i + 1);
                                tempIndices[2] = (ushort)(i + 2);
                                tempIndices[3] = (ushort)(i + 0);
                                tempIndices[4] = (ushort)(i + 2);
                                tempIndices[5] = (ushort)(i + 3);
                                chunkIndices.AddRange(ci, tempIndices);
                                i += 4;
                                ci += 6;
                            }
                        }
                    }
                }
            }
        }
        //Console.Out.WriteLine($"vert4: {sw.Elapsed.TotalMicroseconds}us");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void addAO(int x, int y, int z, int x1, int y1, int z1, out int x2, out int y2, out int z2) {
        x2 = x + x1;
        y2 = y + y1;
        z2 = z + z1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3D<int> addAO(int x, int y, int z, int x1, int y1, int z1) {
        return new(x + x1, y + y1, z + z1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3D<int> getOffset(ref int arr, int dir, int idx, int vert) {
        // array has 6 directions, 4 indices which each contain 3 AOs of 3 ints each
        // 36 = 3 * 3 * 4
        // 9 = 3 * 3
        var index = (dir * 36) + idx * (9) + vert * 3;
        var x = Unsafe.Add(ref arr, index);
        var y = Unsafe.Add(ref arr, index + 1);
        var z = Unsafe.Add(ref arr, index + 2);
        return new(x, y, z);
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
        ref T arrayRef = ref MemoryMarshal.GetArrayDataReference(arr);
        Unsafe.Add(ref arrayRef, index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T accessRef<T>(ref T arr, int index) {
        return Unsafe.Add(ref arr, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void accessRef<T>(ref T arr, int index, T value) {
        Unsafe.Add(ref arr, index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void setRange<T>(T[] arr, int index, T[] values) {
        ref T arrayRef = ref MemoryMarshal.GetArrayDataReference(arr);
        for (int i = 0; i < values.Length; i++) {
            Unsafe.Add(ref arrayRef, index + i) = values[i];
        }
    }

/*private Vector3D<int>[] getNeighbours(RawDirection side) {
    return offsetTable[(int)side];
}*/
}