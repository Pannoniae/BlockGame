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
    private TimerAction updateMemory;

    // values for f3
    private long privateMemory;
    private long workingSet;
    private long GCMemory;

    public override void activate() {
        base.activate();
        debugStr = new StringBuilder(500);
        GD = Game.GD;
        D = new Debug();

        var version = Text.createText(this, new Vector2(2, 2), "BlockGame v0.0.1");
        version.shadowed = true;
        elements.Add(version);
        var seed = Random.Shared.Next(int.MaxValue);
        world = new World(seed);
        world.generate();
        updateMemory = Game.instance.setInterval(200, updateMemoryMethod);
    }

    public override void deactivate() {
        base.deactivate();
        updateMemory.enabled = false;
    }

    private void updateMemoryMethod() {
        privateMemory = Game.proc.PrivateMemorySize64;
        workingSet = Game.proc.WorkingSet64;
        GCMemory = GC.GetTotalMemory(false);
    }


    public override void update(double dt) {
        //gui.screen.update(dt);
        world.player.pressedMovementKey = false;
        world.player.strafeVector = new Vector2D<double>(0, 0);
        world.player.inputVector = new Vector3D<double>(0, 0, 0);
        if (Game.focused && !Game.lockingMouse) {
            world.player.updateInput(dt);
        }
        world.update(dt);
        world.player.update(dt);

        // turn on for stress testing:)
        //Utils.wasteMemory(dt, 200);
        var newPos = world.naiveRaycastBlock(out Game.instance.previousPos);
        bool meshOutline = Game.instance.targetedPos != newPos && newPos != default;
        Game.instance.targetedPos = newPos;
        if (meshOutline) {
            world.renderer.meshBlockOutline();
        }
    }

    public override void render(double dt, double interp) {
        GD.DepthTestingEnabled = true;
        Game.instance.metrics.clear();

        //world.mesh();
        world.player.camera.calculateFrustum();
        //Console.Out.WriteLine(world.player.camera.frustum);
        world.renderer.render(interp);
        world.player.render(dt, interp);

        if (Game.instance.targetedPos.HasValue) {
            world.renderer.drawBlockOutline(interp);
        }
        D.update(interp);
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
        if (Game.focused) {
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
        if (!Game.focused) {
            return;
        }

        if (Game.firstFrame) {
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

        Game.firstFrame = false;
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
            Game.instance.resize(new Vector2D<int>(Game.width, Game.height));
        }


        // guiscale test
        if (keyboard.IsKeyPressed(Key.ControlLeft)) {
            if (key >= Key.Number0 && key <= Key.Number9) {
                GUI.guiScale = (ushort)(key - Key.Number0);
            }
        }
        else {
            world.player.updatePickBlock(keyboard, key, scancode);
        }
    }

    public override void click(Vector2 pos) {
        base.click(pos);
        // if no longer holding, the player isn't clicking into the window anymore
        if (Game.focused && Game.lockingMouse) {
            Game.lockingMouse = false;
        }
    }

    public override void resize(Vector2D<int> size) {
        base.resize(size);
        world.player.camera.setViewport(size.X, size.Y);
    }

    public override void draw() {
        base.draw();

        var gui = Game.gui;
        //GD.FaceCullingEnabled = false;
        //GD.BlendState = BlendState.Opaque;
        //GD.DepthTestingEnabled = false;
        GD.ResetBufferStates();
        GD.ResetVertexArrayStates();
        GD.ResetShaderProgramStates();
        //GD.ResetTextureStates();

        GD.ShaderProgram = GUI.instance.shader;
        var centreX = Game.centreX;
        var centreY = Game.centreY;


        // draw box
        gui.tb.Draw(gui.colourTexture,
            new RectangleF(new PointF(Game.centreX, Game.centreY),
                new SizeF(16 * GUI.guiScale, 16 * GUI.guiScale)),
            new Color4b(240, 240, 240));
        gui.tb.End();
        gui.drawBlock(world, Blocks.DIRT, Game.centreX + (world.worldTick % Game.centreX * 0), Game.centreY, 16);
        gui.tb.Begin();
        // setup blending
        //GD.BlendingEnabled = true;
        //GD.BlendState = bs;
        /*
         * shader.use();
           shader.setUniform(uMVP, viewProj);
           shader.setUniform(uCameraPos, world.player.camera.renderPosition(interp));
           shader.setUniform(drawDistance, World.RENDERDISTANCE * Chunk.CHUNKSIZE);
           shader.setUniform(fogColour, defaultClearColour);
           shader.setUniform(blockTexture, 0);
         */


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
        //GD.BlendState = Game.initialBlendState;

        //tb.DrawString(gui.guiFont, "BlockGame", Vector2.Zero, Color4b.White);
        //tb.DrawString(gui.guiFont, "BlockGame", new Vector2(0, 20), Color4b.White);
        //tb.DrawString(gui.guiFont, "BlockGame", new Vector2(0, 40), Color4b.White);
        //tb.DrawString(gui.guiFont, "BlockGame", new Vector2(0, 60), Color4b.Red);
        if (!Game.focused) {
            var pauseText = "-PAUSED-";
            Vector2 offset = gui.guiFont.Measure(pauseText);
            gui.tb.DrawString(gui.guiFont, pauseText, new Vector2(Game.centreX, Game.centreY),
                Color4b.OrangeRed, Vector2.One, 0f, offset / 2);
        }

        gui.draw(gui.guiTexture, new Vector2(0, 300), gui.buttonRect);

        // Draw block display
        var blockStr = Blocks.get(world.player.hotbar.getSelected()).name;
        gui.drawStringCentred(blockStr, new Vector2(Game.centreX, Game.height - 120),
            Color4b.White);

        var i = Game.instance;
        var p = world.player;
        var c = p.camera;
        var m = Game.instance.metrics;
        var loadedChunks = world.chunks.Count;
        if (debugScreen) {
            debugStr.Clear();
            debugStr.AppendLine($"{p.position.X:0.000}, {p.position.Y:0.000}, {p.position.Z:0.000}");
            debugStr.AppendLine($"vx:{p.velocity.X:0.000}, vy:{p.velocity.Y:0.000}, vz:{p.velocity.Z:0.000}, vl:{p.velocity.Length:0.000}");
            debugStr.AppendLine($"ax:{p.accel.X:0.000}, ay:{p.accel.Y:0.000}, az:{p.accel.Z:0.000}");
            debugStr.AppendLine($"cf:{c.forward.X:0.000}, {c.forward.Y:0.000}, {c.forward.Z:0.000}");
            debugStr.AppendLine($"g:{p.onGround} j:{p.jumping}");
            debugStr.AppendLine(i.targetedPos.HasValue
                ? $"{i.targetedPos.Value.X}, {i.targetedPos.Value.Y}, {i.targetedPos.Value.Z} {i.previousPos.Value.X}, {i.previousPos.Value.Y}, {i.previousPos.Value.Z}"
                : "No target");
            debugStr.AppendLine($"rC:{m.renderedChunks} rV:{m.renderedVerts}");
            debugStr.AppendLine($"lC:{loadedChunks} lCs:{loadedChunks * Chunk.CHUNKHEIGHT}");
            debugStr.AppendLine($"FOV:{p.camera.hfov}");

            debugStr.AppendLine($"FPS:{i.fps} (ft:{i.ft * 1000:0.##}ms)");
            debugStr.AppendLine($"W:{Game.width} H:{Game.height}");
            debugStr.AppendLine($"CX:{Game.centreX} CY:{Game.centreY}");
            debugStr.AppendLine(
                $"M:{privateMemory / Constants.MEGABYTES:0.###}:{workingSet / Constants.MEGABYTES:0.###} (h:{GCMemory / Constants.MEGABYTES:0.###})");
            gui.tb.DrawString(gui.guiFont, debugStr.ToString(),
                new Vector2(elements[0].bounds.Left, elements[0].bounds.Bottom), Color4b.White);


            D.drawLine(new Vector3D<double>(0, 0, 0), new Vector3D<double>(1, 1, 1), Color4b.Red);
            D.drawLine(new Vector3D<double>(1, 1, 1), new Vector3D<double>(24, 24, 24), Color4b.Red);
            //D.drawAABB(p.aabb);
            D.flushLines();
        }
    }

    public override void imGuiDraw() {

    }
}