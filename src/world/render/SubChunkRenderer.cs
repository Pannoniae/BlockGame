using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BlockGame.ui;
using BlockGame.util;
using Silk.NET.Maths;

namespace BlockGame;

public class SubChunkRenderer : IDisposable {
    public SubChunk subChunk;


    // we need it here because completely full chunks are also empty of any rendering
    private bool isEmptyRenderOpaque;
    private bool isEmptyRenderTranslucent;

    private VAO? vao;
    private VAO? watervao;

    private bool hasOnlySolid;

    private int uChunkPos;
    private int dummyuChunkPos;

    private static readonly Func<int, bool> AOtest = bl => bl != -1 && Blocks.isSolid(bl);

    // we cheated GC! there is only one list preallocated
    // we need 16x16x16 blocks, each block has max. 24 vertices
    // for indices we need the full 36

    // actually we don't need a list, regular arrays will do because it's only a few megs of space and it's shared
    // in the future when we want multithreaded meshing, we can just allocate like 4-8 of them and it will still be in the ballpark of 10MB
    private static List<BlockVertexPacked> chunkVertices = new(2048);
    private static List<ushort> chunkIndices = new(2048);
    // YZX again
    private static ushort[] neighbours = new ushort[Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX];
    private static byte[] neighbourLights = new byte[Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX];
    private static ArrayBlockData?[] neighbourSections = new ArrayBlockData?[27];

    private Stopwatch sw = new Stopwatch();

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

    public static readonly short[] offsetTableCompact = [

        // west
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,

        // east
        1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 - 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 - 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,

        // south
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 0 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 0 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 0 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 0 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,

        // north
        1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 0 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 0 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 0 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        -1 + 0 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 0 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,

        // down
        0 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        0 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, 1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        0 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        0 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, -1 + -1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,

        // up
        0 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
        0 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, -1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        0 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + -1 * Chunk.CHUNKSIZEEX,
        0 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 0 * Chunk.CHUNKSIZEEX, 1 + 1 * Chunk.CHUNKSIZEEXSQ + 1 * Chunk.CHUNKSIZEEX,
    ];

    public SubChunkRenderer(SubChunk subChunk) {
        this.subChunk = subChunk;
        uChunkPos = Game.worldShader.getUniformLocation("uChunkPos");
        dummyuChunkPos = Game.dummyShader.getUniformLocation("uChunkPos");
    }

    private static bool opaqueBlocks(int b) {
        return b != 0 && Blocks.get(b).type != BlockType.TRANSLUCENT;
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
        //zsw.Restart();
        if (subChunk.world.renderer.fastChunkSwitch) {
            vao?.Dispose();
            vao = new ExtremelySharedBlockVAO(subChunk.world.renderer.chunkVAO);
            watervao?.Dispose();
            watervao = new ExtremelySharedBlockVAO(subChunk.world.renderer.chunkVAO);
        }
        else {
            vao?.Dispose();
            vao = new SharedBlockVAO();
            watervao?.Dispose();
            watervao = new SharedBlockVAO();
        }

        // if the section is empty, nothing to do
        // if is empty, just return, don't need to get neighbours
        if (subChunk.isEmpty) {
            return;
        }

        //Console.Out.WriteLine($"PartMeshing0.5: {sw.Elapsed.TotalMicroseconds}us");
        // first we render everything which is NOT translucent
        //lock (meshingLock) {
        setupNeighbours();

        // if chunk is full, don't mesh either
        if (hasOnlySolid) {
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
            if (subChunk.world.renderer.fastChunkSwitch) {
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
        if (subChunk.blocks.hasTranslucentBlocks()) {
            // then we render everything which is translucent (water for now)
            constructVertices(VertexConstructionMode.TRANSLUCENT);
            //Console.Out.WriteLine($"PartMeshing1.4: {sw.Elapsed.TotalMicroseconds}us {chunkIndices.Count}");
            if (chunkIndices.Count > 0) {
                isEmptyRenderTranslucent = false;
                if (subChunk.world.renderer.fastChunkSwitch) {
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
        //Console.Out.WriteLine($"Meshing: {sw.Elapsed.TotalMicroseconds}us");
        //sw.Stop();
    }


    public bool isVisible(BoundingFrustum frustum) {
        return frustum.Contains(subChunk.bbbox) != ContainmentType.Disjoint;
    }

    public void drawOpaque() {
        if (!isEmptyRenderOpaque) {
            vao.bind();
            //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            Game.worldShader.setUniform(uChunkPos, new Vector3(subChunk.chunkX * 16f, subChunk.chunkY * 16f, subChunk.chunkZ * 16f));
            uint renderedVerts = vao.render();
            Game.metrics.renderedVerts += (int)renderedVerts;
            Game.metrics.renderedChunks += 1;
        }
    }

    public void drawTransparent(bool dummy) {
        if (!isEmptyRenderTranslucent) {
            watervao.bind();
            var shader = dummy ? Game.dummyShader : Game.worldShader;
            shader.setUniform(dummy ? dummyuChunkPos : uChunkPos, new Vector3(subChunk.chunkX * 16, subChunk.chunkY * 16, subChunk.chunkZ * 16));
            uint renderedTransparentVerts = watervao.render();
            Game.metrics.renderedVerts += (int)renderedTransparentVerts;
        }
    }

    [SkipLocalsInit]
    private void setupNeighbours() {
        //var sw = new Stopwatch();
        //sw.Start();

        hasOnlySolid = subChunk.blocks.isFull();
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
        var world = subChunk.world;
        int y;
        int z;
        int x;

        // setup neighbouring sections
        var coord = subChunk.chunkCoord;
        ref var neighbourSectionsArray = ref MemoryMarshal.GetArrayDataReference(neighbourSections);
        for (y = -1; y <= 1; y++) {
            for (z = -1; z <= 1; z++) {
                for (x = -1; x <= 1; x++) {
                    var sec = world.getChunkSectionUnsafe(new ChunkSectionCoord(coord.x + x, coord.y + y, coord.z + z));
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
                    if (subChunk.chunkCoord.y == 0 && y == -1) {
                        bl = Blocks.DIRT.id;
                    }

                    // set neighbours array element  to block
                    blocksArrayRef = bl;
                    // if neighbour is not solid, we still have to mesh this chunk even though all of it is solid
                    if (hasOnlySolid && !Blocks.isFullBlock(bl)) {
                        hasOnlySolid = false;
                    }

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

        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");
    }

    // sorry for this mess, even fucking calli has big overhead
    [SkipLocalsInit]
    //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
    //unsafe private void constructVertices(delegate*<int, bool> whichBlocks, delegate*<int, bool> neighbourTest) {
    unsafe private void constructVertices(VertexConstructionMode mode) {
        //sw.Start();
        //Console.Out.WriteLine($"vert3: {sw.Elapsed.TotalMicroseconds}us");

        // clear arrays before starting
        chunkVertices.Clear();
        chunkIndices.Clear();

        Span<BlockVertexPacked> tempVertices = stackalloc BlockVertexPacked[4];
        Span<ushort> tempIndices = stackalloc ushort[8].Slice(0, 6);
        Vector128<ushort> indices = Vector128<ushort>.Zero;

        Span<ushort> nba = stackalloc ushort[6];
        Span<byte> lba = stackalloc byte[6];


        // BYTE OF SETTINGS
        // 1 = AO
        // 2 = smooth lighting
        byte settings = (byte)(toByte(Settings.instance.smoothLighting) << 1 | toByte(Settings.instance.AO));
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

        ref ushort blockArrayRef = ref MemoryMarshal.GetArrayDataReference(subChunk.blocks.blocks);
        ref short offsetArray = ref MemoryMarshal.GetArrayDataReference(offsetTableCompact);

        Vector128<ushort> indicesMask = Vector128.Create((ushort)0, 1, 2, 0, 2, 3, 0, 0);
        Vector128<ushort> complement = Vector128.Create((ushort)4, 3, 2, 4, 2, 1, 0, 0);

        // for offset indices
        Vector256<short> multiplyMask = Vector256.Create(1, Chunk.CHUNKSIZEEXSQ, Chunk.CHUNKSIZEEX,
            1, Chunk.CHUNKSIZEEXSQ, Chunk.CHUNKSIZEEX,
            1, Chunk.CHUNKSIZEEXSQ, Chunk.CHUNKSIZEEX,
            0, 0, 0, 0, 0, 0, 0);

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

            Block bl = Blocks.get(blockArrayRef);
            switch (mode) {
                case VertexConstructionMode.OPAQUE:
                    if (!opaqueBlocks(blockArrayRef)) {
                        goto increment;
                    }
                    break;
                case VertexConstructionMode.TRANSLUCENT:
                    if (!Blocks.isTranslucent(blockArrayRef)) {
                        goto increment;
                    }
                    break;
            }

            // unrolled world.toWorldPos
            //float wx = section.chunkX * Chunk.CHUNKSIZE + x;
            //float wy = section.chunkY * Chunk.CHUNKSIZE + y;
            //float wz = section.chunkZ * Chunk.CHUNKSIZE + z;

            if (bl.customRender) {
                var writtenIndices = bl.render(subChunk.world, new Vector3D<int>(x, y, z), chunkVertices, chunkIndices, indices[0]);
                // add the number of written indices to the indices vector
                indices += Vector128.Create(writtenIndices);
                goto increment;
            }

            // calculate texcoords
            Vector128<float> tex;


            // calculate AO for all 8 vertices
            // this is garbage but we'll deal with it later

            // one AO value fits on 2 bits so the whole thing fits in a byte
            byte ao = 0;

            // setup neighbour data
            // if all 6 neighbours are solid, we don't even need to bother iterating the faces
            nba[0] = Unsafe.Add(ref neighbourRef, -1);
            nba[1] = Unsafe.Add(ref neighbourRef, +1);
            nba[2] = Unsafe.Add(ref neighbourRef, -Chunk.CHUNKSIZEEX);
            nba[3] = Unsafe.Add(ref neighbourRef, +Chunk.CHUNKSIZEEX);
            nba[4] = Unsafe.Add(ref neighbourRef, -Chunk.CHUNKSIZEEXSQ);
            nba[5] = Unsafe.Add(ref neighbourRef, +Chunk.CHUNKSIZEEXSQ);
            if (Blocks.isFullBlock(nba[0]) &&
                Blocks.isFullBlock(nba[1]) &&
                Blocks.isFullBlock(nba[2]) &&
                Blocks.isFullBlock(nba[3]) &&
                Blocks.isFullBlock(nba[4]) &&
                Blocks.isFullBlock(nba[5])) {
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
                            test2 = Blocks.notSolid(nb) || !Blocks.isFullBlock(nb);
                            break;
                        case VertexConstructionMode.TRANSLUCENT:
                            test2 = !Blocks.isTranslucent(nb) && (Blocks.notSolid(nb) || !Blocks.isFullBlock(nb));
                            break;
                    }
                    test2 = test2 || (facesRef.nonFullFace && !Blocks.isTranslucent(nb));
                }
                // either neighbour test passes, or neighbour is not air + face is not full
                if (test2) {
                    // if face is none, skip the whole lighting business
                    if (dir == RawDirection.NONE) {
                        goto vertex;
                    }

                    if ((settings & SETTING_SMOOTH_LIGHTING) == 0) {
                        light.First = lba[(byte)dir];
                        light.Second = lba[(byte)dir];
                        light.Third = lba[(byte)dir];
                        light.Fourth = lba[(byte)dir];
                    }
                    else {
                        light.Whole = 0;
                    }
                    // AO requires smooth lighting. Otherwise don't need to deal with sampling any of this
                    if ((settings & 3) != 0) {
                        // ox, oy, oz
                        ushort o;
                        // need to store 9 sbytes so it's a 16-element vector
                        // lx, ly, lz, lo
                        // we need 12 bytes
                        Vector128<byte> l;

                        ao = 0;


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
                            0,
                            Unsafe.Add(ref lightRef, offsets[3]),
                            Unsafe.Add(ref lightRef, offsets[4]),
                            Unsafe.Add(ref lightRef, offsets[5]),
                            0,
                            Unsafe.Add(ref lightRef, offsets[6]),
                            Unsafe.Add(ref lightRef, offsets[7]),
                            Unsafe.Add(ref lightRef, offsets[8]),
                            0,
                            Unsafe.Add(ref lightRef, offsets[9]),
                            Unsafe.Add(ref lightRef, offsets[10]),
                            Unsafe.Add(ref lightRef, offsets[11]),
                            0);

                        o = (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[0]))) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[1]))) << 1) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[2]))) << 2) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[3]))) << 3) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[4]))) << 4) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[5]))) << 5) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[6]))) << 6) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[7]))) << 7) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[8]))) << 8) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[9]))) << 9) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[10]))) << 10) |
                                     (ushort)(Unsafe.BitCast<bool, byte>(Blocks.isFullBlock(
                                         Unsafe.Add(ref neighbourRef, offsets[11]))) << 11));

                        // only apply AO if enabled
                        if ((settings & SETTING_AO) != 0 && !facesRef.noAO) {
                            ao |= (byte)((o & 3) == 3 ? 3 : byte.PopCount((byte)(o & 7)));
                            ao |= (byte)((((o >> 3) & 3) == 3 ? 3 : byte.PopCount((byte)((o >> 3) & 7))) << 2);
                            ao |= (byte)((((o >> 6) & 3) == 3 ? 3 : byte.PopCount((byte)((o >> 6) & 7))) << 4);
                            ao |= (byte)((((o >> 9) & 3) == 3 ? 3 : byte.PopCount((byte)((o >> 9) & 7))) << 6);
                        }

                        // if face is noAO, don't average....
                        if ((settings & SETTING_SMOOTH_LIGHTING) != 0) {
                            // if smooth lighting enabled, average light from neighbour face + the 3 other ones
                            // calculate average
                            l = Vector128.Add(l, Vector128.Create(
                                0, 0, 0, lba[(byte)dir],
                                0, 0, 0, lba[(byte)dir],
                                0, 0, 0, lba[(byte)dir],
                                0, 0, 0, lba[(byte)dir]));


                            var n = l.AsUInt32();
                            // split light and reassemble it again
                            light.First = (byte)(
                                average((n[0] >> 4) & 0x0F0F0F0F,
                                    (byte)(o & 7))
                                << 4 |
                                average(n[0] & 0x0F0F0F0F,
                                    (byte)(o & 7)));
                            light.Second = (byte)(
                                average((n[1] >> 4) & 0x0F0F0F0F,
                                    (byte)((o >> 3) & 7))
                                << 4 |
                                average(n[1] & 0x0F0F0F0F,
                                    (byte)((o >> 3) & 7)));
                            light.Third = (byte)(
                                average((n[2] >> 4) & 0x0F0F0F0F,
                                    (byte)((o >> 6) & 7))
                                << 4 |
                                average(n[2] & 0x0F0F0F0F,
                                    (byte)((o >> 6) & 7)));
                            light.Fourth = (byte)(
                                average((n[3] >> 4) & 0x0F0F0F0F,
                                    (byte)((o >> 9) & 7))
                                << 4 |
                                average(n[3] & 0x0F0F0F0F,
                                    (byte)((o >> 9) & 7)));
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
                    vertex.d = Block.packData((byte)dir, (byte)(ao & 0x3), light.First);

                    vertex = ref tempVertices[1];
                    vertex.x = (ushort)vec[3];
                    vertex.y = (ushort)vec[4];
                    vertex.z = (ushort)vec[5];
                    vertex.u = (ushort)tex[0];
                    vertex.v = (ushort)tex[3];
                    vertex.d = Block.packData((byte)dir, (byte)(ao >> 2 & 0x3), light.Second);

                    vertex = ref tempVertices[2];
                    vertex.x = (ushort)vec[6];
                    vertex.y = (ushort)vec[7];
                    vertex.z = (ushort)vec2[0];
                    vertex.u = (ushort)tex[2];
                    vertex.v = (ushort)tex[3];
                    vertex.d = Block.packData((byte)dir, (byte)(ao >> 4 & 0x3), light.Third);

                    vertex = ref tempVertices[3];
                    vertex.x = (ushort)vec2[1];
                    vertex.y = (ushort)vec2[2];
                    vertex.z = (ushort)vec2[3];
                    vertex.u = (ushort)tex[2];
                    vertex.v = (ushort)tex[1];
                    vertex.d = Block.packData((byte)dir, (byte)(ao >> 6), light.Fourth);
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