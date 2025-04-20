using BlockGame.GL;
using Silk.NET.OpenGL;

namespace BlockGame.util;

public class TextureManager {

    public Silk.NET.OpenGL.GL GL;

    public BTexture2D blockTextureGUI;
    public BTexture2D background;
    public BTextureAtlas blockTexture;
    public BTexture2D lightTexture;

    public Dictionary<string, BTexture2D> textures = new();
    public BTexture2D waterOverlay;

    public TextureManager(Silk.NET.OpenGL.GL GL) {
        this.GL = GL;

        blockTextureGUI = new BTexture2D("textures/blocks.png");
        background = new BTexture2D("textures/bg.png");
        blockTexture = new BTextureAtlas("textures/blocks.png", 16);
        lightTexture = new BTexture2D("textures/lightmap.png");
        waterOverlay = new BTexture2D("textures/water.png");
    }

    public BTexture2D get(string path) {
        if (!textures.TryGetValue(path, out _)) {
            textures[path] = new BTexture2D(path);;
        }
        return textures[path];
    }
}