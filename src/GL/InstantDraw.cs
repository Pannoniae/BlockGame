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