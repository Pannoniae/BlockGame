using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.ui;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;
using BoundingFrustum = System.Numerics.BoundingFrustum;
using Debug = System.Diagnostics.Debug;

namespace BlockGame;

public partial class WorldRenderer {
    private static readonly Func<int, bool> AOtest = bl => bl != -1 && Block.isSolid(bl);

    // we cheated GC! there is only one list preallocated
    // we need 16x16x16 blocks, each block has max. 24 vertices
    // for indices we need the full 36

    // actually we don't need a list, regular arrays will do because it's only a few megs of space and it's shared
    // in the future when we want multithreaded meshing, we can just allocate like 4-8 of them and it will still be in the ballpark of 10MB
    private static readonly List<BlockVertexPacked> chunkVertices = new(2048);

    // YZX again
    private static readonly uint[] neighbours =
        GC.AllocateUninitializedArray<uint>(Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX);

    private static readonly byte[]
        neighbourLights =
            GC.AllocateUninitializedArray<byte>(Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX);

    // 3x3x3 local cache for smooth lighting optimization
    public const int LOCALCACHESIZE = 3;
    public const int LOCALCACHESIZE_SQ = 9;
    public const int LOCALCACHESIZE_CUBE = 27;
    
    private static readonly uint[] localBlockCache = new uint[LOCALCACHESIZE_CUBE];
    private static readonly byte[] localLightCache = new byte[LOCALCACHESIZE_CUBE];

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

    public static ReadOnlySpan<short> lightOffsets => [-1, +1, -18, +18, -324, +324];

    private static bool opaqueBlocks(uint b) {
        return b != 0 && Block.get(b.getID()).layer != RenderLayer.TRANSLUCENT;
    }

    private static bool notOpaqueBlocks(uint b) {
        return b == 0 || Block.get(b.getID()).layer == RenderLayer.TRANSLUCENT;
    }


    /// <summary>
    /// TODO store the number of blocks in the chunksection and only allocate the vertex list up to that length
    /// </summary>
    public void meshChunk(SubChunk subChunk) {
        //sw.Restart();
        subChunk.vao?.Dispose();
        subChunk.vao = new SharedBlockVAO(chunkVAO);
        subChunk.watervao?.Dispose();
        subChunk.watervao = new SharedBlockVAO(chunkVAO);

        var currentVAO = subChunk.vao;
        var currentWaterVAO = subChunk.watervao;

        subChunk.hasRenderOpaque = false;
        subChunk.hasRenderTranslucent = false;

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
            subChunk.hasRenderOpaque = true;
            currentVAO.bindVAO();
            var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
            currentVAO.upload(finalVertices, (uint)finalVertices.Length);
            //Console.Out.WriteLine($"PartMeshing1.2: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
        }
        else {
            subChunk.hasRenderOpaque = false;
        }

        //}
        //lock (meshingLock) {
        if (subChunk.blocks.hasTranslucentBlocks()) {
            // then we render everything which is translucent (water for now)
            constructVertices(subChunk, VertexConstructionMode.TRANSLUCENT);
            //Console.Out.WriteLine($"PartMeshing1.4: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
            if (chunkVertices.Count > 0) {
                subChunk.hasRenderTranslucent = true;
                currentWaterVAO.bindVAO();

                var tFinalVertices = CollectionsMarshal.AsSpan(chunkVertices);
                currentWaterVAO.upload(tFinalVertices, (uint)tFinalVertices.Length);
                //Console.Out.WriteLine($"PartMeshing1.7: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
                //world.sortedTransparentChunks.Add(this);
            }
            else {
                //world.sortedTransparentChunks.Remove(this);
                subChunk.hasRenderTranslucent = false;
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

    /// <summary>
    /// Batched visibility check for 8 subchunks at once. Updates the isRendered field for each subchunk based on visibility.
    /// </summary>
    public static unsafe void isVisibleEight(SubChunk[] subChunks, BoundingFrustum frustum) {
        // Extract AABBs from subchunks
        Span<AABB> aabbs = stackalloc AABB[8];
        for (int i = 0; i < 8; i++) {
            aabbs[i] = subChunks[i].box;
        }

        // Get visibility mask (1 bit = outside/not visible, 0 bit = visible)
        byte outsideMask = frustum.outsideCameraUpDownEight(aabbs);

        // Update isRendered for each subchunk
        for (int i = 0; i < 8; i++) {
            subChunks[i].isRendered = (outsideMask & (1 << i)) == 0;
        }
    }

    /*private void setUniformPosUBO(SubChunkCoord coord, Vector3D cameraPos) {
        var chunkUniforms = new ChunkUniforms(
            new Vector3(
                (float)(coord.x * 16 - cameraPos.X),
                (float)(coord.y * 16 - cameraPos.Y),
                (float)(coord.z * 16 - cameraPos.Z)
            )
        );

        chunkUBO.updateData(in chunkUniforms);
        chunkUBO.upload();
    }*/

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
        var vao = subChunk.vao;
        if (subChunk.hasRenderOpaque) {
            vao.bind();
            //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            setUniformPos(coord, worldShader, cameraPos);
            uint renderedVerts = vao.render();
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedSubChunks += 1;
        }
    }

    public void drawTransparent(SubChunk subChunk, Vector3D cameraPos) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent) {
            watervao.bind();
            setUniformPosWater(coord, waterShader, cameraPos);
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public void drawTransparentDummy(SubChunk subChunk, Vector3D cameraPos) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent) {
            watervao.bind();
            setUniformPosDummy(coord, dummyShader, cameraPos);
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public void drawOpaqueUBO(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var vao = subChunk.vao;
        if (subChunk.hasRenderOpaque) {
            vao.bind();

            uint renderedVerts = vao.renderBaseInstance(idx);
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedSubChunks += 1;
        }
    }

    public void drawTransparentUBO(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent) {
            watervao.bind();

            uint renderedTransparentVerts = watervao.renderBaseInstance(idx);
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    public void drawOpaqueCMDL(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var vao = subChunk.vao;
        if (subChunk.hasRenderOpaque) {
            vao.addCMDLCommand();

            uint renderedVerts = vao.renderCMDL(idx);
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedSubChunks += 1;
        }
    }

    public void drawTransparentCMDL(SubChunk subChunk, uint idx) {
        var coord = subChunk.coord;
        var watervao = subChunk.watervao;
        if (subChunk.hasRenderTranslucent) {
            watervao.addCMDLCommand();

            uint renderedTransparentVerts = watervao.renderCMDL(idx);
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    /// <summary>
    /// Add opaque chunks to the bindless indirect buffer for batch rendering
    /// </summary>
    public void addOpaqueToBindlessBuffer(SubChunk subChunk, uint instanceId) {
        if (subChunk.hasRenderOpaque) {
            subChunk.vao.addChunkCommand(bindlessBuffer, instanceId, elementAddress, elementLen);
        }
    }

    /// <summary>
    /// Add transparent chunks to the bindless indirect buffer for batch rendering
    /// </summary>
    public void addTransparentToBindlessBuffer(SubChunk subChunk, uint instanceId) {
        if (subChunk.hasRenderTranslucent) {
            subChunk.watervao.addChunkCommand(bindlessBuffer, instanceId, elementAddress, elementLen);
        }
    }

    /// <summary>
    /// Populate 3x3x3 local cache around a specific block position for smooth lighting optimization.
    /// This reduces redundant memory access when sampling neighbors for lighting calculations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void populateLocalCache3x3x3(int blockX, int blockY, int blockZ) {
        // Convert to 18x18x18 cache coordinates (add 1 for border)
        int baseX = blockX + 1;
        int baseY = blockY + 1; 
        int baseZ = blockZ + 1;
        
        ref uint neighboursRef = ref MemoryMarshal.GetArrayDataReference(neighbours);
        ref byte lightsRef = ref MemoryMarshal.GetArrayDataReference(neighbourLights);
        
        // Copy 3x3x3 area from 18x18x18 cache to local 3x3x3 cache
        for (int dy = -1; dy <= 1; dy++) {
            for (int dz = -1; dz <= 1; dz++) {
                for (int dx = -1; dx <= 1; dx++) {
                    // Source index in 18x18x18 cache
                    int sourceIdx = (baseY + dy) * Chunk.CHUNKSIZEEXSQ + (baseZ + dz) * Chunk.CHUNKSIZEEX + (baseX + dx);
                    // Destination index in 3x3x3 cache
                    int destIdx = (dy + 1) * LOCALCACHESIZE_SQ + (dz + 1) * LOCALCACHESIZE + (dx + 1);
                    
                    localBlockCache[destIdx] = Unsafe.Add(ref neighboursRef, sourceIdx);
                    localLightCache[destIdx] = Unsafe.Add(ref lightsRef, sourceIdx);
                }
            }
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
        var blocks = subChunk.blocks;
        ref uint sourceBlockArrayRef = ref MemoryMarshal.GetArrayDataReference(blocks.blocks);
        ref byte sourceLightArrayRef = ref MemoryMarshal.GetArrayDataReference(blocks.light);
        ref uint blocksArrayRef = ref MemoryMarshal.GetArrayDataReference(neighbours);
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
                    var sec = world.getSubChunkUnsafe(new SubChunkCoord(coord.x + x, coord.y + y, coord.z + z));
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
                    var nn = neighbourSection?.inited == true;
                    var bl = nn
                        ? Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(neighbourSection!.blocks),
                            offset)
                        : (ushort)0;


                    // if below world, pretend it's dirt (so it won't get meshed)
                    if (subChunk.coord.y == 0 && y == -1) {
                        bl = Blocks.DIRT;
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
        Span<uint> nba = stackalloc uint[6];

        Span<uint> blockCache = stackalloc uint[27];
        Span<byte> lightCache = stackalloc byte[27];

        // this is correct!
        ReadOnlySpan<int> normalOrder = [0, 1, 2, 3];
        ReadOnlySpan<int> flippedOrder = [3, 0, 1, 2];


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

        bool test2;
        for (int idx = 0; idx < Chunk.MAXINDEX; idx++) {
            
            // index for array accesses
            int x = idx & 0xF;
            int z = idx >> 4 & 0xF;
            int y = idx >> 8;

            var index = (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1);
            // pre-add index
            ref uint neighbourRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(neighbours), index);
            ref byte lightRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(neighbourLights), index);
            
            switch (mode) {
                case VertexConstructionMode.OPAQUE:
                    if (notOpaqueBlocks(neighbourRef)) {
                        goto increment;
                    }

                    break;
                case VertexConstructionMode.TRANSLUCENT:
                    if (Block.notTranslucent(neighbourRef.getID())) {
                        goto increment;
                    }

                    break;
            }

            // unrolled world.toWorldPos
            //float wx = section.chunkX * Chunk.CHUNKSIZE + x;
            //float wy = section.chunkY * Chunk.CHUNKSIZE + y;
            //float wz = section.chunkZ * Chunk.CHUNKSIZE + z;

            var bl = Block.get(neighbourRef.getID());

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

            model: ;

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
            if (Block.fullBlock[nba[0].getID()] &&
                Block.fullBlock[nba[1].getID()] &&
                Block.fullBlock[nba[2].getID()] &&
                Block.fullBlock[nba[3].getID()] &&
                Block.fullBlock[nba[4].getID()] &&
                Block.fullBlock[nba[5].getID()]) {
                goto increment;
            }

            FourBytes light;

            Unsafe.SkipInit(out light.Whole);
            Unsafe.SkipInit(out light.First);
            Unsafe.SkipInit(out light.Second);
            Unsafe.SkipInit(out light.Third);
            Unsafe.SkipInit(out light.Fourth);

            // get light data too


            ref Face facesRef = ref MemoryMarshal.GetArrayDataReference(bl.model.faces);
            
            // if smooth lighting, fill cache
            if (smoothLighting) {
                fillCache(blockCache, lightCache, ref neighbourRef, ref lightRef);
            }

            for (int d = 0; d < bl.model.faces.Length; d++) {
                var dir = facesRef.direction;


                // if dir = 0, add -1
                // if dir = 1, add +1
                // if dir = 2, add -Chunk.CHUNKSIZEEX
                // if dir = 3, add +Chunk.CHUNKSIZEEX
                // if dir = 4, add -Chunk.CHUNKSIZEEXSQ
                // if dir = 5, add +Chunk.CHUNKSIZEEXSQ
                byte lb;

                test2 = false;

                if (dir == RawDirection.NONE) {
                    // if it's not a diagonal face, don't even bother checking neighbour because we have to render it anyway
                    lb = lightRef;
                    test2 = true;
                    light.First = lightRef;
                    light.Second = lightRef;
                    light.Third = lightRef;
                    light.Fourth = lightRef;
                }
                else {
                    lb = Unsafe.Add(ref lightRef, lightOffsets[(byte)dir]);
                    uint nb = nba[(byte)dir];
                    switch (mode) {
                        case VertexConstructionMode.OPAQUE:
                            test2 = Block.notSolid(nb.getID()) || !Block.isFullBlock(nb.getID());
                            break;
                        case VertexConstructionMode.TRANSLUCENT:
                            test2 = !Block.isTranslucent(nb.getID()) &&
                                    (Block.notSolid(nb.getID()) || !Block.isFullBlock(nb.getID()));
                            break;
                    }

                    test2 = test2 || (facesRef.nonFullFace && !Block.isTranslucent(nb.getID()));
                }


                // either neighbour test passes, or neighbour is not air + face is not full
                if (test2) {
                    // if face is none, skip the whole lighting business
                    if (dir == RawDirection.NONE) {
                        goto vertex;
                    }

                    if (!smoothLighting) {
                        light.First = lb;
                        light.Second = lb;
                        light.Third = lb;
                        light.Fourth = lb;
                    }
                    else {
                        light.Whole = 0;
                    }

                    // AO requires smooth lighting. Otherwise don't need to deal with sampling any of this
                    if (smoothLighting || AO) {
                        
                        // ox, oy, oz

                        ao.Whole = 0;


                        //for (int j = 0; j < 4; j++) {
                        //mult = dirIdx * 36 + j * 9 + vert * 3;
                        // premultiply cuz its faster that way

                        getDirectionOffsetsAndData(dir, ref MemoryMarshal.GetReference(blockCache), ref MemoryMarshal.GetReference(lightCache), lb, out light, out FourBytes o);

                        // only apply AO if enabled
                        if (AO && !facesRef.noAO) {
                            ao.First = (byte)(o.First == 3 ? 3 : byte.PopCount(o.First));
                            ao.Second = (byte)((o.Second & 3) == 3 ? 3 : byte.PopCount(o.Second));
                            ao.Third = (byte)((o.Third & 3) == 3 ? 3 : byte.PopCount(o.Third));
                            ao.Fourth = (byte)((o.Fourth & 3) == 3 ? 3 : byte.PopCount(o.Fourth));
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

                    // determine vertex order to prevent cracks (combine AO and lighting)
                    // extract skylight values and invert them (15-light = darkness)
                    var dark1 = 15 - (light.First & 0xF);
                    var dark2 = 15 - (light.Second & 0xF);
                    var dark3 = 15 - (light.Third & 0xF);
                    var dark4 = 15 - (light.Fourth & 0xF);

                    ReadOnlySpan<int> order = (ao.First + dark1 + ao.Third + dark3 >
                                               ao.Second + dark2 + ao.Fourth + dark4)
                        ? flippedOrder
                        : normalOrder;

                    // add vertices
                    ref var vertex = ref tempVertices[order[0]];
                    vertex.x = (ushort)vec[0];
                    vertex.y = (ushort)vec[1];
                    vertex.z = (ushort)vec[2];
                    vertex.u = (ushort)tex[0];
                    vertex.v = (ushort)tex[1];
                    vertex.cu = Block.packColourB((byte)dir, ao.First);
                    vertex.light = light.First;

                    vertex = ref tempVertices[order[1]];
                    vertex.x = (ushort)vec[3];
                    vertex.y = (ushort)vec[4];
                    vertex.z = (ushort)vec[5];
                    vertex.u = (ushort)tex[0];
                    vertex.v = (ushort)tex[3];
                    vertex.cu = Block.packColourB((byte)dir, ao.Second);
                    vertex.light = light.Second;


                    vertex = ref tempVertices[order[2]];
                    vertex.x = (ushort)vec[6];
                    vertex.y = (ushort)vec[7];
                    vertex.z = (ushort)vec2[0];
                    vertex.u = (ushort)tex[2];
                    vertex.v = (ushort)tex[3];
                    vertex.cu = Block.packColourB((byte)dir, ao.Third);
                    vertex.light = light.Third;

                    vertex = ref tempVertices[order[3]];
                    vertex.x = (ushort)vec2[1];
                    vertex.y = (ushort)vec2[2];
                    vertex.z = (ushort)vec2[3];
                    vertex.u = (ushort)tex[2];
                    vertex.v = (ushort)tex[1];
                    vertex.cu = Block.packColourB((byte)dir, ao.Fourth);
                    vertex.light = light.Fourth;
                    chunkVertices.AddRange(tempVertices);
                    //cv += 4;
                    //ci += 6;
                }

                increment2:
                facesRef = ref Unsafe.Add(ref facesRef, 1);
            }

            // increment the array pointer
            increment: ;
        }
        //Console.Out.WriteLine($"vert4: {sw.Elapsed.TotalMicroseconds}us");
    }


    /**
     * Fills the 3x3x3 local cache with blocks and light values.
     */
    private static void fillCache(Span<uint> blockCache, Span<byte> lightCache, ref uint neighbourRef, ref byte lightRef) {
        // it used to look like this:
        // nba[0] = Unsafe.Add(ref neighbourRef, -1);
        // nba[1] = Unsafe.Add(ref neighbourRef, +1);
        // nba[2] = Unsafe.Add(ref neighbourRef, -Chunk.CHUNKSIZEEX);
        // nba[3] = Unsafe.Add(ref neighbourRef, +Chunk.CHUNKSIZEEX);
        // nba[4] = Unsafe.Add(ref neighbourRef, -Chunk.CHUNKSIZEEXSQ);
        // nba[5] = Unsafe.Add(ref neighbourRef, +Chunk.CHUNKSIZEEXSQ);
        
        // we need to fill the blocks into the cache.
        // indices are -1, 0, 1 for x, y, z
        for (int y = 0; y < LOCALCACHESIZE; y++) {
            for (int z = 0; z < LOCALCACHESIZE; z++) {
                for (int x = 0; x < LOCALCACHESIZE; x++) {
                    // calculate the index in the cache
                    int index = y * LOCALCACHESIZE_SQ + z * LOCALCACHESIZE + x;
                    // calculate the neighbour index
                    int nx = x - 1;
                    int ny = y - 1;
                    int nz = z - 1;

                    // get the block and light value from the neighbour array
                    blockCache[index] = Unsafe.Add(ref neighbourRef, ny * Chunk.CHUNKSIZEEXSQ + nz * Chunk.CHUNKSIZEEX + nx);
                    lightCache[index] = Unsafe.Add(ref lightRef,  ny * Chunk.CHUNKSIZEEXSQ + nz * Chunk.CHUNKSIZEEX + nx);
                }
            }
        }
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
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte average2(uint lightNibble, byte oFlags) {
        /*if (oFlags < 3) {
            // set the 4 bit of oFlags to 0 because it is visible then
            oFlags &= 3;
        }*/
        int mask = 3 | ~((oFlags - 3) >> 31);
        oFlags = (byte)(oFlags & mask);

        // (byte.PopCount((byte)(~oFlags & 0x7)) is "inverse popcount" - count the number of 0s in the byte
        // (~oFlags & 1) is 1 if the first bit is 0, 0 otherwise
        byte inv = (byte)(~oFlags & 0x7);
        var popcnt = BitOperations.PopCount(inv) + 1;
        Debug.Assert(popcnt > 0);
        Debug.Assert(popcnt <= 4);
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
    private static byte calculateVertexLightAndAO(ref byte lightRef, ref uint neighbourRef, int x0, int y0, int z0, int x1, int y1, int z1, int x2, int y2, int z2, byte lb, out byte opacity) {
        
        // since we're using a cache now, we're getting the offsets from the cache which is 3x3x3
        // so +1 is +3, -1 is -3, etc.
        
        // calculate the offsets in the local cache
        int offset0 =  (y0 + 1) * LOCALCACHESIZE_SQ + (z0 + 1) * LOCALCACHESIZE + (x0 + 1);
        int offset1 =  (y1 + 1) * LOCALCACHESIZE_SQ + (z1 + 1) * LOCALCACHESIZE + (x1 + 1);
        int offset2 =  (y2 + 1) * LOCALCACHESIZE_SQ + (z2 + 1) * LOCALCACHESIZE + (x2 + 1);
        
        uint lightValue = (uint)(Unsafe.Add(ref lightRef, offset0) |
                                 (Unsafe.Add(ref lightRef, offset1) << 8) |
                                 (Unsafe.Add(ref lightRef, offset2) << 16) | 
                                 lb << 24);
        
        opacity = (byte)(Unsafe.BitCast<bool, byte>(Block.fullBlock[Unsafe.Add(ref neighbourRef, offset0).getID()]) |
                        Unsafe.BitCast<bool, byte>(Block.fullBlock[Unsafe.Add(ref neighbourRef, offset1).getID()]) << 1 |
                        Unsafe.BitCast<bool, byte>(Block.fullBlock[Unsafe.Add(ref neighbourRef, offset2).getID()]) << 2);
        
        return average2(lightValue, opacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void getDirectionOffsetsAndData(RawDirection dir, ref uint neighbourRef, ref byte lightRef, byte lb, out FourBytes light, out FourBytes o) {
        Unsafe.SkipInit(out o);
        Unsafe.SkipInit(out light);
        switch (dir) {
            case RawDirection.WEST:
                light.First = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, -1, 0, 1, -1, 1, 0, -1, 1, 1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, -1, 0, 1, -1, -1, 0, -1, -1, 1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, -1, 0, -1, -1, -1, 0, -1, -1, -1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, -1, 0, -1, -1, 1, 0, -1, 1, -1, lb, out o.Fourth);
                break;
            case RawDirection.EAST:
                light.First = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 1, 0, -1, 1, 1, 0, 1, 1, -1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 1, 0, -1, 1, -1, 0, 1, -1, -1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 1, 0, 1, 1, -1, 0, 1, -1, 1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 1, 0, 1, 1, 1, 0, 1, 1, 1, lb, out o.Fourth);
                break;
            case RawDirection.SOUTH:
                light.First = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, -1, 0, -1, 0, 1, -1, -1, 1, -1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, -1, 0, -1, 0, -1, -1, -1, -1, -1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 1, 0, -1, 0, -1, -1, 1, -1, -1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 1, 0, -1, 0, 1, -1, 1, 1, -1, lb, out o.Fourth);
                break;
            case RawDirection.NORTH:
                light.First = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 1, 0, 1, 0, 1, 1, 1, 1, 1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 1, 0, 1, 0, -1, 1, 1, -1, 1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, -1, 0, 1, 0, -1, 1, -1, -1, 1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, -1, 0, 1, 0, 1, 1, -1, 1, 1, lb, out o.Fourth);
                break;
            case RawDirection.DOWN:
                light.First = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 0, -1, 1, 1, -1, 0, 1, -1, 1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 0, -1, -1, 1, -1, 0, 1, -1, -1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 0, -1, -1, -1, -1, 0, -1, -1, -1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 0, -1, 1, -1, -1, 0, -1, -1, 1, lb, out o.Fourth);
                break;
            case RawDirection.UP:
                light.First = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 0, 1, 1, -1, 1, 0, -1, 1, 1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 0, 1, -1, -1, 1, 0, -1, 1, -1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 0, 1, -1, 1, 1, 0, 1, 1, -1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(ref lightRef, ref neighbourRef, 0, 1, 1, 1, 1, 0, 1, 1, 1, lb, out o.Fourth);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int toInt(bool b) {
        return Unsafe.As<bool, int>(ref b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte toByte(bool b) {
        return Unsafe.As<bool, byte>(ref b);
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
