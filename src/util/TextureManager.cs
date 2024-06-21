using Silk.NET.OpenGL;
using TrippyGL;
using TrippyGL.ImageSharp;
using Texture = TrippyGL.Texture;

namespace BlockGame.util;

public class TextureManager {

    public GL GL;
    public GraphicsDevice GD;

    public Texture2D blockTextureGUI;
    public BTextureAtlas blockTexture;
    public Texture2D lightTexture;

    public Dictionary<string, Texture> textures = new();

    public TextureManager(GL GL, GraphicsDevice GD) {
        this.GL = GL;
        this.GD = GD;

        blockTextureGUI = Texture2DExtensions.FromFile(GD, "textures/blocks.png");
        blockTexture = new BTextureAtlas("textures/blocks.png", 16);
        lightTexture = Texture2DExtensions.FromFile(GD, "textures/lightmap.png");
    }

    public void load(string path, string name) {
        if (textures.ContainsKey(name))
            return;
        textures[name] = Texture2DExtensions.FromFile(GD, path);
    }

    public Texture get(string name) {
        if (!textures.TryGetValue(name, out Texture? value))
            throw new Exception($"Texture {name} not found");
        return value;
    }
}