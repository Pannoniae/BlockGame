using BlockGame.GL;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.util;

public class TextureManager {

    public Silk.NET.OpenGL.GL GL;

    public BTexture2D blockTextureGUI;
    public BTexture2D background;
    public BTextureAtlas blockTexture;
    public BTexture2D lightTexture;

    public Dictionary<string, BTexture2D> textures = new();
    public BTexture2D waterOverlay;

    public BTexture2D sunTexture;
    public BTexture2D moonTexture;
    
    public readonly Rgba32[] lightmap = new Rgba32[256];

    public TextureManager(Silk.NET.OpenGL.GL GL) {
        this.GL = GL;

        blockTextureGUI = new BTexture2D("textures/blocks.png");
        background = new BTexture2D("textures/bg.png");
        blockTexture = new BTextureAtlas("textures/blocks.png", 16);
        lightTexture = new BTexture2D("textures/lightmap.png");
        waterOverlay = new BTexture2D("textures/water.png");
        sunTexture = new BTexture2D("textures/sun_03.png");
        moonTexture = new BTexture2D("textures/moon_01.png");
        
        // init lightmap
        for (int i = 0; i < 256; i++) {
            lightmap[i] = lightTexture.getPixel(i & 15, i >> 4);
        }
    }

    public BTexture2D get(string path) {
        if (!textures.TryGetValue(path, out _)) {
            textures[path] = new BTexture2D(path);;
        }
        return textures[path];
    }

    public Rgba32 light(int x, int y) {
        return lightmap[y | (x << 4)];
    }
}