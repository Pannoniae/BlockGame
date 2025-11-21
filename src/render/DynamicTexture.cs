using BlockGame.GL;
using BlockGame.main;
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

public class StillWaterTexture : DynamicTexture {
    private int frameIndex;

    public override int updateFreq => 12;

    // NEW: constructor with explicit position (for stitched atlases)
    public StillWaterTexture(BTextureAtlas parent, int x, int y) : base(parent, x, y, 16, 16) { }

    // OLD: constructor with hardcoded position (for legacy system)
    public StillWaterTexture(BTextureAtlas parent) : base(parent, 0, 13 * 16, 16, 16) { }

    protected override void update() {
        if (parent?.imageData.IsEmpty == false) {
            int srcX = (frameIndex % 16) * 16 + atlasX;
            int srcY = atlasY;

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

    // NEW: constructor with explicit position
    public FlowingWaterTexture(BTextureAtlas parent, int x, int y) : base(parent, x, y, 32, 32) {
        src = new Rgba32[width * height];
        initSrc();
    }

    // OLD: constructor with hardcoded position
    public FlowingWaterTexture(BTextureAtlas parent) : base(parent, 1 * parent.atlasSize, 14 * parent.atlasSize, 32, 32) {
        src = new Rgba32[width * height];
        initSrc();
    }

    void initSrc() {
        if (parent?.imageData.IsEmpty == false) {
            var span = parent.imageData.Span;
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    src[y * width + x] = span[(atlasY + y) * parent.image.Width + (atlasX + x)];
                }
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
    private float[] heat;
    private float[] nextHeat;
    private float[] activation;
    private float[] nextActivation;
    private float[] avg;
    private float[] nextAvg;
    private XRandom rng;

    public override int updateFreq => 3;

    // NEW: constructor with explicit position
    public StillLavaTexture(BTextureAtlas parent, int x, int y) : base(parent, x, y, 16, 16) {
        initArrays();
    }

    // OLD: constructor with hardcoded position
    public StillLavaTexture(BTextureAtlas parent) : base(parent, 0, 16 * 16, 16, 16) {
        initArrays();
    }

    void initArrays() {
        heat = new float[width * height];
        nextHeat = new float[width * height];
        activation = new float[width * height];
        nextActivation = new float[width * height];
        avg = new float[width * height];
        nextAvg = new float[width * height];
        rng = new XRandom(1337);

        for (int i = 0; i < heat.Length; i++) {
            float r = rng.NextSingle();
            heat[i] = r * r * 0.8f;
            activation[i] = rng.NextSingle() * 0.6f;
        }
    }

    protected override void update() {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int idx = y * width + x;

                int xm = (x - 1 + width) % width;
                int xp = (x + 1) % width;
                int ym = (y - 1 + height) % height;
                int yp = (y + 1) % height;

                // blur the fuck out of it

                // (apply the heat to the avg array)

                // blur 2x2
                float b = heat[idx] * 0.2f +
                          // 0.6
                          heat[ym * width + x] * 0.15f +
                          heat[yp * width + x] * 0.15f +
                          heat[y * width + xm] * 0.15f +
                          heat[y * width + xp] * 0.15f +
                          // 0.2
                          heat[ym * width + xm] * 0.05f +
                          heat[yp * width + xp] * 0.05f +
                          heat[ym * width + xm] * 0.05f +
                          heat[yp * width + xp] * 0.05f;

                nextAvg[idx] = b;

                float h = heat[idx];
                float a = activation[idx];


                float act = a * 0.1f +
                            (activation[ym * width + x] + activation[yp * width + x] +
                             activation[y * width + xm] + activation[y * width + xp]) * 0.25f;

                float heatd = h * 0.98f +
                              (heat[ym * width + x] + heat[yp * width + x] +
                               heat[y * width + xm] + heat[y * width + xp]) * (0.02f / 4f);

                float prod = a * (1.0f - h) * 6f; // brighter spots
                float decay = h * a * 0.8f;

                if (rng.NextSingle() < 0.4 * Game.fixeddt) {
                    act += 0.4f;
                }

                nextActivation[idx] = Meth.clamp((act - decay) * 0.585f, 0f, 1.0f);
                nextHeat[idx] = Meth.clamp((heatd + prod) * 0.975f, 0f, 2f);
            }
        }

        // swap
        for (int i = 0; i < heat.Length; i++) {
            heat[i] = nextHeat[i];
            activation[i] = nextActivation[i];
            avg[i] = nextAvg[i];
        }

        // render
        for (int i = 0; i < heat.Length; i++) {
            pixels[i] = c(avg[i]);
        }

        markDirty();
    }

    public static Rgba32 c(float i) {
        // 0.0: dark red
        // 0.3: bright red
        // 0.6: orange
        // 0.9: yellow
        // 1.2: white-hot

        // remap from [0,1] to [0,1.5]
        i *= 2f;
        //i = Meth.clamp(i, 0f, 1.5f);

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

        return new Rgba32(
            (byte)Meth.clamp(r, 0f, 255f),
            (byte)Meth.clamp(g, 0f, 255f),
            (byte)Meth.clamp(b, 0f, 255f),
            255);
    }
}

public class FlowingLavaTexture : DynamicTexture {
    private float[] heat;
    private float[] nextHeat;
    private float[] activation;
    private float[] nextActivation;
    private float[] avg;
    private float[] avgNext;
    private XRandom rng;

    public override int updateFreq => 6;

    // NEW: constructor with explicit position
    public FlowingLavaTexture(BTextureAtlas parent, int x, int y) : base(parent, x, y, 32, 32) {
        initArrays();
    }

    // OLD: constructor with hardcoded position
    public FlowingLavaTexture(BTextureAtlas parent) : base(parent, 1 * 16, 17 * 16, 32, 32) {
        initArrays();
    }

    void initArrays() {
        heat = new float[width * height];
        nextHeat = new float[width * height];
        activation = new float[width * height];
        nextActivation = new float[width * height];
        avg = new float[width * height];
        avgNext = new float[width * height];
        rng = new XRandom(1338);

        for (int i = 0; i < heat.Length; i++) {
            float r = rng.NextSingle();
            heat[i] = r * r * 0.8f;
            activation[i] = rng.NextSingle() * 0.6f;
        }
    }

    protected override void update() {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int idx = y * width + x;

                int xm = (x - 1 + width) % width;
                int xp = (x + 1) % width;
                int ym = (y - 1 + height) % height;
                int yp = (y + 1) % height;

                // blur the fuck out of it
                // (apply the heat to the avg array)

                // blur 2x2
                float b = heat[idx] * 0.2f +
                          heat[ym * width + x] * 0.15f +
                          heat[yp * width + x] * 0.15f +
                          heat[y * width + xm] * 0.15f +
                          heat[y * width + xp] * 0.15f +
                          // 0.2
                          heat[ym * width + xm] * 0.05f +
                          heat[yp * width + xp] * 0.05f +
                          heat[ym * width + xm] * 0.05f +
                          heat[yp * width + xp] * 0.05f;

                avgNext[idx] = b;

                float h = heat[idx];
                float a = activation[idx];

                float act = a * 0.1f +
                            (activation[ym * width + x] + activation[yp * width + x] +
                             activation[y * width + xm] + activation[y * width + xp]) * 0.25f;

                float heatd = h * 0.98f +
                              (heat[ym * width + x] + heat[yp * width + x] +
                               heat[y * width + xm] + heat[y * width + xp]) * (0.02f / 4f);


                float prod = a * (1.0f - h) * 6f; // brighter spots
                float decay = h * a * 0.8f;

                if (rng.NextSingle() < 0.4 * Game.fixeddt) {
                    act += 0.4f;
                }

                nextActivation[idx] = Meth.clamp((act - decay) * 0.585f, 0f, 1.0f);
                nextHeat[idx] = Meth.clamp((heatd + prod) * 0.975f, 0f, 2f);
            }
        }

        // swap
        for (int i = 0; i < heat.Length; i++) {
            heat[i] = nextHeat[i];
            activation[i] = nextActivation[i];
        }

        // scroll
        if (tickCounter % 3 == 0) {
            for (int y = 0; y < height; y++) {
                float lastHeat = heat[y * width + width - 1];
                float lastAct = activation[y * width + width - 1];
                for (int x = width - 1; x > 0; x--) {
                    heat[y * width + x] = heat[y * width + x - 1];
                    activation[y * width + x] = activation[y * width + x - 1];
                }

                heat[y * width] = lastHeat;
                activation[y * width] = lastAct;
            }
        }

        // avg
        for (int i = 0; i < heat.Length; i++) {
            avg[i] = avgNext[i];
        }

        // render
        for (int i = 0; i < heat.Length; i++) {
            pixels[i] = StillLavaTexture.c(avg[i]);
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

    public override int updateFreq => 3;

    // NEW: constructor with explicit position
    public FireTexture(BTextureAtlas parent, int x, int y) : base(parent, x, y, 16, 16) {
        noise = new SimplexNoise(777);
    }

    // OLD: constructor with hardcoded position
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

                float sy = ny + dt * 0.4f;

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

                if (ny < 0.3f && dist > 0.8f) {
                    d = 0f;
                }

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