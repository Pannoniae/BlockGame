using BlockGame.GL;
using BlockGame.util;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame;

public class Textures {
    public Silk.NET.OpenGL.Legacy.GL GL;

    public BTexture2D background;
    public BTextureAtlas blockTexture;
    public BTexture2D lightTexture;
    public BTexture2D lightTexture2;

    public Dictionary<string, BTexture2D> textures = new();
    public BTexture2D waterOverlay;

    public BTexture2D sunTexture;
    public BTexture2D moonTexture;
    
    public BTexture2D particleTex;

    public const int LIGHTMAP_SIZE = 16;

    public readonly Rgba32[] lightmap = new Rgba32[256];

    public Textures(Silk.NET.OpenGL.Legacy.GL GL) {
        this.GL = GL;

        background = get("textures/bg.png");
        lightTexture = get("textures/lightmap.png");
        lightTexture2 = get("textures/lightmap.png");
        waterOverlay = get("textures/water.png");
        sunTexture = get("textures/sun_03.png");
        moonTexture = get("textures/moon_01.png");
        
        particleTex = get("textures/particle.png");
        
        blockTexture = new BTextureAtlas("textures/blocks.png", 16);
        blockTexture.reload();

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
        return lightTexture.getPixel(y, x);
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
        const int LIGHTMAP_SIZE = 16;

        const float BASE = 0.04f;
        const float INVBASE = 1 - BASE;

        float ambientBrightness = 1f - skyDarken / 16f;

        // todo add slider
        const float userBrightness = 0f;

        fixed (Rgba32* pData = lightmap) {
            for (int i = 0; i < 256; i++) {
                int skyLevel = i >> 4; // i / 16
                int blockLevel = i & 0xF; // i % 16

                // inv sq law or some shit
                float sa = skyLevel / 15f;
                //sa = sa * sa;
                //sa = sa * sa * (3f - 2f * sa);
                //sa = 0.5f - float.Sin(float.Tan(1.05f - 2.0f * sa) / 3.0f);
                sa = sa * 0.5f * (1 - sa) + sa * sa;
                float ba = blockLevel / 15f;
                //ba = ba * ba;
                //ba = ba * ba * (3f - 2f * ba);
                // ba = 0.5f - float.Sin(float.Tan(1.05f - 2.0f * ba) / 3.0f);
                ba = ba * 0.5f * (1 - ba) + ba * ba;

                float sl = sa * ambientBrightness;
                float bl = ba;


                float br = bl;
                float bg = bl * ((bl * Meth.psiF + Meth.rhoF) * Meth.psiF + Meth.rhoF);
                float bb = bl * (bl * bl * Meth.psiF + Meth.rhoF);

                // scale red down
                float sr = sl * (ambientBrightness * Meth.psiF + Meth.rhoF);
                float sg = sl * (ambientBrightness * Meth.psiF + Meth.rhoF);
                float sb = sl;


                float r = (sr + br) * INVBASE + BASE;
                float g = (sg + bg) * INVBASE + BASE;
                float b = (sb + bb) * INVBASE + BASE;

                r = float.Pow(float.Clamp(r, 0f, 1f), 1f - userBrightness * 0.5f);
                g = float.Pow(float.Clamp(g, 0f, 1f), 1f - userBrightness * 0.5f);
                b = float.Pow(float.Clamp(b, 0f, 1f), 1f - userBrightness * 0.5f);

                // clamp & pack
                r = float.Clamp(r * INVBASE + BASE, 0f, 1f);
                g = float.Clamp(g * INVBASE + BASE, 0f, 1f);
                b = float.Clamp(b * INVBASE + BASE, 0f, 1f);

                pData[i] = new Rgba32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 255);
            }
        }

        lightTexture.updateTexture(lightmap, 0, 0, LIGHTMAP_SIZE, LIGHTMAP_SIZE);
    }

    public void dumpLightmap() {
        using var image = new Image<Rgba32>(LIGHTMAP_SIZE, LIGHTMAP_SIZE);
        for (int x = 0; x < LIGHTMAP_SIZE; x++) {
            for (int y = 0; y < LIGHTMAP_SIZE; y++) {
                image[x, y] = lightmap[x | (y << 4)];
            }
        }

        image.Save("lightmap.png");
    }

    public unsafe void dumpAtlas() {
        var width = (int)blockTexture.width;
        var height = (int)blockTexture.height;
        var pixels = new Rgba32[width * height];
        
        fixed (Rgba32* pixelPtr = pixels) {
            GL.GetTextureImage(blockTexture.handle, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (uint)(width * height * 4), pixelPtr);
        }
        
        using var image = Image.WrapMemory<Rgba32>(pixels, width, height);
        image.Save("atlas.png");
    }
}