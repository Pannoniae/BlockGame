using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using BlockGame.GL;
using FontStashSharp.Interfaces;

namespace BlockGame.util.font;

public class BTexture2DManager : ITexture2DManager {

    public BTexture2DManager() {
        
    }

    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP005:Return type should indicate that the value should be disposed")]
    public object CreateTexture(int width, int height) => new BTexture2D((uint)width, (uint)height);

    public Point GetTextureSize(object texture) {
        var xnaTexture = (BTexture2D)texture;

        return new Point((int)xnaTexture.width, (int)xnaTexture.height);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data) {
        var xnaTexture = (BTexture2D)texture;

        xnaTexture.updateTexture(data, bounds.X, bounds.Y, (uint)bounds.Width, (uint)bounds.Height);
    }
}