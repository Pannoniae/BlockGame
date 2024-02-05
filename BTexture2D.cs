using Silk.NET.OpenGL;

namespace BlockGame;

public class BTexture2D : IDisposable {

    public uint handle;

    public GL GL;

    public BTexture2D(string path) {
        GL = Game.instance.GL;
        handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);

    }

    public void Dispose() {
        // TODO release managed resources here
        GL.DeleteTexture(handle);
    }
}