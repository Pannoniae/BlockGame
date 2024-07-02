using System.Drawing;
using System.Numerics;
using BlockGame.util;
using Cysharp.Text;
using FontStashSharp.RichText;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using TrippyGL;

namespace BlockGame.ui;

public class GameScreen : Screen, IDisposable {

    public static World world;
    public GraphicsDevice GD;

    private Utf16ValueStringBuilder debugStr;
    // for top right corner debug shit
    private Utf16ValueStringBuilder debugStrG;
    public Debug D;

    public bool debugScreen = false;

    public Menu PAUSE_MENU = new PauseMenu();

    private TimerAction updateMemory;

    // values for f3
    private long workingSet;
    private long GCMemory;
    private RichTextLayout rendererText;
    private bool disposed;

    public override void activate() {
        base.activate();
        exitMenu();
        debugStr.Dispose();
        debugStr = ZString.CreateStringBuilder();
        debugStrG.Dispose();
        debugStrG = ZString.CreateStringBuilder();
        GD = Game.GD;
        D = new Debug();

        // create the world first
        var seed = Random.Shared.Next(int.MaxValue);
        world = new World(seed);
        world.loadAroundPlayer();
        updateMemory = Game.instance.setInterval(200, updateMemoryMethod);
        updateMemory = Game.instance.setInterval(50, updateDebugTextMethod);

        // then add the GUI
        var version = Text.createText(this, "version", new Vector2D<int>(2, 2), "BlockGame v0.0.2");
        version.shadowed = true;
        addElement(version);
        var hotbar = new Hotbar(this, "hotbar", new Vector2D<int>(0, -20)) {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        addElement(hotbar);
        rendererText = new RichTextLayout {
            Font = Game.gui.guiFontThin,
            Text = "",
            Width = 150 * GUI.guiScale
        };
    }

    public virtual void Dispose() {
        debugStr.Dispose();
        debugStrG.Dispose();
    }

    private void updateDebugTextMethod() {
        if (debugScreen) {
            var gui = Game.gui;
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


            debugStr.Clear();
            debugStrG.Clear();
            if (Game.devMode) {
                debugStr.AppendFormat("{0:0.000}, {1:0.000}, {2:0.000}\n", p.position.X, p.position.Y, p.position.Z);
                debugStr.AppendFormat("vx:{0:0.000}, vy:{1:0.000}, vz:{2:0.000}, vl:{3:0.000}\n", p.velocity.X, p.velocity.Y, p.velocity.Z, p.velocity.Length);
                debugStr.AppendFormat("ax:{0:0.000}, ay:{1:0.000}, az:{2:0.000}\n", p.accel.X, p.accel.Y, p.accel.Z);
                debugStr.AppendFormat("cf:{0:0.000}, {1:0.000}, {2:0.000}\n", c.forward.X, c.forward.Y, c.forward.Z);
                debugStr.AppendFormat("sl:{0}, bl:{1}\n", sl, bl);
                debugStr.AppendFormat("{0}{1}\n", p.onGround ? 'g' : '-', p.jumping ? 'j' : '-');
                if (i.targetedPos.HasValue) {
                    debugStr.AppendFormat("{0}, {1}, {2} {3}, {4}, {5}\n", i.targetedPos.Value.X, i.targetedPos.Value.Y, i.targetedPos.Value.Z, i.previousPos!.Value.X, i.previousPos.Value.Y, i.previousPos.Value.Z);
                }
                else
                    debugStr.Append("No target\n");
            }

            debugStr.AppendFormat("rC:{0} rV:{1}k\n", m.renderedChunks, m.renderedVerts / 1000);
            debugStr.AppendFormat("lC:{0} lCs:{1}\n", loadedChunks, loadedChunks * Chunk.CHUNKHEIGHT);

            debugStr.AppendFormat("FPS:{0} (ft:{1:0.##}ms)\n", i.fps, i.ft * 1000);
            if (Game.devMode) {
                debugStr.AppendFormat("Seed: {0}\n", world.seed);
            }



            debugStrG.AppendFormat("Renderer: {0}/{1}\n", Game.GL.GetStringS(StringName.Renderer), Game.GL.GetStringS(StringName.Vendor));
            debugStrG.AppendFormat("OpenGL version: {0}\n", Game.GL.GetStringS(StringName.Version));
            debugStrG.AppendFormat("Mem:{0:0.###}MB (proc:{1:0.###}MB)\n", GCMemory / Constants.MEGABYTES, workingSet / Constants.MEGABYTES);
            // calculate textwidth
            rendererText = new RichTextLayout {
                Font = gui.guiFontThin,
                Text = debugStrG.ToString(),
                Width = 150 * GUI.guiScale
            };

        }
    }

    public override void deactivate() {
        base.deactivate();
        world = null;
        updateMemory.enabled = false;
    }

    private void updateMemoryMethod() {
        Game.proc.Refresh();
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
        world.player.camera.calculateFrustum(interp);
        //Console.Out.WriteLine(world.player.camera.frustum);
        world.renderer.render(interp);
        if (Game.instance.targetedPos.HasValue) {
            //Console.Out.WriteLine(Game.instance.targetedPos.Value);
            world.renderer.drawBlockOutline(interp);
        }
        D.update(interp);
    }

    public override void postRender(double dt, double interp) {
        // render entities
        GD.DepthTestingEnabled = false;
        world.player.render(dt, interp);
        //GD.DepthTestingEnabled = true;
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
        base.onMouseDown(mouse, button);
        if (world.inMenu) {
            return;
        }

        switch (button) {
            case MouseButton.Left:
                world.player.breakBlock();
                break;
            case MouseButton.Right:
                world.player.placeBlock();
                break;
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
        newSelection = Utils.mod(newSelection, 10);
        world.player.hotbar.selected = newSelection;

    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);
        if (key == Key.Escape) {
            // hack for back to main menu
            if (!world.inMenu) {
                switchToMenu(PAUSE_MENU);
                world.inMenu = true;
                world.paused = true;
                Game.instance.unlockMouse();
                world.player.catchUpOnPrevVars();
            }
            else {
                backToGame();
            }
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
            world.player.loadChunksAroundThePlayer(Settings.instance.renderDistance);
        }

        if (key == Key.F) {
            world.worldIO.save(world, "world");
        }

        if (key == Key.G) {
            world = WorldIO.load("world");
            Game.instance.resize(new Vector2D<int>(Game.width, Game.height));
        }

        if (key == Key.F9) {
            MemoryUtils.cleanGC();
        }

        if (key == Key.E) {
            if (world.inMenu) {
                backToGame();
            }
            else {
                switchToMenu(new InventoryGUI(new Vector2D<int>(0, 32)));
                ((InventoryGUI)currentMenu!).setup();
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

    public void backToGame() {
        exitMenu();
        world.inMenu = false;
        world.paused = false;
        Game.instance.lockMouse();
        //Game.lockingMouse = true;
    }

    private void backToMainMenu() {
        Game.instance.executeOnMainThread(() => {
            Console.Out.WriteLine("back");
            Game.instance.switchToScreen(MAIN_MENU_SCREEN);
        });
    }

    public override void onMouseUp(Vector2 pos) {
        base.onMouseUp(pos);
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

        GD.ShaderProgram = GUI.instance.shader;
        var centreX = Game.centreX;
        var centreY = Game.centreY;


        if (!world.inMenu) {
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
        }
        if (world.paused) {
            var pauseText = "-PAUSED-";
            gui.drawStringCentred(pauseText, new Vector2(Game.centreX, Game.centreY - 16 * GUI.guiScale),
                Color4b.OrangeRed);
        }

        // Draw block display
        var blockStr = Blocks.get(world.player.hotbar.getSelected().block).name;
        gui.drawStringCentred(blockStr, new Vector2(Game.centreX, Game.height - 120),
            Color4b.White);

        if (debugScreen) {

            D.drawLine(new Vector3D<double>(0, 0, 0), new Vector3D<double>(1, 1, 1), Color4b.Red);
            D.drawLine(new Vector3D<double>(1, 1, 1), new Vector3D<double>(24, 24, 24), Color4b.Red);
            //D.drawAABB(p.aabb);
            D.flushLines();

            var ver = getElement("version");
            gui.drawStringThin(debugStr.AsSpan(),
                new Vector2(ver.bounds.Left, ver.bounds.Bottom), Color4b.White);
            gui.drawRString(rendererText,
                new Vector2(Game.width - 2, 2), TextHorizontalAlignment.Right, Color4b.White);
        }
    }

    /// <summary>
    /// Split a string with newlines at spaces so that the maxLen is roughly respected. (Sorry!)
    /// </summary>
    private static string splitString(Span<char> input, int maxLen) {
        for (int i = maxLen - 3; i < input.Length; i++) {
            // if space, replace with newline
            if (input[i] == ' ') {
                input[i] = '\n';
                return new string(input);
            }
        }
        return new string(input);
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