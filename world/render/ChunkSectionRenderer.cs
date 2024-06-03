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
    public bool isEmptyRenderOpaque;
    public bool isEmptyRenderTranslucent;

    public VAO vao;
    public VAO watervao;

    public bool hasTranslucentBlocks;
    public bool hasOnlySolid;

    public readonly GL GL;

    public static readonly Func<int, bool> AOtest = bl => bl != -1 && Blocks.isSolid(bl);

    // we cheated GC! there is only one list preallocated
    // we need 16x16x16 blocks, each block has max. 24 vertices
    // for indices we need the full 36

    // actually we don't need a list, regular arrays will do because it's only a few megs of space and it's shared
    // in the future when we want multithreaded meshing, we can just allocate like 4-8 of them and it will still be in the ballpark of 10MB
    public static List<BlockVertex> chunkVertices = new(2048);
    public static List<ushort> chunkIndices = new(2048);
    // YZX again
    public static NeighbourBlockDataU neighbours;
    public static NeighbourBlockDataB neighbourLights;
    public static ArrayBlockData?[] neighbourSections = new ArrayBlockData?[27];

    static readonly object meshingLock = new();

    public Stopwatch sw = new Stopwatch();

    public static readonly sbyte[] offsetTable = [
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
        0, -1, 1, 1, -1, 0, 1, -1, 1,
        0, -1, -1, 1, -1, 0, 1, -1, -1,
        0, -1, -1, -1, -1, 0, -1, -1, -1,
        0, -1, 1, -1, -1, 0, -1, -1, 1,


        // up
        0, 1, 1, -1, 1, 0, -1, 1, 1,
        0, 1, -1, -1, 1, 0, -1, 1, -1,
        0, 1, -1, 1, 1, 0, 1, 1, -1,
        0, 1, 1, 1, 1, 0, 1, 1, 1,
    ];

    public ChunkSectionRenderer(ChunkSection section) {
        this.section = section;
    }

    private static bool opaqueBlocks(Block b) {
        return b.id != 0 && b.type != BlockType.TRANSLUCENT;
    }

    private static bool notSolid(Block b) {
        return Blocks.notSolid(b);
    }

    private static bool isSolid(Block b) {
        return Blocks.isSolid(b);
    }

    private static bool notAir(Block b) {
        return b.id != 0;
    }

    private static bool notTranslucent(Block b) {
        return !Blocks.isTranslucent(b);
    }

    /// <summary>
    /// TODO store the number of blocks in the chunksection and only allocate the vertex list up to that length
    /// </summary>
    public void meshChunk() {
        //sw.Start();
        if (section.world.renderer.fastChunkSwitch) {
            vao = new ExtremelySharedBlockVAO(section.world.renderer.chunkVAO);
            watervao = new ExtremelySharedBlockVAO(section.world.renderer.chunkVAO);
        }
        else {
            vao = new SharedBlockVAO();
            watervao = new SharedBlockVAO();
        }

        // if the section is empty, nothing to do
        //if (section.isEmpty) {
        //    return;
        //}

        //Console.Out.WriteLine($"PartMeshing0.5: {sw.Elapsed.TotalMicroseconds}us");
        // first we render everything which is NOT translucent
        lock (meshingLock) {
            setupNeighbours();

            // if chunk is full, don't mesh either
            if (hasOnlySolid) {
                isEmpty = true;
            }

            if (isEmpty) {
                return;
            }

            /*if (World.glob) {
                    MeasureProfiler.StartCollectingData();
                }*/
            //Console.Out.WriteLine($"PartMeshing0.7: {sw.Elapsed.TotalMicroseconds}us");
            constructVertices(VertexConstructionMode.OPAQUE);
            /*if (World.glob) {
                    MeasureProfiler.SaveData();
                }*/
            //Console.Out.WriteLine($"PartMeshing1: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
            if (chunkIndices.Count > 0) {
                isEmptyRenderOpaque = false;
                if (section.world.renderer.fastChunkSwitch) {
                    (vao as ExtremelySharedBlockVAO).bindVAO();
                }
                else {
                    vao.bind();
                }
                var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
                var finalIndices = CollectionsMarshal.AsSpan(chunkIndices);
                vao.upload(finalVertices, finalIndices);
                //Console.Out.WriteLine($"PartMeshing1.2: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
            }
            else {
                isEmptyRenderOpaque = true;
            }
        }
        lock (meshingLock) {
            if (hasTranslucentBlocks) {
                // then we render everything which is translucent (water for now)
                constructVertices(VertexConstructionMode.TRANSLUCENT);
                //Console.Out.WriteLine($"PartMeshing1.4: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
                if (chunkIndices.Count > 0) {
                    isEmptyRenderTranslucent = false;
                    if (section.world.renderer.fastChunkSwitch) {
                        (watervao as ExtremelySharedBlockVAO).bindVAO();
                    }
                    else {
                        watervao.bind();
                    }
                    var tFinalVertices = CollectionsMarshal.AsSpan(chunkVertices);
                    var tFinalIndices = CollectionsMarshal.AsSpan(chunkIndices);
                    watervao.upload(tFinalVertices, tFinalIndices);
                    //Console.Out.WriteLine($"PartMeshing1.7: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
                    //world.sortedTransparentChunks.Add(this);
                }
                else {
                    //world.sortedTransparentChunks.Remove(this);
                    isEmptyRenderTranslucent = true;
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
        if (!isEmptyRenderOpaque && isVisible(camera.frustum)) {
            vao.bind();
            //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

            uint renderedVerts = vao.render();
            Game.instance.metrics.renderedVerts += (int)renderedVerts;
            Game.instance.metrics.renderedChunks += 1;
        }
    }

    public void drawTransparent(PlayerCamera camera) {
        if (hasTranslucentBlocks && !isEmptyRenderTranslucent && isVisible(camera.frustum)) {
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
        isEmpty = true;
        //Console.Out.WriteLine($"vert1: {sw.Elapsed.TotalMicroseconds}us");

        // cache blocks
        // we need a 18x18 area
        // we load the 16x16 from the section itself then get the world for the rest
        // if the chunk section is an EmptyBlockData, don't bother
        // it will always be ArrayBlockData so we can access directly without those pesky BOUNDS CHECKS
        var blockData = section.blocks;
        ref ushort blockArrayRef = ref MemoryMarshal.GetReference<ushort>(blockData.blocks);
        ref byte sourceLightArrayRef = ref MemoryMarshal.GetReference<byte>(blockData.light);
        ref ushort neighboursArrayRef = ref MemoryMarshal.GetReference<ushort>(neighbours);
        ref byte lightArrayRef = ref MemoryMarshal.GetReference<byte>(neighbourLights);
        var world = section.world;
        int y;
        int z;
        int x;

        for (y = 0; y < Chunk.CHUNKSIZE; y++) {
            for (z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (x = 0; x < Chunk.CHUNKSIZE; x++) {
                    // index for array accesses
                    var secIndex = y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x;
                    var index = (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1);

                    var bl = Unsafe.Add(ref blockArrayRef, secIndex);
                    var light = Unsafe.Add(ref sourceLightArrayRef, secIndex);

                    if (bl != 0) {
                        isEmpty = false;
                    }
                    if (Blocks.isTranslucent(bl)) {
                        hasTranslucentBlocks = true;
                    }
                    if (bl == 0 || !Blocks.get(bl).isFullBlock) {
                        hasOnlySolid = false;
                    }
                    Unsafe.Add(ref neighboursArrayRef, index) = bl;
                    Unsafe.Add(ref lightArrayRef, index) = light;
                }
            }
        }

        // if is empty, just return, don't need to get neighbours
        if (isEmpty) {
            return;
        }

        //Console.Out.WriteLine($"vert2: {sw.Elapsed.TotalMicroseconds}us");

        static ushort getBlockFromSection(BlockData data, int x, int y, int z) {
            return data[x, y, z];
        }

        static byte getLightFromSection(BlockData data, int x, int y, int z) {
            return data.getLight(x, y, z);
        }

        static BlockData? gets(ChunkSection section, int x, int y, int z) {
            var sectionPos = World.getChunkSectionPos(x, y, z);
            return access(neighbourSections, (sectionPos.y - section.chunkY + 1) * 9 + (sectionPos.z - section.chunkZ + 1) * 3 + (sectionPos.x - section.chunkX) + 1);
        }

        // setup neighbouring sections
        var coord = section.chunkCoord;
        ref var neighbourSectionsArray = ref MemoryMarshal.GetArrayDataReference(neighbourSections);
        for (y = -1; y <= 1; y++) {
            for (z = -1; z <= 1; z++) {
                for (x = -1; x <= 1; x++) {
                    world.getChunkSectionMaybe(new ChunkSectionCoord(coord.x + x, coord.y + y, coord.z + z), out var sec);
                    Unsafe.Add(ref neighbourSectionsArray, (y + 1) * 9 + (z + 1) * 3 + x + 1) = sec?.blocks!;
                }
            }
        }

        for (y = -1; y < Chunk.CHUNKSIZE + 1; y++) {
            for (z = -1; z < Chunk.CHUNKSIZE + 1; z++) {
                for (x = -1; x < Chunk.CHUNKSIZE + 1; x++) {
                    // if inside the chunk, skip
                    if (x is >= 0 and < Chunk.CHUNKSIZE &&
                        z is >= 0 and < Chunk.CHUNKSIZE &&
                        y is >= 0 and < Chunk.CHUNKSIZE) {
                        // skip this entire loop
                        x = Chunk.CHUNKSIZE - 1;
                        continue;
                    }

                    // index for array accesses
                    var index = (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1);

                    int sx;
                    int sy;
                    int sz;

                    int cx;
                    int cy;
                    int cz;
                    cx = x;
                    cy = y;
                    cz = z;
                    alignBlock(ref cx, ref cy, ref cz);

                    sx = (int)MathF.Floor((float)(section.chunkX * Chunk.CHUNKSIZE + x) / Chunk.CHUNKSIZE);
                    sy = (int)MathF.Floor((float)(section.chunkY * Chunk.CHUNKSIZE + y) / Chunk.CHUNKSIZE);
                    sz = (int)MathF.Floor((float)(section.chunkZ * Chunk.CHUNKSIZE + z) / Chunk.CHUNKSIZE);
                    // get neighbouring section
                    var neighbourSection =
                        Unsafe.Add(ref neighbourSectionsArray, (sy - section.chunkY + 1) * 9 + (sz - section.chunkZ + 1) * 3 + (sx - section.chunkX) + 1);
                    var nn = neighbourSection != null;
                    var bl = nn ? neighbourSection![cx, cy, cz] : (ushort)0;
                    Unsafe.Add(ref neighboursArrayRef, index) = bl;
                    // if neighbour is not solid, we still have to mesh this chunk even though all of it is solid
                    if (bl == 0 || !Blocks.get(bl).isFullBlock) {
                        hasOnlySolid = false;
                    }

                    Unsafe.Add(ref lightArrayRef, index) = nn ? neighbourSection!.getLight(cx, cy, cz) : (byte)0;
                }
            }
        }
        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");
    }

    // if neighbourTest returns true for adjacent block, render, if it returns false, don't
    //[MethodImpl(MethodImplOptions.AggressiveOptimization)]

    // sorry for this mess, even fucking calli has big overhead

    [SkipLocalsInit]
    //unsafe private void constructVertices(delegate*<int, bool> whichBlocks, delegate*<int, bool> neighbourTest) {
    unsafe private void constructVertices(VertexConstructionMode mode) {
        //sw.Start();
        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");

        // clear arrays before starting
        chunkVertices.Clear();
        chunkIndices.Clear();

        Span<BlockVertex> tempVertices = stackalloc BlockVertex[4];
        Span<ushort> tempIndices = stackalloc ushort[6];
        Span<ushort> nba = stackalloc ushort[6];
        Span<byte> lba = stackalloc byte[6];

        ushort i = 0;


        // BYTE OF SETTINGS
        // 1 = AO
        // 2 = smooth lighting
        byte settings = (byte)(toInt(Settings.instance.smoothLighting) << 1 | toInt(Settings.instance.AO));
        const int SETTING_AO = 1;
        const int SETTING_SMOOTH_LIGHTING = 1;
        //ushort cv = 0;
        //ushort ci = 0;

        // helper function to get blocks from cache
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort getBlockFromCacheUnsafe(ref ushort arrayBase, int x, int y, int z) {
            return Unsafe.Add(ref arrayBase, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte getLightFromCacheUnsafe(ref byte arrayBase, int x, int y, int z) {
            return Unsafe.Add(ref arrayBase, (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1));
        }

        ref ushort neighbourRef = ref MemoryMarshal.GetReference<ushort>(neighbours);
        ref byte lightRef = ref MemoryMarshal.GetReference<byte>(neighbourLights);
        ref var offsetArray = ref MemoryMarshal.GetArrayDataReference<sbyte>(offsetTable);

        bool test2 = false;

        for (int y = 0; y < Chunk.CHUNKSIZE; y++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    Block bl = Blocks.get(getBlockFromCacheUnsafe(ref neighbourRef, x, y, z));
                    switch (mode) {
                        case VertexConstructionMode.OPAQUE:
                            if (!opaqueBlocks(bl)) {
                                continue;
                            }
                            break;
                        case VertexConstructionMode.TRANSLUCENT:
                            if (!Blocks.isTranslucent(bl)) {
                                continue;
                            }
                            break;
                    }

                    // unrolled world.toWorldPos
                    float wx = section.chunkX * Chunk.CHUNKSIZE + x;
                    float wy = section.chunkY * Chunk.CHUNKSIZE + y;
                    float wz = section.chunkZ * Chunk.CHUNKSIZE + z;

                    if (bl.customRender) {
                        bl.render(section.world, new Vector3D<int>((int)wx, (int)wy, (int)wz), chunkVertices, chunkIndices, i);
                        continue;
                    }

                    // calculate texcoords
                    Vector4 tex = new Vector4();

                    //float offset = 0.0004f;


                    // calculate AO for all 8 vertices
                    // this is garbage but we'll deal with it later
                    // bottom

                    // one AO value fits on 2 bits so the whole thing fits in a byte
                    byte ao = 0;
                    FourShorts data;
                    FourBytes light;

                    Unsafe.SkipInit(out light.Whole);
                    Unsafe.SkipInit(out light.First);
                    Unsafe.SkipInit(out light.Second);
                    Unsafe.SkipInit(out light.Third);
                    Unsafe.SkipInit(out light.Fourth);

                    // setup neighbour data
                    Block nb;
                    // if all 6 neighbours are solid, we don't even need to bother iterating the faces

                    nba[0] = getBlockFromCacheUnsafe(ref neighbourRef, x - 1, y, z);
                    nba[1] = getBlockFromCacheUnsafe(ref neighbourRef, x + 1, y, z);
                    nba[2] = getBlockFromCacheUnsafe(ref neighbourRef, x, y, z - 1);
                    nba[3] = getBlockFromCacheUnsafe(ref neighbourRef, x, y, z + 1);
                    nba[4] = getBlockFromCacheUnsafe(ref neighbourRef, x, y - 1, z);
                    nba[5] = getBlockFromCacheUnsafe(ref neighbourRef, x, y + 1, z);
                    if (nba[0] != 0 && Blocks.get(nba[0]).isFullBlock &&
                        nba[1] != 0 && Blocks.get(nba[1]).isFullBlock &&
                        nba[2] != 0 && Blocks.get(nba[2]).isFullBlock &&
                        nba[3] != 0 && Blocks.get(nba[3]).isFullBlock &&
                        nba[4] != 0 && Blocks.get(nba[4]).isFullBlock &&
                        nba[5] != 0 && Blocks.get(nba[5]).isFullBlock) {
                        continue;
                    }

                    // get light data too
                    lba[0] = getLightFromCacheUnsafe(ref lightRef, x - 1, y, z);
                    lba[1] = getLightFromCacheUnsafe(ref lightRef, x + 1, y, z);
                    lba[2] = getLightFromCacheUnsafe(ref lightRef, x, y, z - 1);
                    lba[3] = getLightFromCacheUnsafe(ref lightRef, x, y, z + 1);
                    lba[4] = getLightFromCacheUnsafe(ref lightRef, x, y - 1, z);
                    lba[5] = getLightFromCacheUnsafe(ref lightRef, x, y + 1, z);


                    ref var facesRef = ref MemoryMarshal.GetArrayDataReference(bl.model.faces);

                    for (int d = 0; d < bl.model.faces.Length; d++) {
                        Face face = Unsafe.Add(ref facesRef, d);
                        var dir = face.direction;
                        if (dir == RawDirection.NONE) {
                            // if it's not a diagonal face, don't even bother checking neighbour because we have to render it anyway
                        }
                        else {
                            nb = Blocks.get(nba[(byte)dir]);
                            switch (mode) {
                                case VertexConstructionMode.OPAQUE:
                                    test2 = Blocks.notSolid(nb) || !nb.isFullBlock;
                                    break;
                                case VertexConstructionMode.TRANSLUCENT:
                                    test2 = notTranslucent(nb) && (notSolid(nb) || !nb.isFullBlock);
                                    break;
                            }
                            test2 = test2 || face.nonFullFace && !Blocks.isTranslucent(nb);
                        }
                        // either neighbour test passes, or neighbour is not air + face is not full
                        if (test2) {
                            if ((settings & SETTING_SMOOTH_LIGHTING) == 0) {
                                light.First = light.Second = light.Third = light.Fourth = lba[(byte)dir];
                            }
                            // AO requires smooth lighting. Otherwise don't need to deal with sampling any of this
                            if ((settings & SETTING_SMOOTH_LIGHTING) == 1 || (settings & SETTING_AO) == 1) {
                                if (dir != RawDirection.NONE) {

                                    // ox, oy, oz, mult
                                    FourShorts o;
                                    Unsafe.SkipInit(out o);
                                    // bx, by, bz
                                    FourSBytes b;
                                    // lx, ly, lz, lo
                                    FourBytes l;

                                    ao = 0;
                                    light.Whole = 0;

                                    for (int j = 0; j < 4; j++) {
                                        //mult = dirIdx * 36 + j * 9 + vert * 3;
                                        o.Fourth = (ushort)((int)dir * 36 + j * 9);
                                        // premultiply cuz its faster that way
                                        b.First = (sbyte)(x + Unsafe.Add(ref offsetArray, o.Fourth));
                                        o.Fourth++;
                                        b.Second = (sbyte)(y + Unsafe.Add(ref offsetArray, o.Fourth));
                                        o.Fourth++;
                                        b.Third = (sbyte)(z + Unsafe.Add(ref offsetArray, o.Fourth));
                                        o.Fourth++;
                                        o.First = getBlockFromCacheUnsafe(ref neighbourRef, b.First, b.Second, b.Third);
                                        l.First = getLightFromCacheUnsafe(ref lightRef, b.First, b.Second, b.Third);

                                        b.First = (sbyte)(x + Unsafe.Add(ref offsetArray, o.Fourth));
                                        o.Fourth++;
                                        b.Second = (sbyte)(y + Unsafe.Add(ref offsetArray, o.Fourth));
                                        o.Fourth++;
                                        b.Third = (sbyte)(z + Unsafe.Add(ref offsetArray, o.Fourth));
                                        o.Fourth++;
                                        o.Second = getBlockFromCacheUnsafe(ref neighbourRef, b.First, b.Second, b.Third);
                                        l.Second = getLightFromCacheUnsafe(ref lightRef, b.First, b.Second, b.Third);

                                        b.First = (sbyte)(x + Unsafe.Add(ref offsetArray, o.Fourth));
                                        o.Fourth++;
                                        b.Second = (sbyte)(y + Unsafe.Add(ref offsetArray, o.Fourth));
                                        o.Fourth++;
                                        b.Third = (sbyte)(z + Unsafe.Add(ref offsetArray, o.Fourth));
                                        //mult++;
                                        o.Third = getBlockFromCacheUnsafe(ref neighbourRef, b.First, b.Second, b.Third);
                                        l.Third = getLightFromCacheUnsafe(ref lightRef, b.First, b.Second, b.Third);

                                        // only apply AO if enabled
                                        if ((settings & SETTING_AO) == 1 && !face.noAO) {
                                            ao |= (byte)((calculateAOFixed(o.First, o.Second, o.Third) & 0x3) << j * 2);
                                            //Console.Out.WriteLine(ao);
                                        }
                                        if ((settings & SETTING_SMOOTH_LIGHTING) == 1) {
                                            // if smooth lighting enabled, average light from neighbour face + the 3 other ones
                                            // calculate average
                                            l.Fourth = lba[(byte)dir];


                                            // this averages the four light values. If the block is opaque, it ignores the light value.
                                            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
                                            byte average(byte lx, byte ly, byte lz, byte lo, FourShorts o) {
                                                byte flags = 0;
                                                // check ox
                                                if (o.First == 0) {
                                                    flags = 1;
                                                }
                                                if (o.Second == 0) {
                                                    flags |= 2;
                                                }
                                                // if both sides are blocked, don't check the corner, won't be visible anyway
                                                if (o.Third == 0 && flags != 0) {
                                                    flags |= 4;
                                                }
                                                return (byte)((lx * (flags & 1) + ly * ((flags & 2) >> 1) + lz * ((flags & 4) >> 2) + lo) / (BitOperations.PopCount(flags) + 1f));
                                            }

                                            // split light and reassemble it again
                                            light.Whole |= (uint)((byte)(
                                                average((byte)(l.First >> 4), (byte)(l.Second >> 4), (byte)(l.Third >> 4), (byte)(l.Fourth >> 4),
                                                    o)
                                                << 4 |
                                                average((byte)(l.First & 0xF), (byte)(l.Second & 0xF), (byte)(l.Third & 0xF), (byte)(l.Fourth & 0xF),
                                                    o)
                                            ) << j * 8);
                                        }
                                    }
                                }
                            }

                            tex.X = Block.texU(face.min.u);
                            tex.Y = Block.texV(face.min.v);
                            tex.Z = Block.texU(face.max.u);
                            tex.W = Block.texV(face.max.v);

                            data.First = Block.packData((byte)dir, (byte)(ao & 0x3), light.First);
                            data.Second = Block.packData((byte)dir, (byte)(ao >> 2 & 0x3), light.Second);
                            data.Third = Block.packData((byte)dir, (byte)(ao >> 4 & 0x3), light.Third);
                            data.Fourth = Block.packData((byte)dir, (byte)(ao >> 6 & 0x3), light.Fourth);


                            // add vertices

                            tempVertices[0] = new BlockVertex(wx + face.x1, wy + face.y1, wz + face.z1, tex.X, tex.Y, data.First);
                            tempVertices[1] = new BlockVertex(wx + face.x2, wy + face.y2, wz + face.z2, tex.X, tex.W, data.Second);
                            tempVertices[2] = new BlockVertex(wx + face.x3, wy + face.y3, wz + face.z3, tex.Z, tex.W, data.Third);
                            tempVertices[3] = new BlockVertex(wx + face.x4, wy + face.y4, wz + face.z4, tex.Z, tex.Y, data.Fourth);
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
    private static void getOffset(ref int arr, int index, out int x, out int y, out int z) {
        // array has 6 directions, 4 indices which each contain 3 AOs of 3 ints each
        // 36 = 3 * 3 * 4
        // 9 = 3 * 3
        x = Unsafe.Add(ref arr, index);
        y = Unsafe.Add(ref arr, index + 1);
        z = Unsafe.Add(ref arr, index + 2);
    }

    public static void alignBlock(ref int x, ref int y, ref int z) {
        switch (x) {
            case < 0:
                x += Chunk.CHUNKSIZE;
                break;
            case > Chunk.CHUNKSIZE - 1:
                x -= Chunk.CHUNKSIZE;
                break;
        }
        switch (y) {
            case < 0:
                y += Chunk.CHUNKSIZE;
                break;
            case > Chunk.CHUNKSIZE - 1:
                y -= Chunk.CHUNKSIZE;
                break;
        }
        switch (z) {
            case < 0:
                z += Chunk.CHUNKSIZE;
                break;
            case > Chunk.CHUNKSIZE - 1:
                z -= Chunk.CHUNKSIZE;
                break;
        }
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
    public static byte calculateAOFixed(int side1, int side2, int corner) {
        var test1 = Blocks.isSolid(side1);
        var test2 = Blocks.isSolid(side2);
        if (test1 && test2) {
            return 3;
        }
        return (byte)(toInt(Blocks.get(side1).isFullBlock) + toInt(Blocks.get(side2).isFullBlock) + toInt(Blocks.get(corner).isFullBlock));
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
    public static T access<T>(Span<T> arr, int index) {
        ref T arrayRef = ref MemoryMarshal.GetReference(arr);
        return Unsafe.Add(ref arrayRef, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void access<T>(Span<T> arr, int index, T value) {
        ref T arrayRef = ref MemoryMarshal.GetReference(arr);
        Unsafe.Add(ref arrayRef, index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T access<T>(ReadOnlySpan<T> arr, int index) {
        ref T arrayRef = ref MemoryMarshal.GetReference(arr);
        return Unsafe.Add(ref arrayRef, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void access<T>(ReadOnlySpan<T> arr, int index, T value) {
        ref T arrayRef = ref MemoryMarshal.GetReference(arr);
        Unsafe.Add(ref arrayRef, index) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T accessRef<T>(ref T arr, int index) {
        return ref Unsafe.Add(ref arr, index);
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
}

[StructLayout(LayoutKind.Explicit)]
public struct FourShorts {
    [FieldOffset(0)]
    public ulong Whole;
    [FieldOffset(0)]
    public ushort First;
    [FieldOffset(2)]
    public ushort Second;
    [FieldOffset(4)]
    public ushort Third;
    [FieldOffset(6)]
    public ushort Fourth;
}

[StructLayout(LayoutKind.Explicit)]
public struct FourBytes {
    [FieldOffset(0)]
    public uint Whole;
    [FieldOffset(0)]
    public byte First;
    [FieldOffset(1)]
    public byte Second;
    [FieldOffset(2)]
    public byte Third;
    [FieldOffset(3)]
    public byte Fourth;
    public FourBytes(byte b0, byte b1, byte b2, byte b3) {
        First = b0;
        Second = b1;
        Third = b2;
        Fourth = b3;
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct FourSBytes {
    [FieldOffset(0)]
    public int Whole;
    [FieldOffset(0)]
    public sbyte First;
    [FieldOffset(1)]
    public sbyte Second;
    [FieldOffset(2)]
    public sbyte Third;
    [FieldOffset(3)]
    public sbyte Fourth;
}

[StructLayout(LayoutKind.Explicit)]
public struct TwoFloats {
    [FieldOffset(0)]
    public ulong Whole;
    [FieldOffset(0)]
    public float First;
    [FieldOffset(4)]
    public float Second;
}

public enum VertexConstructionMode {
    OPAQUE,
    TRANSLUCENT
}