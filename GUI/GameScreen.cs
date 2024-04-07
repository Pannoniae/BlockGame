using System.Drawing;
using System.Numerics;
using System.Text;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class GameScreen : Screen {

    public static World world;
    public GraphicsDevice GD;

    public StringBuilder debugStr;
    public Debug D;

    public bool debugScreen = false;

    public readonly BlendState bs = new(false, BlendingMode.FuncAdd, BlendingFactor.OneMinusDstColor, BlendingFactor.Zero);

    public GameScreen() {
        debugStr = new StringBuilder(500);
        GD = Game.instance.GD;
        D = new Debug();
    }


    public override void update(double dt) {
        //gui.screen.update(dt);
        world.player.pressedMovementKey = false;
        world.player.strafeVector = new Vector2D<double>(0, 0);
        world.player.inputVector = new Vector3D<double>(0, 0, 0);
        if (Game.instance.focused && !Game.instance.lockingMouse) {
            world.player.updateInput(dt);
        }
        world.update(dt);
        world.player.update(dt);

        Game.instance.targetedPos = world.naiveRaycastBlock(out Game.instance.previousPos);
    }

    public override void render(double dt, double interp) {
        GD.DepthTestingEnabled = true;
        Game.instance.metrics.clear();

        //world.mesh();
        world.player.camera.calculateFrustum();
        //Console.Out.WriteLine(world.player.camera.frustum);
        world.renderer.render(interp);
        if (Game.instance.targetedPos.HasValue) {
            world.renderer.drawBlockOutline(interp);
        }
        D.update(interp);
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
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

                world.player.camera.ModifyDirection(xOffset, yOffset);
            }
        }

        Game.instance.firstFrame = false;
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            Game.instance.unlockMouse();
        }

        if (key == Key.F3) {
            debugScreen = !debugScreen;
        }

        if (key == Key.F) {
            WorldIO.save(world, "world");
        }

        if (key == Key.G) {
            world = WorldIO.load("world");
            Game.instance.resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
        }


        // guiscale test
        if (keyboard.IsKeyPressed(Key.ControlLeft)) {
            if (key >= Key.Number0 && key <= Key.Number9) {
                Game.gui.guiScale = (ushort)(key - Key.Number0);
            }
        }
        else {
            world.player.updatePickBlock(keyboard, key, scancode);
        }
    }

    public override void click(Vector2 pos) {
        base.click(pos);
        // if no longer holding, the player isn't clicking into the window anymore
        if (Game.instance.focused && Game.instance.lockingMouse) {
            Game.instance.lockingMouse = false;
        }
    }

    public override void resize(Vector2D<int> size) {
        world.player.camera.setViewport(size.X, size.Y);
    }

    public override void draw() {
        base.draw();
        GD.ResetBufferStates();
        GD.ResetVertexArrayStates();
        //GD.ResetShaderProgramStates();
        GD.ShaderProgram = GUI.instance.shader;
        var centreX = Game.instance.centreX;
        var centreY = Game.instance.centreY;
        // setup blending
        GD.BlendingEnabled = true;
        GD.BlendState = bs;
        var gui = Game.gui;

        gui.tb.Draw(gui.colourTexture,
            new RectangleF(new PointF(centreX - Constants.crosshairThickness, centreY - Constants.crosshairSize),
                new SizeF(Constants.crosshairThickness * 2, Constants.crosshairSize * 2)),
            new Color4b(240, 240, 240));

        gui.tb.Draw(gui.colourTexture,
            new RectangleF(new PointF(centreX - Constants.crosshairSize, centreY - Constants.crosshairThickness),
                new SizeF(Constants.crosshairSize - Constants.crosshairThickness, Constants.crosshairThickness * 2)),
            new Color4b(240, 240, 240));
        gui.tb.Draw(gui.colourTexture,
            new RectangleF(new PointF(centreX + Constants.crosshairThickness, centreY - Constants.crosshairThickness),
                new SizeF(Constants.crosshairSize - Constants.crosshairThickness, Constants.crosshairThickness * 2)),
            new Color4b(240, 240, 240));
        // reset blending this is messed up
        GD.BlendState = Game.instance.initialBlendState;

        //tb.DrawString(gui.guiFont, "BlockGame", Vector2.Zero, Color4b.White);
        //tb.DrawString(gui.guiFont, "BlockGame", new Vector2(0, 20), Color4b.White);
        //tb.DrawString(gui.guiFont, "BlockGame", new Vector2(0, 40), Color4b.White);
        //tb.DrawString(gui.guiFont, "BlockGame", new Vector2(0, 60), Color4b.Red);
        if (!Game.instance.focused) {
            var pauseText = "-PAUSED-";
            Vector2 offset = gui.guiFont.Measure(pauseText);
            gui.tb.DrawString(gui.guiFont, pauseText, new Vector2(Game.instance.centreX, Game.instance.centreY),
                Color4b.OrangeRed, Vector2.One, 0f, offset / 2);
        }

        gui.draw(gui.guiTexture, new Vector2(0, 300), gui.buttonRect);

        // Draw block display
        var blockStr = Blocks.get(world.player.hotbar.getSelected()).name;
        gui.drawStringCentred(blockStr, new Vector2(Game.instance.centreX, Game.instance.height - 120),
            Color4b.White);

        var i = Game.instance;
        var p = world.player;
        var c = p.camera;
        var m = Game.instance.metrics;
        if (debugScreen) {
            debugStr.Clear();
            debugStr.AppendLine($"{p.position.X:0.###}, {p.position.Y:0.###}, {p.position.Z:0.###}");
            debugStr.AppendLine($"vx:{p.velocity.X:0.000}, vy:{p.velocity.Y:0.000}, vz:{p.velocity.Z:0.000}, vl:{p.velocity.Length:0.000}");
            debugStr.AppendLine($"ax:{p.accel.X:0.000}, ay:{p.accel.Y:0.000}, az:{p.accel.Z:0.000}");
            debugStr.AppendLine($"cf:{c.forward.X:0.000}, {c.forward.Y:0.000}, {c.forward.Z:0.000}");
            debugStr.AppendLine($"g:{p.onGround} j:{p.jumping}");
            debugStr.AppendLine(i.targetedPos.HasValue
                ? $"{i.targetedPos.Value.X}, {i.targetedPos.Value.Y}, {i.targetedPos.Value.Z} {i.previousPos.Value.X}, {i.previousPos.Value.Y}, {i.previousPos.Value.Z}"
                : "No target");
            debugStr.AppendLine($"rC:{m.renderedChunks} rV:{m.renderedVerts}");
            debugStr.AppendLine($"FOV:{p.camera.hfov}");

            debugStr.AppendLine($"FPS:{i.fps} (ft:{i.ft * 1000:0.##}ms)");
            debugStr.AppendLine($"W:{i.width} H:{i.height}");
            debugStr.AppendLine($"CX:{i.centreX} CY:{i.centreY}");
            debugStr.AppendLine(
                $"M:{Game.instance.proc.PrivateMemorySize64 / Constants.MEGABYTES:0.###}:{Game.instance.proc.WorkingSet64 / Constants.MEGABYTES:0.###} (h:{GC.GetTotalMemory(false) / Constants.MEGABYTES:0.###})");
            gui.tb.DrawString(gui.guiFont, debugStr.ToString(), new Vector2(5, 5), Color4b.White);


            D.drawLine(new Vector3D<double>(0, 0, 0), new Vector3D<double>(1, 1, 1), Color4b.Red);
            D.drawLine(new Vector3D<double>(1, 1, 1), new Vector3D<double>(24, 24, 24), Color4b.Red);
            D.flushLines();
        }
    }

    public override void imGuiDraw() {

    }
}