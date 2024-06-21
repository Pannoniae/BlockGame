using BlockGame.GUI;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame;

public class BTextureAtlas : IDisposable {
    public uint handle;
    public int atlasSize;

    public string path;

    public GL GL;

    unsafe public BTextureAtlas(string path, int atlasSize) {
        GL = Game.GL;
        this.atlasSize = atlasSize;
        this.path = path;
        reload();

        var handle2 = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2DArray, handle2);
        GL.TexImage3D(TextureTarget.Texture2DArray, 0, InternalFormat.Rgba8, 16, 16, 16384, 0, PixelFormat.Rgba, PixelType.Byte, null);

    }

    private void generateMipmaps(int width, int height, Span<Rgba32> mipmap, Span<Rgba32> prevMipmap) {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int xSrc = x * 2;
                int ySrc = y * 2;
                int x1 = xSrc + 1;
                int y1 = ySrc + 1;

                Rgba32 c00 = prevMipmap[ySrc * width * 2 + xSrc];
                Rgba32 c01 = prevMipmap[y1 * width * 2 + x1];
                Rgba32 c10 = prevMipmap[ySrc * width * 2 + xSrc];
                Rgba32 c11 = prevMipmap[y1 * width * 2 + x1];

                mipmap[y * width + x] = avgColour(avgColour(c00, c01), avgColour(c10, c11));
            }
        }
    }

    /// <summary>
    /// Average two colours.
    /// </summary>
    private static Rgba32 avgColour(Rgba32 c0, Rgba32 c1) {
        return new Rgba32((byte)((c0.R + c1.R) / 2f), (byte)((c0.G + c1.G) / 2f), (byte)((c0.B + c1.B) / 2f), (byte)((c0.A + c1.A) / 2f));
    }

    unsafe public void reload() {
        handle = GL.GenTexture();
        bind();
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.NearestMipmapLinear);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, -0.4f);
        using Image<Rgba32> image = Image.Load<Rgba32>(path);
        var maxLevel = Settings.instance.mipmapping;
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, maxLevel);
        GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)image.Width, (uint)image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
        if (!image.DangerousTryGetSinglePixelMemory(out var memory)) {
            throw new Exception("Couldn't load the atlas contiguously!");
        }

        Console.Out.WriteLine("Loading textures the proper way!");
        // Load image
        fixed (Rgba32* pixels = &memory.Span.GetPinnableReference()) {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)image.Width, (uint)image.Height,
                PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            // Generate mipmaps
            // we check against 2 so we never generate a mipmap with less pixels than one per texture
            var prevMipmap = memory.Span;
            Span<Rgba32> mipmap;

            int lvls = maxLevel;
            int lvl;
            int width = image.Width, height = image.Height;

            for (lvl = 1; lvl <= lvls; lvl++) {
                if (width > 1) width /= 2;
                if (height > 1) height /= 2;

                mipmap = new Rgba32[width * height];
                generateMipmaps(width, height, mipmap, prevMipmap);
                fixed (Rgba32* mipmapPixels = mipmap) {
                    GL.TexImage2D(TextureTarget.Texture2D, lvl, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, mipmapPixels);
                }
                prevMipmap = mipmap;
            }
        }
    }

    public void bind() {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, handle);
    }

    public void Dispose() {
        // TODO release managed resources here
        GL.DeleteTexture(handle);
    }
}