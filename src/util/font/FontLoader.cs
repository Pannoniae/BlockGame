using BlockGame.main;
using FontStashSharp;
using FontStashSharp.Rasterizers.FreeType;

namespace BlockGame.util.font;

public class FontLoader {
    public TextRenderer renderer;
    public TextRenderer3D renderer3D;
    public FontSystem fontSystem;
    public FontSystem fontSystemThin;
    public float thinFontAspectRatio = 0.75f; // 3:4 ratio (narrower)

    public FontLoader(string name, string name2) {
        var settings = new FontSystemSettings {
            FontLoader = new FreeTypeLoader(),
            TextureWidth = 256,
            TextureHeight = 256
        };
        renderer = new TextRenderer();
        renderer3D = new TextRenderer3D();

        // todo hack something together in a better way
        fontSystem = new FontSystem(settings);
        using var s = Game.assets.open(name);
        fontSystem.AddFont(s);
        fontSystemThin = new FontSystem(settings);
        using FileStream s2 = Game.assets.open(name2);
        fontSystemThin.AddFont(s2);
    }
}