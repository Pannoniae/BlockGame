using System.Numerics;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.Fonts;
using TrippyGL;
using TrippyGL.Fonts;
using TrippyGL.Fonts.Building;
using TrippyGL.Fonts.Extensions;
using TrippyGL.ImageSharp;
using Rectangle = System.Drawing.Rectangle;
using VertexArray = TrippyGL.VertexArray;

namespace BlockGame;

public class GUI {
    public Matrix4x4 ortho;

    public GL GL;

    public FloatVAO crosshair;

    public Shader guiShader;
    private SimpleShaderProgram shader;
    public int projection;
    public int uColor;

    public bool debugScreen = true;

    public int guiScale = 4;

    public TextureBatch tb;
    public Texture2D guiTexture;
    private TextureFont guiFont;

    public Rectangle buttonRect = new Rectangle(0, 0, 64, 16);

    public const int crosshairSize = 10;
    public const int crosshairThickness = 2;

    public const string fontFile = "guifont.fnt";

    private const long MEGABYTES = 1 * 1024 * 1024;

    public GUI() {
        GL = Game.instance.GL;
        crosshair = new FloatVAO();
        guiShader = new Shader(Game.instance.GL, "gui.vert", "gui.frag");
        projection = guiShader.getUniformLocation("projection");
        uColor = guiShader.getUniformLocation("uColor");
        guiTexture = Texture2DExtensions.FromFile(Game.instance.GD, "gui.png");

        if (!File.Exists(fontFile)) {
            var collection = new FontCollection();
            var family = collection.Add("unifont-15.1.04.ttf");
            var font = family.CreateFont(12, FontStyle.Regular);
            using var ff = FontBuilderExtensions.CreateFontFile(font);
            guiFont = ff.CreateFont(Game.instance.GD);
            ff.WriteToFile(fontFile);
        }
        else {
            using var ff = TrippyFontFile.FromFile(fontFile);
            guiFont = ff.CreateFont(Game.instance.GD);
        }

        tb = new TextureBatch(Game.instance.GD);
        shader = SimpleShaderProgram.Create<VertexColorTexture>(Game.instance.GD);
        tb.SetShaderProgram(shader);
        resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
    }

    public void draw() {
        crosshair.bind();
        guiShader.use();
        guiShader.setUniform(projection, ortho);
        guiShader.setUniform(uColor, new Vector4(0.1f, 0.1f, 0.1f, 0.1f));
        crosshair.render();
        Game.instance.GD.ResetStates();
        Game.instance.GD.ShaderProgram = shader;
        tb.Begin(0);
        tb.DrawString(guiFont, "BlockGame", Vector2.Zero, Color4b.White);
        tb.DrawString(guiFont, "BlockGame", new Vector2(0, 20), Color4b.White);
        tb.DrawString(guiFont, "BlockGame", new Vector2(0, 40), Color4b.White);
        tb.DrawString(guiFont, "BlockGame", new Vector2(0, 60), Color4b.Red);
        if (!Game.instance.focused) {
            var pauseText = "-PAUSED-";
            Vector2 offset = guiFont.Measure(pauseText);
            tb.DrawString(guiFont, pauseText, new Vector2(Game.instance.centreX, Game.instance.centreY),
                Color4b.OrangeRed, Vector2.One, 0f, offset / 2);
        }

        draw(tb, guiTexture, new Vector2(0, 300), buttonRect);
        tb.End();
    }

    public void drawCrosshair() {
        var centreX = Game.instance.centreX;
        var centreY = Game.instance.centreY;

        float[] verts = [
            // vertical
            centreX - crosshairThickness, centreY - crosshairSize, 0f,
            centreX - crosshairThickness, centreY + crosshairSize, 0f,
            centreX + crosshairThickness, centreY + crosshairSize, 0f,
            centreX + crosshairThickness, centreY + crosshairSize, 0f,
            centreX + crosshairThickness, centreY - crosshairSize, 0f,
            centreX - crosshairThickness, centreY - crosshairSize, 0f,

            // horizontal
            centreX - crosshairSize, centreY - crosshairThickness, 0f,
            centreX - crosshairSize, centreY + crosshairThickness, 0f,
            centreX + crosshairSize, centreY + crosshairThickness, 0f,
            centreX + crosshairSize, centreY + crosshairThickness, 0f,
            centreX + crosshairSize, centreY - crosshairThickness, 0f,
            centreX - crosshairSize, centreY - crosshairThickness, 0f,
        ];
        crosshair.bind();
        crosshair.upload(verts);
        crosshair.format();
    }

    public void resize(Vector2D<int> size) {
        Game.instance.GD.SetViewport(0, 0, (uint)size.X, (uint)size.Y);
        ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);
        shader.Projection = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);
        drawCrosshair();

    }

    public void imGuiDraw() {
        var i = Game.instance;
        ImGui.Text($"{i.world.player.position.X}, {i.world.player.position.Y}, {i.world.player.position.Z}");
        ImGui.Text(i.targetedPos.HasValue
            ? $"{i.targetedPos.Value.X}, {i.targetedPos.Value.Y}, {i.targetedPos.Value.Z} {i.previousPos.Value.X}, {i.previousPos.Value.Y}, {i.previousPos.Value.Z}"
            : "No target");
        ImGui.Text($"FPS: {i.fps} (ft:{i.frametime * 1000:0.##}ms)");
        ImGui.Text($"W:{i.width} H:{i.height}");
        ImGui.Text($"CX:{i.centreX} CY:{i.centreY}");
        ImGui.Text(
            $"M:{Game.instance.proc.PrivateMemorySize64 / MEGABYTES:0.###} (h:{GC.GetTotalMemory(false) / MEGABYTES:0.###})");
    }


    public void draw(TextureBatch tb, Texture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        if (color == default) {
            tb.Draw(texture, position, source, Color4b.White, guiScale, 0f, origin, depth);
        }
        else {
            tb.Draw(texture, position, source, color, guiScale, 0f, origin, depth);
        }
    }
}