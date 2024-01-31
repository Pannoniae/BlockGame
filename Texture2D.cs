using Silk.NET.OpenGL;

namespace BlockGame;

public class Texture2D : IDisposable {

    public uint handle;

    public GL GL;

    public Texture2D(string path) {
        GL = Game.instance.GL;
        handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);

    }

    public void Dispose() {
        // TODO release managed resources here
        GL.DeleteTexture(handle);
    }
}