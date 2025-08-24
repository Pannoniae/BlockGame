using System.Drawing;
using System.Numerics;
using System.Runtime;
using BlockGame.GL.vertexformats;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Legacy;
using RectangleF = System.Drawing.RectangleF;
using Vector3D = Molten.DoublePrecision.Vector3D;

namespace BlockGame.ui;

public class GameScreen : Screen {
    public Debug D;

    public bool debugScreen = false;
    public bool fpsOnly = false;
    public bool chunkBorders = false;
    public bool music = false;

    public readonly PauseMenu PAUSE_MENU = new();
    public readonly IngameMenu INGAME_MENU = new();
    public readonly ChatMenu CHAT = new();

    private TimerAction updateMemory;
    private TimerAction updateDebugText;

    private UpdateMemoryThread umt;

    // time acceleration for day/night cycle testing
    private float timeAcceleration = 1.0f;
    private float targetTimeAcceleration = 1.0f;


    private bool disposed;
    private long altF10Press;
    private long f3Press = -1;

    public override void activate() {
        D = new Debug();

        switchToMenu(INGAME_MENU);

        umt?.stop();
        umt = new UpdateMemoryThread(this);

        umt.start();

        //updateMemory = Game.setInterval(200, updateMemoryMethod);
        updateDebugText = Game.setInterval(100, INGAME_MENU.updateDebugTextMethod);
    }


    public override void deactivate() {
        base.deactivate();
        //Game.renderer = null;
        //updateMemory.enabled = false;
        updateDebugText.enabled = false;
        Game.clearInterval(updateDebugText);
    }


    public override void update(double dt) {
        base.update(dt);
        if (!currentMenu.isModal()) {
            INGAME_MENU.update(dt);
        }

        // update current tick
        CHAT.tick++;

        var world = Game.world;

        // time control for day/night cycle testing
        if (Game.keyboard.IsKeyPressed(Key.KeypadAdd)) {
            // speed up time
            targetTimeAcceleration = Math.Min(targetTimeAcceleration * 2.0f, 64.0f);
            //Console.Out.WriteLine($"Time acceleration: {targetTimeAcceleration}x");
        }
        else if (Game.keyboard.IsKeyPressed(Key.KeypadSubtract)) {
            // slow down time
            targetTimeAcceleration = Math.Max(targetTimeAcceleration / 2.0f, 0.25f);
            //Console.Out.WriteLine($"Time acceleration: {targetTimeAcceleration}x");
        }
        else {
            targetTimeAcceleration = 1.0f; // reset to normal speed
        }

        // smooth time acceleration transition
        if (Math.Abs(timeAcceleration - targetTimeAcceleration) > 0.01f) {
            timeAcceleration = Meth.lerp(timeAcceleration, targetTimeAcceleration, (float)(dt * 2.0)); // 2x lerp speed
        }
        else {
            timeAcceleration = targetTimeAcceleration;
        }

        // apply time acceleration (frame-rate independent)
        if (timeAcceleration != 1.0f) {
            int additionalTicks = (int)((timeAcceleration - 1.0f) * dt * 60); // 60 TPS base
            world.worldTick += additionalTicks;
        }

        // if user holds down alt + f10 for 5 seconds, crash the game lul
        if (Game.keyboard.IsKeyPressed(Key.AltLeft) && Game.keyboard.IsKeyPressed(Key.F10) &&
            Game.permanentStopwatch.ElapsedMilliseconds > altF10Press + 5000) {
            MemoryUtils.crash("Alt + F10 pressed for 5 seconds, SKILL ISSUE BITCH!");
        }
        
        // check for F3 release behavior
        if (f3Press != -1 && !Game.keyboard.IsKeyPressed(Key.F3)) {
            // F3 was released - check if it was a short press
            var pressDuration = Game.permanentStopwatch.ElapsedMilliseconds - f3Press;
            if (pressDuration < 400) {
                // short press - toggle debug screen or fps mode
                if (Game.keyboard.IsKeyPressed(Key.ShiftLeft)) {
                    fpsOnly = !fpsOnly;
                } else {
                    debugScreen = !debugScreen;
                }
            }
            f3Press = -1;
        }
        
        // we update input here (shit doesn't work in non-main thread)
        

        world.player.pressedMovementKey = false;
        world.player.strafeVector = new Vector3D(0, 0, 0);
        world.player.inputVector = new Vector3D(0, 0, 0);

        if (!world.paused && !Game.lockingMouse) {
            if (currentMenu == INGAME_MENU) {
                world.player.updateInput(dt);
            }

            world.update(dt);
            world.player.update(dt);
        }

        world.renderUpdate(dt);
        Game.renderer.update(dt);

        // turn on for stress testing:)
        //Utils.wasteMemory(dt, 200);
        var prevTargetedPos = Game.instance.targetedPos;
        var col = Raycast.raycast(world);

        Game.raycast = col;
        // previous pos
        if (col.hit) {
            Game.instance.targetedPos = col.block;
            Game.instance.previousPos = col.previous;
        }
        else {
            Game.instance.targetedPos = null;
            Game.instance.previousPos = null;
        }

    }

    public override void render(double dt, double interp) {
        base.render(dt, interp);
        Game.metrics.clear();

        var world = Game.world;

        //world.mesh();
        world.player.camera.calculateFrustum(interp);
        //Console.Out.WriteLine(world.player.camera.frustum);
        Game.renderer.render(interp);
        if (Game.instance.targetedPos.HasValue) {
            //Console.Out.WriteLine(Game.instance.targetedPos.Value);
            Game.renderer.drawBlockOutline(interp);
        }

        D.renderTick(interp);
        // update here because in the main menu, we don't have a world
        Game.fontLoader.renderer3D.renderTick(interp);
        if (!currentMenu.isModal()) {
            INGAME_MENU.render(dt, interp);
        }

        const string text = "THIS IS A LONG TEXT\nmultiple lines!";
        /*Game.gui.drawStringOnBlock(text, new Vector3I(0, 100, 0), RawDirection.WEST, 2f);
        Game.gui.drawStringOnBlock(text, new Vector3I(0, 100, 0), RawDirection.EAST, 2f);
        Game.gui.drawStringOnBlock(text, new Vector3I(0, 100, 0), RawDirection.SOUTH, 2f);
        Game.gui.drawStringOnBlock(text, new Vector3I(0, 100, 0), RawDirection.NORTH, 2f);
        Game.gui.drawStringOnBlock(text, new Vector3I(0, 100, 0), RawDirection.DOWN, 2f);
        Game.gui.drawStringOnBlock(text, new Vector3I(0, 100, 0), RawDirection.UP, 2f);*/
    }

    public override void postRender(double dt, double interp) {
        if (!currentMenu.isModal()) {
            INGAME_MENU.postRender(dt, interp);
        }

        // render entities
        Game.GL.Disable(EnableCap.DepthTest);
        Game.world.player.render(dt, interp);
        Game.GL.Enable(EnableCap.DepthTest);
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
        base.onMouseDown(mouse, button);
        if (Game.world.inMenu || currentMenu != INGAME_MENU) {
            return;
        }

        switch (button) {
            case MouseButton.Left:
                Game.world.player.breakBlock();
                break;
            case MouseButton.Right:
                Game.world.player.placeBlock();
                break;
            case MouseButton.Middle:
                Game.world.player.pickBlock();
                break;
        }
    }

    public override void onMouseMove(IMouse mouse, Vector2 pos) {
        base.onMouseMove(mouse, pos);
        if (!Game.focused || Game.world.inMenu || currentMenu != INGAME_MENU) {
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

                Game.player.camera.ModifyDirection(xOffset, yOffset);
            }
        }

        Game.firstFrame = false;
    }

    public override void scroll(IMouse mouse, ScrollWheel scroll) {
        int y = (int)Math.Clamp(-scroll.Y, -1, 1);
        var newSelection = Game.player.hotbar.selected + y;
        newSelection = Meth.mod(newSelection, 10);
        Game.player.hotbar.selected = newSelection;
    }

    public override void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyUp(keyboard, key, scancode);
        // if there is a menu open, don't allow any other keypresses from this handler
        if (currentMenu.isBlockingInput() && currentMenu != INGAME_MENU) {
            return;
        }
    }

    public override void onKeyChar(IKeyboard keyboard, char c) {
        base.onKeyChar(keyboard, c);
        // if there is a menu open, don't allow any other keypresses from this handler
        if (currentMenu.isBlockingInput() && currentMenu != INGAME_MENU) {
            return;
        }
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);

        if (key == Key.Escape) {
            if (currentMenu == CHAT) {
                CHAT.closeChat();
            }

            // hack for back to main menu
            else if (!Game.world.inMenu && !Game.world.paused) {
                pause();
            }
            else if (currentMenu != Menu.SETTINGS) {
                backToGame();
            }
        }

        // if there is a menu open, don't allow any other keypresses from this handler
        if (currentMenu.isBlockingInput() && currentMenu != INGAME_MENU) {
            return;
        }

        var world = Game.world;

        switch (key) {
            case Key.F3:
                // cancel any pending F3 toggle if other keys besides Shift are pressed
                var pressedKeys = keyboard.GetPressedKeys().Count;
                var hasShift = keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight);
                
                if (pressedKeys > (hasShift ? 2 : 1)) {
                    f3Press = -1;
                } else {
                    // start F3 timeout window
                    f3Press = Game.permanentStopwatch.ElapsedMilliseconds;
                }
                break;
            case Key.F4:
                INGAME_MENU.ToggleSegmentedMode();
                break;
            // reload chunks
            case Key.A when keyboard.IsKeyPressed(Key.F3):
                remeshWorld(Settings.instance.renderDistance);
                break;
            case Key.F:
                world.worldIO.save(world, world.name);
                break;
            case Key.G:
                world?.Dispose();
                world = WorldIO.load("level1");
                Game.instance.resize(new Vector2D<int>(Game.width, Game.height));
                break;
            case Key.F8:
                Game.noUpdate = !Game.noUpdate;
                break;
            case Key.F9:
                // on shift, just clean GC
                if (keyboard.IsKeyPressed(Key.ShiftLeft)) {
                    Console.Out.WriteLine("Cleaning GC");
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    break;
                }

                MemoryUtils.cleanGC();
                //Game.mm();
                break;
            case Key.F10: {
                altF10Press = Game.permanentStopwatch.ElapsedMilliseconds;

                // print vmem
                var vmem = MemoryUtils.getVRAMUsage(out _);
                if (vmem == -1) {
                    Console.Out.WriteLine("Can't get VRAM usage");
                }
                else {
                    Console.Out.WriteLine($"VRAM usage: {vmem / (1024 * 1024)}MB");
                }

                Console.Out.WriteLine("Alignment of array: " +
                                      MemoryUtils.getAlignment(Game.world.chunkList[0].blocks[0].blocks));
                Console.Out.WriteLine("Alignment of array: " +
                                      MemoryUtils.getAlignment(Game.world.chunkList[0].blocks[0].blocks[0]));
                break;
            }

            case Key.E: {
                if (world.inMenu) {
                    backToGame();
                }
                else {
                    switchToMenu(new InventoryMenu(new Vector2I(0, 32)));
                    ((InventoryMenu)currentMenu!).setup();
                    world.inMenu = true;
                    Game.instance.unlockMouse();
                }

                break;
            }
            case Key.Space: {
                if (Game.permanentStopwatch.ElapsedMilliseconds <
                    world.player.spacePress + Constants.flyModeDelay * 1000) {
                    world.player.flyMode = !world.player.flyMode;
                }

                world.player.spacePress = Game.permanentStopwatch.ElapsedMilliseconds;
                break;
            }
            case Key.T: {
                if (currentMenu == INGAME_MENU) {
                    Game.instance.executeOnMainThread(() => {
                        Game.instance.unlockMouse();
                        switchToMenu(CHAT);
                    });
                }
                else if (currentMenu == CHAT) {
                    Game.instance.lockMouse();
                    switchToMenu(INGAME_MENU);
                }

                break;
            }
            case Key.M: {
                // toggle music
                music = !music;
                if (music) {
                    Game.snd.unmuteMusic();
                }
                else {
                    Game.snd.muteMusic();
                }

                break;
            }

            // time control for day/night cycle testing
            case Key.KeypadMultiply: {
                // reset time speed
                targetTimeAcceleration = 1.0f;
                Console.Out.WriteLine("Time acceleration: 1x (normal)");
                break;
            }
            case Key.KeypadDivide: {
                // pause time
                targetTimeAcceleration = 0.0f;
                Console.Out.WriteLine("Time paused");
                break;
            }
            
            case Key.B when keyboard.IsKeyPressed(Key.F3): {
                // cycle metadata of targeted block
                cycleBlockMetadata();
                break;
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

    public void remeshWorld(int oldRenderDist) {
        var world = Game.world;
        Console.Out.WriteLine("remeshed!");
        
        // free up memory from the block arraypool - we probably don't need that much
        ArrayBlockData.blockPool.trim();
        ArrayBlockData.lightPool.trim();
        
        Game.renderer.setUniforms();
        foreach (var chunk in world.chunks.Values) {
            // don't set chunk if not loaded yet, else we will have broken chunkgen/lighting errors
            if (chunk.status >= ChunkStatus.MESHED) {
                // just unload everything
                chunk.status = ChunkStatus.MESHED - 1;
            }
        }

        world.player.loadChunksAroundThePlayer(Settings.instance.renderDistance);
    }

    public void pause() {
        switchToMenu(PAUSE_MENU);
        Game.world.inMenu = true;
        Game.world.paused = true;
        Game.instance.unlockMouse();
        Game.world.player.catchUpOnPrevVars();
    }

    public void backToGame() {
        switchToMenu(INGAME_MENU);
        Game.world.inMenu = false;
        Game.world.paused = false;
        Game.instance.lockMouse();
        //Game.lockingMouse = true;
    }

    private void backToMainMenu() {
        Game.instance.executeOnMainThread(() => {
            Console.Out.WriteLine("back");
            Game.instance.switchToScreen(MAIN_MENU_SCREEN);
        });
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        base.onMouseUp(pos, button);
        // if no longer holding, the player isn't clicking into the window anymore
        if (Game.focused && Game.lockingMouse) {
            Game.lockingMouse = false;
        }
    }

    public override void resize(Vector2I size) {
        base.resize(size);
        Game.world.player.camera.setViewport(size.X, size.Y);
    }

    public override void draw() {
        base.draw();
        if (!currentMenu.isModal()) {
            INGAME_MENU.draw();
        }

        var gui = Game.gui;

        // clear depth buffer so the gui can use it properly
        //Game.GL.Clear(ClearBufferMask.DepthBufferBit);

        Game.graphics.batchShader.use();
        var centreX = Game.centreX;
        var centreY = Game.centreY;


        if (currentMenu == INGAME_MENU || currentMenu == CHAT) {
            // Draw crosshair
            gui.tb.Draw(gui.colourTexture,
                new RectangleF(new PointF(centreX - Constants.crosshairThickness, centreY - Constants.crosshairSize),
                    new SizeF(Constants.crosshairThickness * 2, Constants.crosshairSize * 2)),
                new Color4b(240, 240, 240));

            gui.tb.Draw(gui.colourTexture,
                new RectangleF(new PointF(centreX - Constants.crosshairSize, centreY - Constants.crosshairThickness),
                    new SizeF(Constants.crosshairSize - Constants.crosshairThickness,
                        Constants.crosshairThickness * 2)),
                new Color4b(240, 240, 240));
            gui.tb.Draw(gui.colourTexture,
                new RectangleF(
                    new PointF(centreX + Constants.crosshairThickness, centreY - Constants.crosshairThickness),
                    new SizeF(Constants.crosshairSize - Constants.crosshairThickness,
                        Constants.crosshairThickness * 2)),
                new Color4b(240, 240, 240));

            // Draw debug lines
            if (debugScreen && !fpsOnly) {
                D.drawLine(new Vector3D(0, 0, 0), new Vector3D(1, 1, 1), Color4b.Red);
                D.drawLine(new Vector3D(1, 1, 1), new Vector3D(24, 24, 24), Color4b.Red);
            }

            // Draw chunk borders
            if (chunkBorders) {
                drawChunkBorders();
            }

            // Draw chat

            var msgLimit = currentMenu == CHAT ? 20 : 10;
            var currentTick = CHAT.tick;
            for (int i = 0; i < CHAT.getMessages().Size && i < msgLimit; i++) {
                // if 200 ticks have passed, don't show the message
                var age = currentTick - CHAT.getMessages()[i].ticks;
                if (age < 200 || currentMenu == CHAT) {
                    float a = 1;
                    if (currentMenu != CHAT) {
                        // fade out from 180 to 200 ticks (from 1 to 0)

                        // from 0 to 50
                        var remTicks = age - 180;
                        if (remTicks > 0) {
                            a = 1 - remTicks / 20f;
                        }
                    }

                    if (a > 0) {
                        var msgHeight = gui.uiHeight - 42 - (8 * i);

                        gui.drawUI(gui.colourTexture, RectangleF.FromLTRB(4, msgHeight, 4 + 320, msgHeight + 9),
                            color: new Color4b(0, 0, 0, MathF.Min(a, 0.5f)));
                        gui.drawStringUIThin(CHAT.getMessages()[i].message, new Vector2(6, msgHeight),
                            new Color4b(1, 1, 1, a));
                    }
                }
            }
        }

        if (Game.world.paused && currentMenu == PAUSE_MENU) {
            var pauseText = "-PAUSED-";
            gui.drawStringCentred(pauseText, new Vector2(Game.centreX, Game.centreY - 16 * GUI.guiScale),
                Color4b.OrangeRed);
        }
    }

    private void drawChunkBorders() {
        var world = Game.world;

        // draw chunk borders
        var playerPos = world.player.position;
        var playerChunkPos = World.getChunkPos((int)playerPos.X, (int)playerPos.Z);
        var chunkPos = new ChunkCoord(playerChunkPos.x, playerChunkPos.z);
        world.getChunkMaybe(new ChunkCoord(chunkPos.x, chunkPos.z), out var chunk);
        if (chunk != null) {
            var chunkWorldPos = World.toWorldPos(chunkPos, new Vector3I(0, 0, 0));
            var colour = Color4b.Red;
            colour = chunk.status switch {
                ChunkStatus.MESHED => Color4b.Blue,
                ChunkStatus.LIGHTED => Color4b.Green,
                ChunkStatus.POPULATED => Color4b.Yellow,
                ChunkStatus.GENERATED => Color4b.Orange,
                ChunkStatus.EMPTY => Color4b.Gray,
                _ => colour
            };
            
            // todo when we'll have a proper GL state tracker, we'll "uncomment" these and set them in the tracker
            // in case the previous code changes
            // for now, just don't set shit because the GUI code is already setup the same way
            //Game.GL.Enable(EnableCap.Blend);
            //Game.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //Game.GL.Disable(EnableCap.DepthTest);
            //Game.GL.Disable(EnableCap.CullFace);
            
            // draw translucent planes for chunk borders (north, south, east, west faces)
            var planeColour = new Color4b(colour.R, colour.G, colour.B, 32);
            
            D.idc.begin(PrimitiveType.Quads);
            // north face
            D.drawTranslucentPlane(
                new Vector3D(chunkWorldPos.X, 0, chunkWorldPos.Z),
                new Vector3D(chunkWorldPos.X, World.WORLDHEIGHT, chunkWorldPos.Z),
                new Vector3D(chunkWorldPos.X + Chunk.CHUNKSIZE, World.WORLDHEIGHT, chunkWorldPos.Z),
                new Vector3D(chunkWorldPos.X + Chunk.CHUNKSIZE, 0, chunkWorldPos.Z),
                planeColour);

            // south face
            D.drawTranslucentPlane(
                new Vector3D(chunkWorldPos.X + Chunk.CHUNKSIZE, 0, chunkWorldPos.Z + Chunk.CHUNKSIZE),
                new Vector3D(chunkWorldPos.X + Chunk.CHUNKSIZE, World.WORLDHEIGHT, chunkWorldPos.Z + Chunk.CHUNKSIZE),
                new Vector3D(chunkWorldPos.X, World.WORLDHEIGHT, chunkWorldPos.Z + Chunk.CHUNKSIZE),
                new Vector3D(chunkWorldPos.X, 0, chunkWorldPos.Z + Chunk.CHUNKSIZE),
                planeColour);

            // west face
            D.drawTranslucentPlane(
                new Vector3D(chunkWorldPos.X, 0, chunkWorldPos.Z + Chunk.CHUNKSIZE),
                new Vector3D(chunkWorldPos.X, World.WORLDHEIGHT, chunkWorldPos.Z + Chunk.CHUNKSIZE),
                new Vector3D(chunkWorldPos.X, World.WORLDHEIGHT, chunkWorldPos.Z),
                new Vector3D(chunkWorldPos.X, 0, chunkWorldPos.Z),
                planeColour);

            // east face
            D.drawTranslucentPlane(
                new Vector3D(chunkWorldPos.X + Chunk.CHUNKSIZE, 0, chunkWorldPos.Z),
                new Vector3D(chunkWorldPos.X + Chunk.CHUNKSIZE, World.WORLDHEIGHT, chunkWorldPos.Z),
                new Vector3D(chunkWorldPos.X + Chunk.CHUNKSIZE, World.WORLDHEIGHT, chunkWorldPos.Z + Chunk.CHUNKSIZE),
                new Vector3D(chunkWorldPos.X + Chunk.CHUNKSIZE, 0, chunkWorldPos.Z + Chunk.CHUNKSIZE),
                planeColour);
            D.idc.end();
            

            // draw 16x16x16 subchunk wireframes
            D.idc.begin(PrimitiveType.Lines);
            for (int sy = 0; sy < Chunk.CHUNKHEIGHT; sy++) {
                var subChunkWorldPos = new Vector3I(chunkWorldPos.X, sy * Chunk.CHUNKSIZE, chunkWorldPos.Z);
                var min = subChunkWorldPos;
                var max = subChunkWorldPos + new Vector3I(Chunk.CHUNKSIZE, Chunk.CHUNKSIZE, Chunk.CHUNKSIZE);

                // use dimmer color for subchunk wireframes
                var wireColor = new Color4b((byte)(colour.R / 2), (byte)(colour.G / 2), (byte)(colour.B / 2), 255);

                // draw 12 edges of the subchunk wireframe
                // bottom face edges
                D.drawLine(new Vector3D(min.X, min.Y, min.Z), new Vector3D(max.X, min.Y, min.Z), wireColor);
                D.drawLine(new Vector3D(max.X, min.Y, min.Z), new Vector3D(max.X, min.Y, max.Z), wireColor);
                D.drawLine(new Vector3D(max.X, min.Y, max.Z), new Vector3D(min.X, min.Y, max.Z), wireColor);
                D.drawLine(new Vector3D(min.X, min.Y, max.Z), new Vector3D(min.X, min.Y, min.Z), wireColor);

                // top face edges
                D.drawLine(new Vector3D(min.X, max.Y, min.Z), new Vector3D(max.X, max.Y, min.Z), wireColor);
                D.drawLine(new Vector3D(max.X, max.Y, min.Z), new Vector3D(max.X, max.Y, max.Z), wireColor);
                D.drawLine(new Vector3D(max.X, max.Y, max.Z), new Vector3D(min.X, max.Y, max.Z), wireColor);
                D.drawLine(new Vector3D(min.X, max.Y, max.Z), new Vector3D(min.X, max.Y, min.Z), wireColor);

                // vertical edges
                D.drawLine(new Vector3D(min.X, min.Y, min.Z), new Vector3D(min.X, max.Y, min.Z), wireColor);
                D.drawLine(new Vector3D(max.X, min.Y, min.Z), new Vector3D(max.X, max.Y, min.Z), wireColor);
                D.drawLine(new Vector3D(max.X, min.Y, max.Z), new Vector3D(max.X, max.Y, max.Z), wireColor);
                D.drawLine(new Vector3D(min.X, min.Y, max.Z), new Vector3D(min.X, max.Y, max.Z), wireColor);
            }
            D.idc.end();
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

    public override void clear(double dt, double interp) {
        var world = Game.world;
        var clearColour = world?.getHorizonColour(world.worldTick) ?? WorldRenderer.defaultClearColour;
        Game.graphics.clearColor(clearColour);
        Game.GL.ClearDepth(1f);
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void openSettings() {
        Menu.SETTINGS.prevMenu = PAUSE_MENU;
        switchToMenu(Menu.SETTINGS);
    }
    
    private void cycleBlockMetadata() {
        if (!Game.instance.targetedPos.HasValue) {
            return;
        }

        var pos = Game.instance.targetedPos.Value;
        var world = Game.world;
        var blockValue = world.getBlockRaw(pos.X, pos.Y, pos.Z);
        var blockId = blockValue.getID();
        var currentMeta = blockValue.getMetadata();
        
        if (blockId == 0 || !Block.tryGet(blockId, out var block)) {
            return;
        }

        var maxMeta = block.maxValidMetadata();
        if (maxMeta == 0) {
            return;
        }

        // cycle to next metadata value
        var newMeta = (byte)((currentMeta + 1) % (maxMeta + 1));
        var newBlockValue = blockValue.setMetadata(newMeta);
        
        world.setBlockMetadataRemesh(pos.X, pos.Y, pos.Z, newBlockValue);
    }
}

public class UpdateMemoryThread(GameScreen screen) {
    private GameScreen screen = screen;
    public volatile bool stopped;

    public void run() {
        while (true) {
            if (stopped) {
                break;
            }

            updateMemoryMethod();
            // sleep 200ms
            Thread.Sleep(200);
        }
    }

    public void stop() {
        stopped = true;
    }

    public void start() {
        stopped = false;

        // run thread
        var thread = new Thread(run) {
            IsBackground = true,
            Name = "UpdateMemoryThread"
        };
        thread.Start();
    }

    public void updateMemoryMethod() {
        Game.proc.Refresh();
        screen.INGAME_MENU.workingSet = Game.proc.WorkingSet64;
        screen.INGAME_MENU.GCMemory = GC.GetTotalMemory(false);
    }
}