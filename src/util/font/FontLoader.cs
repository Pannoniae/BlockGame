using FontStashSharp;

namespace BlockGame.util.font;

public class FontLoader {
    public TextRenderer renderer;
    public TextRenderer3D renderer3D;
    public FontSystem fontSystem;
    public FontSystem fontSystemThin;
    public FontSystem fontSystemThin3D;

    public FontLoader(string name, string name2) {
        var settings = new FontSystemSettings
        {
            FontLoader = new BDFLoader(),
            TextureWidth = 256,
            TextureHeight = 256
        };
        renderer = new TextRenderer(Game.GD);
        renderer3D = new TextRenderer3D(Game.GD);

        // todo hack something together in a better way
        fontSystem = new FontSystem(settings);
        fontSystem.AddFont(File.ReadAllBytes(name));
        fontSystemThin = new FontSystem(settings);
        fontSystemThin.AddFont(File.ReadAllBytes(name2));
        fontSystemThin3D = new FontSystem(settings);
        fontSystemThin3D.AddFont(File.ReadAllBytes(name2));
    }
}