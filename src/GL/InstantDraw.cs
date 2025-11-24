using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.world;
using JetBrains.Annotations;
using Silk.NET.OpenGL.Legacy;
using PrimitiveType = Silk.NET.OpenGL.Legacy.PrimitiveType;

namespace BlockGame.GL;

[PublicAPI]
// Define fog types as an enum for better code readability
public enum FogType {
    Linear = 0,
    Exp = 1,
    Exp2 = 2
}

public abstract class InstantDraw<T> where T : unmanaged {
    protected PrimitiveType vertexType;

    public int maxVertices;
    protected readonly List<T> vertices;
    public int currentVertex = 0;

    protected readonly Silk.NET.OpenGL.Legacy.GL GL;

    protected uint VAO;
    protected uint VBO;


    protected Shader instantShader;
    protected int uMVP;
    protected int uModelView;
    protected int uModel;
    protected int uFogColour;
    protected int uFogStart;
    protected int uFogEnd;
    protected int uFogEnabled;
    protected int uFogType; // Added for fog type
    protected int uFogDensity; // Added for exp/exp2 fog

    // Fog settings
    protected Vector4 fogColour = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
    protected float fogStart = 10.0f;
    protected float fogEnd = 100.0f;
    protected bool fogEnabled = false; // Disabled by default
    protected FogType fogType = FogType.Linear;
    protected float fogDensity = 0.01f;

    // dirty tracking for fog
    protected bool fogDirty = true;
    protected bool modelDirty = true;
    protected bool viewDirty = true;
    protected bool projDirty = true;

    // vertex tint
    public Color tint = Color.White;

    // Matrix components for automatic matrix computation
    protected MatrixStack? modelMatrix;
    protected Matrix4x4 viewMatrix = Matrix4x4.Identity;
    protected Matrix4x4 projMatrix = Matrix4x4.Identity;


    public InstantDraw(int maxVertices) {
        vertices = new List<T>(maxVertices);
        this.maxVertices = maxVertices;
        GL = Game.GL;
    }

    public virtual void setup() {
        unsafe {
            VAO = GL.CreateVertexArray();
            VBO = GL.CreateBuffer();
            Game.graphics.vao(VAO);
            Game.graphics.vertex(VBO);
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(maxVertices * sizeof(T)), (void*)0,
                BufferStorageMask.DynamicStorageBit);
            format();
        }
    }

    public abstract void format();

    protected void resizeStorage() {
        var newMaxVertices = maxVertices * 2;

        unsafe {
            // delete old buffer
            GL.DeleteBuffer(VBO);

            // create new buffer with double capacity
            VBO = GL.CreateBuffer();
            Game.graphics.vao(VAO);
            Game.graphics.vertex(VBO);
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(newMaxVertices * sizeof(T)), (void*)0,
                BufferStorageMask.DynamicStorageBit);

            maxVertices = newMaxVertices;


            format();
        }
    }

    // Fog control methods
    public void enableFog(bool enable) {
        if (fogEnabled != enable) {
            fogEnabled = enable;
            fogDirty = true;
        }
    }

    public void fogColor(Vector4 color) {
        if (fogColour != color) {
            fogColour = color;
            fogDirty = true;
        }
    }

    public void fogDistance(float start, float end) {
        if (fogStart != start || fogEnd != end) {
            fogStart = start;
            fogEnd = end;
            fogDirty = true;
        }
    }

    // New methods for fog type and density
    public void setFogType(FogType type) {
        if (fogType != type) {
            fogType = type;
            fogDirty = true;
        }
    }

    public void setFogDensity(float density) {
        if (!fogDensity.Equals(density)) {
            fogDensity = density;
            fogDirty = true;
        }
    }

    /** calculate exp2 fog density from target distance where fog should be opaque
     * @param distance where fog reaches ~99% opacity
     * @return density value for exp2 fog */
    public static float calcExp2FogDensity(float distance, float threshold = 0.01f) {
        return float.Sqrt(-float.Log(threshold)) / distance;
    }

    /** calculate exp fog density from target distance where fog should be opaque
     * @param distance where fog reaches ~99% opacity
     * @return density value for exp fog */
    public static float calcExpFogDensity(float distance, float threshold = 0.01f) {
        return -float.Log(threshold) / distance;
    }

    public void setColour(Color c) {
        tint = c;
    }

    public void setMV(ref Matrix4x4 modelView) {
        modelMatrix = null; // Disable automatic matrix computation
        instantShader.setUniform(uModelView, ref modelView);
        modelDirty = false;
        viewDirty = false;
    }

    public void setMVP(ref Matrix4x4 mvp) {
        modelMatrix = null; // Disable automatic matrix computation
        instantShader.setUniform(uMVP, ref mvp);
        modelDirty = false;
        viewDirty = false;
        projDirty = false;
    }

    public void model(MatrixStack? m) {
        modelMatrix = m;
        modelDirty = true;
    }

    public void view(Matrix4x4 v) {
        viewMatrix = v;
        viewDirty = true;
    }

    public void proj(Matrix4x4 p) {
        projMatrix = p;
        projDirty = true;
    }

    public void applyMat() {
        //instantShader.use();
        // Compute final matrices from components if available
        if (modelMatrix != null && (modelDirty || viewDirty || projDirty)) {
            var model = modelMatrix.top;
            var mv = model * viewMatrix;
            var mvp = model * viewMatrix * projMatrix;
            if (uModel != -1) {
                instantShader.setUniform(uModel, ref model);
            }

            instantShader.setUniform(uModelView, ref mv);
            instantShader.setUniform(uMVP, ref mvp);
            modelDirty = false;
            viewDirty = false;
            projDirty = false;
        }
    }

    /// <summary>
    /// Set the primitive mode for the instant draw.
    /// When set to quads, it will use triangles on the OpenGL side - but it will be transparently handled by using the shared indices.
    /// </summary>
    public void begin(PrimitiveType type) {
        currentVertex = 0;
        vertices.Clear();
        vertexType = type;
    }

    public virtual void addVertex(T vertex) {
        // resize VBO before adding if needed
        if (currentVertex >= maxVertices) {
            resizeStorage();
        }

        vertices.Add(vertex);
        currentVertex++;
    }

    public void end() {
        // nothing to do
        if (currentVertex == 0) {
            return;
        }

        Game.graphics.vao(VAO);

        // Apply current fog settings
        instantShader.use();

        // Compute final matrices from components if available
        if (modelMatrix != null && (modelDirty || viewDirty || projDirty)) {
            var model = modelMatrix.top;
            var mv = model * viewMatrix;
            var mvp = model * viewMatrix * projMatrix;

            if (uModel != -1) instantShader.setUniform(uModel, ref model);
            instantShader.setUniform(uModelView, ref mv);
            instantShader.setUniform(uMVP, ref mvp);
            modelDirty = false;
            viewDirty = false;
            projDirty = false;
        }

        if (fogDirty) {
            if (fogEnabled) {
                instantShader.setUniform(uFogEnabled, true);
                instantShader.setUniform(uFogColour, fogColour);
                instantShader.setUniform(uFogStart, fogStart);
                instantShader.setUniform(uFogEnd, fogEnd);
                instantShader.setUniform(uFogType, (int)fogType);
                instantShader.setUniform(uFogDensity, fogDensity);
            }
            else {
                instantShader.setUniform(uFogEnabled, false);
            }
            fogDirty = false;
        }

        // Upload buffer
        unsafe {
            Game.graphics.vertex(VBO);
            Game.GL.InvalidateBufferData(VBO);
            fixed (T* v = CollectionsMarshal.AsSpan(vertices)) {
                GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(currentVertex * sizeof(T)), v);
            }
        }

        // handle the vertex type
        var effectiveMode = vertexType;
        if (vertexType == PrimitiveType.Quads) {
            unsafe {
                effectiveMode = PrimitiveType.Triangles;
                Game.graphics.index(Game.graphics.fatQuadIndices);
                GL.DrawElements(effectiveMode, (uint)(currentVertex * (6 / 4f)), DrawElementsType.UnsignedInt,
                    (void*)0);
            }
        }
        else {
            GL.DrawArrays(effectiveMode, 0, (uint)currentVertex);
        }

        vertices.Clear();
        currentVertex = 0;
    }
}

public class InstantDrawTexture(int maxVertices) : InstantDraw<BlockVertexTinted>(maxVertices) {
    public int instantTexture;

    public bool reused = false;

    public override void setup() {
        base.setup();
        instantShader = Game.graphics.instantTextureShader;
        instantTexture = instantShader.getUniformLocation("tex");
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
        try {
            uModel = instantShader.getUniformLocation(nameof(uModel));
        }
        catch (InputException e) {
            uModel = -1;
        }

        uFogColour = instantShader.getUniformLocation(nameof(fogColour));
        uFogStart = instantShader.getUniformLocation(nameof(fogStart));
        uFogEnd = instantShader.getUniformLocation(nameof(fogEnd));
        uFogEnabled = instantShader.getUniformLocation(nameof(fogEnabled));
        uFogType = instantShader.getUniformLocation(nameof(fogType));
        uFogDensity = instantShader.getUniformLocation(nameof(fogDensity));
        instantShader.setUniform(instantTexture, 0);

        // Set default fog type
        instantShader.setUniform(uFogType, (int)FogType.Linear);
        instantShader.setUniform(uFogDensity, fogDensity);
    }

    public override void format() {
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 2, VertexAttribType.Float, false, 0 + 6 * sizeof(ushort));
        GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 10 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);

        GL.BindVertexBuffer(0, VBO, 0, 12 * sizeof(ushort));
    }

    public void setTexture(BTexture2D texture) {
        //instantShader.use();
        Game.graphics.tex(0, texture);
    }

    public override void addVertex(BlockVertexTinted vertex) {
        // resize VBO before adding if needed
        if (currentVertex >= maxVertices - 1) {
            resizeStorage();
        }

        // apply tint
        vertex.r = (byte)((vertex.r * tint.R) * (1 / 255f));
        vertex.g = (byte)((vertex.g * tint.G) * (1 / 255f));
        vertex.b = (byte)((vertex.b * tint.B) * (1 / 255f));
        vertex.a = (byte)((vertex.a * tint.A) * (1 / 255f));

        vertices.Add(vertex);
        currentVertex++;
    }
}

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public static class ListHack<T> {
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
    public static extern ref T[] getItems(List<T> list);
}

public class InstantDrawColour(int maxVertices) : InstantDraw<VertexTinted>(maxVertices) {
    public override void setup() {
        base.setup();
        instantShader = Game.graphics.instantColourShader;
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
        try {
            uModel = instantShader.getUniformLocation(nameof(uModel));
        }
        catch (InputException e) {
            uModel = -1;
        }

        uFogColour = instantShader.getUniformLocation(nameof(fogColour));
        uFogStart = instantShader.getUniformLocation(nameof(fogStart));
        uFogEnd = instantShader.getUniformLocation(nameof(fogEnd));
        uFogEnabled = instantShader.getUniformLocation(nameof(fogEnabled));
        uFogType = instantShader.getUniformLocation(nameof(fogType));
        uFogDensity = instantShader.getUniformLocation(nameof(fogDensity));


        // Initialise fog as disabled
        instantShader.setUniform(uFogEnabled, false);

        // Set default fog type
        instantShader.setUniform(uFogType, (int)FogType.Linear);
        instantShader.setUniform(uFogDensity, fogDensity);
    }

    public override void format() {
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 4, VertexAttribType.UnsignedByte, true, 0 + 6 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);

        GL.BindVertexBuffer(0, VBO, 0, 8 * sizeof(ushort));
    }

    public override void addVertex(VertexTinted vertex) {
        // resize VBO before adding if needed
        if (currentVertex >= maxVertices - 1) {
            resizeStorage();
        }

        // apply tint
        vertex.r = (byte)((vertex.r * tint.R) * (1 / 255f));
        vertex.g = (byte)((vertex.g * tint.G) * (1 / 255f));
        vertex.b = (byte)((vertex.b * tint.B) * (1 / 255f));
        vertex.a = (byte)((vertex.a * tint.A) * (1 / 255f));

        vertices.Add(vertex);
        currentVertex++;
    }
}

public class FastInstantDrawTexture : InstantDraw<BlockVertexTinted> {
    public int instantTexture;

    // 4-region ring buffer to avoid sync
    private int currentRegion = 0;
    private int regionOffset = 0;

    // the cake is a lie
    // the NV dualcore driver has another frame worth of buffering, so this WILL flicker. Solution? use 4 regions instead.
    // note: if someone complains about flickering, increase this number
    // we explicitly don't sync shit and don't map shit coherently either.
    // the good news is that this is very fast, the bad news is that if the buffer isn't large enough, well, bad things happen:tm:
    // NOTE: we force-disabled it lol
    // if we don't, change it back!
    //private const int regionCount = 4;
    private static int regionCount = 3;

    // persistent mapped pointer
    private unsafe BlockVertexTinted* mappedPtr = null;

    public FastInstantDrawTexture(int maxVertices) : base(maxVertices) {
        if (Game.isIntegratedCard) {
            regionCount = 4;
        }
    }

    public override void setup() {
        // allocate 3x buffer for ring buffering with persistent mapping
        unsafe {
            VAO = GL.CreateVertexArray();
            VBO = GL.CreateBuffer();
            Game.graphics.vao(VAO);
            GL.NamedBufferStorage(VBO, (uint)(maxVertices * regionCount * sizeof(BlockVertexTinted)), (void*)0,
                //BufferStorageMask.MapPersistentBit | BufferStorageMask.MapWriteBit | BufferStorageMask.MapCoherentBit);
                BufferStorageMask.MapPersistentBit | BufferStorageMask.MapWriteBit);

            // map persistently
            mappedPtr = (BlockVertexTinted*)GL.MapNamedBufferRange(VBO, 0,
                (nuint)(maxVertices * regionCount * sizeof(BlockVertexTinted)),
                (uint)(MapBufferAccessMask.PersistentBit |
                       MapBufferAccessMask.WriteBit |
                       //MapBufferAccessMask.CoherentBit |
                       MapBufferAccessMask.InvalidateBufferBit |
                       MapBufferAccessMask.UnsynchronizedBit |
                       MapBufferAccessMask.FlushExplicitBit));

            format();

            Log.info($"Initialized FastInstantDrawTexture with {maxVertices} vertices per region, {regionCount} regions ({maxVertices * regionCount} total).");
        }

        instantShader = Game.graphics.instantTextureShader;
        instantTexture = instantShader.getUniformLocation("tex");
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
        try {
            uModel = instantShader.getUniformLocation(nameof(uModel));
        }
        catch (InputException e) {
            uModel = -1;
        }

        uFogColour = instantShader.getUniformLocation(nameof(fogColour));
        uFogStart = instantShader.getUniformLocation(nameof(fogStart));
        uFogEnd = instantShader.getUniformLocation(nameof(fogEnd));
        uFogEnabled = instantShader.getUniformLocation(nameof(fogEnabled));
        uFogType = instantShader.getUniformLocation(nameof(fogType));
        uFogDensity = instantShader.getUniformLocation(nameof(fogDensity));
        instantShader.setUniform(instantTexture, 0);

        // Set default fog type
        instantShader.setUniform(uFogType, (int)FogType.Linear);
        instantShader.setUniform(uFogDensity, fogDensity);
    }

    public override void format() {
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 2, VertexAttribType.Float, false, 0 + 6 * sizeof(ushort));
        GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 10 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);

        GL.BindVertexBuffer(0, VBO, 0, 12 * sizeof(ushort));
    }

    public void setTexture(BTexture2D texture) {
        //instantShader.use();
        Game.graphics.tex(0, texture);
    }

    public new void begin(PrimitiveType type) {
        currentVertex = 0;
        vertexType = type;
    }

    public override void addVertex(BlockVertexTinted vertex) {
        // resize VBO before adding if needed
        if (currentVertex >= maxVertices - 1) {
            resizeStorage();
        }

        // apply tint
        vertex.r = (byte)((vertex.r * tint.R) * (1 / 255f));
        vertex.g = (byte)((vertex.g * tint.G) * (1 / 255f));
        vertex.b = (byte)((vertex.b * tint.B) * (1 / 255f));
        vertex.a = (byte)((vertex.a * tint.A) * (1 / 255f));

        // write directly to mapped ptr with region offset
        unsafe {
            mappedPtr[regionOffset + currentVertex++] = vertex;
        }
    }

    protected new void resizeStorage() {
        var newMaxVertices = maxVertices * 2;

        unsafe {
            // unmap old buffer
            if (mappedPtr != null) {
                GL.UnmapNamedBuffer(VBO);
                mappedPtr = null;
            }

            // delete old buffer
            GL.DeleteBuffer(VBO);

            // create new buffer with 3x capacity for ring buffering
            VBO = GL.CreateBuffer();
            Game.graphics.vao(VAO);
            GL.NamedBufferStorage(VBO, (uint)(newMaxVertices * regionCount * sizeof(BlockVertexTinted)), (void*)0,
                //BufferStorageMask.MapPersistentBit | BufferStorageMask.MapWriteBit | BufferStorageMask.MapCoherentBit);
                BufferStorageMask.MapPersistentBit | BufferStorageMask.MapWriteBit);

            // remap persistently
            mappedPtr = (BlockVertexTinted*)GL.MapNamedBufferRange(VBO, 0,
                (nuint)(newMaxVertices * regionCount * sizeof(BlockVertexTinted)),
                (uint)(MapBufferAccessMask.PersistentBit |
                       MapBufferAccessMask.WriteBit |
                       //MapBufferAccessMask.CoherentBit |
                       MapBufferAccessMask.InvalidateBufferBit |
                       MapBufferAccessMask.UnsynchronizedBit |
                       MapBufferAccessMask.FlushExplicitBit));

            maxVertices = newMaxVertices;

            Log.info($"Resized FastInstantDrawTexture to {maxVertices} vertices per region.");

            format();
        }

        // DON'T reset region - new buffer is independent, keep rotating
    }

    /** Pre-reserve capacity for known vertex count - zero overhead addVertex after this */
    public void reserve(int count) {
        if (count <= maxVertices) {
            return;
        }

        // find next power of 2
        int newSize = maxVertices;
        while (newSize < count) newSize *= 2;

        // resize once!
        maxVertices = newSize;
        resizeStorage();
    }

    public void reserve(int count, int mult) {
        // todo
    }

    public void addVertexE(BlockVertexTinted vertex) {
        // apply tint
        vertex.r = (byte)((vertex.r * tint.R) * (1 / 255f));
        vertex.g = (byte)((vertex.g * tint.G) * (1 / 255f));
        vertex.b = (byte)((vertex.b * tint.B) * (1 / 255f));
        vertex.a = (byte)((vertex.a * tint.A) * (1 / 255f));

        // write directly to mapped ptr with region offset
        unsafe {
            mappedPtr[regionOffset + currentVertex++] = vertex;
        }
    }

    public ref BlockVertexTinted getRefE() {
        // get ref directly from mapped ptr with region offset
        unsafe {
            return ref mappedPtr[regionOffset + currentVertex++];
        }
    }

    public new void end() {
        if (currentVertex == 0) return;

        Game.graphics.vao(VAO);
        instantShader.use();

        // set up matrices/fog
        if (modelMatrix != null && (modelDirty || viewDirty || projDirty)) {
            var model = modelMatrix.top;
            var mv = model * viewMatrix;
            var mvp = model * viewMatrix * projMatrix;

            if (uModel != -1) instantShader.setUniform(uModel, ref model);
            instantShader.setUniform(uModelView, ref mv);
            instantShader.setUniform(uMVP, ref mvp);
            modelDirty = false;
            viewDirty = false;
            projDirty = false;
        }

        if (fogDirty) {
            if (fogEnabled) {
                instantShader.setUniform(uFogEnabled, true);
                instantShader.setUniform(uFogColour, fogColour);
                instantShader.setUniform(uFogStart, fogStart);
                instantShader.setUniform(uFogEnd, fogEnd);
                instantShader.setUniform(uFogType, (int)fogType);
                instantShader.setUniform(uFogDensity, fogDensity);
            }
            else {
                instantShader.setUniform(uFogEnabled, false);
            }
            fogDirty = false;
        }

        // data already in GPU via mapped pointer, just draw
        regionOffset = currentRegion * maxVertices;

        var effectiveMode = vertexType;
        if (vertexType == PrimitiveType.Quads) {
            unsafe {
                effectiveMode = PrimitiveType.Triangles;
                Game.graphics.index(Game.graphics.fatQuadIndices);
                Game.GL.DrawElementsBaseVertex(effectiveMode, (uint)(currentVertex * (6 / 4f)),
                    DrawElementsType.UnsignedInt,
                    (void*)0, regionOffset);
            }
        }
        else {
            GL.DrawArrays(effectiveMode, regionOffset, (uint)currentVertex);
        }

        currentVertex = 0;
        // advance to next region
        currentRegion = (currentRegion + 1) % regionCount;
        regionOffset = currentRegion * maxVertices;
    }

    /**
     * Draw but don't throw the verts away.
     */
    public void endReuse(bool last) {
        // nothing to do
        if (currentVertex == 0) {
            return;
        }

        Game.graphics.vao(VAO);

        // Apply current fog settings
        instantShader.use();

        // Compute final matrices from components if available
        if (modelMatrix != null && (modelDirty || viewDirty || projDirty)) {
            var model = modelMatrix.top;
            var mv = model * viewMatrix;
            var mvp = model * viewMatrix * projMatrix;

            if (uModel != -1) instantShader.setUniform(uModel, ref model);
            instantShader.setUniform(uModelView, ref mv);
            instantShader.setUniform(uMVP, ref mvp);
            modelDirty = false;
            viewDirty = false;
            projDirty = false;
        }

        if (fogDirty) {
            if (fogEnabled) {
                instantShader.setUniform(uFogEnabled, true);
                instantShader.setUniform(uFogColour, fogColour);
                instantShader.setUniform(uFogStart, fogStart);
                instantShader.setUniform(uFogEnd, fogEnd);
                instantShader.setUniform(uFogType, (int)fogType);
                instantShader.setUniform(uFogDensity, fogDensity);
            }
            else {
                instantShader.setUniform(uFogEnabled, false);
            }
            fogDirty = false;
        }

        // data is already in GPU memory via mapped pointer, just draw
        regionOffset = currentRegion * maxVertices;

        var effectiveMode = vertexType;
        if (vertexType == PrimitiveType.Quads) {
            unsafe {
                effectiveMode = PrimitiveType.Triangles;
                Game.graphics.index(Game.graphics.fatQuadIndices);
                Game.GL.DrawElementsBaseVertex(effectiveMode, (uint)(currentVertex * (6 / 4f)),
                    DrawElementsType.UnsignedInt,
                    (void*)0, regionOffset);
            }
        }
        else {
            GL.DrawArrays(effectiveMode, regionOffset, (uint)currentVertex);
        }

        if (last) {
            currentVertex = 0;
            // advance to next region
            currentRegion = (currentRegion + 1) % regionCount;
            regionOffset = currentRegion * maxVertices;
        }
    }

    /** Upload to GPU without rendering - for batched range rendering */
    public void reuseUpload() {
        if (currentVertex == 0) return;

        Game.graphics.vao(VAO);
        instantShader.use();

        // set up matrices/fog
        if (modelMatrix != null && (modelDirty || viewDirty || projDirty)) {
            var model = modelMatrix.top;
            var mv = model * viewMatrix;
            var mvp = model * viewMatrix * projMatrix;

            if (uModel != -1) instantShader.setUniform(uModel, ref model);
            instantShader.setUniform(uModelView, ref mv);
            instantShader.setUniform(uMVP, ref mvp);
            modelDirty = false;
            viewDirty = false;
            projDirty = false;
        }

        if (fogDirty) {
            if (fogEnabled) {
                instantShader.setUniform(uFogEnabled, true);
                instantShader.setUniform(uFogColour, fogColour);
                instantShader.setUniform(uFogStart, fogStart);
                instantShader.setUniform(uFogEnd, fogEnd);
                instantShader.setUniform(uFogType, (int)fogType);
                instantShader.setUniform(uFogDensity, fogDensity);
            }
            else {
                instantShader.setUniform(uFogEnabled, false);
            }
            fogDirty = false;
        }

        // data already in GPU via mapped pointer - no upload needed
    }

    /** Draw a range of vertices from the uploaded buffer */
    public void renderRange(int offset, int count) {
        if (count == 0) return;

        // use existing regionOffset (set at end of previous frame / in reuseUpload!!)
        int finalOffset = regionOffset + offset;

        var effectiveMode = vertexType;
        if (vertexType == PrimitiveType.Quads) {
            unsafe {
                effectiveMode = PrimitiveType.Triangles;
                Game.graphics.index(Game.graphics.fatQuadIndices);
                uint indexCount = (uint)(count * (6 / 4f));
                Game.GL.DrawElementsBaseVertex(effectiveMode, indexCount, DrawElementsType.UnsignedInt,
                    (void*)0, finalOffset);
            }
        }
        else {
            GL.DrawArrays(effectiveMode, finalOffset, (uint)count);
        }

        //Console.Out.WriteLine(maxVertices);
        //Console.Out.WriteLine("C:" + currentVertex);
    }

    /** Cleanup after range rendering */
    public void finishReuse() {
        currentVertex = 0;
        // advance to next region
        currentRegion = (currentRegion + 1) % regionCount;
        regionOffset = currentRegion * maxVertices;
    }
}

public class InstantDrawEntity(int maxVertices) : InstantDraw<EntityVertex>(maxVertices) {
    protected int uLightDir1;
    protected int uLightRatio1;
    protected int uLightDir2;
    protected int uLightRatio2;

    // from above!!
    // what if light came from 45deg so it only shades off-grid shit
    protected Vector3 lightDir = Vector3.Normalize(new Vector3(1.0f, 1.5f, 1.0f));
    protected Vector3 lightDir2 = Vector3.Normalize(new Vector3(-1.0f, 1.5f, -1.0f));

    /** The ratio between direct and ambient light. 1 = only direct, 0 = only ambient
     *  TODO we don't have ambient light colouring or any of that shit, rn it's just a simple lerp
     */
    protected const float lightRatio = Meth.psiF + (Meth.kappaF - Meth.sqrt2F) * Meth.sqrt2F;

    public override void setup() {
        base.setup();
        instantShader = Game.graphics.instantEntityShader;
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
        try {
            uModel = instantShader.getUniformLocation(nameof(uModel));
        }
        catch (InputException e) {
            uModel = -1;
        }

        uFogColour = instantShader.getUniformLocation(nameof(fogColour));
        uFogStart = instantShader.getUniformLocation(nameof(fogStart));
        uFogEnd = instantShader.getUniformLocation(nameof(fogEnd));
        uFogEnabled = instantShader.getUniformLocation(nameof(fogEnabled));
        uFogType = instantShader.getUniformLocation(nameof(fogType));
        uFogDensity = instantShader.getUniformLocation(nameof(fogDensity));
        uLightDir1 = instantShader.getUniformLocation(nameof(lightDir));
        uLightRatio1 = instantShader.getUniformLocation(nameof(lightRatio));
        uLightDir2 = instantShader.getUniformLocation("lightDir2");
        uLightRatio2 = instantShader.getUniformLocation("lightRatio2");

        // Initialize fog as disabled
        instantShader.setUniform(uFogEnabled, false);

        // Set default fog type
        instantShader.setUniform(uFogType, (int)FogType.Linear);
        instantShader.setUniform(uFogDensity, fogDensity);

        // Set default lighting
        instantShader.setUniform(uLightDir1, lightDir);
        instantShader.setUniform(uLightRatio1, lightRatio);

        instantShader.setUniform(uLightDir2, lightDir2);
        instantShader.setUniform(uLightRatio2, lightRatio);
    }

    public override void format() {
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);
        GL.EnableVertexAttribArray(3);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 2, VertexAttribType.Float, false, 0 + 6 * sizeof(ushort));
        GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 10 * sizeof(ushort));
        GL.VertexAttribFormat(3, 4, VertexAttribType.Int2101010Rev, true, 0 + 12 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);
        GL.VertexAttribBinding(3, 0);

        GL.BindVertexBuffer(0, VBO, 0, 14 * sizeof(ushort));
    }

    public override void addVertex(EntityVertex vertex) {
        // resize VBO before adding if needed
        if (currentVertex >= maxVertices - 1) {
            resizeStorage();
        }

        // apply tint
        vertex.r = (byte)((vertex.r * tint.R) * (1 / 255f));
        vertex.g = (byte)((vertex.g * tint.G) * (1 / 255f));
        vertex.b = (byte)((vertex.b * tint.B) * (1 / 255f));
        vertex.a = (byte)((vertex.a * tint.A) * (1 / 255f));

        vertices.Add(vertex);
        currentVertex++;
    }
}

public class FastInstantDrawEntity : InstantDraw<EntityVertex> {
    public int instantTexture;

    protected int uLightDir1;
    protected int uLightRatio1;
    protected int uLightDir2;
    protected int uLightRatio2;

    // from above!!
    // what if light came from 45deg so it only shades off-grid shit
    protected Vector3 lightDir = Vector3.Normalize(new Vector3(1.0f, 1.5f, 1.0f));
    protected Vector3 lightDir2 = Vector3.Normalize(new Vector3(-1.0f, 1.5f, -1.0f));

    /** The ratio between direct and ambient light. 1 = only direct, 0 = only ambient
     *  TODO we don't have ambient light colouring or any of that shit, rn it's just a simple lerp
     */
    protected const float lightRatio = Meth.psiF + (Meth.kappaF - Meth.sqrt2F) * Meth.sqrt2F;

    // fixed-region ring buffer with explicit frame boundaries
    private int currentRegion = 0;
    private int writePosition = 0;  // position within current region
    private int peak = 0;
    private static int regionCount = 3;

    // persistent mapped pointer
    private unsafe EntityVertex* mappedPtr = null;

    public FastInstantDrawEntity(int maxVertices) : base(maxVertices) {
        if (Game.isIntegratedCard) {
            regionCount = 4;
        }
    }

    public override void setup() {
        // allocate regionCount × maxVertices with persistent mapping
        unsafe {
            VAO = GL.CreateVertexArray();
            VBO = GL.CreateBuffer();
            Game.graphics.vao(VAO);
            GL.NamedBufferStorage(VBO, (uint)(maxVertices * regionCount * sizeof(EntityVertex)), (void*)0,
                BufferStorageMask.MapPersistentBit | BufferStorageMask.MapWriteBit);

            // map persistently
            mappedPtr = (EntityVertex*)GL.MapNamedBufferRange(VBO, 0,
                (nuint)(maxVertices * regionCount * sizeof(EntityVertex)),
                (uint)(MapBufferAccessMask.PersistentBit |
                       MapBufferAccessMask.WriteBit |
                       MapBufferAccessMask.InvalidateBufferBit |
                       MapBufferAccessMask.UnsynchronizedBit |
                       MapBufferAccessMask.FlushExplicitBit));

            format();

            Log.info($"Initialized FastInstantDrawEntity with {maxVertices} vertices per region, {regionCount} regions ({maxVertices * regionCount} total).");
        }

        instantShader = Game.graphics.instantEntityShader;
        instantTexture = instantShader.getUniformLocation("tex");
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
        try {
            uModel = instantShader.getUniformLocation(nameof(uModel));
        }
        catch (InputException e) {
            uModel = -1;
        }

        uFogColour = instantShader.getUniformLocation(nameof(fogColour));
        uFogStart = instantShader.getUniformLocation(nameof(fogStart));
        uFogEnd = instantShader.getUniformLocation(nameof(fogEnd));
        uFogEnabled = instantShader.getUniformLocation(nameof(fogEnabled));
        uFogType = instantShader.getUniformLocation(nameof(fogType));
        uFogDensity = instantShader.getUniformLocation(nameof(fogDensity));
        uLightDir1 = instantShader.getUniformLocation(nameof(lightDir));
        uLightRatio1 = instantShader.getUniformLocation(nameof(lightRatio));
        uLightDir2 = instantShader.getUniformLocation("lightDir2");
        uLightRatio2 = instantShader.getUniformLocation("lightRatio2");
        instantShader.setUniform(instantTexture, 0);

        // Initialize fog as disabled
        instantShader.setUniform(uFogEnabled, false);

        // Set default fog type
        instantShader.setUniform(uFogType, (int)FogType.Linear);
        instantShader.setUniform(uFogDensity, fogDensity);

        // Set default lighting
        instantShader.setUniform(uLightDir1, lightDir);
        instantShader.setUniform(uLightRatio1, lightRatio);

        instantShader.setUniform(uLightDir2, lightDir2);
        instantShader.setUniform(uLightRatio2, lightRatio);
    }

    public override void format() {
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);
        GL.EnableVertexAttribArray(3);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 2, VertexAttribType.Float, false, 0 + 6 * sizeof(ushort));
        GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 10 * sizeof(ushort));
        GL.VertexAttribFormat(3, 4, VertexAttribType.Int2101010Rev, true, 0 + 12 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);
        GL.VertexAttribBinding(3, 0);

        GL.BindVertexBuffer(0, VBO, 0, 14 * sizeof(ushort));
    }

    public new void begin(PrimitiveType type) {
        currentVertex = 0;
        vertices.Clear();
        vertexType = type;
    }

    public override void addVertex(EntityVertex vertex) {
        // apply tint
        vertex.r = (byte)((vertex.r * tint.R) * (1 / 255f));
        vertex.g = (byte)((vertex.g * tint.G) * (1 / 255f));
        vertex.b = (byte)((vertex.b * tint.B) * (1 / 255f));
        vertex.a = (byte)((vertex.a * tint.A) * (1 / 255f));

        vertices.Add(vertex);
        currentVertex++;
    }

    protected new void resizeStorage() {
        var newMaxVertices = maxVertices * 2;

        unsafe {
            // unmap old buffer
            if (mappedPtr != null) {
                GL.UnmapNamedBuffer(VBO);
                mappedPtr = null;
            }

            // delete old buffer
            GL.DeleteBuffer(VBO);

            // create new buffer with double capacity per region
            VBO = GL.CreateBuffer();
            Game.graphics.vao(VAO);
            GL.NamedBufferStorage(VBO, (uint)(newMaxVertices * regionCount * sizeof(EntityVertex)), (void*)0,
                BufferStorageMask.MapPersistentBit | BufferStorageMask.MapWriteBit);

            // remap new buffer
            mappedPtr = (EntityVertex*)GL.MapNamedBufferRange(VBO, 0,
                (nuint)(newMaxVertices * regionCount * sizeof(EntityVertex)),
                (uint)(MapBufferAccessMask.PersistentBit |
                       MapBufferAccessMask.WriteBit |
                       MapBufferAccessMask.InvalidateBufferBit |
                       MapBufferAccessMask.UnsynchronizedBit |
                       MapBufferAccessMask.FlushExplicitBit));

            maxVertices = newMaxVertices;

            Log.info($"Resized FastInstantDrawEntity to {maxVertices} vertices per region ({maxVertices * regionCount} total).");

            format();
        }

        // reset to start of current region after resize
        writePosition = 0;
        peak = 0;
    }

    public new void end() {
        if (currentVertex == 0) return;

        // calculate absolute position in buffer
        int regionStart = currentRegion * maxVertices;
        int absolutePosition = regionStart + writePosition;

        Game.graphics.vao(VAO);
        instantShader.use();

        // set up matrices/fog
        if (modelMatrix != null && (modelDirty || viewDirty || projDirty)) {
            var model = modelMatrix.top;
            var mv = model * viewMatrix;
            var mvp = model * viewMatrix * projMatrix;

            if (uModel != -1) instantShader.setUniform(uModel, ref model);
            instantShader.setUniform(uModelView, ref mv);
            instantShader.setUniform(uMVP, ref mvp);
            modelDirty = false;
            viewDirty = false;
            projDirty = false;
        }

        if (fogDirty) {
            if (fogEnabled) {
                instantShader.setUniform(uFogEnabled, true);
                instantShader.setUniform(uFogColour, fogColour);
                instantShader.setUniform(uFogStart, fogStart);
                instantShader.setUniform(uFogEnd, fogEnd);
                instantShader.setUniform(uFogType, (int)fogType);
                instantShader.setUniform(uFogDensity, fogDensity);
            }
            else {
                instantShader.setUniform(uFogEnabled, false);
            }
            fogDirty = false;
        }

        // copy vertices to mapped buffer at absolute position
        unsafe {
            fixed (EntityVertex* src = CollectionsMarshal.AsSpan(vertices)) {
                NativeMemory.Copy(src, mappedPtr + absolutePosition, (nuint)(currentVertex * sizeof(EntityVertex)));
            }
        }

        // draw from absolute position
        var effectiveMode = vertexType;
        if (vertexType == PrimitiveType.Quads) {
            unsafe {
                effectiveMode = PrimitiveType.Triangles;
                Game.graphics.index(Game.graphics.fatQuadIndices);
                Game.GL.DrawElementsBaseVertex(effectiveMode, (uint)(currentVertex * (6 / 4f)),
                    DrawElementsType.UnsignedInt, (void*)0, absolutePosition);
            }
        }
        else {
            GL.DrawArrays(effectiveMode, absolutePosition, (uint)currentVertex);
        }

        // advance write position within current region
        writePosition += currentVertex;
        peak = int.Max(peak, writePosition);

        vertices.Clear();
        currentVertex = 0;
    }

    /** rotate regions and handle resizing */
    public void endFrame() {
        // check if we need to resize based on peak usage
        if (peak > maxVertices * 0.8f) {
            Log.info($"FastInstantDrawEntity peak usage ({peak}/{maxVertices}), resizing...");
            resizeStorage();
            // after resize, stay in current region (writePosition already reset to 0)
        }
        else {
            // rotate to next region
            currentRegion = (currentRegion + 1) % regionCount;
            writePosition = 0;
            peak = 0;
        }
    }
}