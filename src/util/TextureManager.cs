using Silk.NET.OpenGL;
using TrippyGL;
using TrippyGL.ImageSharp;
using Texture = TrippyGL.Texture;

namespace BlockGame.util;

public class TextureManager {

    public GL GL;
    public GraphicsDevice GD;

    public Texture2D blockTextureGUI;
    public Texture2D background;
    public BTextureAtlas blockTexture;
    public BTexture2D lightTexture;

    public Dictionary<string, Texture2D> textures = new();

    public TextureManager(GL GL, GraphicsDevice GD) {
        this.GL = GL;
        this.GD = GD;

        blockTextureGUI = Texture2DExtensions.FromFile(GD, "textures/blocks.png");
        background = Texture2DExtensions.FromFile(GD, "textures/bg.png");
        blockTexture = new BTextureAtlas("textures/blocks.png", 16);
        lightTexture = new BTexture2D("textures/lightmap.png");
    }

    public Texture2D get(string path) {
        if (!textures.TryGetValue(path, out _)) {
            textures[path] = Texture2DExtensions.FromFile(GD, path);;
        }
        return textures[path];
    }
}