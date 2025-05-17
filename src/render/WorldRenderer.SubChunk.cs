using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BlockGame.GL;
using BlockGame.ui;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;
using BoundingFrustum = System.Numerics.BoundingFrustum;

namespace BlockGame;

public partial class WorldRenderer {
    // we need it here because completely full chunks are also empty of any rendering
    private Dictionary<SubChunkCoord, bool> hasRenderOpaque = new();
    private Dictionary<SubChunkCoord, bool> hasRenderTranslucent = new();

    private Dictionary<SubChunkCoord, SharedBlockVAO?> vao = new();
    private Dictionary<SubChunkCoord, SharedBlockVAO?> watervao = new();

    private static int uChunkPos;
    private static int dummyuChunkPos;
    private static int wateruChunkPos;

    private static readonly Func<int, bool> AOtest = bl => bl != -1 && Block.isSolid(bl);

    // we cheated GC! there is only one list preallocated
    // we need 16x16x16 blocks, each block has max. 24 vertices
    // for indices we need the full 36

    // actually we don't need a list, regular arrays will do because it's only a few megs of space and it's shared
    // in the future when we want multithreaded meshing, we can just allocate like 4-8 of them and it will still be in the ballpark of 10MB
    private static readonly List<BlockVertexPacked> chunkVertices = new(2048);

    // YZX again
    private static readonly ushort[] neighbours = new ushort[Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX];

    private static readonly byte[]
        neighbourLights = new byte[Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX];

    private static readonly ArrayBlockData?[] neighbourSections = new ArrayBlockData?[27];

    private static Stopwatch sw = new Stopwatch();

    /// Given a reasonably modern .NET runtime, this weird construct is optimised to the array being emitted to the data section of the binary.
    /// This is very good for us because this shouldn't ever change (unless the game goes 4D? lol) and it's a fair amount faster to access
    /// because no field access needed at all.
    /// See https://github.com/dotnet/roslyn/pull/61414 for more.
    public static ReadOnlySpan<sbyte> offsetTable => [
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

    public static ReadOnlySpan<short> offsetTableCompact => [
        // west
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        -1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        -1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        -1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        -1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,

        // east
        1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 - 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 - 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,

        // south
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 0 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        -1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 0 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        -1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 0 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 0 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,

        // north
        1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 0 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 0 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 0 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 0 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,

        // down
        0 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        0 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        0 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        -1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        0 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        -1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,

        // up
        0 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        -1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        0 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        -1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        0 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        0 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX,
        1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
    ];

    private static bool opaqueBlocks(int b) {
        return b != 0 && Block.get(b).layer != RenderLayer.TRANSLUCENT;
    }

    private static bool notOpaqueBlocks(int b) {
        return b == 0 || Block.get(b).layer == RenderLayer.TRANSLUCENT;
    }


    /// <summary>
    /// TODO store the number of blocks in the chunksection and only allocate the vertex list up to that length
    /// </summary>
    public void meshChunk(SubChunk subChunk) {
        //sw.Restart();
        vao.GetValueOrDefault(subChunk.coord)?.Dispose();
        vao[subChunk.coord] = new SharedBlockVAO(chunkVAO);
        watervao.GetValueOrDefault(subChunk.coord)?.Dispose();
        watervao[subChunk.coord] = new SharedBlockVAO(chunkVAO);

        var currentVAO = vao[subChunk.coord];
        var currentWaterVAO = watervao[subChunk.coord];

        hasRenderOpaque[subChunk.coord] = false;
        hasRenderTranslucent[subChunk.coord] = false;

        // if the section is empty, nothing to do
        // if is empty, just return, don't need to get neighbours
        if (subChunk.isEmpty) {
            return;
        }

        //Console.Out.WriteLine($"PartMeshing0.5: {sw.Elapsed.TotalMicroseconds}us");
        // first we render everything which is NOT translucent
        //lock (meshingLock) {
        setupNeighbours(subChunk);

        // if chunk is full, don't mesh either
        // status update: this is actually bullshit and causes rendering bugs with *weird* worlds such as "all stone until building height". So this won't work anymore
        //if (subChunk.hasOnlySolid) {
        //return;
        //}

        /*if (World.glob) {
                MeasureProfiler.StartCollectingData();
            }*/
        //Console.Out.WriteLine($"PartMeshing0.7: {sw.Elapsed.TotalMicroseconds}us");
        constructVertices(subChunk, VertexConstructionMode.OPAQUE);
        /*if (World.glob) {
                MeasureProfiler.SaveData();
            }*/
        //Console.Out.WriteLine($"PartMeshing1: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
        if (chunkVertices.Count > 0) {
            hasRenderOpaque[subChunk.coord] = true;
            currentVAO.bindVAO();
            var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
            currentVAO.upload(finalVertices, (uint)finalVertices.Length);
            //Console.Out.WriteLine($"PartMeshing1.2: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
        }
        else {
            hasRenderOpaque[subChunk.coord] = false;
        }

        //}
        //lock (meshingLock) {
        if (subChunk.blocks.hasTranslucentBlocks()) {
            // then we render everything which is translucent (water for now)
            constructVertices(subChunk, VertexConstructionMode.TRANSLUCENT);
            //Console.Out.WriteLine($"PartMeshing1.4: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
            if (chunkVertices.Count > 0) {
                hasRenderTranslucent[subChunk.coord] = true;
                currentWaterVAO.bindVAO();

                var tFinalVertices = CollectionsMarshal.AsSpan(chunkVertices);
                currentWaterVAO.upload(tFinalVertices, (uint)tFinalVertices.Length);
                //Console.Out.WriteLine($"PartMeshing1.7: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
                //world.sortedTransparentChunks.Add(this);
            }
            else {
                //world.sortedTransparentChunks.Remove(this);
                hasRenderTranslucent[subChunk.coord] = false;
            }
        }
        //}
        //Console.Out.WriteLine($"Meshing: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
        //if (!subChunk.isEmpty && !hasRenderOpaque && !hasRenderTranslucent) {
        //    Console.Out.WriteLine($"CHUNKDATA: {subChunk.Block.blockCount} {subChunk.Block.isFull()}");
        //}
        //sw.Stop();
    }


    public bool isVisible(SubChunk subChunk, BoundingFrustum frustum) {
        return !frustum.outsideCameraUpDown(subChunk.box);
    }

    private void setUniformPos(SubChunkCoord coord, Shader s, Vector3D cameraPos) {
        s.setUniformBound(uChunkPos, (float)(coord.x * 16 - cameraPos.X), (float)(coord.y * 16 - cameraPos.Y),
            (float)(coord.z * 16 - cameraPos.Z));
    }

    private void setUniformPosWater(SubChunkCoord coord, Shader s, Vector3D cameraPos) {
        s.setUniformBound(wateruChunkPos, (float)(coord.x * 16 - cameraPos.X), (float)(coord.y * 16 - cameraPos.Y),
            (float)(coord.z * 16 - cameraPos.Z));
    }

    private void setUniformPosDummy(SubChunkCoord coord, Shader s, Vector3D cameraPos) {
        s.setUniformBound(dummyuChunkPos, (float)(coord.x * 16 - cameraPos.X), (float)(coord.y * 16 - cameraPos.Y),
            (float)(coord.z * 16 - cameraPos.Z));
    }

    public void drawOpaque(SubChunk subChunk, Vector3D cameraPos) {
        var coord = subChunk.coord;
        var vao = this.vao[coord];
        if (hasRenderOpaque[coord]) {
            vao.bind();
            //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            setUniformPos(coord, shader, cameraPos);
            uint renderedVerts = vao.render();
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedSubChunks += 1;
        }
    }

    public void drawTransparent(SubChunk subChunk, Vector3D cameraPos) {
        var coord = subChunk.coord;
        var watervao = this.watervao[coord];
        if (hasRenderTranslucent[coord]) {
            watervao.bind();
            setUniformPosWater(coord, waterShader, cameraPos);
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public void drawTransparentDummy(SubChunk subChunk, Vector3D cameraPos) {
        var coord = subChunk.coord;
        var watervao = this.watervao[coord];
        if (hasRenderTranslucent[coord]) {
            watervao.bind();
            setUniformPosDummy(coord, dummyShader, cameraPos);
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    [SkipLocalsInit]
    private void setupNeighbours(SubChunk subChunk) {
        //var sw = new Stopwatch();
        //sw.Start();

        //Console.Out.WriteLine($"vert1: {sw.Elapsed.TotalMicroseconds}us");

        // cache blocks
        // we need a 18x18 area
        // we load the 16x16 from the section itself then get the world for the rest
        // if the chunk section is an EmptyBlockData, don't bother
        // it will always be ArrayBlockData so we can access directly without those pesky BOUNDS CHECKS
        ref ushort sourceBlockArrayRef = ref MemoryMarshal.GetArrayDataReference(subChunk.blocks.blocks);
        ref byte sourceLightArrayRef = ref MemoryMarshal.GetArrayDataReference(subChunk.blocks.light);
        ref ushort blocksArrayRef = ref MemoryMarshal.GetArrayDataReference(neighbours);
        ref byte lightArrayRef = ref MemoryMarshal.GetArrayDataReference(neighbourLights);
        var world = this.world;
        int y;
        int z;
        int x;

        // setup neighbouring sections
        var coord = subChunk.coord;
        ref var neighbourSectionsArray = ref MemoryMarshal.GetArrayDataReference(neighbourSections);
        for (y = -1; y <= 1; y++) {
            for (z = -1; z <= 1; z++) {
                for (x = -1; x <= 1; x++) {
                    var sec = world.getChunkSectionUnsafe(new SubChunkCoord(coord.x + x, coord.y + y, coord.z + z));
                    neighbourSectionsArray = sec?.blocks;
                    neighbourSectionsArray = ref Unsafe.Add(ref neighbourSectionsArray, 1)!;
                }
            }
        }

        // reset counters
        neighbourSectionsArray = ref MemoryMarshal.GetArrayDataReference(neighbourSections);

        for (y = -1; y < Chunk.CHUNKSIZE + 1; y++) {
            for (z = -1; z < Chunk.CHUNKSIZE + 1; z++) {
                for (x = -1; x < Chunk.CHUNKSIZE + 1; x++) {
                    //Console.Out.WriteLine($"{i} {x} {y} {z}");

                    // if inside the chunk, load from section
                    if (x is >= 0 and < Chunk.CHUNKSIZE &&
                        z is >= 0 and < Chunk.CHUNKSIZE &&
                        y is >= 0 and < Chunk.CHUNKSIZE) {
                        blocksArrayRef = sourceBlockArrayRef;
                        lightArrayRef = sourceLightArrayRef;

                        // increment
                        sourceBlockArrayRef = ref Unsafe.Add(ref sourceBlockArrayRef, 1);
                        sourceLightArrayRef = ref Unsafe.Add(ref sourceLightArrayRef, 1);

                        goto increment;
                    }

                    // index for array accesses
                    //var index = (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1);

                    // aligned position (between 0 and 16)
                    // yes this shouldn't work but it does
                    // it makes EVERYTHING positive by cutting off the sign bit
                    // so -1 becomes 15, -2 becomes 14, etc.
                    int offset = (y & 0xF) * Chunk.CHUNKSIZESQ + (z & 0xF) * Chunk.CHUNKSIZE + (x & 0xF);

                    // section position (can be -1, 0, 1)
                    // get neighbouring section
                    var neighbourSection =
                        Unsafe.Add(ref neighbourSectionsArray, ((y >> 4) + 1) * 9 + ((z >> 4) + 1) * 3 + (x >> 4) + 1);
                    var nn = neighbourSection != null && neighbourSection.inited;
                    var bl = nn
                        ? Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(neighbourSection!.blocks),
                            offset)
                        : (ushort)0;

                    // if below world, pretend it's dirt (so it won't get meshed)
                    if (subChunk.coord.y == 0 && y == -1) {
                        bl = Block.DIRT.id;
                    }

                    // set neighbours array element to block
                    blocksArrayRef = bl;

                    // set light array element to light
                    lightArrayRef = nn
                        ? Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(neighbourSection!.light),
                            offset)
                        : (byte)15;

                    // increment
                    increment:
                    blocksArrayRef = ref Unsafe.Add(ref blocksArrayRef, 1);
                    lightArrayRef = ref Unsafe.Add(ref lightArrayRef, 1);
                }
            }
        }

        // if fullbright, just overwrite all lights to 15
        if (Game.graphics.fullbright) {
            neighbourLights.AsSpan().Fill(15);
        }

        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");
    }

    // sorry for this mess
    [SkipLocalsInit]
    //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private unsafe void constructVertices(SubChunk subChunk, VertexConstructionMode mode) {
        //sw.Start();
        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");

        // clear arrays before starting
        chunkVertices.Clear();

        Span<BlockVertexPacked> tempVertices = stackalloc BlockVertexPacked[4];

        Span<ushort> nba = stackalloc ushort[6];
        Span<byte> lba = stackalloc byte[6];


        // BYTE OF SETTINGS
        // 1 = AO
        // 2 = smooth lighting
        var smoothLighting = Settings.instance.smoothLighting;
        var AO = Settings.instance.AO;
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

        ref ushort blockArrayRef = ref MemoryMarshal.GetArrayDataReference(subChunk.blocks.blocks);
        ref short offsetArray = ref MemoryMarshal.GetReference(offsetTableCompact);

        bool test2;
        for (int idx = 0; idx < Chunk.MAXINDEX; idx++) {
            // index for array accesses
            int x = idx & 0xF;
            int z = idx >> 4 & 0xF;
            int y = idx >> 8;

            var index = (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1);
            // pre-add index
            ref ushort neighbourRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(neighbours), index);
            ref byte lightRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(neighbourLights), index);


            switch (mode) {
                case VertexConstructionMode.OPAQUE:
                    if (notOpaqueBlocks(blockArrayRef)) {
                        goto increment;
                    }

                    break;
                case VertexConstructionMode.TRANSLUCENT:
                    if (Block.notTranslucent(blockArrayRef)) {
                        goto increment;
                    }

                    break;
            }

            // unrolled world.toWorldPos
            //float wx = section.chunkX * Chunk.CHUNKSIZE + x;
            //float wy = section.chunkY * Chunk.CHUNKSIZE + y;
            //float wz = section.chunkZ * Chunk.CHUNKSIZE + z;

            var bl = Block.get(blockArrayRef);

            /*switch (Block.renderType[bl.id]) {
                case RenderType.CUBE:
                    // get UVs from block
                    break;
                case RenderType.CROSS:
                    break;
                case RenderType.MODEL:
                    goto model;
                    break;
                case RenderType.CUSTOM:
                    bl.render(world, new Vector3I(x, y, z), chunkVertices);
                    goto increment;
                default:
                    throw new ArgumentOutOfRangeException();
            }*/

            model:;

            // calculate texcoords
            Vector128<float> tex;


            // calculate AO for all 8 vertices
            // this is garbage but we'll deal with it later

            // one AO value fits on 2 bits so the whole thing fits in a byte
            FourBytes ao;
            ao.Whole = 0;
            Unsafe.SkipInit(out ao.First);
            Unsafe.SkipInit(out ao.Second);
            Unsafe.SkipInit(out ao.Third);
            Unsafe.SkipInit(out ao.Fourth);

            // setup neighbour data
            // if all 6 neighbours are solid, we don't even need to bother iterating the faces
            nba[0] = Unsafe.Add(ref neighbourRef, -1);
            nba[1] = Unsafe.Add(ref neighbourRef, +1);
            nba[2] = Unsafe.Add(ref neighbourRef, -Chunk.CHUNKSIZEEX);
            nba[3] = Unsafe.Add(ref neighbourRef, +Chunk.CHUNKSIZEEX);
            nba[4] = Unsafe.Add(ref neighbourRef, -Chunk.CHUNKSIZEEXSQ);
            nba[5] = Unsafe.Add(ref neighbourRef, +Chunk.CHUNKSIZEEXSQ);
            if (Block.isFullBlock(nba[0]) &&
                Block.isFullBlock(nba[1]) &&
                Block.isFullBlock(nba[2]) &&
                Block.isFullBlock(nba[3]) &&
                Block.isFullBlock(nba[4]) &&
                Block.isFullBlock(nba[5])) {
                goto increment;
            }

            FourBytes light;

            Unsafe.SkipInit(out light.Whole);
            Unsafe.SkipInit(out light.First);
            Unsafe.SkipInit(out light.Second);
            Unsafe.SkipInit(out light.Third);
            Unsafe.SkipInit(out light.Fourth);

            // get light data too
            lba[0] = Unsafe.Add(ref lightRef, -1);
            lba[1] = Unsafe.Add(ref lightRef, +1);
            lba[2] = Unsafe.Add(ref lightRef, -Chunk.CHUNKSIZEEX);
            lba[3] = Unsafe.Add(ref lightRef, +Chunk.CHUNKSIZEEX);
            lba[4] = Unsafe.Add(ref lightRef, -Chunk.CHUNKSIZEEXSQ);
            lba[5] = Unsafe.Add(ref lightRef, +Chunk.CHUNKSIZEEXSQ);


            ref Face facesRef = ref MemoryMarshal.GetArrayDataReference(bl.model.faces);

            for (int d = 0; d < bl.model.faces.Length; d++) {
                var dir = facesRef.direction;

                test2 = false;

                if (dir == RawDirection.NONE) {
                    // if it's not a diagonal face, don't even bother checking neighbour because we have to render it anyway
                    test2 = true;
                    light.First = lightRef;
                    light.Second = lightRef;
                    light.Third = lightRef;
                    light.Fourth = lightRef;
                }
                else {
                    ushort nb = nba[(byte)dir];
                    switch (mode) {
                        case VertexConstructionMode.OPAQUE:
                            test2 = Block.notSolid(nb) || !Block.isFullBlock(nb);
                            break;
                        case VertexConstructionMode.TRANSLUCENT:
                            test2 = !Block.isTranslucent(nb) && (Block.notSolid(nb) || !Block.isFullBlock(nb));
                            break;
                    }

                    test2 = test2 || (facesRef.nonFullFace && !Block.isTranslucent(nb));
                }

                // either neighbour test passes, or neighbour is not air + face is not full
                if (test2) {
                    // if face is none, skip the whole lighting business
                    if (dir == RawDirection.NONE) {
                        goto vertex;
                    }

                    if (!smoothLighting) {
                        light.First = lba[(byte)dir];
                        light.Second = lba[(byte)dir];
                        light.Third = lba[(byte)dir];
                        light.Fourth = lba[(byte)dir];
                    }
                    else {
                        light.Whole = 0;
                    }

                    // AO requires smooth lighting. Otherwise don't need to deal with sampling any of this
                    if (smoothLighting || AO) {
                        // ox, oy, oz
                        FourBytes o;
                        // need to store 9 sbytes so it's a 16-element vector
                        // lx, ly, lz, lo
                        // we need 12 bytes
                        Vector128<byte> l;

                        ao.Whole = 0;


                        //for (int j = 0; j < 4; j++) {
                        //mult = dirIdx * 36 + j * 9 + vert * 3;
                        // premultiply cuz its faster that way

                        // load the vector with the offsets
                        // we need 12 offsets
                        var offsets = Vector256.LoadUnsafe(ref Unsafe.Add(ref offsetArray, (int)dir * 12));
                        l = Vector128.Create(
                            Unsafe.Add(ref lightRef, offsets[0]),
                            Unsafe.Add(ref lightRef, offsets[1]),
                            Unsafe.Add(ref lightRef, offsets[2]),
                            lba[(byte)dir],
                            Unsafe.Add(ref lightRef, offsets[3]),
                            Unsafe.Add(ref lightRef, offsets[4]),
                            Unsafe.Add(ref lightRef, offsets[5]),
                            lba[(byte)dir],
                            Unsafe.Add(ref lightRef, offsets[6]),
                            Unsafe.Add(ref lightRef, offsets[7]),
                            Unsafe.Add(ref lightRef, offsets[8]),
                            lba[(byte)dir],
                            Unsafe.Add(ref lightRef, offsets[9]),
                            Unsafe.Add(ref lightRef, offsets[10]),
                            Unsafe.Add(ref lightRef, offsets[11]),
                            lba[(byte)dir]);

                        o.First = (byte)(Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                             Unsafe.Add(ref neighbourRef, offsets[0])]) |
                                         Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                             Unsafe.Add(ref neighbourRef, offsets[1])]) << 1 |
                                         Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                             Unsafe.Add(ref neighbourRef, offsets[2])]) << 2);

                        o.Second = (byte)(Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                              Unsafe.Add(ref neighbourRef, offsets[3])]) |
                                          Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                              Unsafe.Add(ref neighbourRef, offsets[4])]) << 1 |
                                          Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                              Unsafe.Add(ref neighbourRef, offsets[5])]) << 2);

                        o.Third = (byte)(Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                             Unsafe.Add(ref neighbourRef, offsets[6])]) |
                                         Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                             Unsafe.Add(ref neighbourRef, offsets[7])]) << 1 |
                                         Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                             Unsafe.Add(ref neighbourRef, offsets[8])]) << 2);

                        o.Fourth = (byte)(Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                              Unsafe.Add(ref neighbourRef, offsets[9])]) |
                                          Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                              Unsafe.Add(ref neighbourRef, offsets[10])]) << 1 |
                                          Unsafe.BitCast<bool, byte>(Block.fullBlock[
                                              Unsafe.Add(ref neighbourRef, offsets[11])]) << 2);

                        // only apply AO if enabled
                        if (AO && !facesRef.noAO) {
                            ao.First = (byte)(o.First == 3 ? 3 : byte.PopCount(o.First));
                            ao.Second = (byte)((o.Second & 3) == 3 ? 3 : byte.PopCount(o.Second));
                            ao.Third = (byte)((o.Third & 3) == 3 ? 3 : byte.PopCount(o.Third));
                            ao.Fourth = (byte)((o.Fourth & 3) == 3 ? 3 : byte.PopCount(o.Fourth));
                        }

                        // if face is noAO, don't average....
                        if (smoothLighting) {
                            // if smooth lighting enabled, average light from neighbour face + the 3 other ones
                            // calculate average


                            var n = l.AsUInt32();
                            // split light and reassemble it again
                            light.First = average2(n[0], o.First);
                            light.Second = average2(n[1], o.Second);
                            light.Third = average2(n[2], o.Third);
                            light.Fourth = average2(n[3], o.Fourth);
                        }
                        //}
                    }

                    vertex:
                    /*tex.X = facesRef.min.u * 16f / Block.atlasSize;
                    tex.Y = facesRef.min.v * 16f / Block.atlasSize;
                    tex.Z = facesRef.max.u * 16f / Block.atlasSize;
                    tex.W = facesRef.max.v * 16f / Block.atlasSize;*/

                    tex = Vector128.Create(facesRef.min.u, facesRef.min.v, facesRef.max.u, facesRef.max.v);

                    // divide by texture size / atlas size, multiply by scaling factor
                    const float factor = Block.atlasRatio * 32768f;
                    tex = Vector128.Multiply(tex, factor);

                    Vector256<float> vec = Vector256.Create(
                        x + facesRef.x1,
                        y + facesRef.y1,
                        z + facesRef.z1,
                        x + facesRef.x2,
                        y + facesRef.y2,
                        z + facesRef.z2,
                        x + facesRef.x3,
                        y + facesRef.y3);

                    vec = Vector256.Add(vec, Vector256.Create(16f));
                    vec = Vector256.Multiply(vec, 256);

                    Vector128<float> vec2 = Vector128.Create(
                        z + facesRef.z3,
                        x + facesRef.x4,
                        y + facesRef.y4,
                        z + facesRef.z4);

                    vec2 = Vector128.Add(vec2, Vector128.Create(16f));
                    vec2 = Vector128.Multiply(vec2, 256);

                    // add vertices
                    ref var vertex = ref tempVertices[0];
                    vertex.x = (ushort)vec[0];
                    vertex.y = (ushort)vec[1];
                    vertex.z = (ushort)vec[2];
                    vertex.u = (ushort)tex[0];
                    vertex.v = (ushort)tex[1];
                    vertex.c = Block.packColour((byte)dir, ao.First, light.First);

                    vertex = ref tempVertices[1];
                    vertex.x = (ushort)vec[3];
                    vertex.y = (ushort)vec[4];
                    vertex.z = (ushort)vec[5];
                    vertex.u = (ushort)tex[0];
                    vertex.v = (ushort)tex[3];
                    vertex.c = Block.packColour((byte)dir, ao.Second, light.Second);

                    vertex = ref tempVertices[2];
                    vertex.x = (ushort)vec[6];
                    vertex.y = (ushort)vec[7];
                    vertex.z = (ushort)vec2[0];
                    vertex.u = (ushort)tex[2];
                    vertex.v = (ushort)tex[3];
                    vertex.c = Block.packColour((byte)dir, ao.Third, light.Third);

                    vertex = ref tempVertices[3];
                    vertex.x = (ushort)vec2[1];
                    vertex.y = (ushort)vec2[2];
                    vertex.z = (ushort)vec2[3];
                    vertex.u = (ushort)tex[2];
                    vertex.v = (ushort)tex[1];
                    vertex.c = Block.packColour((byte)dir, ao.Fourth, light.Fourth);
                    chunkVertices.AddRange(tempVertices);
                    //cv += 4;
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

    // this averages the four light values. If the block is opaque, it ignores the light value.
    // oFlags are opacity of side1, side2 and corner
    // (1 == opaque, 0 == transparent)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte average(FourBytes lightNibble, byte oFlags) {
        // if both sides are blocked, don't check the corner, won't be visible anyway
        // if corner == 0 && side1 and side2 aren't both true, then corner is visible
        //if ((oFlags & 4) == 0 && oFlags != 3) {
        if (oFlags < 3) {
            // set the 4 bit of oFlags to 0 because it is visible then
            oFlags &= 3;
        }

        // (byte.PopCount((byte)(~oFlags & 0x7)) is "inverse popcount" - count the number of 0s in the byte
        // (~oFlags & 1) is 1 if the first bit is 0, 0 otherwise
        var inv = ~oFlags;
        return (byte)((lightNibble.First * (inv & 1) +
                       lightNibble.Second * ((inv & 2) >> 1) +
                       lightNibble.Third * ((inv & 4) >> 2) +
                       lightNibble.Fourth)
                      / (BitOperations.PopCount((byte)(inv & 0x7)) + 1));
    }

    // this averages the four light values. If the block is opaque, it ignores the light value.
    // oFlags are opacity of side1, side2 and corner
    // (1 == opaque, 0 == transparent)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte average(uint lightNibble, byte oFlags) {
        // if both sides are blocked, don't check the corner, won't be visible anyway
        // if corner == 0 && side1 and side2 aren't both true, then corner is visible
        //if ((oFlags & 4) == 0 && oFlags != 3) {
        if (oFlags < 3) {
            // set the 4 bit of oFlags to 0 because it is visible then
            oFlags &= 3;
        }

        // (byte.PopCount((byte)(~oFlags & 0x7)) is "inverse popcount" - count the number of 0s in the byte
        // (~oFlags & 1) is 1 if the first bit is 0, 0 otherwise
        var inv = ~oFlags;
        return (byte)(((lightNibble & 0xFF) * (inv & 1) +
                          ((lightNibble >> 8) & 0xFF) * ((inv & 2) >> 1) +
                          ((lightNibble >> 16) & 0xFF) * ((inv & 4) >> 2) +
                          (lightNibble >> 24) & 0xFF)
                      / (BitOperations.PopCount((byte)(inv & 0x7)) + 1));
    }

    /// <summary>
    /// average but does blocklight and skylight at once
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte average2(uint lightNibble, byte oFlags) {
        if (oFlags < 3) {
            // set the 4 bit of oFlags to 0 because it is visible then
            oFlags &= 3;
        }

        // (byte.PopCount((byte)(~oFlags & 0x7)) is "inverse popcount" - count the number of 0s in the byte
        // (~oFlags & 1) is 1 if the first bit is 0, 0 otherwise
        var inv = ~oFlags;
        var popcnt = BitOperations.PopCount((byte)(inv & 0x7)) + 1;
        var sky = (byte)(((lightNibble & 0xF) * (inv & 1) +
                          (lightNibble >> 8 & 0xF) * ((inv & 2) >> 1) +
                          (lightNibble >> 16 & 0xF) * ((inv & 4) >> 2) +
                          (lightNibble >> 24 & 0xF))
                         / popcnt);

        var block = (byte)(((lightNibble >> 4 & 0xF) * (inv & 1) +
                            (lightNibble >> 12 & 0xF) * ((inv & 2) >> 1) +
                            (lightNibble >> 20 & 0xF) * ((inv & 4) >> 2) +
                            (lightNibble >> 28 & 0xF))
                           / popcnt);
        return (byte)(sky | block << 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void addAO(int x, int y, int z, int x1, int y1, int z1, out int x2, out int y2, out int z2) {
        x2 = x + x1;
        y2 = y + y1;
        z2 = z + z1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3I addAO(int x, int y, int z, int x1, int y1, int z1) {
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
        // return side1 + side2 + corner
        // which is conveniently already stored!
        return (flags & 3) == 3 ? (byte)3 : byte.PopCount(flags);
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
}

public enum VertexConstructionMode {
    OPAQUE,
    TRANSLUCENT
}

[StructLayout(LayoutKind.Explicit)]
public struct FourShorts {
    [FieldOffset(0)] public ulong Whole;
    [FieldOffset(0)] public ushort First;
    [FieldOffset(2)] public ushort Second;
    [FieldOffset(4)] public ushort Third;
    [FieldOffset(6)] public ushort Fourth;
}

[StructLayout(LayoutKind.Explicit)]
public struct FourBytes {
    [FieldOffset(0)] public uint Whole;
    [FieldOffset(0)] public byte First;
    [FieldOffset(1)] public byte Second;
    [FieldOffset(2)] public byte Third;
    [FieldOffset(3)] public byte Fourth;

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
    [FieldOffset(0)] public int Whole;
    [FieldOffset(0)] public sbyte First;
    [FieldOffset(1)] public sbyte Second;
    [FieldOffset(2)] public sbyte Third;
    [FieldOffset(3)] public sbyte Fourth;
}

[StructLayout(LayoutKind.Explicit)]
public struct TwoFloats {
    [FieldOffset(0)] public ulong Whole;
    [FieldOffset(0)] public float First;
    [FieldOffset(4)] public float Second;
}