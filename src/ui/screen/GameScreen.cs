using System.Drawing;
using System.Numerics;
using BlockGame.util;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame.ui;

public class GameScreen : Screen {

    public World world;
    public GraphicsDevice GD;

    public Debug D;

    public bool debugScreen = false;

    public Menu PAUSE_MENU = new PauseMenu();
    public IngameMenu INGAME_MENU = new IngameMenu();

    private TimerAction updateMemory;
    private TimerAction updateDebugText;


    private bool disposed;

    public override void activate() {
        GD = Game.GD;
        D = new Debug();

        switchToMenu(INGAME_MENU);

        updateMemory = Game.instance.setInterval(200, INGAME_MENU.updateMemoryMethod);
        updateDebugText = Game.instance.setInterval(50, INGAME_MENU.updateDebugTextMethod);
    }

    public void setWorld(World inWorld) {
        world?.Dispose();
        // create the world first
        world = inWorld;
    }


    public override void deactivate() {
        base.deactivate();
        world?.Dispose();
        world = null;
        updateMemory.enabled = false;
        updateDebugText.enabled = false;
    }


    public override void update(double dt) {
        base.update(dt);
        if (!currentMenu.isModal()) {
            INGAME_MENU.update(dt);
        }

        world.player.pressedMovementKey = false;
        world.player.strafeVector = new Vector3D<double>(0, 0, 0);
        world.player.inputVector = new Vector3D<double>(0, 0, 0);
        if (!world.paused && !Game.lockingMouse) {
            world.player.updateInput(dt);
            world.update(dt);
            world.player.update(dt);
        }
        world.renderUpdate();

        // turn on for stress testing:)
        //Utils.wasteMemory(dt, 200);
        var prevTargetedPos = Game.instance.targetedPos;
        var col = Raycast.raycast(world);
        // previous pos
        if (col.hit) {
            Game.instance.targetedPos = col.block;
            Game.instance.previousPos = col.previous;
        }
        else {
            Game.instance.targetedPos = null;
            Game.instance.previousPos = null;
        }

        bool meshOutline = col.hit && prevTargetedPos != Game.instance.targetedPos;
        if (meshOutline) {
            world.renderer.meshBlockOutline();
        }
    }

    public override void render(double dt, double interp) {
        base.render(dt, interp);
        if (!currentMenu.isModal()) {
            INGAME_MENU.render(dt, interp);
        }
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
        if (!currentMenu.isModal()) {
            INGAME_MENU.postRender(dt, interp);
        }
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
            if (!world.inMenu && !world.paused) {
                pause();
            }
            else {
                backToGame();
            }
        }

        // if there is a menu open, don't allow any other keypresses from this handler
        if (currentMenu.isModal() && currentMenu != INGAME_MENU) {
            return;
        }

        if (key == Key.F3) {
            debugScreen = !debugScreen;
        }

        // reload chunks
        if (key == Key.A && keyboard.IsKeyPressed(Key.F3)) {
            remeshWorld(Settings.instance.renderDistance);
        }

        if (key == Key.F) {
            world.worldIO.save(world, world.name);
        }

        if (key == Key.G) {
            world?.Dispose();
            world = WorldIO.load("level1");
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
                switchToMenu(new InventoryMenu(new Vector2D<int>(0, 32)));
                ((InventoryMenu)currentMenu!).setup();
                world.inMenu = true;
                Game.instance.unlockMouse();
            }
        }

        if (key == Key.Space) {
            if (Game.permanentStopwatch.ElapsedMilliseconds < world.player.spacePress + Constants.flyModeDelay * 1000) {
                world.player.flyMode = !world.player.flyMode;
            }
            world.player.spacePress = Game.permanentStopwatch.ElapsedMilliseconds;
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

    public void remeshWorld(int oldRenderDist) {
        Console.Out.WriteLine("remeshed!");
        setUniforms();
        foreach (var chunk in world.chunks.Values) {
            // don't set chunk if not loaded yet, else we will have broken chunkgen/lighting errors
            if (chunk.status >= ChunkStatus.MESHED) {
                if (oldRenderDist < Settings.instance.renderDistance) {
                    // just unload everything
                    chunk.status = ChunkStatus.MESHED - 1;
                }
                else {
                    chunk.meshChunk();
                }
            }
        }
        world.player.loadChunksAroundThePlayer(Settings.instance.renderDistance);

        // free up memory from the block arraypool - we probably don't need that much
        ArrayBlockData.blockPool.trim();
        ArrayBlockData.lightPool.trim();
    }

    public void setUniforms() {
        world.renderer.setUniforms();
    }

    public void pause() {
        switchToMenu(PAUSE_MENU);
        world.inMenu = true;
        world.paused = true;
        Game.instance.unlockMouse();
        world.player.catchUpOnPrevVars();
    }

    public void backToGame() {
        switchToMenu(INGAME_MENU);
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
        if (!currentMenu.isModal()) {
            INGAME_MENU.draw();
        }

        var gui = Game.gui;

        GD.ShaderProgram = GUI.instance.shader;
        var centreX = Game.centreX;
        var centreY = Game.centreY;


        if (currentMenu == INGAME_MENU) {
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

            if (debugScreen) {

                D.drawLine(new Vector3D<double>(0, 0, 0), new Vector3D<double>(1, 1, 1), Color4b.Red);
                D.drawLine(new Vector3D<double>(1, 1, 1), new Vector3D<double>(24, 24, 24), Color4b.Red);
                //D.drawAABB(p.aabb);
                D.flushLines();
            }
        }

        if (world.paused && currentMenu == PAUSE_MENU) {
            var pauseText = "-PAUSED-";
            gui.drawStringCentred(pauseText, new Vector2(Game.centreX, Game.centreY - 16 * GUI.guiScale),
                Color4b.OrangeRed);
        }
    }

    public override void postDraw() {
        base.postDraw();
        if (!currentMenu.isModal()) {
            INGAME_MENU.postDraw();
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

    public override void clear(GraphicsDevice GD, double dt, double interp) {
        GD.ClearColor = WorldRenderer.defaultClearColour;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
    }
    public override void onKeyUp(IKeyboard keyboard, Key key, int scancode) {

    }

    public void openSettings() {
        Menu.SETTINGS.prevMenu = PAUSE_MENU;
        switchToMenu(Menu.SETTINGS);
    }
}