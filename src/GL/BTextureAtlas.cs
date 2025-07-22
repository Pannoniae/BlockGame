using BlockGame.ui;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace BlockGame.GL;

public class BTextureAtlas : IDisposable {
    public uint handle;
    public int atlasSize;

    public string path;

    public Silk.NET.OpenGL.GL GL;

    /// <summary>
    /// Image memory
    /// </summary>
    private Image<Rgba32> image;
    private Memory<Rgba32> memory;

    private int i;
    private int ticks;

    public BTextureAtlas(string path, int atlasSize) {
        GL = Game.GL;
        this.atlasSize = atlasSize;
        this.path = path;
        reload();

        //var handle2 = GL.GenTexture();
        //GL.ActiveTexture(TextureUnit.Texture0);
        //GL.BindTexture(TextureTarget.Texture2DArray, handle2);
        //GL.TexImage3D(TextureTarget.Texture2DArray, 0, InternalFormat.Rgba8, 16, 16, 2048, 0, PixelFormat.Rgba, PixelType.Byte, null);

    }

    private void generateMipmap(int left, int top, int width, int height, Span<Rgba32> mipmap, Span<Rgba32> prevMipmap) {
        for (int y = top; y < height; y++) {
            for (int x = left; x < width; x++) {
                int xSrc = x * 2;
                int ySrc = y * 2;
                int x1 = xSrc + 1;
                int y1 = ySrc + 1;

                var c00 = prevMipmap[ySrc * width * 2 + xSrc];
                var c01 = prevMipmap[y1 * width * 2 + x1];
                var c10 = prevMipmap[ySrc * width * 2 + xSrc];
                var c11 = prevMipmap[y1 * width * 2 + x1];

                mipmap[y * width + x] = avgColour(c00, c01, c10, c11);
            }
        }
    }

    /// <summary>
    /// Average two colours.
    /// </summary>
    private static Rgba32 avgColour(Rgba32 c0, Rgba32 c1) {
        return new Rgba32((byte)((c0.R + c1.R) / 2f), (byte)((c0.G + c1.G) / 2f), (byte)((c0.B + c1.B) / 2f), (byte)((c0.A + c1.A) / 2f));
    }

    private static Rgba32 avgColour(Rgba32 c0, Rgba32 c1, Rgba32 c2, Rgba32 c3) {
        return new Rgba32((byte)((c0.R + c1.R + c2.R + c3.R) / 4f),
            (byte)((c0.G + c1.G + c2.G + c3.G) / 4f),
            (byte)((c0.B + c1.B + c2.B + c3.B) / 4f),
            (byte)((c0.A + c1.A + c2.A + c3.A) / 4f));
    }

    unsafe private void generateMipmaps(Span<Rgba32> pixelArray, int imageWidth, int imageHeight, int maxLevel) {
        fixed (Rgba32* pixels = &pixelArray.GetPinnableReference()) {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)imageWidth, (uint)imageHeight,
                PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            // Generate mipmaps
            // we check against 2 so we never generate a mipmap with less pixels than one per texture
            var prevMipmap = pixelArray;

            int lvl;
            int width = imageWidth;
            int height = imageHeight;

            for (lvl = 1; lvl <= maxLevel; lvl++) {
                if (width > 1) width /= 2;
                if (height > 1) height /= 2;

                Span<Rgba32> mipmap = new Rgba32[width * height];
                generateMipmap(0, 0, width, height, mipmap, prevMipmap);
                fixed (Rgba32* mipmapPixels = mipmap) {
                    GL.TexImage2D(TextureTarget.Texture2D, lvl, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, mipmapPixels);
                }
                prevMipmap = mipmap;
            }
        }
    }

    unsafe public void updateTexture(int left, int top, int width, int height, int srcX, int srcY) {
        bind();
        // get pixels from the image
        var pixels = new Rgba32[width * height];
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                pixels[y * width + x] = memory.Span[(srcY + y) * image.Width + srcX + x];
            }
        }
        fixed (Rgba32* pixelsPtr = pixels) {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, left, top, (uint)width, (uint)height, PixelFormat.Rgba, PixelType.UnsignedByte, pixelsPtr);
        }
    }

    unsafe public void reload() {
        GL.DeleteTexture(handle);
        handle = GL.GenTexture();
        bind();
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.NearestMipmapLinear);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, -0.4f);
        image?.Dispose();
        image = Image.Load<Rgba32>(path);
        var maxLevel = Settings.instance.mipmapping;
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, maxLevel);
        GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)image.Width, (uint)image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
        if (!image.DangerousTryGetSinglePixelMemory(out memory)) {
            throw new Exception("Couldn't load the atlas contiguously!");
        }

        //Console.Out.WriteLine("Loading textures the proper way!");
        // Load image
        // Thanks ClassiCube for the idea!
        generateMipmaps(memory.Span, image.Width, image.Height, maxLevel);
    }



    public void bind() {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, handle);
    }

    public void Dispose() {
        GL.DeleteTexture(handle);
    }

    public void update(double dt) {
        if (ticks % 16 == 0) {
            updateTexture(0, 4 * 16, 16, 16, (i % 16) * 16, 4 * 16);
            i++;
        }
        ticks++;
    }
}