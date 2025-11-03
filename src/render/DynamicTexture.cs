using BlockGame.GL;
using BlockGame.util;
using BlockGame.world.worldgen;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render;

public abstract class DynamicTexture {
    public BTextureAtlas parent;

    public readonly int atlasX, atlasY;
    public readonly int width, height;

    protected Rgba32[] pixels;
    protected int tickCounter;
    protected bool needsUpdate;

    public abstract int updateFreq { get; }

    protected DynamicTexture(BTextureAtlas parent, int atlasX, int atlasY, int width, int height) {
        this.parent = parent;
        this.atlasX = atlasX;
        this.atlasY = atlasY;
        this.width = width;
        this.height = height;
        pixels = new Rgba32[width * height];
    }

    public void tick() {
        tickCounter++;
        if (tickCounter % updateFreq == 0) {
            update();
            if (needsUpdate) {
                parent?.updateTexture(atlasX, atlasY, width, height, pixels);
                needsUpdate = false;
            }
        }
    }

    protected abstract void update();

    protected void markDirty() {
        needsUpdate = true;
    }
}

public class StillWaterTexture(BTextureAtlas parent) : DynamicTexture(parent, 0, 13 * 16, 16, 16) {
    private int frameIndex;

    public override int updateFreq => 12;

    protected override void update() {
        if (parent?.imageData.IsEmpty == false) {
            int srcX = (frameIndex % 16) * 16;
            int srcY = 13 * 16;

            var span = parent.imageData.Span;
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    pixels[y * width + x] = span[(srcY + y) * parent.image.Width + srcX + x];
                }
            }

            frameIndex++;
            markDirty();
        }
    }
}

public class FlowingWaterTexture : DynamicTexture {
    private readonly Rgba32[] src;

    public override int updateFreq => 8;

    public FlowingWaterTexture(BTextureAtlas parent) : base(parent, 1 * parent.atlasSize, 14 * parent.atlasSize, 32, 32) {
        src = new Rgba32[width * height];
        var span = parent.imageData.Span;
        int srcX = 1 * parent.atlasSize;
        int srcY = 14 * parent.atlasSize;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                src[y * width + x] = span[(srcY + y) * parent.image.Width + (srcX + x)];
            }
        }
    }

    protected override void update() {
        for (int y = 0; y < height; y++) {
            var lastPixel = src[y * width + width - 1];
            for (int x = width - 1; x > 0; x--) {
                src[y * width + x] = src[y * width + x - 1];
            }

            src[y * width] = lastPixel;
        }

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                pixels[y * width + x] = src[y * width + x];
            }
        }

        markDirty();
    }
}

public class StillLavaTexture : DynamicTexture {
    private readonly Rgba32[] baseTexture;
    private readonly SimplexNoise noise;

    public override int updateFreq => 15;

    public StillLavaTexture(BTextureAtlas parent) : base(parent, 0, 16 * 16, 16, 16) {
        baseTexture = new Rgba32[width * height];
        noise = new SimplexNoise(42);

        var span = parent.imageData.Span;
        int srcX = 0;
        int srcY = 16 * 16;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                baseTexture[y * width + x] = span[(srcY + y) * parent.image.Width + srcX + x];
            }
        }
    }

    protected override void update() {

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int idx = y * width + x;

                var n = noise.noise3_XYBeforeZ(x / 6f, y / 6f, tickCounter / 224f);
                var b = 0.8f + (n + 1f) / 2f * 0.3f;
                var b2 = (b + 1f) / 2f;

                // clamp to [0,1]
                b = Meth.clamp(b, 0f, 1f);
                b2 = Meth.clamp(b2, 0f, 1f);

                pixels[idx] = new Rgba32(
                    (byte)(baseTexture[idx].R * b2),
                    (byte)(baseTexture[idx].G * b),
                    (byte)(baseTexture[idx].B * b),
                    255);
            }
        }

        markDirty();
    }
}

public class FlowingLavaTexture : DynamicTexture {
    private readonly Rgba32[] baseTexture;
    private int scrollOffset;
    private readonly SimplexNoise noise;

    public override int updateFreq => 15;

    public FlowingLavaTexture(BTextureAtlas parent) : base(parent, 1 * 16, 17 * 16, 32, 32) {
        baseTexture = new Rgba32[width * height];
        noise = new SimplexNoise(42);

        var span = parent.imageData.Span;
        int srcX = 1 * 16;
        int srcY = 17 * 16;

        // store base texture
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                baseTexture[y * width + x] = span[(srcY + y) * parent.image.Width + srcX + x];
            }
        }
    }

    protected override void update() {
        scrollOffset = (scrollOffset + 1) % width;

        // apply noise to texture with scroll
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int srcX = (x - scrollOffset + width) % width;
                int srcIdx = y * width + srcX;
                int dstIdx = y * width + x;

                // the noise scrolls with the scrolling!
                var n = noise.noise3_XYBeforeZ(srcX / 6f, y / 6f, tickCounter / 224f);
                var b = 0.8f + (n + 1f) / 2f * 0.3f;
                // average between b and 1 for red channel
                var b2 = (b + 1f) / 2f;

                // clamp to [0,1]
                b = Meth.clamp(b, 0f, 1f);
                b2 = Meth.clamp(b2, 0f, 1f);

                pixels[dstIdx] = new Rgba32(
                    (byte)(baseTexture[srcIdx].R * b2),
                    (byte)(baseTexture[srcIdx].G * b),
                    (byte)(baseTexture[srcIdx].B * b),
                    255);
            }
        }

        markDirty();
    }
}

/**
 * Piece of shit code lol, this should really be fixed up..
 */
public class FireTexture : DynamicTexture {
    private readonly SimplexNoise noise;
    private const int renderHeight = 16;

    public override int updateFreq => 2;

    public FireTexture(BTextureAtlas parent) : base(parent, 3 * 16, 14 * 16, 16, 16) {
        noise = new SimplexNoise(777);
    }

    protected override void update() {
        //float t = tickCounter * 0.03f;
        float dt = tickCounter * 0.04f;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int vy = y + (renderHeight - height);
                float nx = x / (float)(width - 1);
                float ny = vy / (float)(renderHeight - 1); // 0=top, 1=bottom

                float sy = ny + dt * 0.8f;

                float d1 = noise.noise3_XYBeforeZ(x * 0.5f, sy * 4f, dt * 0.7f * 0.05f);
                float d2 = noise.noise3_XYBeforeZ(x * 1.5f, sy * 9f, dt * 1.3f * 0.05f);
                float d3 = noise.noise3_XYBeforeZ(x * 3.0f, sy * 16f, dt * 2.1f * 0.05f);

                float rd =
                    (d1 + 1f) * 0.5f * 0.7f +
                    (d2 + 1f) * 0.5f * 0.2f * ny +
                    (d3 + 1f) * 0.5f * 0.1f * ny;

                float dist = Math.Abs(nx - 0.5f) * 2f;
                float edge = 1f - dist * dist; // reduce density at edges

                float bias = ny * edge;
                float d = rd + bias;

                //if (ny > 0.9f && dist < 0.6f) {
                    //d = 1f;
                //}

                float cutoff = 0.45f + (1f - ny) * 0.15f;
                if (d < cutoff) {
                    pixels[y * width + x] = new Rgba32(0, 0, 0, 0);
                    continue;
                }

                float t1 = noise.noise3_XYBeforeZ(x * 0.6f, sy * 5f, dt * 0.6f * 0.05f);
                float t2 = noise.noise3_XYBeforeZ(x * 1.5f, sy * 8f, dt * 1.1f * 0.05f);
                float t3 = noise.noise3_XYBeforeZ(x * 3.2f, sy * 15f, dt * 2.2f * 0.05f);
                float t =
                    (t1 + 1f) * 0.5f * 0.5f +
                    (t2 + 1f) * 0.5f * 0.3f +
                    (t3 + 1f) * 0.5f * 0.2f;

                float ri = 0.5f + ny * 0.8f + (d - cutoff) * 0.8f;

                float tm = 0.3f + (1f - ny) * 1.2f;
                float i = ri * (0.7f + t * tm * 1.0f);

                float dedge = (d - cutoff) * 16f;
                if (dedge < 1f) {
                    i *= Meth.clamp(float.Sqrt(dedge), 0.6f, 1f);
                }

                i = Meth.clamp(i, 0f, 2.0f);

                // in theory:
                // 0.0: lowblack
                // 0.5: red
                // 1.0: orange-yellow
                // 2.0: white

                // mostly red
                float r = Meth.clamp(255f * float.Min(1f, i * 1.2f), 0f, 255f);

                // red -> yellow
                float gNorm = float.Min(1f, i * (1 / 1.4f));
                float g = 255f * gNorm * gNorm;
                g = Meth.clamp(g, 0f, 255f);

                // sparingly at very high intensity
                const float bthreshold = 1.2f;
                float b = i > bthreshold ? 255f * ((i - bthreshold) / (2.0f - bthreshold)) : 0f;
                b = Meth.clamp(b, 0f, 255f);

                pixels[y * width + x] = new Rgba32((byte)r, (byte)g, (byte)b, 255);
            }
        }

        markDirty();
    }
}