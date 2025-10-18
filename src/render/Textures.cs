using BlockGame.GL;
using BlockGame.main;
using BlockGame.util;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render;

public class Textures {
    public Silk.NET.OpenGL.Legacy.GL GL;

    public BTexture2D background;
    public BTextureAtlas blockTexture;
    public BTextureAtlas itemTexture;
    public BTexture2D lightTexture;
    public BTexture2D lightTexture2;

    public Dictionary<string, BTexture2D> textures = new();
    public BTexture2D waterOverlay;

    public BTexture2D sunTexture;
    public BTexture2D moonTexture;
    public BTexture2D cloudTexture;

    public BTexture2D particleTex;

    public const int LIGHTMAP_SIZE = 16;

    public readonly Rgba32[] lightmap = new Rgba32[256];
    public BTexture2D human;

    public Textures(Silk.NET.OpenGL.Legacy.GL GL) {
        this.GL = GL;

        background = get("textures/bg.png");
        lightTexture = get("textures/lightmap.png");
        lightTexture2 = get("textures/lightmap.png");
        waterOverlay = get("textures/water.png");
        sunTexture = get("textures/sun_03.png");
        moonTexture = get("textures/moon_01.png");
        cloudTexture = get("textures/clouds.png");

        particleTex = get("textures/particle.png");
        
        blockTexture = new BTextureAtlas("textures/blocks.png", 16);

        itemTexture = new BTextureAtlas("textures/items.png", 16);

        human = get("textures/character.png");


        reloadAll();
    }

    public BTexture2D get(string path) {
        if (!textures.TryGetValue(path, out _)) {
            textures[path] = new BTexture2D(path);
            textures[path].reload();
        }

        return textures[path];
    }

    public Rgba32 light(int x, int y) {
        return lightmap[x | (y << 4)];
    }

    /**
     * Update dynamic textures
     * <param name="dt"></param>
     */
    public void update(double dt) {
        // update animated textures in block atlas
        blockTexture.update(dt);
        itemTexture.update(dt);

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
        const float BASE2 = 0.16f;
        const float INVBASE = 1 - BASE;
        const float INVBASE2 = 1 - BASE2;

        float ambientBrightness = 1f - skyDarken / 16f;

        // todo add slider
        // todo add this later
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
                ba = ba * Meth.rhoF * (1 - ba) + ba * ba * 1.17f;

                float sl = sa * ambientBrightness;
                float bl = ba;


                float br = bl;
                float bg = bl * ((bl * Meth.psiF + Meth.rhoF) * Meth.psiF + Meth.rhoF);
                float bb = bl * (bl * bl * Meth.psiF + Meth.rhoF);

                // scale red down
                float sr = sl * (ambientBrightness * Meth.psiF + Meth.rhoF);
                float sg = sl * (ambientBrightness * Meth.psiF + Meth.rhoF);
                float sb = sl;

                // screen blend
                float r = (1f - (1f - sr) * (1f - br)) * INVBASE + BASE;
                float g = (1f - (1f - sg) * (1f - bg)) * INVBASE + BASE;
                float b = (1f - (1f - sb) * (1f - bb)) * INVBASE + BASE;

                // clamp & pack
                r = float.Clamp(r, 0f, 1f);
                g = float.Clamp(g, 0f, 1f);
                b = float.Clamp(b, 0f, 1f);

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

    /** reload all textures from disk */
    public void reloadAll() {
        Game.graphics.invalidateTextures();
        // reload all cached textures
        foreach (var tex in textures.Values) {
            tex.reload();
        }

        // reload atlases
        blockTexture.reload();
        itemTexture.reload();

        // regenerate lightmap
        for (int i = 0; i < 256; i++) {
            lightmap[i] = lightTexture.getPixel(i & 15, i >> 4);
        }
    }
}