using Silk.NET.OpenGL;
using TrippyGL;

namespace BlockGame;

public class GLStateTracker {
    public GL GL;
    public GraphicsDevice GD;

    // this is how many textures we track
    public const int MAX_BINDINGS = 8;

    public int activeTex;

    //public uint[] texBindings2D = new uint[8];

    public int boundTex0;
    public int boundTex1;

    public int currentShader;

    public GLStateTracker(GL gl, GraphicsDevice gd) {
        GL = gl;
        GD = gd;
    }

    public void save() {
        GL.GetInteger(GetPName.ActiveTexture, out activeTex);

        GL.ActiveTexture(TextureUnit.Texture0 + 0);
        GL.GetInteger(GetPName.TextureBinding2D, out boundTex0);
        GL.ActiveTexture(TextureUnit.Texture0 + 1);
        GL.GetInteger(GetPName.TextureBinding2D, out boundTex1);

        // save shader
        GL.GetInteger(GetPName.CurrentProgram, out currentShader);
    }

    public void load() {
        GL.ActiveTexture(TextureUnit.Texture0 + 0);
        GL.BindTexture(TextureTarget.Texture2D, (uint)boundTex0);
        GL.ActiveTexture(TextureUnit.Texture0 + 1);
        GL.BindTexture(TextureTarget.Texture2D, (uint)boundTex1);

        GL.ActiveTexture((TextureUnit)activeTex);

        // restore shader
        GL.UseProgram((uint)currentShader);
        GD.ResetBufferStates();

    }
}