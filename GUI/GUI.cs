using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.Fonts;
using TrippyGL;
using TrippyGL.Fonts;
using TrippyGL.Fonts.Building;
using TrippyGL.Fonts.Extensions;
using TrippyGL.ImageSharp;
using PrimitiveType = TrippyGL.PrimitiveType;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

/// <summary>
/// GUI class which can draw onto the screen.
/// Supports scaling with guiScale.
/// </summary>
public class GUI {

    public GL GL;
    public GraphicsDevice GD;

    public SimpleShaderProgram shader;

    public int guiScale = 4;

    public TextureBatcher tb;
    public Texture2D guiTexture;
    public Texture2D colourTexture;
    public TextureFont guiFont;
    public TextureFont guiFontUnicode;

    public Rectangle buttonRect = new(0, 0, 64, 16);

    public static GUI instance;

    public GUI() {
        GL = Game.instance.GL;
        GD = Game.instance.GD;
        tb = new TextureBatcher(Game.instance.GD);
        shader = SimpleShaderProgram.Create<VertexColorTexture>(Game.instance.GD, excludeWorldMatrix: true);
        tb.SetShaderProgram(shader);
        guiTexture = Texture2DExtensions.FromFile(Game.instance.GD, "textures/gui.png");
        colourTexture = Texture2DExtensions.FromFile(Game.instance.GD, "textures/debug.png");
        instance = this;
    }

    public void loadFonts() {
        if (!File.Exists(Constants.fontFile)) {
            var collection = new FontCollection();
            var family = collection.Add("fonts/unifont-15.1.04.ttf");
            var font = family.CreateFont(12, FontStyle.Regular);
            using var ff = FontBuilderExtensions.CreateFontFile(font, (char)0, (char)127);
            guiFont = ff.CreateFont(Game.instance.GD);
            ff.WriteToFile(Constants.fontFile);
        }
        else {
            using var ff = TrippyFontFile.FromFile(Constants.fontFile);
            guiFont = ff.CreateFont(Game.instance.GD);
        }
    }

    public void loadUnicodeFont() {
        if (!File.Exists(Constants.fontFileUnicode)) {
            var collection = new FontCollection();
            var family = collection.Add("fonts/unifont-15.1.04.ttf");
            var font = family.CreateFont(12, FontStyle.Regular);
            using var ffu = FontBuilderExtensions.CreateFontFile(font, (char)0, (char)0x3000);
            guiFontUnicode = ffu.CreateFont(Game.instance.GD);
            ffu.WriteToFile(Constants.fontFileUnicode);
        }
        else {
            using var ffu = TrippyFontFile.FromFile(Constants.fontFileUnicode);
            guiFontUnicode = ffu.CreateFont(Game.instance.GD);
        }
    }



    public void resize(Vector2D<int> size) {
        shader.Projection = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);
        //worldShader.Projection = Game.instance.world.player.camera.getProjectionMatrix();
        //worldShader.View = Game.instance.world.player.camera.getViewMatrix(1);
    }


    public void draw(Texture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position, source, color == default ? Color4b.White : color, guiScale, 0f, origin, depth);
    }

    public void drawString(string text, Vector2 position, Color4b color = default) {
        tb.DrawString(guiFont, text, position, color == default ? Color4b.White : color);
    }

    public void drawStringCentred(string text, Vector2 position, Color4b color = default) {
        var offsetX = guiFont.Measure(text).X / 2;
        tb.DrawString(guiFont, text, new Vector2(position.X - offsetX, position.Y), color == default ? Color4b.White : color);
    }

    public void drawLineWorld(TextureBatcher tb, Texture2D texture, Vector3 start, Vector3 end, Color4b color = default) {
        // TODO
        GD.DrawArrays(PrimitiveType.Lines, 0, 2);
    }
}