using System.Drawing;
using BlockGame.GL;
using FontStashSharp.Interfaces;
using Silk.NET.OpenGL;
using TrippyGL;

namespace BlockGame.util.font;

public class Texture2DManager : ITexture2DManager {
    public GraphicsDevice GraphicsDevice { get; }

    public Texture2DManager(GraphicsDevice device) {
        GraphicsDevice = device ?? throw new ArgumentNullException(nameof(device));
    }

    public object CreateTexture(int width, int height) => new Texture2D(GraphicsDevice, (uint)width, (uint)height);

    public Point GetTextureSize(object texture) {
        var xnaTexture = (Texture2D)texture;

        return new Point((int)xnaTexture.Width, (int)xnaTexture.Height);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
        var xnaTexture = (Texture2D)texture;

        xnaTexture.SetData<byte>(data, bounds.X, bounds.Y, (uint)bounds.Width, (uint)bounds.Height, PixelFormat.Rgba);
    }
}

public class BTexture2DManager : ITexture2DManager {
    public GraphicsDevice GraphicsDevice { get; }

    public BTexture2DManager(GraphicsDevice device) {
        GraphicsDevice = device ?? throw new ArgumentNullException(nameof(device));
    }

    public object CreateTexture(int width, int height) => new BTexture2D((uint)width, (uint)height);

    public Point GetTextureSize(object texture) {
        var xnaTexture = (BTexture2D)texture;

        return new Point((int)xnaTexture.width, (int)xnaTexture.height);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
        var xnaTexture = (BTexture2D)texture;

        xnaTexture.SetData<byte>(data, bounds.X, bounds.Y, (uint)bounds.Width, (uint)bounds.Height);
    }
}