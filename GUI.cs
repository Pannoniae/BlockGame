using System.Drawing;
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
using BlendingFactor = TrippyGL.BlendingFactor;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class GUI {

    public GL GL;
    public GraphicsDevice GD;

    private SimpleShaderProgram shader;

    public bool debugScreen = true;

    public int guiScale = 4;

    public TextureBatch tb;
    public Texture2D guiTexture;
    public Texture2D colourTexture;
    private TextureFont guiFont;

    public Rectangle buttonRect = new(0, 0, 64, 16);

    public readonly BlendState bs = new(false, BlendingMode.FuncAdd, BlendingFactor.OneMinusDstColor, BlendingFactor.Zero);

    public GUI() {
        GL = Game.instance.GL;
        GD = Game.instance.GD;
        guiTexture = Texture2DExtensions.FromFile(Game.instance.GD, "gui.png");
        colourTexture = Texture2DExtensions.FromFile(Game.instance.GD, "debug.png");

        if (!File.Exists(Constants.fontFile)) {
            var collection = new FontCollection();
            var family = collection.Add("unifont-15.1.04.ttf");
            var font = family.CreateFont(12, FontStyle.Regular);
            using var ff = FontBuilderExtensions.CreateFontFile(font);
            guiFont = ff.CreateFont(Game.instance.GD);
            ff.WriteToFile(Constants.fontFile);
        }
        else {
            using var ff = TrippyFontFile.FromFile(Constants.fontFile);
            guiFont = ff.CreateFont(Game.instance.GD);
        }

        tb = new TextureBatch(Game.instance.GD);
        shader = SimpleShaderProgram.Create<VertexColorTexture>(Game.instance.GD);
        tb.SetShaderProgram(shader);
        resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
    }

    public void draw() {
        GD.ResetStates();
        GD.ShaderProgram = shader;
        var centreX = Game.instance.centreX;
        var centreY = Game.instance.centreY;
        // setup blending
        GD.BlendingEnabled = true;
        GD.BlendState = bs;

        tb.Begin();
        tb.Draw(colourTexture, new RectangleF(new PointF(centreX - Constants.crosshairThickness, centreY - Constants.crosshairSize), new SizeF(Constants.crosshairThickness * 2, Constants.crosshairSize * 2)),
            new Color4b(240, 240, 240));

        tb.Draw(colourTexture, new RectangleF(new PointF(centreX - Constants.crosshairSize, centreY - Constants.crosshairThickness), new SizeF(Constants.crosshairSize - Constants.crosshairThickness, Constants.crosshairThickness * 2)),
            new Color4b(240, 240, 240));
        tb.Draw(colourTexture, new RectangleF(new PointF(centreX + Constants.crosshairThickness, centreY - Constants.crosshairThickness), new SizeF(Constants.crosshairSize - Constants.crosshairThickness, Constants.crosshairThickness * 2)),
            new Color4b(240, 240, 240));
        tb.End();
        // reset blending this is messed up
        GD.BlendState = Game.instance.initialBlendState;

        tb.Begin();
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

    public void resize(Vector2D<int> size) {
        Game.instance.GD.SetViewport(0, 0, (uint)size.X, (uint)size.Y);
        shader.Projection = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);
    }

    public void imGuiDraw() {
        var i = Game.instance;
        var p = i.world.player;
        var c = p.camera;
        var m = Game.instance.metrics;
        ImGui.Text($"{p.position.X:0.###}, {p.position.Y:0.###}, {p.position.Z:0.###}");
        ImGui.Text($"vx:{p.velocity.X:0.000}, vy:{p.velocity.Y:0.000}, vz:{p.velocity.Z:0.000}, vl:{p.velocity.Length:0.000}");
        ImGui.Text($"ax:{p.accel.X:0.000}, ay:{p.accel.Y:0.000}, az:{p.accel.Z:0.000}");
        ImGui.Text($"cf:{c.forward.X:0.000}, {c.forward.Y:0.000}, {c.forward.Z:0.000}");
        ImGui.Text($"g:{p.onGround} j:{p.jumping}");
        ImGui.Text(i.targetedPos.HasValue
            ? $"{i.targetedPos.Value.X}, {i.targetedPos.Value.Y}, {i.targetedPos.Value.Z} {i.previousPos.Value.X}, {i.previousPos.Value.Y}, {i.previousPos.Value.Z}"
            : "No target");
        ImGui.Text($"rC: {m.renderedChunks} rV:{m.renderedVerts}");

        ImGui.Text($"FPS: {i.fps} (ft:{i.ft * 1000:0.##}ms)");
        ImGui.Text($"W:{i.width} H:{i.height}");
        ImGui.Text($"CX:{i.centreX} CY:{i.centreY}");
        ImGui.Text(
            $"M:{Game.instance.proc.PrivateMemorySize64  / Constants.MEGABYTES:0.###}:{Game.instance.proc.WorkingSet64 / Constants.MEGABYTES:0.###} (h:{GC.GetTotalMemory(false) / Constants.MEGABYTES:0.###})");

    }


    public void draw(TextureBatch tb, Texture2D texture, Vector2 position, Rectangle? source = null,
        Color4b color = default, Vector2 origin = default, float depth = 0f) {
        tb.Draw(texture, position, source, color == default ? Color4b.White : color, guiScale, 0f, origin, depth);
    }
}