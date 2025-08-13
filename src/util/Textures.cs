using BlockGame.GL;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.util;

public class Textures {
    public Silk.NET.OpenGL.GL GL;

    public BTexture2D background;
    public BTextureAtlas blockTexture;
    public BTexture2D lightTexture;

    public Dictionary<string, BTexture2D> textures = new();
    public BTexture2D waterOverlay;

    public BTexture2D sunTexture;
    public BTexture2D moonTexture;

    public const int LIGHTMAP_SIZE = 16;

    /** Lightmap texture data */
    public byte[] textureData = new byte[LIGHTMAP_SIZE * LIGHTMAP_SIZE * 4];

    public readonly Rgba32[] lightmap = new Rgba32[256];

    public Textures(Silk.NET.OpenGL.GL GL) {
        this.GL = GL;

        background = new BTexture2D("textures/bg.png");
        blockTexture = new BTextureAtlas("textures/blocks.png", 16);
        lightTexture = new BTexture2D("textures/lightmap.png");
        waterOverlay = new BTexture2D("textures/water.png");
        sunTexture = new BTexture2D("textures/sun_03.png");
        moonTexture = new BTexture2D("textures/moon_01.png");

        // reload textures
        background.reload();
        blockTexture.reload();
        lightTexture.reload();
        waterOverlay.reload();
        sunTexture.reload();
        moonTexture.reload();

        // init lightmap
        for (int i = 0; i < 256; i++) {
            lightmap[i] = lightTexture.getPixel(i & 15, i >> 4);
        }
    }

    public BTexture2D get(string path) {
        if (!textures.TryGetValue(path, out _)) {
            textures[path] = new BTexture2D(path);
            textures[path].reload();
        }

        return textures[path];
    }

    public Rgba32 light(int x, int y) {
        return lightmap[y | (x << 4)];
    }

    /**
     * Update dynamic textures
     * <param name="dt"></param>
     */
    public void update(double dt) {
        // update animated textures in block atlas
        blockTexture.update(dt);

        // update lightmap every 5 ticks
        // or don't, this doesn't eat our performance yet
        //if (Game.globalTick % 5 == 0) {
        if (Game.world != null) {
            float skyDarken = Game.world.getSkyDarkenFloat(Game.world.worldTick);
            updateLightmap(skyDarken);
        }
        //}
    }

    private unsafe void updateLightmap(float skyDarken) {
        var tex = lightTexture;
        const int LIGHTMAP_SIZE = 16;
    
        Array.Clear(textureData);
        float skyAtten = skyDarken / 16f;
    
        for (int skyLight = 0; skyLight < LIGHTMAP_SIZE; skyLight++) {
            float skyBrightness = skyLight / 15f;
            skyBrightness *= 1f - skyAtten * 0.8f;
            skyBrightness = MathF.Pow(skyBrightness, 2.2f);
        
            for (int blockLight = 0; blockLight < LIGHTMAP_SIZE; blockLight++) {
                float blockBrightness = MathF.Pow(blockLight / 15f, 2.2f);
            
                // Blocklight: warm yellow-orange (torch color)
                float blockR = blockBrightness;
                float blockG = blockBrightness * 0.9f;
                float blockB = blockBrightness * 0.7f;
            
                // Skylight: white->blue transition
                float skyR = skyBrightness * (1f - skyAtten * 0.3f);
                float skyG = skyBrightness * (1f - skyAtten * 0.15f);
                float skyB = skyBrightness;
            
                // Take maximum of each channel separately
                float r = MathF.Max(blockR, skyR);
                float g = MathF.Max(blockG, skyG);
                float b = MathF.Max(blockB, skyB);
            
                int idx = (skyLight * LIGHTMAP_SIZE + blockLight) * 4;
                textureData[idx + 0] = (byte)(r * 255);
                textureData[idx + 1] = (byte)(g * 255);
                textureData[idx + 2] = (byte)(b * 255);
                textureData[idx + 3] = 255;
            }
        }
    
        tex.updateTexture(textureData, 0, 0, LIGHTMAP_SIZE, LIGHTMAP_SIZE);
    }
}