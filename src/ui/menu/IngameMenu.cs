using System.Numerics;
using BlockGame.GL.vertexformats;
using BlockGame.util;
using Cysharp.Text;
using FontStashSharp.RichText;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.Input;
using Silk.NET.OpenGL;


namespace BlockGame.ui;

public class IngameMenu : Menu, IDisposable {
    public RichTextLayout rendererText;

    // values for f3
    public long workingSet;
    public long GCMemory;

    private Utf16ValueStringBuilder debugStr;

    // for top right corner debug shit
    private Utf16ValueStringBuilder debugStrG;

    // Frametime graph data
    private const int FRAMETIME_HISTORY_SIZE = 400;
    private float[] frametimeHistory = new float[FRAMETIME_HISTORY_SIZE];
    private int frametimeHistoryIndex = 0;
    private float minFrametime = float.MaxValue;
    private float maxFrametime = float.MinValue;
    private const int GRAPH_HEIGHT = 80 * 4;
    private const int GRAPH_WIDTH = 200 * 4;
    private const int GRAPH_PADDING = 5;
    private bool frametimeGraphEnabled = true;

    public IngameMenu() {
        debugStr.Dispose();
        debugStr = ZString.CreateStringBuilder();
        debugStrG.Dispose();
        debugStrG = ZString.CreateStringBuilder();
        // then add the GUI
        var version = Text.createText(this, "version", new Vector2I(2, 2), Game.VERSION);
        version.shadowed = true;
        addElement(version);
        var hotbar = new Hotbar(this, "hotbar", new Vector2I(0, -20)) {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        addElement(hotbar);
        rendererText = new RichTextLayout {
            Font = Game.gui.guiFontThin,
            Text = "",
            Width = 150 * GUI.guiScale
        };

        // Initialize frametime history
        for (int i = 0; i < FRAMETIME_HISTORY_SIZE; i++) {
            frametimeHistory[i] = 0f;
        }
    }

    public override void render(double dt, double interp) {
        base.render(dt, interp);
        UpdateFrametimeHistory((float)Game.instance.ft * 1000f);
    }

    // No longer needed - we're using fixed thresholds

    private void DrawFrametimeGraph() {
        var screen = (GameScreen)this.screen;

        // Only draw when debug screen is enabled
        if (screen.debugScreen || screen.fpsOnly) {
            var gui = Game.gui;

            // Fixed size and position in bottom-left
            int graphX = GRAPH_PADDING;
            int graphY = Game.height - GRAPH_HEIGHT - GRAPH_PADDING;

            // Background rectangle with transparency
            gui.tb.Draw(gui.colourTexture,
                new System.Drawing.RectangleF(graphX, graphY, GRAPH_WIDTH, GRAPH_HEIGHT),
                new Color4b(0, 0, 0, 180));

            // Fixed 60 FPS threshold (16.67ms)
            const float MAX_FRAMETIME = 33.3f; // Cap display at 30 FPS for readability

            // Calculate y position of 60 FPS line (16.67ms)
            float sixtyFpsY = graphY + (16.67f / MAX_FRAMETIME) * GRAPH_HEIGHT;

            // Draw guide line
            gui.tb.Draw(gui.colourTexture,
                new System.Drawing.RectangleF(graphX, sixtyFpsY, GRAPH_WIDTH, 1),
                new Color4b(0, 255, 0, 150)); // 60 FPS line (green)

            // Draw FPS marker
            gui.drawStringThin("60 FPS",
                new Vector2(graphX + 5, sixtyFpsY - 12),
                new Color4b(0, 255, 0));

            // Draw vertical bars
            const float BAR_WIDTH = GRAPH_WIDTH / (float)FRAMETIME_HISTORY_SIZE;

            for (int i = 0; i < FRAMETIME_HISTORY_SIZE; i++) {
                int idx = (frametimeHistoryIndex + i) % FRAMETIME_HISTORY_SIZE;

                if (frametimeHistory[idx] <= 0)
                    continue;

                // Cap at MAX_FRAMETIME ms
                float frametime = frametimeHistory[idx];

                // Calculate positions
                float x = graphX + (i * GRAPH_WIDTH / (float)FRAMETIME_HISTORY_SIZE);

                // Calculate bar height based on frametime
                float barHeight = (frametime / MAX_FRAMETIME) * GRAPH_HEIGHT;
                float y = graphY + GRAPH_HEIGHT - barHeight;

                // Lerp between green and red based on frametime. Between 0 and 60FPS from green to yellow, between 60FPS and 30FPS from yellow to red
                // Determine color based on performance (lerp between values)
                Color4b barColor;
                const float SIXTY_FPS = 16.6f;
                const float THIRTY_FPS = 33.3f;

                if (frametime < SIXTY_FPS) {
                    float t = frametime / SIXTY_FPS; // 0 to 1
                    barColor = new Color4b(
                        (byte)(255 * t),
                        255,
                        0
                    );
                }
                else if (frametime < THIRTY_FPS) {
                    float t = (frametime - SIXTY_FPS) / (THIRTY_FPS - SIXTY_FPS); // 0 to 1
                    barColor = new Color4b(
                        255,
                        (byte)(255 * (1 - t)),
                        0
                    );
                }
                else {
                    barColor = new Color4b(255, 0, 0);
                }

                // Draw bar
                gui.tb.Draw(gui.colourTexture,
                    new System.Drawing.RectangleF(x, y, BAR_WIDTH, barHeight),
                    barColor);
            }
        }
    }

    private void UpdateFrametimeHistory(float frametime) {
        // Cap extremely high framerates to avoid visual glitches
        //frametime = Math.Max(frametime, 0.1f);

        // Store the value and increment the index
        frametimeHistory[frametimeHistoryIndex] = frametime;
        frametimeHistoryIndex = (frametimeHistoryIndex + 1) % FRAMETIME_HISTORY_SIZE;
    }

    public override void draw() {
        base.draw();
        var screen = (GameScreen)this.screen;
        if (screen.debugScreen && !screen.fpsOnly) {
            var ver = getElement("version");
            Game.gui.drawStringThin(debugStr.AsSpan(),
                new Vector2(ver.bounds.Left, ver.bounds.Bottom), Color4b.White);
            Game.gui.drawRString(rendererText,
                new Vector2(Game.width - 2, 2), TextHorizontalAlignment.Right, Color4b.White);
        }

        // Draw frametime graph if enabled
        if (frametimeGraphEnabled) {
            DrawFrametimeGraph();
        }

        // Draw block display
        var blockStr = Block.get(Game.world.player.hotbar.getSelected().block).name;
        Game.gui.drawStringCentredUI(blockStr, new Vector2(Game.gui.uiCentreX, Game.gui.uiHeight - 30),
            Color4b.White);
    }

    public void Dispose() {
        debugStr.Dispose();
        debugStrG.Dispose();
    }

    public void updateDebugTextMethod() {
        var screen = Screen.GAME_SCREEN;
        if (screen.debugScreen && !screen.fpsOnly) {
            var gui = Game.gui;
            var i = Game.instance;
            var p = Game.player;
            var c = p.camera;
            var m = Game.metrics;
            var loadedChunks = Game.world.chunks.Count;
            var pos = p.position.toBlockPos();
            // current block
            //var cb = world.getBlock(pos);
            var sl = Game.world.getSkyLight(pos.X, pos.Y, pos.Z);
            var bl = Game.world.getBlockLight(pos.X, pos.Y, pos.Z);
            Game.world.getChunkMaybe(World.getChunkPos(new Vector2I(pos.X, pos.Z)), out var chunk);

            bool inited;
            // if outside, don't bother
            if (pos.Y < 0 || pos.Y >= Chunk.CHUNKHEIGHT * Chunk.CHUNKSIZE) {
                inited = false;
            }
            else {
                inited = chunk?.blocks[pos.Y >> 4].inited ?? false;
            }


            debugStr.Clear();
            debugStrG.Clear();
            if (Game.devMode) {
                // calculate facing
                var facing = cameraFacing(c.forward);

                debugStr.AppendFormat("{0:0.000}, {1:0.000}, {2:0.000}\n", p.position.X, p.position.Y, p.position.Z);
                debugStr.AppendFormat("vx:{0:0.000}, vy:{1:0.000}, vz:{2:0.000}, vl:{3:0.000}\n", p.velocity.X,
                    p.velocity.Y, p.velocity.Z, p.velocity.Length());
                debugStr.AppendFormat("ax:{0:0.000}, ay:{1:0.000}, az:{2:0.000}\n", p.accel.X, p.accel.Y, p.accel.Z);
                debugStr.AppendFormat("cf:{0:0.000}, {1:0.000}, {2:0.000} {3}\n", c.forward.X, c.forward.Y, c.forward.Z,
                    facing);
                debugStr.AppendFormat("sl:{0}, bl:{1}, i:{2}\n", sl, bl, inited);
                debugStr.AppendFormat("{0}{1}\n", p.onGround ? 'g' : '-', p.jumping ? 'j' : '-');
                if (i.targetedPos.HasValue) {
                    debugStr.AppendFormat("{0}, {1}, {2} {3}, {4}, {5}\n", i.targetedPos.Value.X, i.targetedPos.Value.Y,
                        i.targetedPos.Value.Z, i.previousPos!.Value.X, i.previousPos.Value.Y, i.previousPos.Value.Z);
                }
                else
                    debugStr.Append("No target\n");
            }

            debugStr.AppendFormat("rC:{0} rSC:{1} rV:{2}k\n", m.renderedChunks, m.renderedSubChunks,
                m.renderedVerts / 1000);
            debugStr.AppendFormat("lC:{0} lCs:{1}\n", loadedChunks, loadedChunks * Chunk.CHUNKHEIGHT);

            debugStr.AppendFormat("FPS:{0} (ft:{1:0.##}ms)\n", i.fps, i.ft * 1000);

            // show FB info
            if (Settings.instance.framebufferEffects) {
                var fbw = Game.width * Settings.instance.ssaa;
                var fbh = Game.height * Settings.instance.ssaa;
                debugStr.AppendFormat("FB:{0}x{1} ({2})\n", fbw, fbh, Settings.instance.getAAText());
            }
            else {
                debugStr.AppendFormat("FB:{0}x{1} (0fx)\n", Game.width, Game.height);
            }

            if (Game.devMode) {
                debugStr.AppendFormat("Seed: {0}\n", Game.world.seed);
            }

            long vmem = MemoryUtils.getVRAMUsage(out var stat);
            debugStrG.AppendFormat("Renderer: {0}/{1}\n", Game.GL.GetStringS(StringName.Renderer),
                Game.GL.GetStringS(StringName.Vendor));
            debugStrG.AppendFormat("OpenGL version: {0}\n", Game.GL.GetStringS(StringName.Version));
            debugStrG.AppendFormat("Mem:{0:0.###}MB (proc:{1:0.###}MB)\nvmem: {2:0.###}MB ({3})",
                GCMemory / Constants.MEGABYTES,
                workingSet / Constants.MEGABYTES, vmem / Constants.MEGABYTES, stat);
            // calculate textwidth
            rendererText = new RichTextLayout {
                Font = gui.guiFontThin,
                Text = debugStrG.ToString(),
                Width = 150 * GUI.guiScale
            };
        }
    }

    public static string cameraFacing(Vector3D direction) {
        // Check for up/down first
        double verticalThreshold = Math.Cos(45);
        if (direction.Y > verticalThreshold) {
            return "Facing up";
        }

        if (direction.Y < -verticalThreshold) {
            return "Facing down";
        }

        // If not facing strongly up or down, determine horizontal direction
        double absX = Math.Abs(direction.X);
        double absZ = Math.Abs(direction.Z);

        if (absX > absZ) {
            return direction.X > 0 ? "Facing east" : "Facing west";
        }
        else {
            return direction.Z > 0 ? "Facing north" : "Facing south";
        }
    }
}