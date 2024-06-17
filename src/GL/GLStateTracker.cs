using Silk.NET.OpenGL;
using TrippyGL;

namespace BlockGame;

public class GLStateTracker {
    public GL GL;
    public GraphicsDevice GD;

    public GLStateTracker(GL gl, GraphicsDevice gd) {
        GL = gl;
        GD = gd;
    }

    public void save() {
    }

    public void load() {
        GD.ResetInternalStates();
    }
}