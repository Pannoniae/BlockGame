using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class GameScreen : Screen {

    public readonly BlendState bs = new(false, BlendingMode.FuncAdd, BlendingFactor.OneMinusDstColor, BlendingFactor.Zero);

    public GameScreen(GUI gui, GraphicsDevice GD, TextureBatch tb) : base(gui, GD, tb) {

    }


    public override void update(double dt) {
        var world = Game.instance.world;
        //gui.screen.update(dt);
        world.player.pressedMovementKey = false;
        if (Game.instance.focused && !Game.instance.lockingMouse) {
            world.player.updateInput(dt);
        }
        world.update(dt);
        world.player.update(dt);

        Game.instance.targetedPos = world.naiveRaycastBlock(out Game.instance.previousPos);
    }

    public override void render(double dt, double interp) {
        var world = Game.instance.world;
        GD.DepthTestingEnabled = true;
        Game.instance.metrics.clear();

        //world.mesh();
        world.draw(interp);
        if (Game.instance.targetedPos.HasValue) {
            world.drawBlockOutline(interp);
        }
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
        var world = Game.instance.world;
        if (Game.instance.focused) {
            if (button == MouseButton.Left) {
                world.player.breakBlock();
            }
            else if (button == MouseButton.Right) {
                world.player.placeBlock();
            }
        }
        else {
            Game.instance.lockMouse();
        }
    }

    public override void onMouseMove(IMouse mouse, Vector2 position) {
        if (!Game.instance.focused) {
            return;
        }

        if (Game.instance.firstFrame) {
            Game.instance.lastMousePos = position;
        }
        else {
            const float lookSensitivity = 0.1f;
            if (Game.instance.lastMousePos == default) {
                Game.instance.lastMousePos = position;
            }
            else {
                var xOffset = (position.X - Game.instance.lastMousePos.X) * lookSensitivity;
                var yOffset = (position.Y - Game.instance.lastMousePos.Y) * lookSensitivity;
                Game.instance.lastMousePos = position;

                Game.instance.world.player.camera.ModifyDirection(xOffset, yOffset);
            }
        }

        Game.instance.firstFrame = false;
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            Game.instance.unlockMouse();
        }

        if (key == Key.F3) {
            gui.debugScreen = !gui.debugScreen;
        }

        if (key == Key.F) {
            Game.instance.world.save("world");
        }

        if (key == Key.G) {
            Game.instance.world = World.load("world");
            Game.instance.resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
        }

        Game.instance.world.player.updatePickBlock(keyboard, key, scancode);
    }

    public override void click(Vector2 pos) {
        base.click(pos);
        // if no longer holding, the player isn't clicking into the window anymore
        if (Game.instance.focused && Game.instance.lockingMouse) {
            Game.instance.lockingMouse = false;
        }
    }

    public override void resize(Vector2D<int> size) {
        Game.instance.world.player.camera.aspectRatio = (float)size.X / size.Y;
    }

    public override void draw() {
        base.draw();
        GD.ResetStates();
        GD.ShaderProgram = gui.shader;
        var centreX = Game.instance.centreX;
        var centreY = Game.instance.centreY;
        // setup blending
        GD.BlendingEnabled = true;
        GD.BlendState = bs;

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