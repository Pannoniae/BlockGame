using System.Drawing;
using System.Numerics;
using System.Runtime;
using BlockGame.GL;
using BlockGame.logic;
using BlockGame.main;
using BlockGame.render;
using BlockGame.render.model;
using BlockGame.ui.element;
using BlockGame.ui.menu;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.chunk;
using Molten;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Legacy;
using Vector3D = Molten.DoublePrecision.Vector3D;

namespace BlockGame.ui.screen;

public class GameScreen : Screen {
    public Debug D;

    public bool debugScreen = false;
    public bool fpsOnly = false;
    public bool chunkBorders = false;
    public bool entityAABBs = false;
    public bool mobPathfinding = false;
    public bool music = false;

    public readonly PauseMenu PAUSE_MENU = new();
    public readonly DeathMenu DEATH_MENU = new();
    public readonly IngameMenu INGAME_MENU = new();
    public readonly ChatMenu CHAT = new();

    private HotbarGUI? hotbar;

    private TimerAction updateDebugText;

    private UpdateMemoryThread umt;

    // time acceleration for day/night cycle testing
    private float timeAcceleration = 1.0f;
    private float targetTimeAcceleration = 1.0f;


    private bool disposed;
    private long altF10Press;
    private long altF7Press;
    private long f3Press = -1;

    public override void activate() {
        D = new Debug();

        // lock mouse & activate ingame menu
        backToGame();

        // create hotbar and add to ingame menu
        hotbar = new HotbarGUI(INGAME_MENU, "hotbar", new Vector2I(0, -20)) {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        INGAME_MENU.addElement(hotbar);

        umt?.stop();
        umt = new UpdateMemoryThread(this);

        umt.start();

        //updateMemory = Game.setInterval(200, updateMemoryMethod);
        updateDebugText = Game.setInterval(100, INGAME_MENU.updateDebugTextMethod);
    }


    public override void deactivate() {
        base.deactivate();

        // remove hotbar from menu
        if (hotbar != null) {
            INGAME_MENU.removeElement("hotbar");
            hotbar = null;
        }

        //Game.renderer = null;
        //updateMemory.enabled = false;
        updateDebugText.enabled = false;
        Game.clearInterval(updateDebugText);
    }

    public void trim() {
        umt?.needTrim = true;
    }


    public override void update(double dt) {
        if (!currentMenu.isModal() && currentMenu != INGAME_MENU) {
            INGAME_MENU.update(dt);
        }
        base.update(dt);

        var world = Game.world;

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

        // update current tick
        CHAT.tick++;

        // time control for day/night cycle testing
        if (Game.keyboard.IsKeyPressed(Key.KeypadAdd)) {
            // speed up time
            targetTimeAcceleration = Math.Min(targetTimeAcceleration * 2.0f, 32.0f);
        }
        else if (Game.keyboard.IsKeyPressed(Key.KeypadSubtract)) {
            // slow down time
            targetTimeAcceleration = Math.Max(targetTimeAcceleration / 2.0f, 0.25f);
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
            for (int i = 0; i < additionalTicks; i++) {
                world.update(dt);
            }
        }

        // if user holds down alt + f10 for 5 seconds, crash the game lul
        if (Game.keyboard.IsKeyPressed(Key.AltLeft) && Game.keyboard.IsKeyPressed(Key.F10) &&
            Game.permanentStopwatch.ElapsedMilliseconds > altF10Press + 5000) {
            Game.instance.executeOnMainThread(() =>
                MemoryUtils.crash("Alt + F10 pressed for 5 seconds, SKILL ISSUE BITCH!"));
        }

        // same for f7 but managed crash
        if (Game.keyboard.IsKeyPressed(Key.AltLeft) && Game.keyboard.IsKeyPressed(Key.F7) &&
            Game.permanentStopwatch.ElapsedMilliseconds > altF7Press + 5000) {
            Game.instance.executeOnMainThread(() =>
                SkillIssueException.throwNew("Alt + F7 pressed for 5 seconds, SKILL ISSUE BITCH!"));
        }

        // check for F3 release behavior
        if (f3Press != -1 && !Game.keyboard.IsKeyPressed(Key.F3)) {
            // F3 was released - check if it was a short press
            var pressDuration = Game.permanentStopwatch.ElapsedMilliseconds - f3Press;
            if (pressDuration < 400) {
                // short press - toggle debug screen or fps mode
                if (Game.keyboard.IsKeyPressed(Key.ShiftLeft)) {
                    fpsOnly = !fpsOnly;
                }
                else {
                    debugScreen = !debugScreen;
                }
            }

            f3Press = -1;
        }

        // we update input here (shit doesn't work in non-main thread)


        world.player.strafeVector = new Vector3D(0, 0, 0);
        world.player.inputVector = new Vector3D(0, 0, 0);

        if (!world.paused && !Game.lockingMouse) {
            if (currentMenu == INGAME_MENU) {
                Game.player.updateInput(dt);
            }
            Game.player.blockHandling(dt);
            world.update(dt);
            //world.player.update(dt);
        }

        world.renderUpdate(dt);
        Game.renderer.update(dt);
        Game.renderer.updateRandom(dt);
    }

    public override void render(double dt, double interp) {
        base.render(dt, interp);
        Game.metrics.clear();

        var world = Game.world;

        //world.mesh();
        Game.camera.calculateFrustum(interp);
        Game.renderer.render(interp);
        if (Game.instance.targetedPos.HasValue) {
            Game.renderer.drawBlockOutline(interp);
        }

        D.renderTick(interp);
        // update here because in the main menu, we don't have a world
        Game.fontLoader.renderer3D.renderTick(interp);
        if (!currentMenu.isModal() && currentMenu != INGAME_MENU) {
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
        if (!currentMenu.isModal() && currentMenu != INGAME_MENU) {
            INGAME_MENU.postRender(dt, interp);
        }

        // render entities
        //Game.GL.Disable(EnableCap.DepthTest);

        // since we have 3d items, we don't bother with disabling because it will be fucked. HOWEVER, we do a bit of depth clearing...
        Game.GL.Clear(ClearBufferMask.DepthBufferBit);
        Game.world.player.render(dt, interp);
        //Game.GL.Enable(EnableCap.DepthTest);
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {

        // also return if we're ingame!! so we won't handle bs "clicks"
        if (Game.world.inMenu || currentMenu != INGAME_MENU || currentMenu == INGAME_MENU) {
            return;
        }

        base.onMouseDown(mouse, button);
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

                Game.player.handleMouseInput(xOffset, yOffset);
            }
        }

        Game.firstFrame = false;
    }

    public override void scroll(IMouse mouse, ScrollWheel scroll) {
        base.scroll(mouse, scroll);
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

            // if death screen, DO NOT DO IT
            if (currentMenu == DEATH_MENU) {
                return;
            }

            if (currentMenu == CHAT) {
                CHAT.closeChat();
            }

            // hack for back to main menu
            else if (!Game.world.inMenu && !Game.world.paused) {
                pause();
            }
            else {
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
                }
                else {
                    // start F3 timeout window
                    f3Press = Game.permanentStopwatch.ElapsedMilliseconds;
                }

                break;
            case Key.F4:
                INGAME_MENU.ToggleSegmentedMode();
                break;
            case Key.F5:
                Game.camera.cycleMode();
                break;
            // reload chunks
            case Key.A when keyboard.IsKeyPressed(Key.F3):
                remeshWorld(Settings.instance.renderDistance);
                break;
            case Key.F:
                world.worldIO.save(world, world.name);
                break;
            case Key.F8 when keyboard.IsKeyPressed(Key.ShiftLeft):
                Game.noUpdate = !Game.noUpdate;
                break;
            case Key.F9:
                // on shift, just clean GC
                if (keyboard.IsKeyPressed(Key.ShiftLeft)) {
                    Log.info("Cleaning GC");
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
                    Log.warn("Can't get VRAM usage");
                }
                else {
                    Log.info($"VRAM usage: {vmem / (1024 * 1024)}MB");
                }

                break;
            }

            case Key.F7: {
                altF7Press = Game.permanentStopwatch.ElapsedMilliseconds;
                break;
            }

            case Key.E: {
                if (world.inMenu) {
                    backToGame();
                }
                else {
                    // open appropriate inventory menu based on gamemode
                    if (Game.gamemode == GameMode.survival) {
                        switchToMenu(new SurvivalInventoryMenu(new Vector2I(0, 32)));
                        ((SurvivalInventoryMenu)currentMenu!).setup();
                    } else {
                        switchToMenu(new CreativeInventoryMenu(new Vector2I(0, 32)));
                        ((CreativeInventoryMenu)currentMenu!).setup();
                    }
                    Game.instance.unlockMouse();
                }

                break;
            }
            case Key.Space: {
                if (Game.permanentStopwatch.ElapsedMilliseconds <
                    world.player.spacePress + Constants.flyModeDelay * 1000) {
                    if (Game.gamemode.flying) {
                        world.player.flyMode = !world.player.flyMode;
                    }
                }

                world.player.spacePress = Game.permanentStopwatch.ElapsedMilliseconds;
                break;
            }
            case Key.T: {
                if (keyboard.IsKeyPressed(Key.F3)) {
                    // reload all textures
                    Game.textures.reloadAll();
                    Log.info("Reloaded all textures");
                }
                else {
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
                }

                break;
            }
            case Key.M: {
                if (keyboard.IsKeyPressed(Key.F3)) {
                    // reload all entity models
                    EntityRenderers.reloadAll();
                    BlockEntityRenderers.reloadAll();
                    Log.info("Reloaded all entity models");
                }
                else {
                    // toggle music
                    music = !music;
                    if (music) {
                        Game.snd.unmuteMusic();
                    }
                    else {
                        Game.snd.muteMusic();
                    }
                }


                break;
            }

            // time control for day/night cycle testing
            case Key.KeypadMultiply: {
                // reset time speed
                targetTimeAcceleration = 1.0f;
                Log.info("Time acceleration: 1x (normal)");
                break;
            }
            case Key.KeypadDivide: {
                // pause time
                targetTimeAcceleration = 0.0f;
                Log.info("Time paused");
                break;
            }

            case Key.B when keyboard.IsKeyPressed(Key.F3): {
                // cycle metadata of targeted block
                cycleBlockMetadata();
                break;
            }
            case Key.C when keyboard.IsKeyPressed(Key.F3): {
                // toggle frustum freeze
                Game.camera.frustumFrozen = !Game.camera.frustumFrozen;
                Log.info("Frustum freeze: " + (Game.camera.frustumFrozen ? "ON" : "OFF"));
                break;
            }
            case Key.X when keyboard.IsKeyPressed(Key.F3): {
                // toggle entity AABB rendering and pathfinding visualisation
                entityAABBs = !entityAABBs;
                mobPathfinding = entityAABBs;
                Log.info("Entity AABBs & pathfinding: " + (entityAABBs ? "ON" : "OFF"));
                break;
            }
            case Key.R when keyboard.IsKeyPressed(Key.F3): {
                // regenerate all chunks (worldgen testing!)
                regenAllChunks();
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

    public void regenAllChunks() {
        var world = Game.world;
        Log.info("Regenerating all chunks...");

        var ch = world.chunks.Pairs;

        List<(long Key, Chunk Value)> l = [];
        // copy
        foreach (var chunk in ch) {
            l.Add((chunk.Key, chunk.Value));
        }

        // destroy and clear all chunks
        foreach (var chunk in l) {
            world.unloadChunkWithHammer(chunk.Value.coord);
        }

        // delete chunk files from disk so they regenerate
        foreach (var coord in l) {
            var path = WorldIO.getChunkString(world.name, new ChunkCoord(coord.Key));
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        // clear renderer queues
        while (Game.renderer.meshingQueue.TryDequeue(out _)) { }

        // clear light queues
        world.skyLightQueue.Clear();
        world.skyLightRemovalQueue.Clear();
        world.blockLightQueue.Clear();
        world.blockLightRemovalQueue.Clear();

        // free pools
        ArrayBlockData.blockPool.clear();
        ArrayBlockData.lightPool.clear();
        PaletteBlockData.arrayPool.clear();
        PaletteBlockData.arrayPoolU.clear();
        PaletteBlockData.arrayPoolUS.clear();
        WorldIO.saveBlockPool.clear();
        WorldIO.saveLightPool.clear();
        HeightMap.heightPool.clear();

        // reload around player
        world.player.loadChunksAroundThePlayer(Settings.instance.renderDistance);

        // trigger "chunk change"
        Game.setTimeout(200, () => {
            world.player.onChunkChanged();
        });

        Log.info("Chunk regeneration complete");
    }

    public void remeshWorld(int oldRenderDist) {
        var world = Game.world;

        // free up memory from the block arraypool - we probably don't need that much
        ArrayBlockData.blockPool.clear();
        ArrayBlockData.lightPool.clear();
        PaletteBlockData.arrayPool.clear();
        PaletteBlockData.arrayPoolU.clear();
        PaletteBlockData.arrayPoolUS.clear();

        WorldIO.saveBlockPool.clear();
        WorldIO.saveLightPool.clear();
        HeightMap.heightPool.clear();


        Game.renderer.setUniforms();
        foreach (var chunk in world.chunks) {
            // don't set chunk if not loaded yet, else we will have broken chunkgen/lighting errors
            if (chunk.status >= ChunkStatus.LIGHTED) {
                // mark for remeshing by clearing VAOs and dirtying
                for (int y = 0; y < Chunk.CHUNKHEIGHT; y++) {
                    var subChunk = chunk.subChunks[y];
                    subChunk.vao?.Dispose();
                    subChunk.vao = null;
                    subChunk.watervao?.Dispose();
                    subChunk.watervao = null;
                    world.dirtyChunk(new SubChunkCoord(chunk.coord.x, y, chunk.coord.z));
                }
                chunk.status = ChunkStatus.LIGHTED;
            }
        }

        world.player.loadChunksAroundThePlayer(Settings.instance.renderDistance);

        // queue up ANOTHER freeing because we'll be saving a lot of chunks now
        // todo is this REALLY needed??
        Game.setTimeout(4000, () => {
            ArrayBlockData.blockPool.clear();
            ArrayBlockData.lightPool.clear();
            PaletteBlockData.arrayPool.clear();
            PaletteBlockData.arrayPoolU.clear();
            PaletteBlockData.arrayPoolUS.clear();

            WorldIO.saveBlockPool.clear();
            WorldIO.saveLightPool.clear();
            HeightMap.heightPool.clear();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(generation: 2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        });
    }

    public void pause() {
        // save world when opening pause menu (so if the player ragequits or whatever it won't be fucked)
        Game.world.worldIO.save(Game.world, Game.world.name);

        // also free up memory!
        trim();

        switchToMenu(PAUSE_MENU);
        Game.instance.unlockMouse();
    }

    public void backToGame() {
        switchToMenu(INGAME_MENU);
        Game.instance.lockMouse();
    }

    private void backToMainMenu() {
        Game.instance.executeOnMainThread(() => {
            Log.debug("back");
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
        Game.camera.setViewport(size.X, size.Y);
    }

    public override void draw() {
        if (!currentMenu.isModal() && currentMenu != INGAME_MENU) {
            INGAME_MENU.draw();
        }
        base.draw();

        var gui = Game.gui;

        // clear depth buffer so the gui can use it properly
        //Game.GL.Clear(ClearBufferMask.DepthBufferBit);

        var centreX = Game.centreX;
        var centreY = Game.centreY;


        if (currentMenu == INGAME_MENU || currentMenu == CHAT) {
            // Draw crosshair
            gui.tb.Draw(gui.colourTexture,
                new RectangleF(new PointF(centreX - Constants.crosshairThickness, centreY - Constants.crosshairSize),
                    new SizeF(Constants.crosshairThickness * 2, Constants.crosshairSize * 2)),
                new Color(240, 240, 240));

            gui.tb.Draw(gui.colourTexture,
                new RectangleF(new PointF(centreX - Constants.crosshairSize, centreY - Constants.crosshairThickness),
                    new SizeF(Constants.crosshairSize - Constants.crosshairThickness,
                        Constants.crosshairThickness * 2)),
                new Color(240, 240, 240));
            gui.tb.Draw(gui.colourTexture,
                new RectangleF(
                    new PointF(centreX + Constants.crosshairThickness, centreY - Constants.crosshairThickness),
                    new SizeF(Constants.crosshairSize - Constants.crosshairThickness,
                        Constants.crosshairThickness * 2)),
                new Color(240, 240, 240));

            // Draw debug lines
            if (debugScreen && !fpsOnly) {
                D.idc.begin(PrimitiveType.Lines);
                D.drawLine(new Vector3D(0, 0, 0), new Vector3D(1, 1, 1), Color.Red);
                D.drawLine(new Vector3D(1, 1, 1), new Vector3D(24, 24, 24), Color.Red);
                D.idc.end();
            }

            // Draw chunk borders
            if (chunkBorders) {
                drawChunkBorders();
            }

            // Draw entity AABBs
            if (entityAABBs) {
                drawEntityAABBs();
            }

            // Draw mob pathfinding
            if (mobPathfinding) {
                drawMobPathfinding();
            }

            // Draw chat

            var msgLimit = currentMenu == CHAT ? 20 : 10;
            var currentTick = CHAT.tick;
            for (int i = 0; i < CHAT.getMessages().Count && i < msgLimit; i++) {
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
                            color: new Color(0, 0, 0, MathF.Min(a, 0.5f)));
                        gui.drawStringUIThin(CHAT.getMessages()[i].message, new Vector2(6, msgHeight),
                            new Color(1, 1, 1, a));
                    }
                }
            }
        }

        if (Game.world.paused && currentMenu == PAUSE_MENU) {
            var pauseText = "-PAUSED-";
            gui.drawStringCentred(pauseText, new Vector2(Game.centreX, Game.centreY - 16 * GUI.guiScale),
                Color.OrangeRed);
        }

        // draw red damage tint when taking damage
        if (Game.player != null && Game.player.dmgTime > 0) {
            float alpha = (float)Game.player.dmgTime / 30f * 0.3f; // max 30% opacity
            gui.drawUI(gui.colourTexture,
                new RectangleF(0, 0, Game.width, Game.height),
                color: new Color(1f, 0f, 0f, alpha));
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
            var colour = Color.Red;
            colour = chunk.status switch {
                ChunkStatus.MESHED => Color.Blue,
                ChunkStatus.LIGHTED => Color.Green,
                ChunkStatus.POPULATED => Color.Yellow,
                ChunkStatus.GENERATED => Color.Orange,
                ChunkStatus.EMPTY => Color.Gray,
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
            var planeColour = new Color(colour.R, colour.G, colour.B, (byte)32);

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
                var wireColor = new Color((byte)(colour.R / 2), (byte)(colour.G / 2), (byte)(colour.B / 2), (byte)255);

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

    private void drawEntityAABBs() {
        var world = Game.world;

        var playerPos = world.player.position;
        const double renderRange = 32.0;

        var mat = Game.graphics.model;
        mat.push();
        mat.loadIdentity();

        D.idc.begin(PrimitiveType.Lines);

        // iterate through all entities
        foreach (var entity in world.entities) {

            var distance = (entity.position - playerPos).Length();
            if (distance > renderRange) continue;

            // update the entity's AABB based on current position
            entity.aabb = entity.calcAABB(entity.position);

            // use different colours for different entity types
            var c = entity switch {
                Player => Color.Green,
                _ => Color.Yellow
            };

            // draw the AABB wireframe
            D.drawAABB(entity.aabb, c);

        }

        D.idc.end();
        mat.pop();
    }

    private void drawMobPathfinding() {
        var world = Game.world;

        var playerPos = world.player.position;
        const double renderRange = 32.0;

        var mat = Game.graphics.model;
        mat.push();
        mat.loadIdentity();

        D.idc.begin(PrimitiveType.Lines);

        foreach (var entity in world.entities) {
            if (entity is not Mob mob) continue;

            var d = (entity.position - playerPos).Length();
            if (d > renderRange) {
                continue;
            }

            // skip if no path
            if (mob.path == null || mob.path.isEmpty()) {
                continue;
            }

            var path = mob.path;
            var nodes = path.nodes;

            // draw lines between path nodes
            for (int i = 0; i < nodes.Count - 1; i++) {
                var current = nodes[i];
                var next = nodes[i + 1];

                // the centre of the block
                var from = new Vector3D(current.x + 0.5, current.y + 0.5, current.z + 0.5);
                var to = new Vector3D(next.x + 0.5, next.y + 0.5, next.z + 0.5);

                // use different colours for completed vs remaining path
                var c = i < path.current ? Color.Gray : Color.Cyan;

                D.drawLine(from, to, c);
            }

            // draw current target node with a brighter colour
            if (!path.isFinished()) {
                var current = path.getCurrent();
                if (current != null) {
                    var pos = new Vector3D(current.x + 0.5, current.y + 0.5, current.z + 0.5);
                    const double o = 0.5;

                    // draw a small cube around the current target node
                    D.drawLine(new Vector3D(pos.X - o, pos.Y - o, pos.Z - o),
                        new Vector3D(pos.X + o, pos.Y - o, pos.Z - o), Color.Yellow);
                    D.drawLine(new Vector3D(pos.X + o, pos.Y - o, pos.Z - o),
                        new Vector3D(pos.X + o, pos.Y + o, pos.Z - o), Color.Yellow);
                    D.drawLine(new Vector3D(pos.X + o, pos.Y + o, pos.Z - o),
                        new Vector3D(pos.X - o, pos.Y + o, pos.Z - o), Color.Yellow);
                    D.drawLine(new Vector3D(pos.X - o, pos.Y + o, pos.Z - o),
                        new Vector3D(pos.X - o, pos.Y - o, pos.Z - o), Color.Yellow);

                    D.drawLine(new Vector3D(pos.X - o, pos.Y - o, pos.Z + o),
                        new Vector3D(pos.X + o, pos.Y - o, pos.Z + o), Color.Yellow);
                    D.drawLine(new Vector3D(pos.X + o, pos.Y - o, pos.Z + o),
                        new Vector3D(pos.X + o, pos.Y + o, pos.Z + o), Color.Yellow);
                    D.drawLine(new Vector3D(pos.X + o, pos.Y + o, pos.Z + o),
                        new Vector3D(pos.X - o, pos.Y + o, pos.Z + o), Color.Yellow);
                    D.drawLine(new Vector3D(pos.X - o, pos.Y + o, pos.Z + o),
                        new Vector3D(pos.X - o, pos.Y - o, pos.Z + o), Color.Yellow);

                    D.drawLine(new Vector3D(pos.X - o, pos.Y - o, pos.Z - o),
                        new Vector3D(pos.X - o, pos.Y - o, pos.Z + o), Color.Yellow);
                    D.drawLine(new Vector3D(pos.X + o, pos.Y - o, pos.Z - o),
                        new Vector3D(pos.X + o, pos.Y - o, pos.Z + o), Color.Yellow);
                    D.drawLine(new Vector3D(pos.X + o, pos.Y + o, pos.Z - o),
                        new Vector3D(pos.X + o, pos.Y + o, pos.Z + o), Color.Yellow);
                    D.drawLine(new Vector3D(pos.X - o, pos.Y + o, pos.Z - o),
                        new Vector3D(pos.X - o, pos.Y + o, pos.Z + o), Color.Yellow);
                }
            }
        }

        D.idc.end();
        mat.pop();
    }

    public override void postDraw() {
        if (!currentMenu.isModal() && currentMenu != INGAME_MENU) {
            INGAME_MENU.postDraw();
        }
        base.postDraw();
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
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void openSettings() {
        SETTINGS_SCREEN.prevScreen = GAME_SCREEN;
        Game.instance.switchToScreen(SETTINGS_SCREEN);
    }

    private void cycleBlockMetadata() {
        if (!Game.instance.targetedPos.HasValue) {
            return;
        }

        var pos = Game.instance.targetedPos.Value;
        var world = Game.world;
        var blockValue = world.getBlockRaw(pos.X, pos.Y, pos.Z);
        var blockID = blockValue.getID();
        var currentMeta = blockValue.getMetadata();

        var block = Block.get(blockID);
        if (blockID == 0 || block == null) {
            return;
        }

        var maxMeta = block.maxValidMetadata();
        if (maxMeta == 0) {
            return;
        }

        // cycle to next metadata value
        var newMeta = (byte)((currentMeta + 1) % (maxMeta + 1));
        var newBlockValue = blockValue.setMetadata(newMeta);

        world.setBlockMetadata(pos.X, pos.Y, pos.Z, newBlockValue);
    }
}

public class UpdateMemoryThread(GameScreen screen) {
    private GameScreen screen = screen;
    public volatile bool stopped;

    public volatile bool needTrim;

    public void run() {
        while (true) {
            if (stopped) {
                break;
            }

            updateMemoryMethod();

            // we're also responsible for periodically trimming SharedBlockVAO! yes this is fucked but shhhh
            if (needTrim || (SharedBlockVAO.lastTrim + 60000 < Game.permanentStopwatch.ElapsedMilliseconds && SharedBlockVAO.c > 512)) {
                // if 60s has passed AND we have pending ones, trim
                MemoryUtils.cleanGC();
                SharedBlockVAO.c = 0;
                SharedBlockVAO.lastTrim = Game.permanentStopwatch.ElapsedMilliseconds;
                needTrim = false;
            }

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