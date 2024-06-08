using FontStashSharp;
using TrippyGL;

namespace BlockGame.util.font;

public class FontLoader {
    public Renderer renderer;
    public FontSystem fontSystem;
    public FontSystem fontSystemThin;
    private TextureBatcher tb;

    public FontLoader(TextureBatcher tb, string name, string name2) {
        var settings = new FontSystemSettings
        {
            FontLoader = new BDFLoader(),
            TextureWidth = 256,
            TextureHeight = 256
        };
        this.tb = tb;
        renderer = new Renderer(Game.GD, tb);

        // todo hack something together in a better way
        fontSystem = new FontSystem(settings);
        fontSystem.AddFont(File.ReadAllBytes(name));
        fontSystemThin = new FontSystem(settings);
        fontSystemThin.AddFont(File.ReadAllBytes(name2));
    }
}