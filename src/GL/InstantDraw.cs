using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame;

public class InstantDraw {
    private const int MAX_VERTICES = 1024;

    private readonly InstantVertex[] vertices = new InstantVertex[MAX_VERTICES];

    private uint VAO;
    private uint VBO;

    public GL GL;
    private int currentVertex = 0;

    private Shader shader;

    public InstantDraw() {
        unsafe {
            GL = Game.GL;
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(MAX_VERTICES * sizeof(InstantVertex)), 0, BufferStorageMask.DynamicStorageBit);
            format();
        }
    }

    public void format() {
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 2, VertexAttribType.HalfFloat, false, 0 + 6 * sizeof(ushort));
        GL.VertexAttribIFormat(2, 1, VertexAttribIType.UnsignedShort, 0 + 8 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);
    }

    public void update(double interp) {
        shader.use();
    }

    public void addVertex(InstantVertex vertex) {
        if (currentVertex >= MAX_VERTICES - 1) {
            finish();
        }
        //Console.Out.TWriteLine(currentLine);
        currentVertex++;
    }

    public void finish() {
        // nothing to do
        if (currentVertex == 0) {
            return;
        }
        // upload buffer


        // bind VAO, draw elements
        GL.BindVertexArray(VAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, (uint)currentVertex);

        currentVertex = 0;
    }
}

[StructLayout(LayoutKind.Sequential, Size = 20)]
public readonly struct InstantVertex {
    // position
    public readonly float x;
    public readonly float y;
    public readonly float z;
    // UV
    public readonly Half u;
    public readonly Half v;
    // colour   
    public readonly byte r;
    public readonly byte g;
    public readonly byte b;
    public readonly byte a;

    public InstantVertex(float x, float y, float z, Half u, Half v, byte r, byte g, byte b, byte a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }
}