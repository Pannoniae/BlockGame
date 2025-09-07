using BlockGame.src.render;
using BlockGame.ui;
using BlockGame.util;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace BlockGame.GL;

public class BTextureAtlas : BTexture2D, IDisposable {
    
    public int atlasSize;
    
    public bool firstLoad = true;
    
    public List<DynamicTexture> dtextures = [];
    
    public Rgba32[] mipmap;

    public BTextureAtlas(string path, int atlasSize) : base(path) {
        GL = Game.GL;
        this.atlasSize = atlasSize;
        this.path = path;

        //var handle2 = GL.GenTexture();
        //GL.ActiveTexture(TextureUnit.Texture0);
        //GL.BindTexture(TextureTarget.Texture2DArray, handle2);
        //GL.TexImage3D(TextureTarget.Texture2DArray, 0, InternalFormat.Rgba8, 16, 16, 2048, 0, PixelFormat.Rgba, PixelType.Byte, null);
    }
    
    public void addDynamicTexture(DynamicTexture dt) {
        dtextures.Add(dt);
    }
    
    public void updateTexture(int x, int y, int width, int height, Rgba32[] pixels) {
        updateTexture(pixels, x, y, (uint)width, (uint)height);
    }

    private static void generateMipmap(int left, int top, int width, int height, Span<Rgba32> mipmap, ReadOnlySpan<Rgba32> prevMipmap) {
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

                mipmap[y * width + x] = avgColourWeighted(c00, c01, c10, c11);
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
    
    
    /**
     * Same as above, except that it's alpha-weighted by the given colour. i.e. it won't produce black artifacts on alpha because the colour contribution is weighed by the alpha.
     *
     * Basically the equivalent of this shader:
     *
     * for (float i = something; i &lt; somethingElse; i++) {
     *   c.rgb += colorSample.rgb * colorSample.a;
     *   c.a += colorSample.a;
     * }
     * c.rgb /= c.a;
     * c.a /= sampleCount;
     * 
     * 
     */
    private static Rgba32 avgColourWeighted(Rgba32 c0, Rgba32 c1, Rgba32 c2, Rgba32 c3) {
        float a0 = c0.A / 255f;
        float a1 = c1.A / 255f;
        float a2 = c2.A / 255f;
        float a3 = c3.A / 255f;
    
        float totalAlpha = a0 + a1 + a2 + a3;
    
        if (totalAlpha == 0)
            return new Rgba32(0, 0, 0, 0);
    
        float r = (c0.R * a0 + c1.R * a1 + c2.R * a2 + c3.R * a3) / totalAlpha;
        float g = (c0.G * a0 + c1.G * a1 + c2.G * a2 + c3.G * a3) / totalAlpha;
        float b = (c0.B * a0 + c1.B * a1 + c2.B * a2 + c3.B * a3) / totalAlpha;
    
        return new Rgba32(
            (byte)(r + 0.5f),
            (byte)(g + 0.5f),
            (byte)(b + 0.5f),
            (byte)(totalAlpha * 63.75f + 0.5f)  // 63.75 = 255/4
        );
    }

    unsafe private void generateMipmaps(Span<Rgba32> pixelArray, int imageWidth, int imageHeight, int maxLevel) {
        fixed (Rgba32* pixels = &pixelArray.GetPinnableReference()) {
            GL.TextureSubImage2D(handle, 0, 0, 0, (uint)imageWidth, (uint)imageHeight,
                PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            // Generate mipmaps
            // we check against 2 so we never generate a mipmap with less pixels than one per texture
            var prevMipmap = pixelArray;

            int lvl;
            int width = imageWidth;
            int height = imageHeight;
            
            
            // no need to clear, we overwrite anyway!
            //Array.Clear(mipmap);
            for (lvl = 1; lvl <= maxLevel; lvl++) {
                if (width > 1) width /= 2;
                if (height > 1) height /= 2;

                Span<Rgba32> mipmap = new Rgba32[width * height];
                generateMipmap(0, 0, width, height, mipmap, prevMipmap);
                fixed (Rgba32* mipmapPixels = mipmap) {
                    GL.TextureSubImage2D(handle, lvl, 0, 0, (uint)width, (uint)height,
                        PixelFormat.Rgba, PixelType.UnsignedByte, mipmapPixels);
                }
                prevMipmap = mipmap;
            }
        }
    }

    public override unsafe void reload() {
        GL.DeleteTexture(handle);
        handle = GL.CreateTexture(TextureTarget.Texture2D);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.NearestMipmapLinear);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, -0.4f);
        image?.Dispose();
        image = Image.Load<Rgba32>(path);
        var maxLevel = Settings.instance.mipmapping;
        GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, maxLevel);
        
        // Calculate maximum possible mipmap levels based on texture dimensions
        const uint maxPossibleLevels = 5u;
        GL.TextureStorage2D(handle, maxPossibleLevels, SizedInternalFormat.Rgba8, (uint)image.Width, (uint)image.Height);
        if (!image.DangerousTryGetSinglePixelMemory(out imageData)) {
            throw new SkillIssueException("Couldn't load the atlas contiguously!");
        }

        /*fixed (Rgba32* pixels = &memory.Span.GetPinnableReference()) {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)image.Width, (uint)image.Height,
                PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        }*/

        //mipmap = new Rgba32[width * height];

        //Console.Out.WriteLine("Loading textures the proper way!");
        // Load image
        // Thanks ClassiCube for the idea!
        generateMipmaps(imageData.Span, image.Width, image.Height, maxLevel);

        if (firstLoad) {
            addDynamicTexture(new StillWaterTexture(this));
            addDynamicTexture(new FlowingWaterTexture(this));
        }

        firstLoad = false;
    }



    public void bind() {
        Game.graphics.tex(0, handle);
    }

    public void Dispose() {
        GL.DeleteTexture(handle);
    }

    public void update(double dt) {
        foreach (var dtexture in dtextures) {
            dtexture.tick();
        }
        // update mipmaps
        if (Settings.instance.mipmapping > 0) {
            generateMipmaps(imageData.Span, image.Width, image.Height, Settings.instance.mipmapping);
        }
    }
}