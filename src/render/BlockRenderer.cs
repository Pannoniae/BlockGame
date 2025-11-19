using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.chunk;
using Molten;
using Debug = System.Diagnostics.Debug;

namespace BlockGame.render;

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

    // 3x3x3 local cache for smooth lighting optimisation
    public const int LOCALCACHESIZE = 3;
    public const int LOCALCACHESIZE_SQ = 9;
    public const int LOCALCACHESIZE_CUBE = 27;

    private static readonly BlockData?[] neighbourSections = new BlockData?[27];

    public static ReadOnlySpan<short> lightOffsets => [-1, +1, -18, +18, -324, +324];

    public World? world;

    public UVPair forceTex = new UVPair(-1, -1);

    /** Hack to convert between vertices. */
    private readonly List<BlockVertexPacked> _listHack = new(24);

    private bool isRenderingWorld;

    public bool smoothLighting;
    public bool AO;

    public ref struct RenderContext {
        [InlineArray(27)]
        public struct ArrayBlockCache {
            public uint block;
        }

        [InlineArray(27)]
        public struct ArrayLightCache {
            public byte light;
        }

        [InlineArray(4)]
        public struct ArrayColourCache {
            public Vector4 colour;
        }

        [InlineArray(4)]
        public struct ArrayLightColourCache {
            public byte light;
        }

        [InlineArray(4)]
        public struct ArrayVertexCache {
            public BlockVertexPacked vertex;
        }

        public ArrayBlockCache blockCache;
        public ArrayLightCache lightCache;
        public ArrayColourCache colourCache;
        public ArrayLightColourCache lightColourCache;
        public ArrayVertexCache vertexCache;

        public int vertexCount;

        public Color currentTint;

        public bool shouldFlipVertices;

        public readonly uint getBlock() {
            // this is unsafe but we know the cache is always 27 elements
            return blockCache[13];
        }

        public readonly byte getLight() {
            // this is unsafe but we know the cache is always 27 elements
            return lightCache[13];
        }

        public readonly uint getBlockCached(int x, int y, int z) {
            // this is unsafe but we know the cache is always 27 elements
            return blockCache[(y + 1) * LOCALCACHESIZE_SQ + (z + 1) * LOCALCACHESIZE + (x + 1)];
        }

        public readonly byte getLightCached(int x, int y, int z) {
            // this is unsafe but we know the cache is always 27 elements
            return lightCache[(y + 1) * LOCALCACHESIZE_SQ + (z + 1) * LOCALCACHESIZE + (x + 1)];
        }
    }

    public static unsafe RenderContext* _ctx;

    public static unsafe ref RenderContext ctx => ref Unsafe.AsRef<RenderContext>(_ctx);

    public static unsafe void setCtx(ref RenderContext context) {
        _ctx = (RenderContext*)Unsafe.AsPointer(ref context);
    }


    public uint getBlock() {
        unsafe {
            // this is unsafe but we know the cache is always 27 elements
            return ctx.blockCache[13];
        }
    }

    public byte getLight() {
        unsafe {
            // this is unsafe but we know the cache is always 27 elements
            return ctx.lightCache[13];
        }
    }

    public uint getBlockCached(int x, int y, int z) {
        unsafe {
            // this is unsafe but we know the cache is always 27 elements
            return ctx.blockCache[(y + 1) * LOCALCACHESIZE_SQ + (z + 1) * LOCALCACHESIZE + (x + 1)];
        }
    }

    public byte getLightCached(int x, int y, int z) {
        unsafe {
            // this is unsafe but we know the cache is always 27 elements
            return ctx.lightCache[(y + 1) * LOCALCACHESIZE_SQ + (z + 1) * LOCALCACHESIZE + (x + 1)];
        }
    }

    // setup for world context
    // do we need this?
    public unsafe void setupWorld(bool smoothLighting = true, bool AO = true) {
        this.smoothLighting = smoothLighting && Settings.instance.smoothLighting;
        this.AO = AO && Settings.instance.AO;
        isRenderingWorld = true;
    }

    // setup for standalone context
    public void setupStandalone() {
        this.smoothLighting = false;
        this.AO = false;
        isRenderingWorld = false;
    }

    public void setWorld(World? world) {
        // clean all the previous stuff
        Array.Clear(neighbourSections);

        this.world = world;

        smoothLighting = smoothLighting && Settings.instance.smoothLighting;
        AO = AO && Settings.instance.AO;
        isRenderingWorld = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe byte calculateVertexLightAndAO(int x0, int y0, int z0, int x1, int y1, int z1, int x2, int y2,
        int z2, byte lb, out byte opacity) {
        // since we're using a cache now, we're getting the offsets from the cache which is 3x3x3
        // so +1 is +3, -1 is -3, etc.

        // calculate the offsets in the local cache
        int offset0 = (y0 + 1) * LOCALCACHESIZE_SQ + (z0 + 1) * LOCALCACHESIZE + (x0 + 1);
        int offset1 = (y1 + 1) * LOCALCACHESIZE_SQ + (z1 + 1) * LOCALCACHESIZE + (x1 + 1);
        int offset2 = (y2 + 1) * LOCALCACHESIZE_SQ + (z2 + 1) * LOCALCACHESIZE + (x2 + 1);

        uint lightValue = (uint)(ctx.lightCache[offset0] |
                                 (ctx.lightCache[offset1] << 8) |
                                 (ctx.lightCache[offset2] << 16) |
                                 lb << 24);

        opacity = (byte)((Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[offset0].getID()])) |
                         (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[offset1].getID()]) << 1) |
                         (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[offset2].getID()]) << 2));

        return average2(lightValue, opacity);
    }

    /**
     * TODO there could be a 4x version of this, which does all 4 vertices at once for a face.... and average2 could have an average2x4 version too which does 128 bits at once and writes an entire opacity uint at once
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void getDirectionOffsetsAndData(RawDirection dir, byte lb, out FourBytes light, out FourBytes o) {
        Unsafe.SkipInit(out o);
        Unsafe.SkipInit(out light);
        switch (dir) {
            case RawDirection.WEST:
                light.First = calculateVertexLightAndAO(-1, 0, 1, -1, 1, 0, -1, 1, 1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(-1, 0, 1, -1, -1, 0, -1, -1, 1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(-1, 0, -1, -1, -1, 0, -1, -1, -1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(-1, 0, -1, -1, 1, 0, -1, 1, -1, lb, out o.Fourth);
                break;
            case RawDirection.EAST:
                light.First = calculateVertexLightAndAO(1, 0, -1, 1, 1, 0, 1, 1, -1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(1, 0, -1, 1, -1, 0, 1, -1, -1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(1, 0, 1, 1, -1, 0, 1, -1, 1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(1, 0, 1, 1, 1, 0, 1, 1, 1, lb, out o.Fourth);
                break;
            case RawDirection.SOUTH:
                light.First = calculateVertexLightAndAO(-1, 0, -1, 0, 1, -1, -1, 1, -1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(-1, 0, -1, 0, -1, -1, -1, -1, -1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(1, 0, -1, 0, -1, -1, 1, -1, -1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(1, 0, -1, 0, 1, -1, 1, 1, -1, lb, out o.Fourth);
                break;
            case RawDirection.NORTH:
                light.First = calculateVertexLightAndAO(1, 0, 1, 0, 1, 1, 1, 1, 1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(1, 0, 1, 0, -1, 1, 1, -1, 1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(-1, 0, 1, 0, -1, 1, -1, -1, 1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(-1, 0, 1, 0, 1, 1, -1, 1, 1, lb, out o.Fourth);
                break;
            case RawDirection.DOWN:
                light.First = calculateVertexLightAndAO(0, -1, 1, 1, -1, 0, 1, -1, 1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(0, -1, -1, 1, -1, 0, 1, -1, -1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(0, -1, -1, -1, -1, 0, -1, -1, -1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(0, -1, 1, -1, -1, 0, -1, -1, 1, lb, out o.Fourth);
                break;
            case RawDirection.UP:
                light.First = calculateVertexLightAndAO(0, 1, 1, -1, 1, 0, -1, 1, 1, lb, out o.First);
                light.Second = calculateVertexLightAndAO(0, 1, -1, -1, 1, 0, -1, 1, -1, lb, out o.Second);
                light.Third = calculateVertexLightAndAO(0, 1, -1, 1, 1, 0, 1, 1, -1, lb, out o.Third);
                light.Fourth = calculateVertexLightAndAO(0, 1, 1, 1, 1, 0, 1, 1, 1, lb, out o.Fourth);
                break;
        }
    }

    // Helper methods for custom blocks

    /// <summary>
    /// Standard face culling logic - checks if neighbour is solid and full.
    /// Custom blocks can override Block.cullFace instead of using this.
    /// </summary>
    public bool shouldCullFace(RawDirection dir) {
        if (dir == RawDirection.NONE) {
            return false; // never cull non-directional faces
        }

        var vec = Direction.getDirection(dir);
        uint neighbourBlock = getBlockCached(vec.X, vec.Y, vec.Z);
        return Block.fullBlock[neighbourBlock.getID()];
    }

    /// <summary>
    /// Calculate lighting and AO for a face's 4 vertices.
    /// Handles both smooth lighting and simple lighting automatically.
    /// </summary>
    public void calculateFaceLighting(RawDirection dir, out FourBytes light, out FourBytes ao) {
        Unsafe.SkipInit(out light);
        Unsafe.SkipInit(out ao);

        var theLight = getLightCached(0, 0, 0);
        var d = Direction.getDirection(dir);
        var neighbourLight = getLightCached(d.X, d.Y, d.Z);
        byte lb = dir == RawDirection.NONE ? theLight : neighbourLight;

        if (!smoothLighting && !AO) {
            // simple lighting - uniform for all vertices
            light = new FourBytes(lb, lb, lb, lb);
            ao = new FourBytes(0, 0, 0, 0);
        }

        else {
            getDirectionOffsetsAndData(dir, lb, out light, out FourBytes o);

            if (AO) {
                ao.First = (byte)(o.First == 3 ? 3 : byte.PopCount(o.First));
                ao.Second = (byte)((o.Second & 3) == 3 ? 3 : byte.PopCount(o.Second));
                ao.Third = (byte)((o.Third & 3) == 3 ? 3 : byte.PopCount(o.Third));
                ao.Fourth = (byte)((o.Fourth & 3) == 3 ? 3 : byte.PopCount(o.Fourth));
            }
            else {
                ao = new FourBytes(0, 0, 0, 0);
            }
        }
    }


    public unsafe void applyFaceLighting(RawDirection dir) {
        calculateFaceLighting(dir, out FourBytes light, out FourBytes ao);

        // calculate AO flip decision based on same logic as constructVertices
        if (AO && smoothLighting) {
            var dark0 = (~light.First & 0xF);
            var dark1 = (~light.Second & 0xF);
            var dark2 = (~light.Third & 0xF);
            var dark3 = (~light.Fourth & 0xF);

            ctx.shouldFlipVertices = ao.First + dark0 + ao.Third + dark2 > ao.Second + dark1 + ao.Fourth + dark3;
        }
        else {
            ctx.shouldFlipVertices = false;
        }

        Span<float> aoArray = [1.0f, 0.75f, 0.5f, 0.25f];
        Span<float> a = [0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1];


        for (int i = 0; i < 4; i++) {
            float tint = (dir == RawDirection.NONE
                ? 1f
                : // no shading for non-directional faces, no AO!
                a[(byte)dir]) * aoArray[ao.bytes[i]];

            //var res = (tint);
            // set alpha to 1!
            // todo do we really need this? user can fuck it up for himself whatever
            //res.W = 1;
            ctx.colourCache[i] = new Vector4(tint, tint, tint, 1);
            ctx.lightColourCache[i] = light.bytes[i];
        }
    }

    /// <summary>
    /// Apply simple uniform lighting without AO for faces like torch rendering.
    /// Uses the current block's light level, not neighbour light.
    /// </summary>
    public unsafe void applySimpleLighting(RawDirection dir) {
        ctx.shouldFlipVertices = false;

        var blockLight = getLightCached(0, 0, 0);

        // uniform lighting for all 4 vertices
        Span<float> a = [0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1];

        var tint = dir == RawDirection.NONE
            ? 1f
            : // no shading for non-directional faces
            a[(byte)dir]; // no AO!

        for (int i = 0; i < 4; i++) {
            ctx.colourCache[i] = new Vector4(tint, tint, tint, 1);
            ctx.lightColourCache[i] = blockLight;
        }
    }

    /**
     * applySimpleLighting but no directional shading lol
     */
    public unsafe void applySimpleLightingNoDir() {
        ctx.shouldFlipVertices = false;

        var blockLight = getLightCached(0, 0, 0);

        // uniform lighting for all 4 vertices

        for (int i = 0; i < 4; i++) {
            ctx.colourCache[i] = new Vector4(1, 1, 1, 1);
            ctx.lightColourCache[i] = blockLight;
        }
    }


    /// <summary>
    /// Convert texture UV coordinates to atlas coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> getAtlasCoords(float uMin, float vMin, float uMax, float vMax) {
        var tex = Vector128.Create(uMin, vMin, uMax, vMax);
        const float factor = Block.atlasRatio * 32768f;
        return Vector128.Multiply(tex, factor);
    }

    /// <summary>
    /// Render a quad face with custom geometry, full lighting, AO, and advanced features.
    /// Positions are in local block coordinates (0-1 range typically).
    /// </summary>
    public void renderQuadCull(List<BlockVertexPacked> vertices, RawDirection dir, int x, int y, int z,
        float x1, float y1, float z1, float x2, float y2, float z2,
        float x3, float y3, float z3, float x4, float y4, float z4,
        float uMin, float vMin, float uMax, float vMax) {
        // check culling
        ushort blockID = getBlock().getID();
        bool shouldRender;

        if (Block.customCulling[blockID]) {
            var block = Block.get(blockID);
            shouldRender = block.cullFace(this, x, y, z, dir);
        }
        else {
            shouldRender = !shouldCullFace(dir);
        }

        if (!shouldRender) {
            return;
        }


        // calculate lighting and AO
        applyFaceLighting(dir);

        begin();
        vertex(x + x1, y + y1, z + z1, uMin, vMin);
        vertex(x + x2, y + y2, z + z2, uMin, vMax);
        vertex(x + x3, y + y3, z + z3, uMax, vMax);
        vertex(x + x4, y + y4, z + z4, uMax, vMin);
        end(vertices);
    }


    /// <summary>
    /// Render a quad face with custom geometry, full lighting, AO, and advanced features.
    /// Positions are in local block coordinates (0-1 range typically).
    /// </summary>
    public void renderQuad(List<BlockVertexPacked> vertices, RawDirection dir, int x, int y, int z,
        float x1, float y1, float z1, float x2, float y2, float z2,
        float x3, float y3, float z3, float x4, float y4, float z4,
        float uMin, float vMin, float uMax, float vMax) {
        // calculate lighting and AO
        applyFaceLighting(dir);

        begin();
        vertex(x + x1, y + y1, z + z1, uMin, vMin);
        vertex(x + x2, y + y2, z + z2, uMin, vMax);
        vertex(x + x3, y + y3, z + z3, uMax, vMax);
        vertex(x + x4, y + y4, z + z4, uMax, vMin);
        end(vertices);
    }

    /// <summary>
    /// Render a double-sided quad (visible from both sides).
    /// </summary>
    public void renderQuadDoubleSided(List<BlockVertexPacked> vertices, RawDirection dir, int x, int y, int z,
        float x1, float y1, float z1, float x2, float y2, float z2,
        float x3, float y3, float z3, float x4, float y4, float z4,
        float uMin, float vMin, float uMax, float vMax) {
        // front face
        applyFaceLighting(dir);
        begin();
        vertex(x + x1, y + y1, z + z1, uMin, vMin);
        vertex(x + x2, y + y2, z + z2, uMin, vMax);
        vertex(x + x3, y + y3, z + z3, uMax, vMax);
        vertex(x + x4, y + y4, z + z4, uMax, vMin);
        end(vertices);

        // back face (reversed winding)
        applyFaceLighting(dir);
        begin();
        vertex(x + x4, y + y4, z + z4, uMax, vMin);
        vertex(x + x3, y + y3, z + z3, uMax, vMax);
        vertex(x + x2, y + y2, z + z2, uMin, vMax);
        vertex(x + x1, y + y1, z + z1, uMin, vMin);
        end(vertices);
    }

    // Immediate-mode vertex building API

    /// <summary>
    /// Start building a face. Caller must provide a vertex cache (typically stackalloc BlockVertexPacked[4]).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void begin() {
        ctx.vertexCount = 0;
    }

    /// <summary>
    /// Add a vertex to the current face being built.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void vertex(float x, float y, float z, float u, float v, byte light, Vector4 tint) {
        // multiply tint by the stored colour
        var c = tint * ctx.colourCache[ctx.vertexCount];

        var col = Meth.f2b(c);

        ref var vert = ref ctx.vertexCache[ctx.vertexCount];
        vert.x = (ushort)((x + 16f) * 256f);
        vert.y = (ushort)((y + 16f) * 256f);
        vert.z = (ushort)((z + 16f) * 256f);
        vert.u = (ushort)(u * 32768);
        vert.v = (ushort)(v * 32768);
        vert.light = light;
        vert.cu = col;
        ctx.vertexCount++;
    }

    /// <summary>
    /// Add a vertex to the current face being built.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void vertex(float x, float y, float z, float u, float v, Vector4 tint) {
        // multiply tint by the stored colour
        var c = tint * ctx.colourCache[ctx.vertexCount];

        var col = Meth.f2b(c);

        ref var vert = ref ctx.vertexCache[ctx.vertexCount];
        vert.x = (ushort)((x + 16f) * 256f);
        vert.y = (ushort)((y + 16f) * 256f);
        vert.z = (ushort)((z + 16f) * 256f);
        vert.u = (ushort)(u * 32768);
        vert.v = (ushort)(v * 32768);
        vert.light = ctx.lightColourCache[ctx.vertexCount];
        vert.cu = col;
        ctx.vertexCount++;
    }

    /// <summary>
    /// Add a vertex to the current face being built.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void vertex(float x, float y, float z, float u, float v, byte light) {
        // multiply tint by the stored colour
        // there is no tint though!
        var c = ctx.colourCache[ctx.vertexCount];

        var col = Meth.f2b(c);

        ref var vert = ref ctx.vertexCache[ctx.vertexCount];
        vert.x = (ushort)((x + 16f) * 256f);
        vert.y = (ushort)((y + 16f) * 256f);
        vert.z = (ushort)((z + 16f) * 256f);
        vert.u = (ushort)(u * 32768);
        vert.v = (ushort)(v * 32768);
        vert.light = light;
        vert.cu = col;
        ctx.vertexCount++;
    }

    /// <summary>
    /// Add a vertex to the current face being built.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void vertex(float x, float y, float z, float u, float v) {
        // multiply tint by the stored colour
        // there is no tint though!
        var c = ctx.colourCache[ctx.vertexCount];

        var col = Meth.f2b(c);

        ref var vert = ref ctx.vertexCache[ctx.vertexCount];
        vert.x = (ushort)((x + 16f) * 256f);
        vert.y = (ushort)((y + 16f) * 256f);
        vert.z = (ushort)((z + 16f) * 256f);
        vert.u = (ushort)(u * 32768);
        vert.v = (ushort)(v * 32768);
        vert.light = ctx.lightColourCache[ctx.vertexCount];
        vert.cu = col;
        ctx.vertexCount++;
    }


    /// <summary>
    /// Finish building the current face and add vertices to the output list.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // i have no idea why it complains about redundant span creation (it's very much not redundant) but now it won't!
    [SuppressMessage("ReSharper", "RedundantExplicitParamsArrayCreation")]
    public void end(List<BlockVertexPacked> vertices) {
        ref var vertexCache = ref ctx.vertexCache;
        if (ctx.shouldFlipVertices) {
            // apply AO flip - reorder vertices: 0,1,2,3 -> 3,0,1,2

            // how about NOT copying shit all over the place? so slopcode begin
            // we'll have only ONE temp

            var v0 = vertexCache[0];

            vertexCache[0] = vertexCache[3];
            vertexCache[3] = vertexCache[2];
            vertexCache[2] = vertexCache[1];
            vertexCache[1] = v0;
        }

        // add the vertices to the list
        vertices.AddRange(ctx.vertexCache);
        ctx.vertexCount = 0; // reset
    }

    /// <summary>
    /// Finish building the current face and add both front and back faces (double-sided).
    /// </summary>
    [SuppressMessage("ReSharper", "RedundantExplicitParamsArrayCreation")]
    public void endTwo(List<BlockVertexPacked> vertices) {
        ref var vertexCache = ref ctx.vertexCache;

        // front face
        if (ctx.shouldFlipVertices) {
            var v0 = vertexCache[0];
            vertexCache[0] = vertexCache[3];
            vertexCache[3] = vertexCache[2];
            vertexCache[2] = vertexCache[1];
            vertexCache[1] = v0;
        }

        vertices.AddRange(ctx.vertexCache);

        // back face
        var temp0 = vertexCache[0];
        var temp1 = vertexCache[1];
        vertexCache[0] = vertexCache[3];
        vertexCache[1] = vertexCache[2];
        vertexCache[2] = temp1;
        vertexCache[3] = temp0;

        vertices.AddRange(ctx.vertexCache);
        ctx.vertexCount = 0;
    }

    /// <summary>
    /// Core block rendering method that handles both world and GUI stuff.
    /// </summary>
    public void renderBlock(Block block, byte metadata, Vector3I worldPos, List<BlockVertexTinted> vertices,
        VertexConstructionMode mode = VertexConstructionMode.OPAQUE,
        byte lightOverride = 255,
        Color tintOverride = default,
        bool cullFaces = true) {
        // SETUP CACHE
        RenderContext c = new();
        setCtx(ref c);

        vertices.Clear();

        if (isRenderingWorld) {
            fillCache(ref MemoryMarshal.GetReference(neighbours), ref MemoryMarshal.GetReference(neighbourLights));
            renderBlockWorld(block, worldPos, vertices, mode, cullFaces);
        }
        else {
            renderBlockStandalone(block, worldPos, vertices, lightOverride, tintOverride, metadata);
        }
    }

    /** totally not a gross hack */
    public void renderBlock(Block block, byte metadata, Vector3I worldPos, List<BlockVertexLighted> vertices,
        VertexConstructionMode mode = VertexConstructionMode.OPAQUE,
        byte lightOverride = 255,
        Color tintOverride = default,
        bool cullFaces = true) {
        // SETUP CACHE
        RenderContext c = new();
        setCtx(ref c);

        vertices.Clear();
        renderBlockStandalone(block, worldPos, vertices, lightOverride, tintOverride, metadata);
    }

    [SkipLocalsInit]
    private unsafe void renderBlockWorld(Block block, Vector3I worldPos, List<BlockVertexTinted> vertices, VertexConstructionMode mode,
        bool cullFaces) {
        Span<BlockVertexTinted> tempVertices = stackalloc BlockVertexTinted[4];

        ref Face facesRef = ref MemoryMarshal.GetArrayDataReference(block.model.faces);

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
            var tex = UVPair.texCoords(texCoords);
            var texMax = UVPair.texCoords(texCoordsMax);

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
            byte light = world.getLightC(worldPos.X, worldPos.Y, worldPos.Z);

            var tint = WorldRenderer.calculateTint((byte)dir, 0, light);

            // create vertices
            tempVertices[0] = new BlockVertexTinted(x1, y1, z1, tex.X, tex.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[1] = new BlockVertexTinted(x2, y2, z2, tex.X, texMax.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[2] = new BlockVertexTinted(x3, y3, z3, texMax.X, texMax.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[3] = new BlockVertexTinted(x4, y4, z4, texMax.X, tex.Y, tint.R, tint.G, tint.B, tint.A);

            vertices.AddRange(tempVertices);
        }
    }

    [SkipLocalsInit]
    private void renderBlockStandalone(Block block, Vector3I worldPos, List<BlockVertexLighted> vertices, byte lightOverride,
        Color tintOverride, byte metadata = 0) {
        Span<BlockVertexLighted> tempVertices = stackalloc BlockVertexLighted[4];

        uint blockID = block.getID();
        var bl = Block.get(blockID.getID());

        // we render to a temp list
        _listHack.Clear();

        // turn off AO and smooth lighting for standalone rendering! it won't work properly and it will mess the lighting up
        // because we don't have a proper cache

        setupStandalone();

        // setup (fake) cache
        fillCacheStandalone(blockID.setMetadata(metadata), lightOverride);

        renderBlockSwitch(bl, 0, 0, 0, metadata, _listHack);

        if (Block.renderType[(int)blockID] != RenderType.MODEL) {
            // now we convert it to the REAL vertices
            foreach (var vertex in _listHack) {
                // convert to tinted vertex
                // we need to restore the UVs (so multiply by inverse atlas)
                // and we need to uncompress the positions
                var lightedVertex = new BlockVertexLighted {
                    x = vertex.x / 256f - 16f,
                    y = vertex.y / 256f - 16f,
                    z = vertex.z / 256f - 16f,
                    u = vertex.u / 32768f,
                    v = vertex.v / 32768f,
                    cu = vertex.cu,
                    light = vertex.light
                };

                vertices.Add(lightedVertex);
            }

            return;
        }

        var faces = block.model.faces;

        for (int d = 0; d < faces.Length; d++) {
            var face = faces[d];
            var dir = face.direction;

            // texture coordinates
            var texCoords = face.min;
            var texCoordsMax = face.max;
            var tex = UVPair.texCoords(texCoords);
            var texMax = UVPair.texCoords(texCoordsMax);

            // check for forced texture override (for breaking overlay)
            if (forceTex.u >= 0 && forceTex.v >= 0) {
                tex = UVPair.texCoords(forceTex);
                texMax = UVPair.texCoords(new UVPair(forceTex.u + 1, forceTex.v + 1));
            }

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
            Color tint;
            if (tintOverride != default) {
                // use provided tint
                tint = tintOverride * WorldRenderer.calculateTint((byte)dir, 0, lightOverride);
            }
            else {
                // calculate tint based on direction and light
                tint = WorldRenderer.calculateTint((byte)dir, 0, lightOverride);
            }

            // create vertices
            tempVertices[0] = new BlockVertexLighted(x1, y1, z1, tex.X, tex.Y, tint.R, tint.G, tint.B, tint.A, lightOverride);
            tempVertices[1] = new BlockVertexLighted(x2, y2, z2, tex.X, texMax.Y, tint.R, tint.G, tint.B, tint.A, lightOverride);
            tempVertices[2] = new BlockVertexLighted(x3, y3, z3, texMax.X, texMax.Y, tint.R, tint.G, tint.B, tint.A, lightOverride);
            tempVertices[3] = new BlockVertexLighted(x4, y4, z4, texMax.X, tex.Y, tint.R, tint.G, tint.B, tint.A, lightOverride);

            vertices.AddRange(tempVertices);
        }
    }

    [SkipLocalsInit]
    private void renderBlockStandalone(Block block, Vector3I worldPos, List<BlockVertexTinted> vertices, byte lightOverride,
        Color tintOverride, byte metadata = 0) {
        Span<BlockVertexTinted> tempVertices = stackalloc BlockVertexTinted[4];

        uint blockID = block.getID();
        var bl = Block.get(blockID.getID());

        // we render to a temp list
        _listHack.Clear();

        // turn off AO and smooth lighting for standalone rendering! it won't work properly and it will mess the lighting up
        // because we don't have a proper cache

        setupStandalone();

        // setup (fake) cache
        fillCacheStandalone(blockID.setMetadata(metadata), lightOverride);

        renderBlockSwitch(bl, 0, 0, 0, metadata, _listHack);

        if (Block.renderType[(int)blockID] != RenderType.MODEL) {
            // now we convert it to the REAL vertices
            foreach (var vertex in _listHack) {
                // convert to tinted vertex
                // we need to restore the UVs (so multiply by inverse atlas)
                // and we need to uncompress the positions
                var tintedVertex = new BlockVertexTinted();
                tintedVertex.x = vertex.x / 256f - 16f;
                tintedVertex.y = vertex.y / 256f - 16f;
                tintedVertex.z = vertex.z / 256f - 16f;
                tintedVertex.u = vertex.u / 32768f;
                tintedVertex.v = vertex.v / 32768f;

                // apply lighting from vertex.light to the base colour in vertex.cu
                var blocklight = (byte)(vertex.light >> 4);
                var skylight = (byte)(vertex.light & 0xF);
                var lightColor = WorldRenderer.getLightColour(skylight, blocklight);

                // unpack base colour
                var r = (byte)(vertex.cu & 0xFF);
                var g = (byte)((vertex.cu >> 8) & 0xFF);
                var b = (byte)((vertex.cu >> 16) & 0xFF);
                var a = (byte)((vertex.cu >> 24) & 0xFF);

                // multiply by light
                r = (byte)(r / 255f * lightColor.R);
                g = (byte)(g / 255f * lightColor.G);
                b = (byte)(b / 255f * lightColor.B);

                tintedVertex.cu = (uint)(r | (g << 8) | (b << 16) | (a << 24));
                vertices.Add(tintedVertex);
            }

            return;
        }

        var faces = block.model.faces;

        for (int d = 0; d < faces.Length; d++) {
            var face = faces[d];
            var dir = face.direction;

            // texture coordinates
            var texCoords = face.min;
            var texCoordsMax = face.max;
            var tex = UVPair.texCoords(texCoords);
            var texMax = UVPair.texCoords(texCoordsMax);

            // check for forced texture override (for breaking overlay)
            if (forceTex.u >= 0 && forceTex.v >= 0) {
                tex = UVPair.texCoords(forceTex);
                texMax = UVPair.texCoords(new UVPair(forceTex.u + 1, forceTex.v + 1));
            }

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
            Color tint;
            if (tintOverride != default) {
                // use provided tint
                tint = tintOverride * WorldRenderer.calculateTint((byte)dir, 0, lightOverride);
            }
            else {
                // calculate tint based on direction and light
                tint = WorldRenderer.calculateTint((byte)dir, 0, lightOverride);
            }

            // create vertices
            tempVertices[0] = new BlockVertexTinted(x1, y1, z1, tex.X, tex.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[1] = new BlockVertexTinted(x2, y2, z2, tex.X, texMax.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[2] = new BlockVertexTinted(x3, y3, z3, texMax.X, texMax.Y, tint.R, tint.G, tint.B, tint.A);
            tempVertices[3] = new BlockVertexTinted(x4, y4, z4, texMax.X, tex.Y, tint.R, tint.G, tint.B, tint.A);

            vertices.AddRange(tempVertices);
        }
    }


    private void renderBlockSwitch(Block bl, int x, int y, int z, byte metadata, List<BlockVertexPacked> vertices) {
        switch (Block.renderType[bl.getID()]) {
            case RenderType.CUBE:
                // standard cube using static texture
                var uvs = bl.uvs;
                var tx = uvs[0]; // use first texture for all faces
                var txm = tx + 1;
                var uvx = UVPair.texCoords(tx);
                var uvxm = UVPair.texCoords(txm);

                if (forceTex.u >= 0 && forceTex.v >= 0) {
                    uvx = UVPair.texCoords(forceTex);
                    uvxm = UVPair.texCoords(new UVPair(forceTex.u + 1, forceTex.v + 1));
                }

                renderCube(x & 0xF, y & 0xF, z & 0xF, vertices, 0, 0, 0, 1, 1, 1, uvx.X, uvx.Y, uvxm.X, uvxm.Y);
                break;
            case RenderType.CUBE_DYNTEXTURE:
                // cube with per-face dynamic textures based on metadata
                renderCubeDynamic(bl, x & 0xF, y & 0xF, z & 0xF, vertices, metadata);
                break;
            case RenderType.GRASS:
                // same but also get grass colour
                renderGrass(bl, x & 0xF, y & 0xF, z & 0xF, vertices, metadata);
                break;
            case RenderType.CROSS:
                renderCross(bl, x & 0xF, y & 0xF, z & 0xF, vertices, metadata);
                break;
            case RenderType.FIRE:
                renderFire(bl, x & 0xF, y & 0xF, z & 0xF, vertices, metadata);
                break;
            case RenderType.MODEL:
                break;
            case RenderType.CUSTOM:
                bl.render(this, x, y, z, vertices);
                break;
        }
    }


    public void meshChunk(SubChunk subChunk) {
        // SETUP CACHE
        RenderContext c = new();
        setCtx(ref c);

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

        setupNeighbours(subChunk);

        constructVertices(subChunk, RenderLayer.SOLID, chunkVertices);

        if (chunkVertices.Count > 0) {
            subChunk.hasRenderOpaque = true;
            currentVAO.bindVAO();
            var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
            currentVAO.upload(finalVertices, (uint)finalVertices.Length);
        }
        else {
            subChunk.hasRenderOpaque = false;
        }

        if (subChunk.blocks.hasTranslucentBlocks() && !Settings.instance.opaqueWater) {
            constructVertices(subChunk, RenderLayer.TRANSLUCENT, chunkVertices);
            if (chunkVertices.Count > 0) {
                subChunk.hasRenderTranslucent = true;
                currentWaterVAO.bindVAO();

                var tFinalVertices = CollectionsMarshal.AsSpan(chunkVertices);
                currentWaterVAO.upload(tFinalVertices, (uint)tFinalVertices.Length);
            }
            else {
                subChunk.hasRenderTranslucent = false;
            }
        }
    }

    [SkipLocalsInit]
    private void setupNeighbours(SubChunk subChunk) {
        // cache blocks
        // we need a 18x18 area
        // we load the 16x16 from the section itself then get the world for the rest
        var blocks = subChunk.blocks;
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

        var bes = subChunk.renderedBlockEntities;
        bes.Clear();

        for (y = -1; y < Chunk.CHUNKSIZE + 1; y++) {
            for (z = -1; z < Chunk.CHUNKSIZE + 1; z++) {
                for (x = -1; x < Chunk.CHUNKSIZE + 1; x++) {
                    // if inside the chunk, load from section using proper method calls
                    if (x is >= 0 and < Chunk.CHUNKSIZE &&
                        z is >= 0 and < Chunk.CHUNKSIZE &&
                        y is >= 0 and < Chunk.CHUNKSIZE) {
                        blocksArrayRef = blocks.getRaw(x, y, z);
                        lightArrayRef = blocks.getLight(x, y, z);

                        // handle block entities
                        if (Block.isBlockEntity[blocksArrayRef.getID()]) {
                            var by = subChunk.coord.y * Chunk.CHUNKSIZE + y;
                            var be = subChunk.chunk.getBlockEntity(x, by, z);
                            // check if renderable
                            if (be != null && Registry.BLOCK_ENTITIES.hasRenderer[Registry.BLOCK_ENTITIES.getID(be.type)]) {
                                bes.Add(be.pos);
                            }
                        }

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
                        ? neighbourSection!.getRaw((x & 0xF), (y & 0xF), (z & 0xF))
                        : 0;


                    // if below world, pretend it's dirt (so it won't get meshed)
                    if (subChunk.coord.y == 0 && y == -1) {
                        bl = Block.DIRT.id;
                    }

                    // set neighbours array element to block
                    blocksArrayRef = bl;

                    // set light array element to light
                    lightArrayRef = nn
                        ? neighbourSection!.getLight((x & 0xF), (y & 0xF), (z & 0xF))
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

    /**
     * Cube renderer with dynamic per-face textures based on metadata.
     */
    public void renderCubeDynamic(Block bl, int x, int y, int z, List<BlockVertexPacked> vertices, byte metadata) {
        // render each face with its own texture
        for (int faceIdx = 0; faceIdx < 6; faceIdx++) {
            var tex = bl.getTexture(faceIdx, metadata);
            var texm = tex + 1;

            if (forceTex.u >= 0 && forceTex.v >= 0) {
                tex = forceTex;
                texm = new UVPair(forceTex.u + 1, forceTex.v + 1);
            }

            var uvd = UVPair.texCoords(tex);
            var uvdm = UVPair.texCoords(texm);

            // render the specific face
            renderCubeFace(x, y, z, vertices, (RawDirection)faceIdx, uvd.X, uvd.Y, uvdm.X, uvdm.Y);
        }
    }

    /**
     * Cube renderer with dynamic per-face textures based on metadata. Also applies grass tinting.
     */
    public void renderGrass(Block bl, int x, int y, int z, List<BlockVertexPacked> vertices, byte metadata) {
        // render each face with its own texture
        for (int faceIdx = 0; faceIdx < 6; faceIdx++) {
            var tex = bl.getTexture(faceIdx, metadata);
            var texm = tex + 1;

            if (forceTex.u >= 0 && forceTex.v >= 0) {
                tex = forceTex;
                texm = new UVPair(forceTex.u + 1, forceTex.v + 1);
            }

            var uvd = UVPair.texCoords(tex);
            var uvdm = UVPair.texCoords(texm);
        }
    }

    /**
     * Cross renderer for plants and similar blocks.
     */
    public void renderCross(Block bl, int x, int y, int z, List<BlockVertexPacked> vertices, byte metadata) {
        var tex = bl.getTexture(0, metadata);
        var texm = tex + 1;

        if (forceTex.u >= 0 && forceTex.v >= 0) {
            tex = forceTex;
            texm = new UVPair(forceTex.u + 1, forceTex.v + 1);
        }

        var uvd = UVPair.texCoords(tex);
        var uvdm = UVPair.texCoords(texm);

        // first
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 0f, y + 1f, z + 0f, uvd.X, uvd.Y);
        vertex(x + 0f, y + 0f, z + 0f, uvd.X, uvdm.Y);
        vertex(x + 1f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
        vertex(x + 1f, y + 1f, z + 1f, uvdm.X, uvd.Y);
        end(vertices);

        // second
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 1f, y + 1f, z + 0f, uvd.X, uvd.Y);
        vertex(x + 1f, y + 0f, z + 0f, uvd.X, uvdm.Y);
        vertex(x + 0f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
        vertex(x + 0f, y + 1f, z + 1f, uvdm.X, uvd.Y);
        end(vertices);

        // do the backsides
        // first backside
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 1f, y + 1f, z + 1f, uvd.X, uvd.Y);
        vertex(x + 1f, y + 0f, z + 1f, uvd.X, uvdm.Y);
        vertex(x + 0f, y + 0f, z + 0f, uvdm.X, uvdm.Y);
        vertex(x + 0f, y + 1f, z + 0f, uvdm.X, uvd.Y);
        end(vertices);

        // second backside
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 0f, y + 1f, z + 1f, uvd.X, uvd.Y);
        vertex(x + 0f, y + 0f, z + 1f, uvd.X, uvdm.Y);
        vertex(x + 1f, y + 0f, z + 0f, uvdm.X, uvdm.Y);
        vertex(x + 1f, y + 1f, z + 0f, uvdm.X, uvd.Y);
        end(vertices);
    }

    /** 4-plane radial pattern, tapered inward at top */
    public void renderFire(Block bl, int x, int y, int z, List<BlockVertexPacked> vertices, byte metadata) {
        var tex = bl.getTexture(0, metadata);
        var texm = tex + 1;

        if (forceTex.u >= 0 && forceTex.v >= 0) {
            tex = forceTex;
            texm = new UVPair(forceTex.u + 1, forceTex.v + 1);
        }

        float inset = 0 / 16f;
        const float topInset = 4 / 16f;

        var uvd = UVPair.texCoords(tex);
        var uvdm = UVPair.texCoords(texm);

        applySimpleLightingNoDir();

        // check if block below can support fire (is solid)
        uint below = getBlockCached(0, -1, 0);
        bool b = Block.collision[below.getID()];

        if (b) {
            begin();
            vertex(x + 0f + inset, y + 1f, z + 1f - inset, uvd.X, uvd.Y);
            vertex(x + 0f, y + 0f, z + 0f, uvd.X, uvdm.Y);
            vertex(x + 1f, y + 0f, z + 0f, uvdm.X, uvdm.Y);
            vertex(x + 1f - inset, y + 1f, z + 1f - inset, uvdm.X, uvd.Y);
            endTwo(vertices);

            begin();
            vertex(x + 0f + inset, y + 1f, z + 0f + inset, uvd.X, uvd.Y);
            vertex(x + 0f, y + 0f, z + 1f, uvd.X, uvdm.Y);
            vertex(x + 1f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 1f - inset, y + 1f, z + 0f + inset, uvdm.X, uvd.Y);
            endTwo(vertices);

            begin();
            vertex(x + 1f - inset, y + 1f, z + 0f + inset, uvd.X, uvd.Y);
            vertex(x + 0f, y + 0f, z + 0f, uvd.X, uvdm.Y);
            vertex(x + 0f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 1f - inset, y + 1f, z + 1f - inset, uvdm.X, uvd.Y);
            endTwo(vertices);

            begin();
            vertex(x + 0f + inset, y + 1f, z + 0f + inset, uvd.X, uvd.Y);
            vertex(x + 1f, y + 0f, z + 0f, uvd.X, uvdm.Y);
            vertex(x + 1f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 0f + inset, y + 1f, z + 1f - inset, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        if (!b) {
            inset = 1 / 16f;
        }

        // north (+Z)
        uint block = getBlockCached(0, 0, 1);
        if (b || Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x, y + 1f, z + 1f - inset, uvd.X, uvd.Y);
            vertex(x, y + 0f, z + 1f, uvd.X, uvdm.Y);
            vertex(x + 1, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 1, y + 1f, z + 1f - inset, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // south (-Z)
        block = getBlockCached(0, 0, -1);
        if (b || Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x + 1, y + 1f, z + 0f + inset, uvd.X, uvd.Y);
            vertex(x + 1, y + 0f, z + 0f, uvd.X, uvdm.Y);
            vertex(x, y + 0f, z + 0f, uvdm.X, uvdm.Y);
            vertex(x, y + 1f, z + 0f + inset, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // west (-X)
        block = getBlockCached(-1, 0, 0);
        if (b || Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x + 0f + inset, y + 1f, z, uvd.X, uvd.Y);
            vertex(x + 0f, y + 0f, z, uvd.X, uvdm.Y);
            vertex(x + 0f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 0f + inset, y + 1f, z + 1f, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // east (+X)
        block = getBlockCached(1, 0, 0);
        if (b || Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x + 1f - inset, y + 1f, z + 1f, uvd.X, uvd.Y);
            vertex(x + 1f, y + 0f, z + 1f, uvd.X, uvdm.Y);
            vertex(x + 1f, y + 0f, z + 0f, uvdm.X, uvdm.Y);
            vertex(x + 1f - inset, y + 1f, z + 0f, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // up (+Y)
        block = getBlockCached(0, 1, 0);
        if (Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x, y + 1f - inset, z, uvd.X, uvd.Y);
            vertex(x, y + 1f - inset, z + 1, uvd.X, uvdm.Y);
            vertex(x + 1, y + 1f - inset, z + 1, uvdm.X, uvdm.Y);
            vertex(x + 1, y + 1f - inset, z, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // down (-Y)
        if (Block.flammable[below.getID()] > 0) {
            begin();
            vertex(x, y + inset, z, uvd.X, uvd.Y);
            vertex(x + 1, y + inset, z, uvdm.X, uvd.Y);
            vertex(x + 1, y + inset, z + 1, uvdm.X, uvdm.Y);
            vertex(x, y + inset, z + 1, uvd.X, uvdm.Y);
            endTwo(vertices);
        }
    }

    public static void renderTorchCube(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices,
        float x0, float y0, float z0, float x1, float y1, float z1,
        float su0, float sv0, float su1, float sv1,
        float tu0, float tv0, float tu1, float tv1,
        float bu0, float bv0, float bu1, float bv1,
        Vector3 ax = default, float angle = 0f, Vector3 pivot = default) {
        bool brot = angle != 0f;


        for (RawDirection i = 0; i < RawDirection.MAX; i++) {
            br.applySimpleLighting(RawDirection.NONE);


            Vector3 v0 = default;
            Vector3 v1 = default;
            Vector3 v2 = default;
            Vector3 v3 = default;

            float u0_ = 0;
            float v0_ = 0;
            float u1_ = 0;
            float v1_ = 0;

            switch (i) {
                case RawDirection.WEST:
                    v0 = new Vector3(x + x0, y + y1, z + z1);
                    v1 = new Vector3(x + x0, y + y0, z + z1);
                    v2 = new Vector3(x + x0, y + y0, z + z0);
                    v3 = new Vector3(x + x0, y + y1, z + z0);
                    u0_ = su0;
                    v0_ = sv0;
                    u1_ = su1;
                    v1_ = sv1;

                    break;
                case RawDirection.EAST:
                    v0 = new Vector3(x + x1, y + y1, z + z0);
                    v1 = new Vector3(x + x1, y + y0, z + z0);
                    v2 = new Vector3(x + x1, y + y0, z + z1);
                    v3 = new Vector3(x + x1, y + y1, z + z1);
                    u0_ = su0;
                    v0_ = sv0;
                    u1_ = su1;
                    v1_ = sv1;
                    break;
                case RawDirection.SOUTH:
                    v0 = new Vector3(x + x0, y + y1, z + z0);
                    v1 = new Vector3(x + x0, y + y0, z + z0);
                    v2 = new Vector3(x + x1, y + y0, z + z0);
                    v3 = new Vector3(x + x1, y + y1, z + z0);
                    u0_ = su0;
                    v0_ = sv0;
                    u1_ = su1;
                    v1_ = sv1;
                    break;
                case RawDirection.NORTH:
                    v0 = new Vector3(x + x1, y + y1, z + z1);
                    v1 = new Vector3(x + x1, y + y0, z + z1);
                    v2 = new Vector3(x + x0, y + y0, z + z1);
                    v3 = new Vector3(x + x0, y + y1, z + z1);
                    u0_ = su0;
                    v0_ = sv0;
                    u1_ = su1;
                    v1_ = sv1;
                    break;
                case RawDirection.DOWN:
                    v0 = new Vector3(x + x1, y + y0, z + z1);
                    v1 = new Vector3(x + x1, y + y0, z + z0);
                    v2 = new Vector3(x + x0, y + y0, z + z0);
                    v3 = new Vector3(x + x0, y + y0, z + z1);
                    u0_ = bu0;
                    v0_ = bv0;
                    u1_ = bu1;
                    v1_ = bv1;
                    break;
                case RawDirection.UP:
                    v0 = new Vector3(x + x0, y + y1, z + z1);
                    v1 = new Vector3(x + x0, y + y1, z + z0);
                    v2 = new Vector3(x + x1, y + y1, z + z0);
                    v3 = new Vector3(x + x1, y + y1, z + z1);
                    u0_ = tu0;
                    v0_ = tv0;
                    u1_ = tu1;
                    v1_ = tv1;
                    break;
            }

            v0 = Meth.transformVertex(v0.X, v0.Y, v0.Z, brot, pivot, angle, ax);
            v1 = Meth.transformVertex(v1.X, v1.Y, v1.Z, brot, pivot, angle, ax);
            v2 = Meth.transformVertex(v2.X, v2.Y, v2.Z, brot, pivot, angle, ax);
            v3 = Meth.transformVertex(v3.X, v3.Y, v3.Z, brot, pivot, angle, ax);

            br.begin();

            br.vertex(v0.X, v0.Y, v0.Z, u0_, v0_);
            br.vertex(v1.X, v1.Y, v1.Z, u0_, v1_);
            br.vertex(v2.X, v2.Y, v2.Z, u1_, v1_);
            br.vertex(v3.X, v3.Y, v3.Z, u1_, v0_);

            br.end(vertices);
        }
    }

    /**
     * Render a standing sign at the given position.
     */
    public void renderSign(int x, int y, int z, List<BlockVertexPacked> vertices, float u0, float v0, float u1, float v1, byte rot) {
        const float width = 1f;
        const float depth = 1f / 16f;
        const float y0 = 6 / 16f;
        const float y1 = 1f;

        const float cx = 0.5f;
        const float cz = 0.5f;

        float a = -rot * (float.Pi / 8.0f); // 22.5deg
        float ca = float.Cos(a);
        float sa = float.Sin(a);

        // ext
        const float hw = width / 2f;
        const float hd = depth / 2f;

        // corners
        const float x0 = cx - hw;
        const float x1 = cx + hw;
        const float z0 = cz - hd;
        const float z1 = cz + hd;

        rp(x0, z0, out float rx0z0, out float rz0z0);
        rp(x1, z0, out float rx1z0, out float rz1z0);
        rp(x0, z1, out float rx0z1, out float rz0z1);
        rp(x1, z1, out float rx1z1, out float rz1z1);

        // UVs
        const float height = y1 - y0;
        float du = u1 - u0;
        float dv = v1 - v0;

        // I fucked with the UVs yes. They very much don't match up. But it works for the wood. If you unwood this, adjust (or not, I don't think anyone will care)
        float fv1 = v0 + dv * height;
        float fu0 = u0 + du * depth;
        float eu1 = u1 - du * depth;
        float ev0 = v0 + dv * height;
        float ev1 = v1 - dv * depth;
        float dv0 = fv1;
        float dv1 = fv1 + dv * depth;


        // we cheat and we do it blatantly
        applySimpleLighting(RawDirection.EAST);

        // front
        begin();
        vertex(x + rx0z0, y + y1, z + rz0z0, u0, v0);
        vertex(x + rx0z0, y + y0, z + rz0z0, u0, fv1);
        vertex(x + rx1z0, y + y0, z + rz1z0, u1, fv1);
        vertex(x + rx1z0, y + y1, z + rz1z0, u1, v0);
        end(vertices);

        applySimpleLighting(RawDirection.DOWN);

        // back
        begin();
        vertex(x + rx1z1, y + y1, z + rz1z1, u0, v0);
        vertex(x + rx1z1, y + y0, z + rz1z1, u0, fv1);
        vertex(x + rx0z1, y + y0, z + rz0z1, u1, fv1);
        vertex(x + rx0z1, y + y1, z + rz0z1, u1, v0);
        end(vertices);

        applySimpleLighting(RawDirection.NORTH);

        // left
        begin();
        vertex(x + rx0z1, y + y1, z + rz0z1, u0, v0);
        vertex(x + rx0z1, y + y0, z + rz0z1, u0, ev0);
        vertex(x + rx0z0, y + y0, z + rz0z0, fu0, ev0);
        vertex(x + rx0z0, y + y1, z + rz0z0, fu0, v0);
        end(vertices);

        applySimpleLighting(RawDirection.SOUTH);

        // right
        begin();
        vertex(x + rx1z0, y + y1, z + rz1z0, eu1, v0);
        vertex(x + rx1z0, y + y0, z + rz1z0, eu1, ev0);
        vertex(x + rx1z1, y + y0, z + rz1z1, u1, ev0);
        vertex(x + rx1z1, y + y1, z + rz1z1, u1, v0);
        end(vertices);

        applySimpleLighting(RawDirection.UP);

        // top
        begin();
        vertex(x + rx0z0, y + y1, z + rz0z0, u0, dv0);
        vertex(x + rx1z0, y + y1, z + rz1z0, u1, dv0);
        vertex(x + rx1z1, y + y1, z + rz1z1, u1, dv1);
        vertex(x + rx0z1, y + y1, z + rz0z1, u0, dv1);
        end(vertices);

        applySimpleLighting(RawDirection.DOWN);

        // bottom
        begin();
        vertex(x + rx0z1, y + y0, z + rz0z1, u0, ev1);
        vertex(x + rx1z1, y + y0, z + rz1z1, u1, ev1);
        vertex(x + rx1z0, y + y0, z + rz1z0, u1, v1);
        vertex(x + rx0z0, y + y0, z + rz0z0, u0, v1);
        end(vertices);
        return;

        void rp(float px, float pz, out float rx, out float rz) {
            float dx = px - cx;
            float dz = pz - cz;
            rx = dx * ca - dz * sa + cx;
            rz = dx * sa + dz * ca + cz;
        }
    }

    /**
     * Render a single cube face with culling and lighting.
     */
    private void renderCubeFace(int x, int y, int z, List<BlockVertexPacked> vertices,
        RawDirection dir, float u0, float v0, float u1, float v1) {
        var vec = Direction.getDirection(dir);
        var nb = getBlockCached(vec.X, vec.Y, vec.Z).getID();

        // check if we should render based on neighbour
        if (Block.fullBlock[nb]) {
            return;
        }

        applyFaceLighting(dir);
        begin();

        switch (dir) {
            case RawDirection.WEST: // 0
                vertex(x + 0, y + 1, z + 1, u0, v0);
                vertex(x + 0, y + 0, z + 1, u0, v1);
                vertex(x + 0, y + 0, z + 0, u1, v1);
                vertex(x + 0, y + 1, z + 0, u1, v0);
                break;
            case RawDirection.EAST: // 1
                vertex(x + 1, y + 1, z + 0, u0, v0);
                vertex(x + 1, y + 0, z + 0, u0, v1);
                vertex(x + 1, y + 0, z + 1, u1, v1);
                vertex(x + 1, y + 1, z + 1, u1, v0);
                break;
            case RawDirection.SOUTH: // 2
                vertex(x + 0, y + 1, z + 0, u0, v0);
                vertex(x + 0, y + 0, z + 0, u0, v1);
                vertex(x + 1, y + 0, z + 0, u1, v1);
                vertex(x + 1, y + 1, z + 0, u1, v0);
                break;
            case RawDirection.NORTH: // 3
                vertex(x + 1, y + 1, z + 1, u0, v0);
                vertex(x + 1, y + 0, z + 1, u0, v1);
                vertex(x + 0, y + 0, z + 1, u1, v1);
                vertex(x + 0, y + 1, z + 1, u1, v0);
                break;
            case RawDirection.DOWN: // 4
                vertex(x + 1, y + 0, z + 1, u0, v0);
                vertex(x + 1, y + 0, z + 0, u0, v1);
                vertex(x + 0, y + 0, z + 0, u1, v1);
                vertex(x + 0, y + 0, z + 1, u1, v0);
                break;
            case RawDirection.UP: // 5
                vertex(x + 0, y + 1, z + 1, u0, v0);
                vertex(x + 0, y + 1, z + 0, u0, v1);
                vertex(x + 1, y + 1, z + 0, u1, v1);
                vertex(x + 1, y + 1, z + 1, u1, v0);
                break;
        }

        end(vertices);
    }

    /**
     * All-in-one chungus cube renderer. Claude helped me a bit with it so it's not very great but hey, if you don't need anything fancy, it's alright.
     * (Assumptions: the lighting is what you'd expect from the faces, you need a properly culled cube, you don't need extra tint, your texture maps 1:1 with world pixels)
     */
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public void renderCube(int x, int y, int z, List<BlockVertexPacked> vertices,
        float x0, float y0, float z0, float x1, float y1, float z1,
        float u0, float v0, float u1, float v1) {
        var ue = u1 - u0;
        var ve = v1 - v0;

        // WEST face
        var westUMin = u0 + ue * z0;
        var westUMax = u0 + ue * z1;
        var westVMin = v0 + ve * (1f - y1);
        var westVMax = v0 + ve * (1f - y0);
        var nb = getBlockCached(-1, 0, 0).getID();

        bool edge = x0 == 0f;
        bool render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.WEST);
            begin();
            vertex(x + x0, y + y1, z + z1, westUMin, westVMin);
            vertex(x + x0, y + y0, z + z1, westUMin, westVMax);
            vertex(x + x0, y + y0, z + z0, westUMax, westVMax);
            vertex(x + x0, y + y1, z + z0, westUMax, westVMin);
            end(vertices);
        }

        // EAST face
        var eastUMin = u0 + ue * z0;
        var eastUMax = u0 + ue * z1;
        var eastVMin = v0 + ve * (1f - y1);
        var eastVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(1, 0, 0).getID();

        edge = x1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.EAST);
            begin();
            vertex(x + x1, y + y1, z + z0, eastUMin, eastVMin);
            vertex(x + x1, y + y0, z + z0, eastUMin, eastVMax);
            vertex(x + x1, y + y0, z + z1, eastUMax, eastVMax);
            vertex(x + x1, y + y1, z + z1, eastUMax, eastVMin);
            end(vertices);
        }

        // SOUTH face
        var southUMin = u0 + ue * x0;
        var southUMax = u0 + ue * x1;
        var southVMin = v0 + ve * (1f - y1);
        var southVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(0, 0, -1).getID();

        edge = z0 == 0f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.SOUTH);
            begin();
            vertex(x + x0, y + y1, z + z0, southUMin, southVMin);
            vertex(x + x0, y + y0, z + z0, southUMin, southVMax);
            vertex(x + x1, y + y0, z + z0, southUMax, southVMax);
            vertex(x + x1, y + y1, z + z0, southUMax, southVMin);
            end(vertices);
        }

        // NORTH face
        var northUMin = u0 + ue * x0;
        var northUMax = u0 + ue * x1;
        var northVMin = v0 + ve * (1f - y1);
        var northVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(0, 0, 1).getID();

        edge = z1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.NORTH);
            begin();
            vertex(x + x1, y + y1, z + z1, northUMin, northVMin);
            vertex(x + x1, y + y0, z + z1, northUMin, northVMax);
            vertex(x + x0, y + y0, z + z1, northUMax, northVMax);
            vertex(x + x0, y + y1, z + z1, northUMax, northVMin);
            end(vertices);
        }

        // DOWN face
        var downUMin = u0 + ue * x0;
        var downUMax = u0 + ue * x1;
        var downVMin = v0 + ve * (1f - z1);
        var downVMax = v0 + ve * (1f - z0);
        nb = getBlockCached(0, -1, 0).getID();

        edge = y0 == 0f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.DOWN);
            begin();
            vertex(x + x1, y + y0, z + z1, downUMin, downVMin);
            vertex(x + x1, y + y0, z + z0, downUMin, downVMax);
            vertex(x + x0, y + y0, z + z0, downUMax, downVMax);
            vertex(x + x0, y + y0, z + z1, downUMax, downVMin);
            end(vertices);
        }

        // UP face
        var upUMin = u0 + ue * x0;
        var upUMax = u0 + ue * x1;
        var upVMin = v0 + ve * (1f - z1);
        var upVMax = v0 + ve * (1f - z0);
        nb = getBlockCached(0, 1, 0).getID();

        edge = y1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.UP);
            begin();
            vertex(x + x0, y + y1, z + z1, upUMin, upVMin);
            vertex(x + x0, y + y1, z + z0, upUMin, upVMax);
            vertex(x + x1, y + y1, z + z0, upUMax, upVMax);
            vertex(x + x1, y + y1, z + z1, upUMax, upVMin);
            end(vertices);
        }
    }

    public void renderSimpleCube(int x, int y, int z, List<BlockVertexPacked> vertices,
        float x0, float y0, float z0, float x1, float y1, float z1,
        float u0, float v0, float u1, float v1) {
        var ue = u1 - u0;
        var ve = v1 - v0;

        // WEST face
        var westUMin = u0 + ue * z0;
        var westUMax = u0 + ue * z1;
        var westVMin = v0 + ve * (1f - y1);
        var westVMax = v0 + ve * (1f - y0);
        var nb = getBlockCached(-1, 0, 0).getID();

        bool edge = x0 == 0f;
        bool render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.WEST);
            begin();
            vertex(x + x0, y + y1, z + z1, westUMin, westVMin);
            vertex(x + x0, y + y0, z + z1, westUMin, westVMax);
            vertex(x + x0, y + y0, z + z0, westUMax, westVMax);
            vertex(x + x0, y + y1, z + z0, westUMax, westVMin);
            end(vertices);
        }

        // EAST face
        var eastUMin = u0 + ue * z0;
        var eastUMax = u0 + ue * z1;
        var eastVMin = v0 + ve * (1f - y1);
        var eastVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(1, 0, 0).getID();

        edge = x1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.EAST);
            begin();
            vertex(x + x1, y + y1, z + z0, eastUMin, eastVMin);
            vertex(x + x1, y + y0, z + z0, eastUMin, eastVMax);
            vertex(x + x1, y + y0, z + z1, eastUMax, eastVMax);
            vertex(x + x1, y + y1, z + z1, eastUMax, eastVMin);
            end(vertices);
        }

        // SOUTH face
        var southUMin = u0 + ue * x0;
        var southUMax = u0 + ue * x1;
        var southVMin = v0 + ve * (1f - y1);
        var southVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(0, 0, -1).getID();

        edge = z0 == 0f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.SOUTH);
            begin();
            vertex(x + x0, y + y1, z + z0, southUMin, southVMin);
            vertex(x + x0, y + y0, z + z0, southUMin, southVMax);
            vertex(x + x1, y + y0, z + z0, southUMax, southVMax);
            vertex(x + x1, y + y1, z + z0, southUMax, southVMin);
            end(vertices);
        }

        // NORTH face
        var northUMin = u0 + ue * x0;
        var northUMax = u0 + ue * x1;
        var northVMin = v0 + ve * (1f - y1);
        var northVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(0, 0, 1).getID();

        edge = z1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.NORTH);
            begin();
            vertex(x + x1, y + y1, z + z1, northUMin, northVMin);
            vertex(x + x1, y + y0, z + z1, northUMin, northVMax);
            vertex(x + x0, y + y0, z + z1, northUMax, northVMax);
            vertex(x + x0, y + y1, z + z1, northUMax, northVMin);
            end(vertices);
        }

        // DOWN face
        var downUMin = u0 + ue * x0;
        var downUMax = u0 + ue * x1;
        var downVMin = v0 + ve * (1f - z1);
        var downVMax = v0 + ve * (1f - z0);
        nb = getBlockCached(0, -1, 0).getID();

        edge = y0 == 0f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.DOWN);
            begin();
            vertex(x + x1, y + y0, z + z1, downUMin, downVMin);
            vertex(x + x1, y + y0, z + z0, downUMin, downVMax);
            vertex(x + x0, y + y0, z + z0, downUMax, downVMax);
            vertex(x + x0, y + y0, z + z1, downUMax, downVMin);
            end(vertices);
        }

        // UP face
        var upUMin = u0 + ue * x0;
        var upUMax = u0 + ue * x1;
        var upVMin = v0 + ve * (1f - z1);
        var upVMax = v0 + ve * (1f - z0);
        nb = getBlockCached(0, 1, 0).getID();

        edge = y1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.UP);
            begin();
            vertex(x + x0, y + y1, z + z1, upUMin, upVMin);
            vertex(x + x0, y + y1, z + z0, upUMin, upVMax);
            vertex(x + x1, y + y1, z + z0, upUMax, upVMax);
            vertex(x + x1, y + y1, z + z1, upUMax, upVMin);
            end(vertices);
        }
    }

    // sorry for this mess
    [SkipLocalsInit]
    //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private unsafe void constructVertices(SubChunk subChunk, RenderLayer layer, List<BlockVertexPacked> vertices) {
        // clear arrays before starting
        chunkVertices.Clear();

        ref RenderContext.ArrayVertexCache tempVertices = ref ctx.vertexCache;
        Span<uint> nba = stackalloc uint[6];

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

        // setup blockrenderer
        setupWorld(smoothLighting, AO);

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

            // if water and we're opaque water and this is solid, do it anyway
            var opaqueWater = Settings.instance.opaqueWater && blockID == Block.WATER.id;
            if (blockID == 0 || (bl?.layer != layer && !opaqueWater)) {
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


            // if smooth lighting, fill cache
            // status update: we fill it regardless otherwise we crash lol
            fillCache(ref neighbourRef, ref lightRef);

            var wp = World.toWorldPos(subChunk.coord, x, y, z);

            renderBlockSwitch(bl, wp.X, wp.Y, wp.Z, neighbourRef.getMetadata(), vertices);

            if (Block.renderType[blockID] != RenderType.MODEL) {
                continue;
            }

            model: ;

            // if you get the faces BEFORE checking it's a model, it will crash on custom blocks
            ref Face facesRef = ref MemoryMarshal.GetArrayDataReference(bl.model.faces);

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

                        FourBytes o;
                        getDirectionOffsetsAndData(dir, lb, out light, out o);

                        // if no smooth lighting, leave it be! we've just calculated a bunch of useless stuff but i dont wanna create another vaguely similar function lol
                        if (!smoothLighting) {
                            // simple lighting - uniform for all vertices
                            light = new FourBytes(lb, lb, lb, lb);
                        }

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

                    if (forceTex.u >= 0 && forceTex.v >= 0) {
                        tex = Vector128.Create(forceTex.u, forceTex.v, forceTex.u + 1, forceTex.v + 1);
                    }

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
    public unsafe void fillCache(ref uint neighbourRef, ref byte lightRef) {
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
                    ctx.blockCache[index] = Unsafe.Add(ref neighbourRef, ny * Chunk.CHUNKSIZEEXSQ + nz * Chunk.CHUNKSIZEEX + nx);
                    ctx.lightCache[index] = Unsafe.Add(ref lightRef, ny * Chunk.CHUNKSIZEEXSQ + nz * Chunk.CHUNKSIZEEX + nx);
                }
            }
        }
    }

    public unsafe void fillCacheEmpty() {
        // fill the cache with empty blocks
        for (int y = 0; y < LOCALCACHESIZE; y++) {
            for (int z = 0; z < LOCALCACHESIZE; z++) {
                for (int x = 0; x < LOCALCACHESIZE; x++) {
                    // calculate the index in the cache
                    int index = y * LOCALCACHESIZE_SQ + z * LOCALCACHESIZE + x;
                    // set the block and light value to empty
                    ctx.blockCache[index] = 0;
                    ctx.lightCache[index] = 15;
                }
            }
        }
    }

    public unsafe void fillCacheStandalone(uint block, byte light) {
        // fill the cache with the given light value
        for (int y = 0; y < LOCALCACHESIZE; y++) {
            for (int z = 0; z < LOCALCACHESIZE; z++) {
                for (int x = 0; x < LOCALCACHESIZE; x++) {
                    int index = y * LOCALCACHESIZE_SQ + z * LOCALCACHESIZE + x;
                    ctx.blockCache[index] = 0;
                    ctx.lightCache[index] = light;
                }
            }
        }

        // set the centre block to the given block
        ctx.blockCache[13] = block;
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
}

public enum VertexConstructionMode {
    OPAQUE,
    TRANSLUCENT
}

[StructLayout(LayoutKind.Explicit)]
public struct FourShorts {
    // for overlap
    [FieldOffset(0)] public unsafe fixed ushort ushorts[4];

    [FieldOffset(0)] public ulong Whole;
    [FieldOffset(0)] public ushort First;
    [FieldOffset(2)] public ushort Second;
    [FieldOffset(4)] public ushort Third;
    [FieldOffset(6)] public ushort Fourth;
}

[StructLayout(LayoutKind.Explicit)]
public struct FourBytes {
    // for overlap
    [FieldOffset(0)] public unsafe fixed byte bytes[4];

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