using BlockGame.main;
using FontStashSharp;
using FontStashSharp.Rasterizers.FreeType;

namespace BlockGame.util.font;

public class FontLoader {
    public TextRenderer renderer;
    public TextRenderer rendererl;
    public TextRenderer3D renderer3D;
    public TextRendererBlockEntity rendererBlockEntity;
    public FontSystem fontSystem;
    public FontSystem fontSystemThin;
    public FontSystem fontSystemThinl;
    public float thinFontAspectRatio = 0.75f; // 3:4 ratio (narrower)

    public FontLoader(string name, string name2) {
        var settings = new FontSystemSettings {
            FontLoader = new FreeTypeLoader(),
            TextureWidth = 256,
            TextureHeight = 256
        };
        renderer = new TextRenderer();
        rendererl = new TextRenderer(linear: true);
        renderer3D = new TextRenderer3D();
        rendererBlockEntity = new TextRendererBlockEntity();

        // todo hack something together in a better way
        fontSystem = new FontSystem(settings);
        using var s = Game.assets.open(name);
        fontSystem.AddFont(s);
        fontSystemThin = new FontSystem(settings);
        using FileStream s2 = Game.assets.open(name2);
        fontSystemThin.AddFont(s2);
        fontSystemThinl = new FontSystem(settings);
        using FileStream s3 = Game.assets.open(name2);
        fontSystemThinl.AddFont(s3);
    }
}