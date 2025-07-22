using System.Numerics;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame.GL;

// Define fog types as an enum for better code readability
public enum FogType {
    Linear = 0,
    Exp = 1,
    Exp2 = 2
}

public abstract class InstantDraw<T> where T : unmanaged {
    
    protected PrimitiveType vertexType;
    
    protected readonly int maxVertices;
    protected readonly T[] vertices;
    protected int currentVertex = 0;

    public readonly Silk.NET.OpenGL.GL GL;
    
    protected uint VAO;
    protected uint VBO;
    
    
    protected Shader instantShader;
    public int uMVP;
    public int uModelView;
    public int uFogColor;
    public int uFogStart;
    public int uFogEnd;
    public int uFogEnabled;
    public int uFogType;      // Added for fog type
    public int uFogDensity;   // Added for exp/exp2 fog

    // Fog settings
    protected Vector4 fogColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
    protected float fogStart = 10.0f;
    protected float fogEnd = 100.0f;
    protected bool fogEnabled = false;  // Disabled by default
    protected FogType fogType = FogType.Linear;
    protected float fogDensity = 0.01f;

    public InstantDraw(int maxVertices) {
        vertices = new T[maxVertices];
        this.maxVertices = maxVertices;
        unsafe {
            GL = Game.GL;
        }
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
    
    // Fog control methods
    public void enableFog(bool enable) {
        fogEnabled = enable;
        instantShader.setUniform(uFogEnabled, enable);
    }

    public void fogColour(Vector4 color) {
        fogColor = color;
        instantShader.setUniform(uFogColor, color);
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
        vertexType = type;
    }

    public void addVertex(T vertex) {
        if (currentVertex >= maxVertices - 1) {
            end();
        }
        //Console.Out.WriteLine(currentLine);
        vertices[currentVertex] = vertex;
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
            instantShader.setUniform(uFogColor, fogColor);
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
            Game.GL.InvalidateBufferData(VBO);
            fixed (T* v = vertices) {
                GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(currentVertex * sizeof(T)), v);
            }
        }
        
        // handle the vertex type
        var effectiveMode = vertexType;
        if (vertexType == PrimitiveType.Quads) {
            unsafe {
                effectiveMode = PrimitiveType.Triangles;
                GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, Game.graphics.fatQuadIndices);
                GL.DrawElements(effectiveMode, (uint)(currentVertex * 6 / 4f), DrawElementsType.UnsignedShort, (void*)0);
            }
        }
        else {
            GL.DrawArrays(effectiveMode, 0, (uint)currentVertex);
        }

        currentVertex = 0;
    }
}

public class InstantDrawTexture(int maxVertices) : InstantDraw<BlockVertexTinted>(maxVertices) {
    
    public static int instantTexture;

    public override void setup() {
        base.setup();
        instantShader = new Shader(GL, nameof(instantShader), "shaders/instantVertex.vert", "shaders/instantVertex.frag");
        instantTexture = instantShader.getUniformLocation("tex");
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
        uFogColor = instantShader.getUniformLocation("fogColor");
        uFogStart = instantShader.getUniformLocation("fogStart");
        uFogEnd = instantShader.getUniformLocation("fogEnd");
        uFogEnabled = instantShader.getUniformLocation("fogEnabled");
        uFogType = instantShader.getUniformLocation("fogType");
        uFogDensity = instantShader.getUniformLocation("fogDensity");
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

    public void setTexture(uint handle) {
        instantShader.use();
        Game.GL.ActiveTexture(TextureUnit.Texture0);
        Game.GL.BindTexture(TextureTarget.Texture2D, handle);
    }
}

public class InstantDrawColour(int maxVertices) : InstantDraw<VertexTinted>(maxVertices) {
    public override void setup() {
        base.setup();
        instantShader = new Shader(GL, nameof(instantShader), "shaders/instantVertexColour.vert", "shaders/instantVertexColour.frag");
        uMVP = instantShader.getUniformLocation(nameof(uMVP));
        uModelView = instantShader.getUniformLocation(nameof(uModelView));
        uFogColor = instantShader.getUniformLocation("fogColor");
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
}