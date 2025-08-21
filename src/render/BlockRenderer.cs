using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.ui;
using BlockGame.util;
using Molten;
using Debug = System.Diagnostics.Debug;

namespace BlockGame;

/// <summary>
/// Unified block rendering system that handles both world rendering (with caches, AO, smooth lighting)
/// and GUI/standalone rendering (simple, all faces, basic lighting).
///
/// ALSO this will be hilariously unsafe because I was thinking for ages about how to handle the world / out of world cases
/// somewhat unified, and I couldn't figure out an okay solution.... and this thing is a global singleton anyway so the chance of
/// misuse/corruption is fuck-all anyway.
///
/// We *do* check the world though before accessing, that can change out underneath us. The block cache won't, though, that's always setup before rendering.
/// TODO do we even need the world? Maybe for fancy shit, we will see!
/// </summary>
public class BlockRenderer {
    
    // in the future when we want multithreaded meshing, we can just allocate like 4-8 of these and it will still be in the ballpark of 10MB
    public static readonly List<BlockVertexPacked> chunkVertices = new(2048);

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

    private static readonly ArrayBlockData?[] neighbourSections = new ArrayBlockData?[27];

    public static ReadOnlySpan<short> lightOffsets => [-1, +1, -18, +18, -324, +324];

    public World? world;
    /** Always 27 elements!
     * You need to initialise the array first before calling fillCache or any of the variants.
     * This is so you can set it up however you want in a modular way without a hardcoded stackalloc, but this requires semi-manual setup.
     * Just copy the existing code and you'll be fine.
     */
    private unsafe uint* blockCache;
    /** Always 27 elements! */
    private unsafe byte* lightCache;
    private bool smoothLighting;
    private bool AO;
    private bool isRenderingWorld;

    public BlockRenderer() {

    }

    // setup for world context
    // do we need this?
    public unsafe void setupWorld(World world, uint* blockCache, byte* lightCache, bool smoothLighting = true, bool AO = true) {
        this.world = world;
        this.blockCache = blockCache;
        this.lightCache = lightCache;
        this.smoothLighting = smoothLighting;
        this.AO = AO;
        isRenderingWorld = true;
    }

    // setup for standalone context
    public void setupStandalone() {
        smoothLighting = false;
        AO = false;
        isRenderingWorld = false;
    }

    public uint getBlock() {
        unsafe {
            // this is unsafe but we know the cache is always 27 elements
            return blockCache[13];
        }
    }
    
    public byte getLight() {
        unsafe {
            // this is unsafe but we know the cache is always 27 elements
            return lightCache[13];
        }
    }
    
    public uint getBlockCached(int x, int y, int z) {
        unsafe {
            // this is unsafe but we know the cache is always 27 elements
            return blockCache[(y + 1) * LOCALCACHESIZE_SQ + (z + 1) * LOCALCACHESIZE + (x + 1)];
        }
    }
    
    public byte getLightCached(int x, int y, int z) {
        unsafe {
            // this is unsafe but we know the cache is always 27 elements
            return lightCache[(y + 1) * LOCALCACHESIZE_SQ + (z + 1) * LOCALCACHESIZE + (x + 1)];
        }
    }

    /// <summary>
    /// Core block rendering method that handles both world and GUI stuff.
    /// </summary>
    public void renderBlock(Block block, Vector3I worldPos, List<BlockVertexTinted> vertices, List<ushort> indices, VertexConstructionMode mode = VertexConstructionMode.OPAQUE,
                           byte lightOverride = 255,
                           Color4b tintOverride = default,
                           bool cullFaces = true) {
        
        vertices.Clear();
        indices.Clear();
        
        if (isRenderingWorld) {
            Span<uint> blockCache = stackalloc uint[LOCALCACHESIZE_CUBE];
            Span<byte> lightCache = stackalloc byte[LOCALCACHESIZE_CUBE];
            fillCache(blockCache, lightCache, ref MemoryMarshal.GetReference(neighbours), ref MemoryMarshal.GetReference(neighbourLights));
            renderBlockWorld(block, worldPos, vertices, indices, mode, cullFaces);
        } else {
            renderBlockStandalone(block, worldPos, vertices, indices, lightOverride, tintOverride);
        }
    }

    [SkipLocalsInit]
    private unsafe void renderBlockWorld(Block block, Vector3I worldPos, List<BlockVertexTinted> vertices, List<ushort> indices, VertexConstructionMode mode, bool cullFaces) {
        
        Span<BlockVertexTinted> tempVertices = stackalloc BlockVertexTinted[4];
        Span<ushort> tempIndices = stackalloc ushort[6];
        
        ref Face facesRef = ref MemoryMarshal.GetArrayDataReference(block.model.faces);
        
        ushort vertexIndex = 0;
        
        for (int d = 0; d < block.model.faces.Length; d++) {
            var face = Unsafe.Add(ref facesRef, d);
            var dir = face.direction;
            
            if (cullFaces && dir != RawDirection.NONE) {
                // get neighbour from cache
                var vec = Direction.getDirection(dir);
                uint neighbourBlock = getBlockCached(vec.X, vec.Y, vec.Z);
                // if neighbour is solid, skip rendering this face
                if (Block.fullBlock[neighbourBlock.getID()]) {
                    continue;
                }
            }
            
            // texcoords
            var texCoords = face.min;
            var texCoordsMax = face.max;
            var tex = Block.texCoords(texCoords);
            var texMax = Block.texCoords(texCoordsMax);
            
            // vertex positions
            float x1 = worldPos.X + face.x1;
            float y1 = worldPos.Y + face.y1;
            float z1 = worldPos.Z + face.z1;
            float x2 = worldPos.X + face.x2;
            float y2 = worldPos.Y + face.y2;
            float z2 = worldPos.Z + face.z2;
            float x3 = worldPos.X + face.x3;
            float y3 = worldPos.Y + face.y3;
            float z3 = worldPos.Z + face.z3;
            float x4 = worldPos.X + face.x4;
            float y4 = worldPos.Y + face.y4;
            float z4 = worldPos.Z + face.z4;
            
            // lighting
            byte light = world?.inWorld(worldPos.X, worldPos.Y, worldPos.Z) == true 
                ? world.getLight(worldPos.X, worldPos.Y, worldPos.Z) 
                : (byte)15;
            
            var tint = WorldRenderer.calculateTint((byte)dir, 0, light);
            
            // create vertices
            tempVertices[0] = new BlockVertexTinted(x1, y1, z1, tex.X, tex.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[1] = new BlockVertexTinted(x2, y2, z2, tex.X, texMax.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[2] = new BlockVertexTinted(x3, y3, z3, texMax.X, texMax.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[3] = new BlockVertexTinted(x4, y4, z4, texMax.X, tex.Y, tint.R, tint.G, tint.B, tint.A);
            
            vertices.AddRange(tempVertices);
            
            // create indices
            tempIndices[0] = vertexIndex;
            tempIndices[1] = (ushort)(vertexIndex + 1);
            tempIndices[2] = (ushort)(vertexIndex + 2);
            tempIndices[3] = vertexIndex;
            tempIndices[4] = (ushort)(vertexIndex + 2);
            tempIndices[5] = (ushort)(vertexIndex + 3);
            
            indices.AddRange(tempIndices);
            vertexIndex += 4;
        }
    }

    [SkipLocalsInit]
    private void renderBlockStandalone(Block block, Vector3I worldPos, List<BlockVertexTinted> vertices, List<ushort> indices, byte lightOverride, Color4b tintOverride) {
        
        Span<BlockVertexTinted> tempVertices = stackalloc BlockVertexTinted[4];
        Span<ushort> tempIndices = stackalloc ushort[6];
        
        ushort vertexIndex = 0;
        var faces = block.model.faces;
        
        for (int d = 0; d < faces.Length; d++) {
            var face = faces[d];
            var dir = face.direction;
            
            // texture coordinates
            var texCoords = face.min;
            var texCoordsMax = face.max;
            var tex = Block.texCoords(texCoords);
            var texMax = Block.texCoords(texCoordsMax);
            
            // vertex positions
            float x1 = worldPos.X + face.x1;
            float y1 = worldPos.Y + face.y1;
            float z1 = worldPos.Z + face.z1;
            float x2 = worldPos.X + face.x2;
            float y2 = worldPos.Y + face.y2;
            float z2 = worldPos.Z + face.z2;
            float x3 = worldPos.X + face.x3;
            float y3 = worldPos.Y + face.y3;
            float z3 = worldPos.Z + face.z3;
            float x4 = worldPos.X + face.x4;
            float y4 = worldPos.Y + face.y4;
            float z4 = worldPos.Z + face.z4;
            
            // calculate tint
            Color4b tint;
            if (tintOverride != default) {
                // use provided tint
                tint = tintOverride * WorldRenderer.calculateTint((byte)dir, 0, lightOverride);
            } else {
                // calculate tint based on direction and light
                tint = WorldRenderer.calculateTint((byte)dir, 0, lightOverride);
            }
            
            // create vertices
            tempVertices[0] = new BlockVertexTinted(x1, y1, z1, tex.X, tex.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[1] = new BlockVertexTinted(x2, y2, z2, tex.X, texMax.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[2] = new BlockVertexTinted(x3, y3, z3, texMax.X, texMax.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[3] = new BlockVertexTinted(x4, y4, z4, texMax.X, tex.Y, tint.R, tint.G, tint.B, tint.A);
            
            vertices.AddRange(tempVertices);
            
            // create indices
            tempIndices[0] = vertexIndex;
            tempIndices[1] = (ushort)(vertexIndex + 1);
            tempIndices[2] = (ushort)(vertexIndex + 2);
            tempIndices[3] = vertexIndex;
            tempIndices[4] = (ushort)(vertexIndex + 2);
            tempIndices[5] = (ushort)(vertexIndex + 3);
            
            indices.AddRange(tempIndices);
            vertexIndex += 4;
        }
    }
    
    
    public void meshChunk(SubChunk subChunk) {
        //sw.Restart();
        subChunk.vao?.Dispose();
        subChunk.vao = new SharedBlockVAO(Game.renderer.chunkVAO);
        subChunk.watervao?.Dispose();
        subChunk.watervao = new SharedBlockVAO(Game.renderer.chunkVAO);

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
        constructVertices(subChunk, RenderLayer.SOLID, chunkVertices);
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
            constructVertices(subChunk, RenderLayer.TRANSLUCENT, chunkVertices);
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

    [SkipLocalsInit]
    private void setupNeighbours(SubChunk subChunk) {
        
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
                        : 0;


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
    }

    // sorry for this mess
    [SkipLocalsInit]
    //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private unsafe void constructVertices(SubChunk subChunk, RenderLayer layer, List<BlockVertexPacked> vertices) {
        // clear arrays before starting
        chunkVertices.Clear();

        Span<BlockVertexPacked> tempVertices = stackalloc BlockVertexPacked[4];
        Span<uint> nba = stackalloc uint[6];

        Span<uint> blockCache = stackalloc uint[27];
        Span<byte> lightCache = stackalloc byte[27];

        // this is correct!
        //ReadOnlySpan<int> normalOrder = [0, 1, 2, 3];
        //ReadOnlySpan<int> flippedOrder = [3, 0, 1, 2];


        // BYTE OF SETTINGS
        // 1 = AO
        // 2 = smooth lighting
        var smoothLighting = Settings.instance.smoothLighting;
        var AO = Settings.instance.AO;
        //ushort cv = 0;
        //ushort ci = 0;

        for (int idx = 0; idx < Chunk.MAXINDEX; idx++) {
            
            // index for array accesses
            int x = idx & 0xF;
            int z = (idx >> 4) & 0xF;
            int y = idx >> 8;

            var index = (y + 1) * Chunk.CHUNKSIZEEXSQ + (z + 1) * Chunk.CHUNKSIZEEX + (x + 1);
            // pre-add index
            ref uint neighbourRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(neighbours), index);
            ref byte lightRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(neighbourLights), index);

            var blockID = neighbourRef.getID();
            var bl = Block.get(blockID);

            if (blockID == 0 || bl.layer != layer) {
                continue;
            }

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
                continue;
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
            
            switch (Block.renderType[blockID]) {
                case RenderType.CUBE:
                    // get UVs from block
                    // todo
                    break;
                case RenderType.CROSS:
                    // todo
                    break;
                case RenderType.MODEL:
                    goto model;
                case RenderType.CUSTOM:
                    bl.render(this, x, y, z, vertices);
                    continue;
            }

            model:;

            for (int d = 0; d < bl.model.faces.Length; d++) {
                var dir = facesRef.direction;
                
                bool test2;
                
                
                
                byte lb = dir == RawDirection.NONE ? lightRef : Unsafe.Add(ref lightRef, lightOffsets[(byte)dir]);

                // check for custom culling
                if (Block.customCulling[blockID]) {
                    test2 = bl.cullFace(this, x, y, z, dir);
                }
                else {
                    if (dir == RawDirection.NONE) {
                        // if it's not a diagonal face, don't even bother checking neighbour because we have to render it anyway
                        test2 = true;
                        light.First = lightRef;
                        light.Second = lightRef;
                        light.Third = lightRef;
                        light.Fourth = lightRef;
                    }
                    else {
                        int nb = nba[(byte)dir].getID();
                        test2 = Block.notSolid(nb) || !Block.isFullBlock(nb) || facesRef.nonFullFace;
                    }
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

                    /*Vector256<float> vec = Vector256.Create(
                        x + facesRef.x1,
                        y + facesRef.y1,
                        z + facesRef.z1,
                        x + facesRef.x2,
                        y + facesRef.y2,
                        z + facesRef.z2,
                        x + facesRef.x3,
                        y + facesRef.y3);*/
                    // OR WE CAN JUST LOAD DIRECTLY!
                    Vector256<float> vec = Vector256.LoadUnsafe(ref Unsafe.As<Face, float>(ref facesRef));
                    // then add all this shit!
                    vec = Vector256.Add(vec, Vector256.Create((float)x, y, z, x, y, z, x, y));

                    vec = Vector256.Add(vec, Vector256.Create(16f));
                    vec = Vector256.Multiply(vec, 256);

                    /*Vector128<float> vec2 = Vector128.Create(
                        z + facesRef.z3,
                        x + facesRef.x4,
                        y + facesRef.y4,
                        z + facesRef.z4);*/
                    
                    
                    Vector128<float> vec2 = Vector128.LoadUnsafe(in Unsafe.AsRef(in facesRef.z3));

                    vec2 = Vector128.Add(vec2, Vector128.Create((float)z, x, y, z));

                    vec2 = Vector128.Add(vec2, Vector128.Create(16f));
                    vec2 = Vector128.Multiply(vec2, 256);

                    // determine vertex order to prevent cracks (combine AO and lighting)
                    // extract skylight values and invert them (15-light = darkness)
                    var dark1 = (~light.First & 0xF);
                    var dark2 = (~light.Second & 0xF);
                    var dark3 = (~light.Third & 0xF);
                    var dark4 = (~light.Fourth & 0xF);

                    /*ReadOnlySpan<int> order = (ao.First + dark1 + ao.Third + dark3 >
                                               ao.Second + dark2 + ao.Fourth + dark4)
                        ? flippedOrder
                        : normalOrder;*/
                    // OR we just shift the index by one and loop it around
                    var shift = (ao.First + dark1 + ao.Third + dark3 >
                                               ao.Second + dark2 + ao.Fourth + dark4).toByte();

                    // add vertices
                    ref var vertex = ref tempVertices[(0 + shift) & 3];
                    vertex.x = (ushort)vec[0];
                    vertex.y = (ushort)vec[1];
                    vertex.z = (ushort)vec[2];
                    vertex.u = (ushort)tex[0];
                    vertex.v = (ushort)tex[1];
                    vertex.cu = Block.packColourB((byte)dir, ao.First);
                    vertex.light = light.First;

                    vertex = ref tempVertices[(1 + shift) & 3];
                    vertex.x = (ushort)vec[3];
                    vertex.y = (ushort)vec[4];
                    vertex.z = (ushort)vec[5];
                    vertex.u = (ushort)tex[0];
                    vertex.v = (ushort)tex[3];
                    vertex.cu = Block.packColourB((byte)dir, ao.Second);
                    vertex.light = light.Second;


                    vertex = ref tempVertices[(2 + shift) & 3];
                    vertex.x = (ushort)vec[6];
                    vertex.y = (ushort)vec[7];
                    vertex.z = (ushort)vec2[0];
                    vertex.u = (ushort)tex[2];
                    vertex.v = (ushort)tex[3];
                    vertex.cu = Block.packColourB((byte)dir, ao.Third);
                    vertex.light = light.Third;

                    vertex = ref tempVertices[(3 + shift) & 3];
                    vertex.x = (ushort)vec2[1];
                    vertex.y = (ushort)vec2[2];
                    vertex.z = (ushort)vec2[3];
                    vertex.u = (ushort)tex[2];
                    vertex.v = (ushort)tex[1];
                    vertex.cu = Block.packColourB((byte)dir, ao.Fourth);
                    vertex.light = light.Fourth;
                    vertices.AddRange(tempVertices);
                    //cv += 4;
                    //ci += 6;
                }
                facesRef = ref Unsafe.Add(ref facesRef, 1);
            }
        }
        //Console.Out.WriteLine($"vert4: {sw.Elapsed.TotalMicroseconds}us");
    }


    /**
     * Fills the 3x3x3 local cache with blocks and light values.
     */
    public unsafe void fillCache(Span<uint> blockCache, Span<byte> lightCache, ref uint neighbourRef, ref byte lightRef) {
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
        
        // set pointers to the cache
        this.blockCache = (uint*)Unsafe.AsPointer(ref blockCache.GetPinnableReference());
        this.lightCache = (byte*)Unsafe.AsPointer(ref lightCache.GetPinnableReference());
    }
    
    public unsafe void fillCacheEmpty(Span<uint> blockCache, Span<byte> lightCache) {
        // fill the cache with empty blocks
        for (int y = 0; y < LOCALCACHESIZE; y++) {
            for (int z = 0; z < LOCALCACHESIZE; z++) {
                for (int x = 0; x < LOCALCACHESIZE; x++) {
                    // calculate the index in the cache
                    int index = y * LOCALCACHESIZE_SQ + z * LOCALCACHESIZE + x;
                    // set the block and light value to empty
                    blockCache[index] = 0;
                    lightCache[index] = 15;
                }
            }
        }
        
        // set pointers to the cache
        this.blockCache = (uint*)Unsafe.AsPointer(ref blockCache.GetPinnableReference());
        this.lightCache = (byte*)Unsafe.AsPointer(ref lightCache.GetPinnableReference());
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
                          ((lightNibble >> 8) & 0xF) * ((inv & 2) >> 1) +
                          ((lightNibble >> 16) & 0xF) * ((inv & 4) >> 2) +
                          ((lightNibble >> 24) & 0xF))
                         / popcnt);

        var block = (byte)((((lightNibble >> 4) & 0xF) * (inv & 1) +
                            ((lightNibble >> 12) & 0xF) * ((inv & 2) >> 1) +
                            ((lightNibble >> 20) & 0xF) * ((inv & 4) >> 2) +
                            ((lightNibble >> 28) & 0xF))
                           / popcnt);
        return (byte)(sky | (block << 4));
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
        
        opacity = (byte)((Unsafe.BitCast<bool, byte>(Block.fullBlock[Unsafe.Add(ref neighbourRef, offset0).getID()])) |
                        (Unsafe.BitCast<bool, byte>(Block.fullBlock[Unsafe.Add(ref neighbourRef, offset1).getID()]) << 1) |
                        (Unsafe.BitCast<bool, byte>(Block.fullBlock[Unsafe.Add(ref neighbourRef, offset2).getID()]) << 2));
        
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