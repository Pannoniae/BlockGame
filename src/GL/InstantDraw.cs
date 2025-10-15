using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.util;
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

    protected int maxVertices;
    protected readonly List<T> vertices;
    protected int currentVertex = 0;

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

    // vertex tint
    protected Color tint = Color.White;

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
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
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
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(newMaxVertices * sizeof(T)), (void*)0,
                BufferStorageMask.DynamicStorageBit);

            maxVertices = newMaxVertices;
            format();
        }
    }

    // Fog control methods
    public void enableFog(bool enable) {
        fogEnabled = enable;
        instantShader.setUniform(uFogEnabled, enable);
    }

    public void fogColor(Vector4 color) {
        fogColour = color;
        instantShader.setUniform(uFogColour, color);
    }

    public void fogDistance(float start, float end) {
        fogStart = start;
        fogEnd = end;
        instantShader.setUniform(uFogStart, start);
        instantShader.setUniform(uFogEnd, end);
    }

    // New methods for fog type and density
    public void setFogType(FogType type) {
        fogType = type;
        instantShader.setUniform(uFogType, (int)type);
    }

    public void setFogDensity(float density) {
        fogDensity = density;
        instantShader.setUniform(uFogDensity, density);
    }

    public void setColour(Color c) {
        tint = c;
    }

    public void setMV(Matrix4x4 modelView) {
        modelMatrix = null; // Disable automatic matrix computation
        instantShader.setUniform(uModelView, modelView);
    }

    public void setMVP(Matrix4x4 mvp) {
        modelMatrix = null; // Disable automatic matrix computation
        instantShader.setUniform(uMVP, mvp);
    }

    public void model(MatrixStack? m) {
        modelMatrix = m;
    }

    public void view(Matrix4x4 v) {
        viewMatrix = v;
    }

    public void proj(Matrix4x4 p) {
        projMatrix = p;
    }

    public void applyMat() {
        //instantShader.use();
        // Compute final matrices from components if available
        if (modelMatrix != null) {
            var model = modelMatrix.top;
            var mv = model * viewMatrix;
            var mvp = model * viewMatrix * projMatrix;
            if (uModel != -1) instantShader.setUniform(uModel, model);
            instantShader.setUniform(uModelView, mv);
            instantShader.setUniform(uMVP, mvp);
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
        if (modelMatrix != null) {
            var model = modelMatrix.top;
            var mv = model * viewMatrix;
            var mvp = model * viewMatrix * projMatrix;

            if (uModel != -1) instantShader.setUniform(uModel, model);
            instantShader.setUniform(uModelView, mv);
            instantShader.setUniform(uMVP, mvp);
        }

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

        // Upload buffer
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
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
                GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, Game.graphics.fatQuadIndices);
                GL.DrawElements(effectiveMode, (uint)(currentVertex * 6 / 4f), DrawElementsType.UnsignedShort,
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
    public static int instantTexture;

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
        GL.VertexAttribFormat(1, 2, VertexAttribType.HalfFloat, false, 0 + 6 * sizeof(ushort));
        GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 8 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);

        GL.BindVertexBuffer(0, VBO, 0, 10 * sizeof(ushort));
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
        vertex.r = (byte)((vertex.r * tint.R) / 255);
        vertex.g = (byte)((vertex.g * tint.G) / 255);
        vertex.b = (byte)((vertex.b * tint.B) / 255);
        vertex.a = (byte)((vertex.a * tint.A) / 255);

        vertices.Add(vertex);
        currentVertex++;
    }
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


        // Initialize fog as disabled
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
        vertex.r = (byte)((vertex.r * tint.R) / 255);
        vertex.g = (byte)((vertex.g * tint.G) / 255);
        vertex.b = (byte)((vertex.b * tint.B) / 255);
        vertex.a = (byte)((vertex.a * tint.A) / 255);

        vertices.Add(vertex);
        currentVertex++;
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
        vertex.r = (byte)((vertex.r * tint.R) / 255);
        vertex.g = (byte)((vertex.g * tint.G) / 255);
        vertex.b = (byte)((vertex.b * tint.B) / 255);
        vertex.a = (byte)((vertex.a * tint.A) / 255);

        vertices.Add(vertex);
        currentVertex++;
    }
}