using FontStashSharp;
using TrippyGL;

namespace BlockGame.font;

public class FontLoader {
    public Renderer renderer;
    public FontSystem fontSystem;
    private TextureBatcher tb;

    public FontLoader(string name, TextureBatcher tb) {
        var settings = new FontSystemSettings
        {
            FontLoader = new BDFLoader()
        };
        this.tb = tb;
        renderer = new Renderer(Game.GD, tb);

        // todo hack something together in a better way
        fontSystem = new FontSystem(settings);
        fontSystem.AddFont(File.ReadAllBytes(name));
    }
}