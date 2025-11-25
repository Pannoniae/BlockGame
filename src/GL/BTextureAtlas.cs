using BlockGame.main;
using BlockGame.render;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.world.block;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace BlockGame.GL;

public class BTextureAtlas : BTexture2D {

    public int atlasSize;

    public bool firstLoad = true;

    public List<DynamicTexture> dtextures = [];

    public Rgba32[] mipmap = null!;

    // tile positions for stitched atlases (null if loaded from single file)
    public Dictionary<(string source, int tx, int ty), Rectangle>? tilePositions;

    public float atlasRatio => atlasSize / (float)width;

    public BTextureAtlas(string path, int atlasSize) : base(path) {
        this.atlasSize = atlasSize;
        this.path = path;

        //var handle2 = GL.GenTexture();
        //GL.ActiveTexture(TextureUnit.Texture0);
        //GL.BindTexture(TextureTarget.Texture2DArray, handle2);
        //GL.TexImage3D(TextureTarget.Texture2DArray, 0, InternalFormat.Rgba8, 16, 16, 2048, 0, PixelFormat.Rgba, PixelType.Byte, null);
    }

    // Constructor for pre-loaded images (from stitched atlases)
    public BTextureAtlas(Image<Rgba32> img, int width, int height, int atlasSize = 16, bool delayInit = false) : base("") {
        this.atlasSize = atlasSize;
        image = img;
        // use actual image dimensions, not passed parameters
        this.width = img.Width;
        this.height = img.Height;
        iwidth = 1.0 / img.Width;
        iheight = 1.0 / img.Height;
        if (!delayInit) {
            uploadToGPU();
        }
    }

    public BTextureAtlas(StitchResult itemResult, int atlasSize = 16) : base("") {
        tilePositions = itemResult.tilePositions;
        this.atlasSize = atlasSize;
        image = itemResult.image;
        // use actual image dimensions, not passed parameters
        width = itemResult.width;
        height = itemResult.height;
        iwidth = 1.0 / itemResult.width;
        iheight = 1.0 / itemResult.height;
        uploadToGPU();
    }

    /**
     * Look up final UV for a tile from a source atlas
     */
    public UVPair uv(string sourcePath, int tx, int ty) {
        if (tilePositions == null)
            throw new InvalidOperationException("Not a stitched atlas! Use the StitchResult constructor.");

        // Add textures/ prefix if not already present
        if (!sourcePath.StartsWith("textures/")) {
            sourcePath = "textures/" + sourcePath;
        }

        var rect = tilePositions[(sourcePath, tx, ty)];
        float u = rect.X * atlasSize / (float)width;
        float v = rect.Y * atlasSize / (float)height;
        return new UVPair(u, v);
    }

    // NEW: Upload pre-loaded image to GPU
    protected unsafe void uploadToGPU() {
        var GL = Game.GL;
        handle = GL.CreateTexture(TextureTarget.Texture2D);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.NearestMipmapLinear);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);

        var maxLevel = Settings.instance.mipmapping;
        GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, maxLevel);

        const uint maxPossibleLevels = 5u;
        GL.TextureStorage2D(handle, maxPossibleLevels, SizedInternalFormat.Rgba8, (uint)width, (uint)height);

        if (!image.DangerousTryGetSinglePixelMemory(out imageData)) {
            throw new SkillIssueException("Couldn't load the atlas contiguously!");
        }

        mipmap = new Rgba32[width * height];
        generateMipmaps(imageData.Span, width, height, maxLevel);

        if (firstLoad) {
            onFirstLoad();
        }

        firstLoad = false;
    }

    public void addDynamicTexture(DynamicTexture dt) {
        dtextures.Add(dt);
    }
    
    public void updateTexture(int x, int y, int width, int height, Rgba32[] pixels) {
        // update CPU-side imageData so the fucking mipmaps regenerate correctly
        var span = imageData.Span;

        // Validate bounds - if out of range, this is a bug that needs fixing
        if (x < 0 || y < 0 || x + width > image.Width || y + height > image.Height) {
            throw new InvalidOperationException($"DynamicTexture out of bounds! pos=({x},{y}) size=({width},{height}) atlas=({image.Width},{image.Height}). Protected region was placed incorrectly or atlas is too small.");
        }

        for (int py = 0; py < height; py++) {
            for (int px = 0; px < width; px++) {
                span[(y + py) * image.Width + (x + px)] = pixels[py * width + px];
            }
        }

        // update the actual GPU texture
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
                var c01 = prevMipmap[ySrc * width * 2 + x1];
                var c10 = prevMipmap[y1 * width * 2 + xSrc];
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
            (byte)(totalAlpha > 0 ? 255 : 0)  // 63.75 = 255/4
        );
    }

    public unsafe void generateMipmaps(Span<Rgba32> pixelArray, int imageWidth, int imageHeight, int maxLevel) {
        var GL = Game.GL;
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

                Span<Rgba32> mipmap = this.mipmap.AsSpan(0, width * height);
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
        // Skip reload for stitched atlases (they're already loaded from memory)
        if (string.IsNullOrEmpty(path)) {
            return;
        }

        var GL = Game.GL;
        GL.DeleteTexture(handle);
        handle = GL.CreateTexture(TextureTarget.Texture2D);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.NearestMipmapLinear);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, -0.4f);
        image?.Dispose();
        using var s = Assets.open(path!);
        image = Image.Load<Rgba32>(s);
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

        width = image.Width;
        height = image.Height;
        iwidth = 1.0 / width;
        iheight = 1.0 / height;

        mipmap = new Rgba32[width * height];

        //Console.Out.WriteLine("Loading textures the proper way!");
        // Load image
        // Thanks ClassiCube for the idea!
        generateMipmaps(imageData.Span, image.Width, image.Height, maxLevel);

        if (firstLoad) {
            onFirstLoad();
        }

        firstLoad = false;
    }

    public virtual void onFirstLoad() {

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

    /**
     * Update atlas from a new stitch result (for texture pack hot-reloading)
     */
    public void updateFromStitch(StitchResult result) {
        // dispose old image
        image?.Dispose();

        // update tile positions
        tilePositions = result.tilePositions;

        // update dimensions
        width = result.width;
        height = result.height;
        iwidth = 1.0 / width;
        iheight = 1.0 / height;

        // update image
        image = result.image;

        // re-upload to GPU
        var GL = Game.GL;
        GL.DeleteTexture(handle);
        uploadToGPU();
    }
}

public class BlockTextureAtlas : BTextureAtlas {
    public Dictionary<string, Rectangle>? protectedRegions;

    // constructor for loading from file path
    public BlockTextureAtlas(string path, int atlasSize) : base(path, atlasSize) { }

    // NEW: constructor for stitched atlases
    public BlockTextureAtlas(StitchResult result)
        : base(result.image, result.width, result.height, 16, delayInit: true) {
        tilePositions = result.tilePositions;
        protectedRegions = result.protectedRegions;
        // Now upload to GPU after protected regions are set
        uploadToGPU();
    }

    /**
     * Get protected region rectangle (for DynamicTextures)
     */
    public Rectangle getRegion(string name) {
        if (protectedRegions == null)
            throw new InvalidOperationException("Not a stitched atlas!");
        return protectedRegions[name];
    }

    /**
     * Update from stitch result (for texture pack reloading)
     */
    public void updateFromStitch(StitchResult result) {
        // update protected regions
        protectedRegions = result.protectedRegions;

        // call base updateFromStitch
        ((BTextureAtlas)this).updateFromStitch(result);
    }


    public override void onFirstLoad() {
        // if we have protected regions, use them to position dynamic textures
        if (Settings.instance.noAnimation) {
            return;
        }

        if (protectedRegions != null) {
            var waterStillRect = getRegion("waterStill");
            addDynamicTexture(new StillWaterTexture(this, waterStillRect.X, waterStillRect.Y));

            var waterFlowRect = getRegion("waterFlowing");
            addDynamicTexture(new FlowingWaterTexture(this, waterFlowRect.X, waterFlowRect.Y));

            var lavaStillRect = getRegion("lavaStill");
            addDynamicTexture(new StillLavaTexture(this, lavaStillRect.X, lavaStillRect.Y));

            var lavaFlowRect = getRegion("lavaFlowing");
            addDynamicTexture(new FlowingLavaTexture(this, lavaFlowRect.X, lavaFlowRect.Y));

            var fireRect = getRegion("fire");
            addDynamicTexture(new FireTexture(this, fireRect.X, fireRect.Y));
        }
        else {
            // fallback to hardcoded positions (old system)
            addDynamicTexture(new StillWaterTexture(this));
            addDynamicTexture(new FlowingWaterTexture(this));
            addDynamicTexture(new StillLavaTexture(this));
            addDynamicTexture(new FlowingLavaTexture(this));
            addDynamicTexture(new FireTexture(this));
        }
    }
}