using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL.vertexformats;
using Silk.NET.OpenGL.Legacy;
using Color = Molten.Color;
using PrimitiveType = Silk.NET.OpenGL.Legacy.PrimitiveType;

namespace BlockGame.GL;

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
    protected int uFogColour;
    protected int uFogStart;
    protected int uFogEnd;
    protected int uFogEnabled;
    protected int uFogType;      // Added for fog type
    protected int uFogDensity;   // Added for exp/exp2 fog

    // Fog settings
    protected Vector4 fogColour = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
    protected float fogStart = 10.0f;
    protected float fogEnd = 100.0f;
    protected bool fogEnabled = false;  // Disabled by default
    protected FogType fogType = FogType.Linear;
    protected float fogDensity = 0.01f;
    
    // vertex tint
    protected Color tint = Color.White;
    

    public InstantDraw(int maxVertices) {
        vertices = new List<T>(maxVertices);
        this.maxVertices = maxVertices;
        GL = main.Game.GL;
    }

    public virtual void setup() {
        unsafe {
            VAO = GL.CreateVertexArray();
            VBO = GL.CreateBuffer();
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(maxVertices * sizeof(T)), (void*)0, BufferStorageMask.DynamicStorageBit);
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
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(newMaxVertices * sizeof(T)), (void*)0, BufferStorageMask.DynamicStorageBit);

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
        instantShader.setUniform(uModelView, modelView);
    }
    
    public void setMVP(Matrix4x4 mvp) {
        instantShader.setUniform(uMVP, mvp);
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
        GL.BindVertexArray(VAO);

        // Apply current fog settings
        instantShader.use();
        if (fogEnabled) {
            instantShader.setUniform(uFogEnabled, true);
            instantShader.setUniform(uFogColour, fogColour);
            instantShader.setUniform(uFogStart, fogStart);
            instantShader.setUniform(uFogEnd, fogEnd);
            instantShader.setUniform(uFogType, (int)fogType);
            instantShader.setUniform(uFogDensity, fogDensity);
        } else {
            instantShader.setUniform(uFogEnabled, false);
        }

        // Upload buffer
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            main.Game.GL.InvalidateBufferData(VBO);
            fixed (T* v = CollectionsMarshal.AsSpan(vertices)) {
                GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(currentVertex * sizeof(T)), v);
            }
        }
        
        // handle the vertex type
        var effectiveMode = vertexType;
        if (vertexType == PrimitiveType.Quads) {
            unsafe {
                effectiveMode = PrimitiveType.Triangles;
                GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, main.Game.graphics.fatQuadIndices);
                GL.DrawElements(effectiveMode, (uint)(currentVertex * 6 / 4f), DrawElementsType.UnsignedShort, (void*)0);
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
        instantShader = main.Game.graphics.instantTextureShader;
        instantTexture = instantShader.getUniformLocation("tex");
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
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
        instantShader.use();
        main.Game.graphics.tex(0, texture);
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
        instantShader = main.Game.graphics.instantColourShader;
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
        uFogColour = instantShader.getUniformLocation("fogColour");
        uFogStart = instantShader.getUniformLocation("fogStart");
        uFogEnd = instantShader.getUniformLocation("fogEnd");
        uFogEnabled = instantShader.getUniformLocation("fogEnabled");
        uFogType = instantShader.getUniformLocation("fogType");
        uFogDensity = instantShader.getUniformLocation("fogDensity");
        

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

    protected int uLightDir;
    protected int uLightRatio;
    
    protected Vector3 lightDir = new Vector3(0.0f, 1.0f, 0.0f);
    /** The ratio between direct and ambient light. 1 = only direct, 0 = only ambient
     *  TODO we don't have ambient light colouring or any of that shit, rn it's just a simple lerp
     */
    protected float lightRatio = 1.0f;
    
    public override void setup() {
        base.setup();
        instantShader = main.Game.graphics.instantEntityShader;
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
        uFogColour = instantShader.getUniformLocation(nameof(fogColor));
        uFogStart = instantShader.getUniformLocation(nameof(fogStart));
        uFogEnd = instantShader.getUniformLocation(nameof(fogEnd));
        uFogEnabled = instantShader.getUniformLocation(nameof(fogEnabled));
        uFogType = instantShader.getUniformLocation(nameof(fogType));
        uFogDensity = instantShader.getUniformLocation(nameof(fogDensity));
        uLightDir = instantShader.getUniformLocation(nameof(lightDir));
        uLightRatio = instantShader.getUniformLocation(nameof(lightRatio));

        // Initialize fog as disabled
        instantShader.setUniform(uFogEnabled, false);
        
        // Set default fog type
        instantShader.setUniform(uFogType, (int)FogType.Linear);
        instantShader.setUniform(uFogDensity, fogDensity);
        
        // Set default lighting
        instantShader.setUniform(uLightDir, lightDir);
        instantShader.setUniform(uLightRatio, lightRatio);
    }

    public override void format() {
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 2, VertexAttribType.HalfFloat, false, 0 + 6 * sizeof(ushort));
        GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 8 * sizeof(ushort));
        GL.VertexAttribFormat(3, 3, VertexAttribType.Int2101010Rev, true, 0 + 10 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);
        GL.VertexAttribBinding(3, 0);

        GL.BindVertexBuffer(0, VBO, 0, 12 * sizeof(ushort));
    }
}