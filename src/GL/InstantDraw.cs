using System.Numerics;
using Silk.NET.OpenGL;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame;

public class InstantDraw {

    public static Shader instantShader;
    public static int instantTexture;
    public static int uMVP;

    private readonly int maxVertices;
    private readonly BlockVertexTinted[] vertices;

    private readonly uint VAO;
    private readonly uint VBO;

    public readonly GL GL;
    private int currentVertex = 0;

    private Shader shader;

    public InstantDraw(int maxVertices) {
        vertices = new BlockVertexTinted[maxVertices];
        this.maxVertices = maxVertices;
        unsafe {
            GL = Game.GL;
            instantShader = new Shader(GL, "shaders/instantVertex.vert", "shaders/instantVertex.frag");
            instantShader.use();
            instantTexture = instantShader.getUniformLocation("tex");
            uMVP = instantShader.getUniformLocation(nameof(uMVP));
            instantShader.setUniform(instantTexture, 0);
            VAO = GL.CreateVertexArray();
            VBO = GL.CreateBuffer();
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(maxVertices * sizeof(BlockVertexTinted)), (void*)0, BufferStorageMask.DynamicStorageBit);
            format();
        }
    }

    public void format() {
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

    public void addVertex(BlockVertexTinted vertex) {
        if (currentVertex >= maxVertices - 1) {
            finish();
        }
        //Console.Out.WriteLine(currentLine);
        vertices[currentVertex] = vertex;
        currentVertex++;
    }

    public void finish() {
        // nothing to do
        if (currentVertex == 0) {
            return;
        }
        GL.BindVertexArray(VAO);
        // upload buffer
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            fixed (BlockVertexTinted* v = vertices) {
                GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(currentVertex * sizeof(BlockVertexTinted)), v);
            }
        }


        // bind VAO, draw elements
        GL.DrawArrays(PrimitiveType.Triangles, 0, (uint)currentVertex);

        currentVertex = 0;
    }
}

public class InstantDrawColour {
    public static Shader instantShader;
    public static int uMVP;
    public static int uModelView;    // Added for fog
    public static int uFogColor;     // Added for fog
    public static int uFogStart;     // Added for fog
    public static int uFogEnd;       // Added for fog
    public static int uFogEnabled;   // Added for fog toggle

    private readonly int maxVertices;
    private readonly VertexTinted[] vertices;

    private readonly uint VAO;
    private readonly uint VBO;

    public readonly GL GL;
    private int currentVertex = 0;

    // Fog settings
    private Vector4 fogColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
    private float fogStart = 10.0f;
    private float fogEnd = 100.0f;
    private bool fogEnabled = false;  // Disabled by default

    public InstantDrawColour(int maxVertices) {
        vertices = new VertexTinted[maxVertices];
        this.maxVertices = maxVertices;
        unsafe {
            GL = Game.GL;
            instantShader = new Shader(GL, "shaders/instantVertexColour.vert", "shaders/instantVertexColour.frag");
            instantShader.use();
            uMVP = instantShader.getUniformLocation(nameof(uMVP));
            uModelView = instantShader.getUniformLocation(nameof(uModelView));
            uFogColor = instantShader.getUniformLocation("fogColor");
            uFogStart = instantShader.getUniformLocation("fogStart");
            uFogEnd = instantShader.getUniformLocation("fogEnd");
            uFogEnabled = instantShader.getUniformLocation("fogEnabled");

            // Initialize fog as disabled
            instantShader.setUniform(uFogEnabled, false);

            VAO = GL.CreateVertexArray();
            VBO = GL.CreateBuffer();
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(maxVertices * sizeof(VertexTinted)), (void*)0, BufferStorageMask.DynamicStorageBit);
            format();
        }
    }

    public void format() {
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 4, VertexAttribType.UnsignedByte, true, 0 + 6 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);

        GL.BindVertexBuffer(0, VBO, 0, 8 * sizeof(ushort));
    }

    // Fog control methods
    public void EnableFog(bool enable) {
        fogEnabled = enable;
        instantShader.use();
        instantShader.setUniform(uFogEnabled, enable);
    }

    public void SetFogColor(Vector4 color) {
        fogColor = color;
        instantShader.use();
        instantShader.setUniform(uFogColor, color);
    }

    public void SetFogDistance(float start, float end) {
        fogStart = start;
        fogEnd = end;
        instantShader.use();
        instantShader.setUniform(uFogStart, start);
        instantShader.setUniform(uFogEnd, end);
    }

    public void addVertex(VertexTinted vertex) {
        if (currentVertex >= maxVertices - 1) {
            finish();
        }
        //Console.Out.WriteLine(currentLine);
        vertices[currentVertex] = vertex;
        currentVertex++;
    }

    public void finish() {
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
        } else {
            instantShader.setUniform(uFogEnabled, false);
        }

        // Upload buffer
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            fixed (VertexTinted* v = vertices) {
                GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(currentVertex * sizeof(VertexTinted)), v);
            }
        }

        GL.DrawArrays(PrimitiveType.Triangles, 0, (uint)currentVertex);
        currentVertex = 0;
    }
}