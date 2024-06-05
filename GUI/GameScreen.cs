using System.Drawing;
using System.Numerics;
using Cysharp.Text;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class GameScreen : Screen {

    public static World world;
    public GraphicsDevice GD;

    public Utf16ValueStringBuilder debugStr;
    public Debug D;

    public bool debugScreen = false;

    public Menu NULLMENU = new Menu();

    private TimerAction updateMemory;

    // values for f3
    private long privateMemory;
    private long workingSet;
    private long GCMemory;

    public override void activate() {
        base.activate();
        currentMenu = NULLMENU;
        debugStr = ZString.CreateStringBuilder();
        GD = Game.GD;
        D = new Debug();

        // create the world first
        var seed = Random.Shared.Next(int.MaxValue);
        world = new World(seed);
        world.generate();
        updateMemory = Game.instance.setInterval(200, updateMemoryMethod);

        // then add the GUI
        var version = Text.createText(this, "version", new Vector2D<int>(2, 2), "BlockGame v0.0.2");
        version.shadowed = true;
        addElement(version);
        var hotbar = new Hotbar(this, "hotbar", new Vector2D<int>(0, -20)) {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        addElement(hotbar);
    }

    public override void deactivate() {
        base.deactivate();
        world = null;
        updateMemory.enabled = false;
    }

    private void updateMemoryMethod() {
        privateMemory = Game.proc.PrivateMemorySize64;
        workingSet = Game.proc.WorkingSet64;
        GCMemory = GC.GetTotalMemory(false);
    }


    public override void update(double dt) {
        base.update(dt);
        world.player.pressedMovementKey = false;
        world.player.strafeVector = new Vector2D<double>(0, 0);
        world.player.inputVector = new Vector3D<double>(0, 0, 0);
        if (!world.paused && !Game.lockingMouse) {
            world.player.updateInput(dt);
            world.update(dt);
            world.player.update(dt);
        }

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
        Game.metrics.clear();

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
        base.onMouseDown(mouse, button);
        if (world.inMenu) {
            return;
        }

        if (world.paused) {
            Game.instance.lockMouse();
            Game.lockingMouse = true;
            world.paused = false;
        }
        else {
            if (button == MouseButton.Left) {
                world.player.breakBlock();
            }
            else if (button == MouseButton.Right) {
                world.player.placeBlock();
            }
        }
    }

    public override void onMouseMove(IMouse mouse, Vector2 pos) {
        base.onMouseMove(mouse, pos);
        if (!Game.focused || world.inMenu) {
            return;
        }

        if (Game.firstFrame) {
            Game.instance.lastMousePos = pos;
        }
        else {
            const float lookSensitivity = 0.1f;
            if (Game.instance.lastMousePos == default) {
                Game.instance.lastMousePos = pos;
            }
            else {
                var xOffset = (pos.X - Game.instance.lastMousePos.X) * lookSensitivity;
                var yOffset = (pos.Y - Game.instance.lastMousePos.Y) * lookSensitivity;
                Game.instance.lastMousePos = pos;

                world.player.camera.ModifyDirection(xOffset, yOffset);
            }
        }

        Game.firstFrame = false;
    }

    public override void scroll(IMouse mouse, ScrollWheel scroll) {
        int y = (int)Math.Clamp(-scroll.Y, -1, 1);
        var newSelection = world.player.hotbar.selected + y;
        newSelection = Utils.mod(newSelection, 9);
        world.player.hotbar.selected = newSelection;

    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);
        if (key == Key.Escape) {
            // hack for back to main menu
            if (!Game.focused) {
                backToMainMenu();
            }

            Game.instance.unlockMouse();
            world.player.catchUpOnPrevVars();
            world.paused = true;
        }

        if (key == Key.F3) {
            debugScreen = !debugScreen;
        }

        // reload chunks
        if (key == Key.A && keyboard.IsKeyPressed(Key.F3)) {
            foreach (var chunk in world.chunks.Values) {
                // don't set chunk if not loaded yet, else we will have broken chunkgen/lighting errors
                if (chunk.status >= ChunkStatus.MESHED) {
                    chunk.status = ChunkStatus.MESHED - 1;
                }
            }
            world.player.loadChunksAroundThePlayer(World.RENDERDISTANCE);
        }

        if (key == Key.F) {
            WorldIO.save(world, "world");
        }

        if (key == Key.G) {
            world = WorldIO.load("world");
            Game.instance.resize(new Vector2D<int>(Game.width, Game.height));
        }

        if (key == Key.E) {
            if (world.inMenu) {
                currentMenu = NULLMENU;
                world.inMenu = false;
                Game.instance.lockMouse();
            }
            else {
                currentMenu = new InventoryGUI(new Vector2D<int>(0, 32));
                ((InventoryGUI)currentMenu).setup();
                world.inMenu = true;
                Game.instance.unlockMouse();
            }
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

    private void backToMainMenu() {
        Game.instance.executeOnMainThread(() => {
            Console.Out.WriteLine("back");
            Game.instance.switchToScreen(MAIN_MENU_SCREEN);
        });
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
        //GD.ResetBufferStates();
        //GD.ResetVertexArrayStates();
        //GD.ResetShaderProgramStates();
        //GD.ResetTextureStates();
        //GD.ResetStates();

        GD.ShaderProgram = GUI.instance.shader;
        var centreX = Game.centreX;
        var centreY = Game.centreY;

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
        if (world.paused) {
            var pauseText = "-PAUSED-";
            gui.drawStringCentred(pauseText, new Vector2(Game.centreX, Game.centreY),
                Color4b.OrangeRed);
        }

        gui.draw(gui.guiTexture, new Vector2(0, 300), gui.buttonRect);

        // Draw block display
        var blockStr = Blocks.get(world.player.hotbar.getSelected().block).name;
        gui.drawStringCentred(blockStr, new Vector2(Game.centreX, Game.height - 120),
            Color4b.White);

        var i = Game.instance;
        var p = world.player;
        var c = p.camera;
        var m = Game.metrics;
        var loadedChunks = world.chunks.Count;
        var pos = p.position.toBlockPos();
        // current block
        //var cb = world.getBlock(pos);
        var sl = world.getSkyLight(pos.X, pos.Y, pos.Z);
        var bl = world.getBlockLight(pos.X, pos.Y, pos.Z);

        var ver = getElement("version");

        if (debugScreen) {
            debugStr.Clear();
            debugStr.AppendFormat("{0:0.000}, {1:0.000}, {2:0.000}\n", p.position.X, p.position.Y, p.position.Z);
            debugStr.AppendFormat("vx:{0:0.000}, vy:{1:0.000}, vz:{2:0.000}, vl:{3:0.000}\n", p.velocity.X, p.velocity.Y, p.velocity.Z, p.velocity.Length);
            debugStr.AppendFormat("ax:{0:0.000}, ay:{1:0.000}, az:{2:0.000}\n", p.accel.X, p.accel.Y, p.accel.Z);
            debugStr.AppendFormat("cf:{0:0.000}, {1:0.000}, {2:0.000}\n", c.forward.X, c.forward.Y, c.forward.Z);
            debugStr.AppendFormat("sl:{0}, bl:{1}\n", sl, bl);
            debugStr.AppendFormat("g:{0} j:{1}\n", p.onGround, p.jumping);
            if (i.targetedPos.HasValue)
                debugStr.AppendFormat("{0}, {1}, {2} {3}, {4}, {5}\n", i.targetedPos.Value.X, i.targetedPos.Value.Y, i.targetedPos.Value.Z, i.previousPos!.Value.X, i.previousPos.Value.Y, i.previousPos.Value.Z);
            else
                debugStr.AppendLine("No target\n");
            debugStr.AppendFormat("rC:{0} rV:{1}\n", m.renderedChunks, m.renderedVerts);
            debugStr.AppendFormat("lC:{0} lCs:{1}\n", loadedChunks, loadedChunks * Chunk.CHUNKHEIGHT);
            debugStr.AppendFormat("FOV:{0}\n", p.camera.hfov);

            debugStr.AppendFormat("FPS:{0} (ft:{1:0.##}ms)\n", i.fps, i.ft * 1000);
            debugStr.AppendFormat("W:{0} H:{1}\n", Game.width, Game.height);
            debugStr.AppendFormat("CX:{0} CY:{1}\n", Game.centreX, Game.centreY);
            debugStr.AppendFormat("M:{0:0.###}:{1:0.###} (h:{2:0.###})\n", privateMemory / Constants.MEGABYTES, workingSet / Constants.MEGABYTES, GCMemory / Constants.MEGABYTES);
            gui.drawString(debugStr.ToString(),
                new Vector2(ver.bounds.Left, ver.bounds.Bottom), Color4b.White);


            D.drawLine(new Vector3D<double>(0, 0, 0), new Vector3D<double>(1, 1, 1), Color4b.Red);
            D.drawLine(new Vector3D<double>(1, 1, 1), new Vector3D<double>(24, 24, 24), Color4b.Red);
            //D.drawAABB(p.aabb);
            D.flushLines();
        }
    }

    public override void postDraw() {
        base.postDraw();
        // draw hotbar
    }

    public override void imGuiDraw() {

    }

    public override void clear(GraphicsDevice GD, double dt, double interp) {
        GD.ClearColor = WorldRenderer.defaultClearColour;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
    }

    public override void onKeyUp(IKeyboard keyboard, Key key, int scancode) {

    }
}