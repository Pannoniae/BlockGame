using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.Fonts;
using TrippyGL;
using TrippyGL.Fonts;
using TrippyGL.Fonts.Building;
using TrippyGL.Fonts.Extensions;
using TrippyGL.ImageSharp;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class GUI {

    public GL GL;
    public GraphicsDevice GD;

    public Screen screen;

    public SimpleShaderProgram shader;

    public bool debugScreen = true;

    public int guiScale = 4;

    public TextureBatch tb;
    public Texture2D guiTexture;
    public Texture2D colourTexture;
    public TextureFont guiFont;

    public Rectangle buttonRect = new(0, 0, 64, 16);
    public int centreX;
    public int centreY;

    public GUI() {
        GL = Game.instance.GL;
        GD = Game.instance.GD;
        centreX = Game.instance.centreX;
        centreY = Game.instance.centreY;
        tb = new TextureBatch(Game.instance.GD);
        shader = SimpleShaderProgram.Create<VertexColorTexture>(Game.instance.GD);
        tb.SetShaderProgram(shader);
        Screens.initScreens(this);
        guiTexture = Texture2DExtensions.FromFile(Game.instance.GD, "textures/gui.png");
        colourTexture = Texture2DExtensions.FromFile(Game.instance.GD, "textures/debug.png");

        if (!File.Exists(Constants.fontFile)) {
            var collection = new FontCollection();
            var family = collection.Add("fonts/unifont-15.1.04.ttf");
            var font = family.CreateFont(12, FontStyle.Regular);
            using var ff = FontBuilderExtensions.CreateFontFile(font);
            guiFont = ff.CreateFont(Game.instance.GD);
            ff.WriteToFile(Constants.fontFile);
        }
        else {
            using var ff = TrippyFontFile.FromFile(Constants.fontFile);
            guiFont = ff.CreateFont(Game.instance.GD);
        }

        screen = Screens.MAIN_MENU;
    }



    public void resize(Vector2D<int> size) {
        Game.instance.GD.SetViewport(0, 0, (uint)size.X, (uint)size.Y);
        shader.Projection = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);
        centreX = Game.instance.centreX;
        centreY = Game.instance.centreY;
        screen.resize();
    }

    public void drawScreen() {
        //tb.Begin();
        screen.draw();
        //tb.End();
    }

    public void imGuiDraw() {
        screen.imGuiDraw();
    }


    public void draw(TextureBatch tb, Texture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position, source, color == default ? Color4b.White : color, guiScale, 0f, origin, depth);
    }
}