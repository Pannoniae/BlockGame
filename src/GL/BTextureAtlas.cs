using System.Numerics;
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

    // CPU copies of miplevels 1..max
    private Rgba32[][] mips = [];

    // tile positions for stitched atlases (null if loaded from single file)
    public Dictionary<(string source, int tx, int ty), Rectangle>? tilePositions;

    public Vector2 atlasRatio => new Vector2(atlasSize / (float)width, atlasSize / (float)height);

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
        if (tilePositions == null) {
            throw new InvalidOperationException("Not a stitched atlas! Use the StitchResult constructor.");
        }

        // Add textures/ prefix if not already present
        if (!sourcePath.StartsWith("textures/")) {
            sourcePath = "textures/" + sourcePath;
        }

        var rect = tilePositions[(sourcePath, tx, ty)];
        float u = rect.X / (float)atlasSize;
        float v = rect.Y / (float)atlasSize;
        return new UVPair(u, v);
    }

    private void createTex() {
        var GL = Game.GL;
        GL.DeleteTexture(handle);
        handle = GL.CreateTexture(TextureTarget.Texture2D);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        GL.TextureParameter(handle, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        // NEAREST between levels too cuz linear would bleed alpha0 black pixels
        // proper fix is emissive in its own texture until then we'll suck it up
        GL.TextureParameter(handle, TextureParameterName.TextureMinFilter, (int)GLEnum.NearestMipmapNearest);
        GL.TextureParameter(handle, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        GL.TextureParameter(handle, TextureParameterName.TextureBaseLevel, 0);
        GL.TextureParameter(handle, TextureParameterName.TextureMaxLevel, Settings.instance.mipmapping);
        getLodBias();
        GL.TextureStorage2D(handle, 5u, SizedInternalFormat.Srgb8Alpha8, (uint)width, (uint)height);
    }

    protected void uploadToGPU() {
        createTex();
        if (!image.DangerousTryGetSinglePixelMemory(out imageData)) {
            throw new SkillIssueException("Couldn't load the atlas contiguously!");
        }

        generateMipmaps(Settings.instance.mipmapping);

        if (firstLoad) {
            onFirstLoad();
        }

        firstLoad = false;
    }

    /**
     * supersampling makes it look more detailed so negativebias to make it look sharper...
     */
    public void getLodBias() {
        var bias = Settings.instance.perSample && Settings.instance.msaa > 1 ? -0.5f * float.Log2(Settings.instance.msaa) : 0f;
        Game.GL.TextureParameter(handle, TextureParameterName.TextureLodBias, bias);
    }

    public void addDynamicTexture(DynamicTexture dt) {
        dtextures.Add(dt);
    }

    public void updateTexture(int x, int y, int width, int height, Rgba32[] pixels) {
        // update CPU-side imageData so the fucking mipmaps regenerate correctly
        var span = imageData.Span;

        // Validate bounds - if out of range, this is a bug that needs fixing
        if (x < 0 || y < 0 || x + width > image.Width || y + height > image.Height) {
            throw new InvalidOperationException(
                $"DynamicTexture out of bounds! pos=({x},{y}) size=({width},{height}) atlas=({image.Width},{image.Height}). Protected region was placed incorrectly or atlas is too small.");
        }

        for (int py = 0; py < height; py++) {
            pixels.AsSpan(py * width, width).CopyTo(span.Slice((y + py) * image.Width + x, width));
        }

        uploadRegion(0, span, image.Width, x, y, width, height);
        var maxLevel = Settings.instance.mipmapping;
        if (maxLevel > 0) {
            genMipMaps(x, y, width, height, maxLevel);
        }
    }

    private unsafe void uploadRegion(int level, ReadOnlySpan<Rgba32> src, int stride, int x, int y, int w, int h) {
        var GL = Game.GL;
        GL.PixelStore(PixelStoreParameter.UnpackRowLength, stride);
        fixed (Rgba32* p = &src[y * stride + x]) {
            GL.TextureSubImage2D(handle, level, x, y, (uint)w, (uint)h, PixelFormat.Rgba, PixelType.UnsignedByte, p);
        }

        GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
    }

    private static void generateMipmap(ReadOnlySpan<Rgba32> src, int srcStride, Span<Rgba32> dst, int dstStride, int x0, int y0, int w, int h) {
        for (int y = y0; y < y0 + h; y++) {
            int ySrc = y * 2;
            for (int x = x0; x < x0 + w; x++) {
                int xSrc = x * 2;
                var c00 = src[ySrc * srcStride + xSrc];
                var c01 = src[ySrc * srcStride + xSrc + 1];
                var c10 = src[(ySrc + 1) * srcStride + xSrc];
                var c11 = src[(ySrc + 1) * srcStride + xSrc + 1];
                dst[y * dstStride + x] = avgopaque(c00, c01, c10, c11);
            }
        }
    }

    // sRGB<->linear lut
    // todo move into meth?
    private static readonly float[] s2l = buildS2L();
    private static readonly byte[] l2s = buildL2S();

    private static float[] buildS2L() {
        var lut = new float[256];
        for (int i = 0; i < 256; i++) {
            float c = i / 255f;
            lut[i] = c <= 0.04045f ? c / 12.92f : float.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        return lut;
    }

    private static byte[] buildL2S() {
        var lut = new byte[4097];
        for (int i = 0; i <= 4096; i++) {
            float c = i / 4096f;
            float s = c <= 0.0031308f ? c * 12.92f : 1.055f * float.Pow(c, 1.0f / 2.4f) - 0.055f;
            lut[i] = (byte)(s * 255f + 0.5f);
        }

        return lut;
    }

    private static Rgba32 avgopaque(Rgba32 c0, Rgba32 c1, Rgba32 c2, Rgba32 c3) {
        int w0 = c0.A > 0 ? 1 : 0, w1 = c1.A > 0 ? 1 : 0, w2 = c2.A > 0 ? 1 : 0, w3 = c3.A > 0 ? 1 : 0;
        int n = w0 + w1 + w2 + w3;
        if (n == 0) {
            return default;
        }

        float inv = 1f / n;
        float r = (s2l[c0.R] * w0 + s2l[c1.R] * w1 + s2l[c2.R] * w2 + s2l[c3.R] * w3) * inv;
        float g = (s2l[c0.G] * w0 + s2l[c1.G] * w1 + s2l[c2.G] * w2 + s2l[c3.G] * w3) * inv;
        float b = (s2l[c0.B] * w0 + s2l[c1.B] * w1 + s2l[c2.B] * w2 + s2l[c3.B] * w3) * inv;
        int a = (c0.A * w0 + c1.A * w1 + c2.A * w2 + c3.A * w3 + n / 2) / n;
        return new Rgba32(l2s[(int)(r * 4096f + 0.5f)], l2s[(int)(g * 4096f + 0.5f)], l2s[(int)(b * 4096f + 0.5f)], (byte)a);
    }

    public void generateMipmaps(int maxLevel) {
        // todo a texture pack reload can change the atlas size so we reallocate but this should really be conditional + invalidate on texture pack reload
        mips = new Rgba32[maxLevel][];
        int w = image.Width, h = image.Height;
        for (int lvl = 0; lvl < maxLevel; lvl++) {
            w = int.Max(w / 2, 1);
            h = int.Max(h / 2, 1);
            mips[lvl] = new Rgba32[w * h];
        }

        uploadRegion(0, imageData.Span, image.Width, 0, 0, image.Width, image.Height);

        if (maxLevel > 0) {
            genMipMaps(0, 0, image.Width, image.Height, maxLevel);
        }
    }

    private void genMipMaps(int x, int y, int w, int h, int maxLevel) {
        ReadOnlySpan<Rgba32> src = imageData.Span;
        int srcW = image.Width, srcH = image.Height;
        int x1 = x + w, y1 = y + h;
        for (int lvl = 1; lvl <= maxLevel; lvl++) {
            int dstW = int.Max(srcW / 2, 1), dstH = int.Max(srcH / 2, 1);
            x >>= 1;
            y >>= 1;
            x1 = int.Min((x1 + 1) >> 1, dstW);
            y1 = int.Min((y1 + 1) >> 1, dstH);
            int rw = int.Max(x1 - x, 1), rh = int.Max(y1 - y, 1);

            var dst = mips[lvl - 1].AsSpan();
            generateMipmap(src, srcW, dst, dstW, x, y, rw, rh);
            uploadRegion(lvl, dst, dstW, x, y, rw, rh);

            src = dst;
            srcW = dstW;
            srcH = dstH;
        }
    }

    public override void reload() {
        // Skip reload for stitched atlases (they're already loaded from memory)
        if (string.IsNullOrEmpty(path)) {
            return;
        }

        image?.Dispose();
        using var s = Assets.open(path!);
        image = Image.Load<Rgba32>(s);
        width = image.Width;
        height = image.Height;
        iwidth = 1.0 / width;
        iheight = 1.0 / height;

        uploadToGPU();
    }

    public virtual void onFirstLoad() {
    }

    public void update(double dt) {
        foreach (var dtexture in dtextures) {
            dtexture.tick();
        }
    }

    /**
     * Update atlas from a new stitch result (for texture pack hot-reloading)
     */
    public void updateFromStitch(StitchResult result) {
        image?.Dispose();
        tilePositions = result.tilePositions;

        width = result.width;
        height = result.height;
        iwidth = 1.0 / width;
        iheight = 1.0 / height;
        image = result.image;

        uploadToGPU();
    }
}

public class BlockTextureAtlas : BTextureAtlas {
    public Dictionary<string, Rectangle>? protectedRegions;

    // constructor for loading from file path
    public BlockTextureAtlas(string path, int atlasSize) : base(path, atlasSize) {
    }

    // NEW: constructor for stitched atlases
    public BlockTextureAtlas(StitchResult result)
        : base(result.image, result.width, result.height, 16, delayInit:true) {
        tilePositions = result.tilePositions;
        protectedRegions = result.protectedRegions;
        // Now upload to GPU after protected regions are set
        uploadToGPU();
    }

    /**
     * Get protected region rectangle (for DynamicTextures)
     */
    public Rectangle getRegion(string name) {
        if (protectedRegions == null) {
            throw new InvalidOperationException("Not a stitched atlas!");
        }

        return protectedRegions[name];
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

    /**
     * Update from stitch result (for texture pack reloading)
     */
    public void updateFromStitch(StitchResult result) {
        // update protected regions
        protectedRegions = result.protectedRegions;

        // call base updateFromStitch
        ((BTextureAtlas)this).updateFromStitch(result);
    }
}