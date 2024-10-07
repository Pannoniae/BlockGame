using System.Numerics;
using BlockGame.util;
using Cysharp.Text;
using FontStashSharp.RichText;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.OpenGL;
using TrippyGL;

namespace BlockGame.ui;

public class IngameMenu : Menu, IDisposable {

    public RichTextLayout rendererText;

    // values for f3
    private long workingSet;
    private long GCMemory;

    private Utf16ValueStringBuilder debugStr;
    // for top right corner debug shit
    private Utf16ValueStringBuilder debugStrG;

    public IngameMenu() {
        debugStr.Dispose();
        debugStr = ZString.CreateStringBuilder();
        debugStrG.Dispose();
        debugStrG = ZString.CreateStringBuilder();
        // then add the GUI
        var version = Text.createText(this, "version", new Vector2I(2, 2), "BlockGame v0.0.2");
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
    }

    public override void draw() {
        base.draw();
        var screen = (GameScreen)this.screen;
        if (screen.debugScreen) {
            var ver = getElement("version");
            Game.gui.drawStringThin(debugStr.AsSpan(),
                new Vector2(ver.bounds.Left, ver.bounds.Bottom), Color4b.White);
            Game.gui.drawRString(rendererText,
                new Vector2(Game.width - 2, 2), TextHorizontalAlignment.Right, Color4b.White);
        }
        // Draw block display
        var blockStr = Blocks.get(screen.world.player.hotbar.getSelected().block).name;
        Game.gui.drawStringCentredUI(blockStr, new Vector2(Game.gui.uiCentreX, Game.gui.uiHeight - 30),
            Color4b.White);
    }

    public void Dispose() {
        debugStr.Dispose();
        debugStrG.Dispose();
    }

    public void updateDebugTextMethod() {
        var screen = Screen.GAME_SCREEN;
        if (screen.debugScreen) {
            var gui = Game.gui;
            var i = Game.instance;
            var p = screen.world.player;
            var c = p.camera;
            var m = Game.metrics;
            var loadedChunks = screen.world.chunks.Count;
            var pos = p.position.toBlockPos();
            // current block
            //var cb = world.getBlock(pos);
            var sl = screen.world.getSkyLight(pos.X, pos.Y, pos.Z);
            var bl = screen.world.getBlockLight(pos.X, pos.Y, pos.Z);


            debugStr.Clear();
            debugStrG.Clear();
            if (Game.devMode) {
                // calculate facing
                var facing = cameraFacing(c.forward);

                debugStr.AppendFormat("{0:0.000}, {1:0.000}, {2:0.000}\n", p.position.X, p.position.Y, p.position.Z);
                debugStr.AppendFormat("vx:{0:0.000}, vy:{1:0.000}, vz:{2:0.000}, vl:{3:0.000}\n", p.velocity.X, p.velocity.Y, p.velocity.Z, p.velocity.Length());
                debugStr.AppendFormat("ax:{0:0.000}, ay:{1:0.000}, az:{2:0.000}\n", p.accel.X, p.accel.Y, p.accel.Z);
                debugStr.AppendFormat("cf:{0:0.000}, {1:0.000}, {2:0.000} {3}\n", c.forward.X, c.forward.Y, c.forward.Z, facing);
                debugStr.AppendFormat("sl:{0}, bl:{1}\n", sl, bl);
                debugStr.AppendFormat("{0}{1}\n", p.onGround ? 'g' : '-', p.jumping ? 'j' : '-');
                if (i.targetedPos.HasValue) {
                    debugStr.AppendFormat("{0}, {1}, {2} {3}, {4}, {5}\n", i.targetedPos.Value.X, i.targetedPos.Value.Y, i.targetedPos.Value.Z, i.previousPos!.Value.X, i.previousPos.Value.Y, i.previousPos.Value.Z);
                }
                else
                    debugStr.Append("No target\n");
            }

            debugStr.AppendFormat("rC:{0} rSC:{1} rV:{2}k\n", m.renderedChunks, m.renderedSubChunks, m.renderedVerts / 1000);
            debugStr.AppendFormat("lC:{0} lCs:{1}\n", loadedChunks, loadedChunks * Chunk.CHUNKHEIGHT);

            debugStr.AppendFormat("FPS:{0} (ft:{1:0.##}ms)\n", i.fps, i.ft * 1000);
            if (Game.devMode) {
                debugStr.AppendFormat("Seed: {0}\n", screen.world.seed);
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

    public void updateMemoryMethod() {
        Game.proc.Refresh();
        workingSet = Game.proc.WorkingSet64;
        GCMemory = GC.GetTotalMemory(false);
    }
}