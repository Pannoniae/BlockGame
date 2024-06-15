using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BlockGame.GUI;
using BlockGame.util;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class ChunkSectionRenderer : IDisposable {
    public ChunkSection section;


    // we need it here because completely full chunks are also empty of any rendering
    public bool isEmpty;
    public bool isEmptyRenderOpaque;
    public bool isEmptyRenderTranslucent;

    public VAO? vao;
    public VAO? watervao;

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
    public static ushort[] neighbours = new ushort[Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX];
    public static byte[] neighbourLights = new byte[Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX];
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

    private static bool notSolid(ushort b) {
        return Blocks.notSolid(b);
    }

    private static bool isSolid(Block b) {
        return Blocks.isSolid(b);
    }

    private static bool notAir(Block b) {
        return b.id != 0;
    }

    private static bool notTranslucent(ushort b) {
        return !Blocks.isTranslucent(b);
    }

    /// <summary>
    /// TODO store the number of blocks in the chunksection and only allocate the vertex list up to that length
    /// </summary>
    public void meshChunk() {
        sw.Restart();
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
        //lock (meshingLock) {
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
        Console.Out.WriteLine($"PartMeshing0.7: {sw.Elapsed.TotalMicroseconds}us");
        constructVertices(VertexConstructionMode.OPAQUE);
        /*if (World.glob) {
                MeasureProfiler.SaveData();
            }*/
        Console.Out.WriteLine($"PartMeshing1: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
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
        //}
        //lock (meshingLock) {
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
        //}
        Console.Out.WriteLine($"Meshing: {sw.Elapsed.TotalMicroseconds}us");
        sw.Stop();
    }

    public ushort toVertex(float f) {
        return (ushort)(f / 16f * ushort.MaxValue);
    }


    public bool isVisible(BoundingFrustum frustum) {
        return frustum.Contains(section.bbbox) != ContainmentType.Disjoint;
    }

    public void drawOpaque() {
        if (!isEmptyRenderOpaque) {
            vao.bind();
            //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

            uint renderedVerts = vao.render();
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedChunks += 1;
        }
    }

    public void drawTransparent() {
        if (hasTranslucentBlocks && !isEmptyRenderTranslucent) {
            watervao.bind();
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
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
        ref ushort blockArrayRef = ref MemoryMarshal.GetArrayDataReference(section.blocks.blocks);
        ref byte sourceLightArrayRef = ref MemoryMarshal.GetArrayDataReference(section.blocks.light);
        ref ushort neighboursArrayRef = ref MemoryMarshal.GetArrayDataReference(neighbours);
        ref byte lightArrayRef = ref MemoryMarshal.GetArrayDataReference(neighbourLights);
        var world = section.world;
        int y;
        int z;
        int x;

        for (int i = 0; i < Chunk.MAXINDEX; i++) {
            // index for array accesses
            x = i & 0xF;
            z = i >> 4 & 0xF;
            y = i >> 8;
            var index = (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1);

            var bl = blockArrayRef;
            var light = sourceLightArrayRef;

            if (isEmpty && bl != 0) {
                isEmpty = false;
            }
            if (!hasTranslucentBlocks && Blocks.isTranslucent(bl)) {
                hasTranslucentBlocks = true;
            }
            if (hasOnlySolid && (bl == 0 || !Blocks.isFullBlock(bl))) {
                hasOnlySolid = false;
            }
            Unsafe.Add(ref neighboursArrayRef, index) = bl;
            Unsafe.Add(ref lightArrayRef, index) = light;

            // increment
            blockArrayRef = ref Unsafe.Add(ref blockArrayRef, 1);
            sourceLightArrayRef = ref Unsafe.Add(ref sourceLightArrayRef, 1);
        }

        // if is empty, just return, don't need to get neighbours
        if (isEmpty) {
            return;
        }

        //Console.Out.WriteLine($"vert2: {sw.Elapsed.TotalMicroseconds}us");

        // setup neighbouring sections
        var coord = section.chunkCoord;
        ref var neighbourSectionsArray = ref MemoryMarshal.GetArrayDataReference(neighbourSections);
        for (y = -1; y <= 1; y++) {
            for (z = -1; z <= 1; z++) {
                for (x = -1; x <= 1; x++) {
                    var sec = world.getChunkSectionUnsafe(new ChunkSectionCoord(coord.x + x, coord.y + y, coord.z + z));
                    Unsafe.Add(ref neighbourSectionsArray, (y + 1) * 9 + (z + 1) * 3 + x + 1) = sec?.blocks!;
                }
            }
        }

        for (y = -1; y < Chunk.CHUNKSIZE + 1; y++) {
            for (z = -1; z < Chunk.CHUNKSIZE + 1; z++) {
                for (x = -1; x < Chunk.CHUNKSIZE + 1; x++) {
                    //Console.Out.WriteLine($"{i} {x} {y} {z}");


                    // if inside the chunk, skip
                    if (x is >= 0 and < Chunk.CHUNKSIZE &&
                        z is >= 0 and < Chunk.CHUNKSIZE &&
                        y is >= 0 and < Chunk.CHUNKSIZE) {
                        // skip this entire loop
                        // also increment the references
                        // we need to add 1 because we are incrementing too!
                        var diff = Chunk.CHUNKSIZE - x;
                        neighboursArrayRef = ref Unsafe.Add(ref neighboursArrayRef, diff);
                        lightArrayRef = ref Unsafe.Add(ref lightArrayRef, diff);
                        x = Chunk.CHUNKSIZE - 1;
                        continue;
                    }

                    // index for array accesses
                    //var index = (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1);

                    // aligned position (between 0 and 16)
                    int cx = (x + 16) % 16;
                    int cy = (y + 16) % 16;
                    int cz = (z + 16) % 16;

                    // section position (can be -1, 0, 1)
                    // get neighbouring section
                    var neighbourSection =
                        Unsafe.Add(ref neighbourSectionsArray, ((y >> 4) + 1) * 9 + ((z >> 4) + 1) * 3 + (x >> 4) + 1);
                    var nn = neighbourSection != null;
                    var bl = nn ? neighbourSection![cx, cy, cz] : (ushort)0;

                    // set neighbours array element  to block
                    neighboursArrayRef = bl;
                    // if neighbour is not solid, we still have to mesh this chunk even though all of it is solid
                    if (hasOnlySolid && !Blocks.isFullBlock(bl)) {
                        hasOnlySolid = false;
                    }

                    // set light array element to light
                    lightArrayRef = nn ? neighbourSection!.getLight(cx, cy, cz) : (byte)15;

                    // increment
                    neighboursArrayRef = ref Unsafe.Add(ref neighboursArrayRef, 1);
                    lightArrayRef = ref Unsafe.Add(ref lightArrayRef, 1);
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
        Span<ushort> tempIndices = stackalloc ushort[8].Slice(0, 6);
        Vector128<ushort> indices = Vector128<ushort>.Zero;

        Span<ushort> nba = stackalloc ushort[6];
        Span<byte> lba = stackalloc byte[6];


        // BYTE OF SETTINGS
        // 1 = AO
        // 2 = smooth lighting
        byte settings = (byte)(toInt(Settings.instance.smoothLighting) << 1 | toInt(Settings.instance.AO));
        const int SETTING_AO = 1;
        const int SETTING_SMOOTH_LIGHTING = 2;
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

        ref ushort blockArrayRef = ref MemoryMarshal.GetArrayDataReference(section.blocks.blocks);
        ref ushort neighbourRef = ref MemoryMarshal.GetArrayDataReference(neighbours);
        ref byte lightRef = ref MemoryMarshal.GetArrayDataReference(neighbourLights);
        ref sbyte offsetArray = ref MemoryMarshal.GetArrayDataReference(offsetTable);

        Vector128<ushort> indicesMask = Vector128.Create((ushort)0, 1, 2, 0, 2, 3, 0, 0);
        Vector128<ushort> complement = Vector128.Create((ushort)4, 3, 2, 4, 2, 1, 0, 0);

        bool test2 = false;
        for (int idx = 0; idx < Chunk.MAXINDEX; idx++) {
            // index for array accesses
            int x = idx & 0xF;
            int z = idx >> 4 & 0xF;
            int y = idx >> 8;

            var index = (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1);

            Block bl = Blocks.get(blockArrayRef);
            switch (mode) {
                case VertexConstructionMode.OPAQUE:
                    if (!opaqueBlocks(bl)) {
                        goto increment;
                    }
                    break;
                case VertexConstructionMode.TRANSLUCENT:
                    if (!Blocks.isTranslucent(bl)) {
                        goto increment;
                    }
                    break;
            }

            // unrolled world.toWorldPos
            float wx = section.chunkX * Chunk.CHUNKSIZE + x;
            float wy = section.chunkY * Chunk.CHUNKSIZE + y;
            float wz = section.chunkZ * Chunk.CHUNKSIZE + z;

            if (bl.customRender) {
                bl.render(section.world, new Vector3D<int>((int)wx, (int)wy, (int)wz), chunkVertices, chunkIndices, indices[0]);
                goto increment;
            }

            // calculate texcoords
            var tex = new Vector4();


            // calculate AO for all 8 vertices
            // this is garbage but we'll deal with it later

            // one AO value fits on 2 bits so the whole thing fits in a byte
            byte ao = 0;

            // setup neighbour data
            // if all 6 neighbours are solid, we don't even need to bother iterating the faces
            nba[0] = Unsafe.Add(ref neighbourRef, index - 1);
            nba[1] = Unsafe.Add(ref neighbourRef, index + 1);
            nba[2] = Unsafe.Add(ref neighbourRef, index - Chunk.CHUNKSIZEEX);
            nba[3] = Unsafe.Add(ref neighbourRef, index + Chunk.CHUNKSIZEEX);
            nba[4] = Unsafe.Add(ref neighbourRef, index - Chunk.CHUNKSIZEEXSQ);
            nba[5] = Unsafe.Add(ref neighbourRef, index + Chunk.CHUNKSIZEEXSQ);
            if (nba[0] != 0 && Blocks.isFullBlock(nba[0]) &&
                nba[1] != 0 && Blocks.isFullBlock(nba[1]) &&
                nba[2] != 0 && Blocks.isFullBlock(nba[2]) &&
                nba[3] != 0 && Blocks.isFullBlock(nba[3]) &&
                nba[4] != 0 && Blocks.isFullBlock(nba[4]) &&
                nba[5] != 0 && Blocks.isFullBlock(nba[5])) {
                goto increment;
            }

            FourBytes light;

            Unsafe.SkipInit(out light.Whole);
            Unsafe.SkipInit(out light.First);
            Unsafe.SkipInit(out light.Second);
            Unsafe.SkipInit(out light.Third);
            Unsafe.SkipInit(out light.Fourth);

            // get light data too
            lba[0] = Unsafe.Add(ref lightRef, index - 1);
            lba[1] = Unsafe.Add(ref lightRef, index + 1);
            lba[2] = Unsafe.Add(ref lightRef, index - Chunk.CHUNKSIZEEX);
            lba[3] = Unsafe.Add(ref lightRef, index + Chunk.CHUNKSIZEEX);
            lba[4] = Unsafe.Add(ref lightRef, index - Chunk.CHUNKSIZEEXSQ);
            lba[5] = Unsafe.Add(ref lightRef, index + Chunk.CHUNKSIZEEXSQ);


            ref Face facesRef = ref MemoryMarshal.GetArrayDataReference(bl.model.faces);

            for (int d = 0; d < bl.model.faces.Length; d++) {
                var dir = facesRef.direction;

                // if bottom of the world, don't bother
                if (y == 0 && dir == RawDirection.DOWN) {
                    goto increment2;
                }

                if (dir == RawDirection.NONE) {
                    // if it's not a diagonal face, don't even bother checking neighbour because we have to render it anyway
                    test2 = true;
                    light.First = light.Second = light.Third = light.Fourth = Unsafe.Add(ref lightRef, index);
                }
                else {
                    ushort nb = nba[(byte)dir];
                    switch (mode) {
                        case VertexConstructionMode.OPAQUE:
                            test2 = Blocks.notSolid(nb) || !Blocks.isFullBlock(nb);
                            break;
                        case VertexConstructionMode.TRANSLUCENT:
                            test2 = notTranslucent(nb) && (notSolid(nb) || !Blocks.isFullBlock(nb));
                            break;
                    }
                    test2 = test2 || facesRef.nonFullFace && !Blocks.isTranslucent(nb);
                }
                // either neighbour test passes, or neighbour is not air + face is not full
                if (test2) {
                    if ((settings & SETTING_SMOOTH_LIGHTING) == 0) {
                        light.Whole = (uint)(lba[(byte)dir] | lba[(byte)dir] << 8 | lba[(byte)dir] << 16 | lba[(byte)dir] << 24);
                    }
                    // AO requires smooth lighting. Otherwise don't need to deal with sampling any of this
                    if ((settings & SETTING_SMOOTH_LIGHTING) != 0 || (settings & SETTING_AO) != 0) {
                        if (dir != RawDirection.NONE) {
                            // ox, oy, oz
                            byte o;
                            // need to store 9 sbytes so it's a 16-element vector
                            Vector128<sbyte> vector;
                            // lx, ly, lz, lo
                            FourBytes l;
                            Unsafe.SkipInit(out l);

                            ao = 0;
                            light.Whole = 0;

                            for (int j = 0; j < 4; j++) {
                                //mult = dirIdx * 36 + j * 9 + vert * 3;
                                // premultiply cuz its faster that way

                                // load the vector with the offsets
                                vector = Vector128.LoadUnsafe(ref Unsafe.Add(ref offsetArray, (int)dir * 36 + j * 9));

                                o = toByte(Blocks.isFullBlock(
                                    Unsafe.Add(ref neighbourRef, index + vector[0] + vector[1] * Chunk.CHUNKSIZEEXSQ + vector[2] * Chunk.CHUNKSIZEEX)));
                                l.First = Unsafe.Add(ref lightRef, index + vector[0] + vector[1] * Chunk.CHUNKSIZEEXSQ + vector[2] * Chunk.CHUNKSIZEEX);

                                o |= (byte)(toByte(Blocks.isFullBlock(
                                    Unsafe.Add(ref neighbourRef, index + vector[3] + vector[4] * Chunk.CHUNKSIZEEXSQ + vector[5] * Chunk.CHUNKSIZEEX))) << 1);
                                l.Second = Unsafe.Add(ref lightRef, index + vector[3] + vector[4] * Chunk.CHUNKSIZEEXSQ + vector[5] * Chunk.CHUNKSIZEEX);

                                //mult++;
                                o |= (byte)(toByte(Blocks.isFullBlock(
                                    Unsafe.Add(ref neighbourRef, index + vector[6] + vector[7] * Chunk.CHUNKSIZEEXSQ + vector[8] * Chunk.CHUNKSIZEEX))) << 2);
                                l.Third = Unsafe.Add(ref lightRef, index + vector[6] + vector[7] * Chunk.CHUNKSIZEEXSQ + vector[8] * Chunk.CHUNKSIZEEX);

                                // only apply AO if enabled
                                if ((settings & SETTING_AO) == 1 && !facesRef.noAO) {
                                    ao |= (byte)((calculateAOFixed(o) & 0x3) << j * 2);
                                }

                                // if face is noAO, don't average....
                                if ((settings & SETTING_SMOOTH_LIGHTING) != 0) {
                                    // if smooth lighting enabled, average light from neighbour face + the 3 other ones
                                    // calculate average
                                    l.Fourth = lba[(byte)dir];

                                    // this averages the four light values. If the block is opaque, it ignores the light value.
                                    // oFlags are opacity of side1, side2 and corner
                                    // (1 == opaque, 0 == transparent)
                                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                                    byte average(FourBytes lightNibble, byte oFlags) {
                                        // if both sides are blocked, don't check the corner, won't be visible anyway
                                        // if corner == 0 && side1 and side2 aren't both true, then corner is visible
                                        //if ((oFlags & 4) == 0 && oFlags != 3) {
                                        if (oFlags < 3) {
                                            // set the 4 bit of oFlags to 0 because it is visible then
                                            oFlags &= 3;
                                        }

                                        // (byte.PopCount((byte)(~oFlags & 0x7)) is "inverse popcount" - count the number of 0s in the byte
                                        // (~oFlags & 1) is 1 if the first bit is 0, 0 otherwise
                                        return (byte)((lightNibble.First * (~oFlags & 1) +
                                                       lightNibble.Second * ((~oFlags & 2) >> 1) +
                                                       lightNibble.Third * ((~oFlags & 4) >> 2) +
                                                       lightNibble.Fourth)
                                                      / (BitOperations.PopCount((byte)(~oFlags & 0x7)) + 1));
                                    }

                                    // split light and reassemble it again
                                    light.Whole |= (uint)((byte)(
                                        average(Unsafe.BitCast<uint, FourBytes>((l.Whole >> 4) & 0x0F0F0F0F),
                                            o)
                                        << 4 |
                                        average(Unsafe.BitCast<uint, FourBytes>(l.Whole & 0x0F0F0F0F),
                                            o)
                                    ) << j * 8);
                                }
                            }
                        }
                    }

                    tex.X = facesRef.min.u * 16f / Block.atlasSize;
                    tex.Y = facesRef.min.v * 16f / Block.atlasSize;
                    tex.Z = facesRef.max.u * 16f / Block.atlasSize;
                    tex.W = facesRef.max.v * 16f / Block.atlasSize;


                    // add vertices

                    tempVertices[0] = new BlockVertex(wx + facesRef.x1, wy + facesRef.y1, wz + facesRef.z1, tex.X, tex.Y,
                        Block.packData((byte)dir, (byte)(ao & 0x3), light.First));
                    tempVertices[1] = new BlockVertex(wx + facesRef.x2, wy + facesRef.y2, wz + facesRef.z2, tex.X, tex.W,
                        Block.packData((byte)dir, (byte)(ao >> 2 & 0x3), light.Second));
                    tempVertices[2] = new BlockVertex(wx + facesRef.x3, wy + facesRef.y3, wz + facesRef.z3, tex.Z, tex.W,
                        Block.packData((byte)dir, (byte)(ao >> 4 & 0x3), light.Third));
                    tempVertices[3] = new BlockVertex(wx + facesRef.x4, wy + facesRef.y4, wz + facesRef.z4, tex.Z, tex.Y,
                        Block.packData((byte)dir, (byte)(ao >> 6), light.Fourth));
                    chunkVertices.AddRange(tempVertices);
                    //cv += 4;

                    indices += indicesMask;
                    // write it back to the span
                    // how does this work?? vector is 8 bytes, span is only 6


                    // write the vector indices into tempIndices while ensuring it fits into 6 bytes
                    Unsafe.WriteUnaligned(
                        ref Unsafe.As<ushort, byte>(ref MemoryMarshal.GetReference(tempIndices)), indices);
                    chunkIndices.AddRange(tempIndices);

                    // add to the indices so they become 4
                    indices += complement;
                    //ci += 6;
                }
                increment2:
                facesRef = ref Unsafe.Add(ref facesRef, 1);
            }
            // increment the array pointer
            increment:
            blockArrayRef = ref Unsafe.Add(ref blockArrayRef, 1);
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
        x = (x + 16) % 16;
        y = (y + 16) % 16;
        z = (z + 16) % 16;
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
    public static byte calculateAOFixed(byte flags) {
        // side1 = 1
        // side2 = 2
        // corner = 4

        // if side1 and side2 are blocked, corner is blocked too
        // if side1 && side2
        if ((flags & 3) == 3) {
            return 3;
        }
        // return side1 + side2 + corner
        // which is conveniently already stored!
        return byte.PopCount(flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int toInt(bool b) {
        return Unsafe.As<bool, int>(ref b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte toByte(bool b) {
        return Unsafe.As<bool, byte>(ref b);
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

    public void Dispose() {
        vao?.Dispose();
        watervao?.Dispose();
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
    public FourBytes(uint whole) {
        Whole = whole;
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