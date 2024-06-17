using Silk.NET.OpenGL;
using TrippyGL;

namespace BlockGame;

public class GLStateTracker {
    public GL GL;
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
        GL.ActiveTexture(TextureUnit.Texture0 + 2);
        GL.GetInteger(GetPName.TextureBinding2D, out boundTex2);
        GL.ActiveTexture(TextureUnit.Texture0 + 3);
        GL.GetInteger(GetPName.TextureBinding2D, out boundTex3);

        // save shader
        GL.GetInteger(GetPName.CurrentProgram, out currentShader);

        // save array buffer
        GL.GetInteger(GetPName.ArrayBufferBinding, out arrayBuffer);
        GL.GetInteger(GetPName.ElementArrayBufferBinding, out elementArrayBuffer);

        // save VAO
        GL.GetInteger(GetPName.VertexArrayBinding, out VAO);
    }

    public void load() {
        GL.ActiveTexture(TextureUnit.Texture0 + 0);
        GL.BindTexture(TextureTarget.Texture2D, (uint)boundTex0);
        GL.ActiveTexture(TextureUnit.Texture0 + 1);
        GL.BindTexture(TextureTarget.Texture2D, (uint)boundTex1);
        GL.ActiveTexture(TextureUnit.Texture0 + 2);
        GL.BindTexture(TextureTarget.Texture2D, (uint)boundTex2);
        GL.ActiveTexture(TextureUnit.Texture0 + 3);
        GL.BindTexture(TextureTarget.Texture2D, (uint)boundTex3);

        GL.ActiveTexture((TextureUnit)activeTex);

        //GD.ResetTextureStates();

        // restore shader
        GL.UseProgram((uint)currentShader);

        // reset bound array buffer
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, (uint)arrayBuffer);
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, (uint)elementArrayBuffer);

        // restore VAO
        GL.BindVertexArray((uint)VAO);

    }
}