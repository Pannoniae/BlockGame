using System.Drawing;
using System.Numerics;
using ImGuiNET;
using TrippyGL;

namespace BlockGame;

public class GameScreen(GUI gui, GraphicsDevice GD, TextureBatch tb)
    : Screen(gui, GD, tb) {
    public override void draw() {
        GD.ResetStates();
        GD.ShaderProgram = gui.shader;
        var centreX = Game.instance.centreX;
        var centreY = Game.instance.centreY;
        // setup blending
        GD.BlendingEnabled = true;
        GD.BlendState = gui.bs;

        tb.Begin();
        tb.Draw(gui.colourTexture, new RectangleF(new PointF(centreX - Constants.crosshairThickness, centreY - Constants.crosshairSize), new SizeF(Constants.crosshairThickness * 2, Constants.crosshairSize * 2)),
            new Color4b(240, 240, 240));

        tb.Draw(gui.colourTexture, new RectangleF(new PointF(centreX - Constants.crosshairSize, centreY - Constants.crosshairThickness), new SizeF(Constants.crosshairSize - Constants.crosshairThickness, Constants.crosshairThickness * 2)),
            new Color4b(240, 240, 240));
        tb.Draw(gui.colourTexture, new RectangleF(new PointF(centreX + Constants.crosshairThickness, centreY - Constants.crosshairThickness), new SizeF(Constants.crosshairSize - Constants.crosshairThickness, Constants.crosshairThickness * 2)),
            new Color4b(240, 240, 240));
        tb.End();
        // reset blending this is messed up
        GD.BlendState = Game.instance.initialBlendState;

        tb.Begin();
        tb.DrawString(gui.guiFont, "BlockGame", Vector2.Zero, Color4b.White);
        tb.DrawString(gui.guiFont, "BlockGame", new Vector2(0, 20), Color4b.White);
        tb.DrawString(gui.guiFont, "BlockGame", new Vector2(0, 40), Color4b.White);
        tb.DrawString(gui.guiFont, "BlockGame", new Vector2(0, 60), Color4b.Red);
        if (!Game.instance.focused) {
            var pauseText = "-PAUSED-";
            Vector2 offset = gui.guiFont.Measure(pauseText);
            tb.DrawString(gui.guiFont, pauseText, new Vector2(Game.instance.centreX, Game.instance.centreY),
                Color4b.OrangeRed, Vector2.One, 0f, offset / 2);
        }

        gui.draw(tb, gui.guiTexture, new Vector2(0, 300), gui.buttonRect);
        tb.End();
    }

    public override void imGuiDraw() {
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
}