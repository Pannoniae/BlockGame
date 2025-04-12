using TrippyGL;

namespace BlockGame.GL;

public class GLStateTracker {
    public Silk.NET.OpenGL.GL GL;
    public GraphicsDevice GD;

    public int activeTex;

    public int boundTex0;
    public int boundTex1;
    public int boundTex2;
    public int boundTex3;

    public int arrayBuffer;
    public int elementArrayBuffer;

    public int currentShader;

    public int VAO;

    public GLStateTracker(Silk.NET.OpenGL.GL gl, GraphicsDevice gd) {
        GL = gl;
        GD = gd;
    }

    public void save() {

    }

    public void load() {
        GD.ResetInternalStates();
    }
}