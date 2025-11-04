using System.Numerics;
using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.util;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.element;

public class WorldEntry : GUIElement {
    public string folderName;
    public string displayName;
    public int seed;
    public long size;
    public long lastPlayed;
    public string generatorName;
    public bool isSelected;

    public event Action<GUIElement>? selected;

    private long lastClickTime = 0;
    private const long DOUBLE_CLICK_MS = 500;

    public static readonly Vector2 textOffset = new(5, 4);

    public WorldEntry(Menu menu, string name, string folderName, string displayName, int seed, long size, long lastPlayed, string generatorName) : base(menu, name) {
        this.folderName = folderName;
        this.displayName = displayName;
        this.seed = seed;
        this.size = size;
        this.lastPlayed = lastPlayed;
        this.generatorName = generatorName;
    }

    public override void draw() {
        // use button textures for normal/hovered/pressed states
        Rectangle src;

        if (pressed) {
            src = Button.pressedButton;
        } else if (hovered) {
            src = Button.hoveredButton;
        } else {
            src = Button.button;
        }

        Game.gui.draw9PatchUI(GUIbounds.X, GUIbounds.Y, GUIbounds.Width, GUIbounds.Height, src, 4, 4, 4, 4);

        // draw selection border
        if (isSelected) {
            Game.gui.drawBorderUI(GUIbounds.X, GUIbounds.Y, GUIbounds.Width, GUIbounds.Height, 1, new Color(100, 150, 255, 255));
        }

        // draw world name
        var namePos = new Vector2(bounds.X + textOffset.X * GUI.guiScale, bounds.Y + textOffset.Y * GUI.guiScale);

        var folderInfo = folderName != displayName ? folderName : "";

        Game.gui.drawString(folderName != displayName ? $"{displayName} ({folderInfo})" : displayName, namePos);

        // draw metadata (seed, size, last played)
        var sizeStr = $"{size / (double)Constants.MEGABYTES:0.##}MB";
        var timeAgo = getTimeAgo(lastPlayed);

        // show folder name if it differs from display name (helps distinguish duplicate "New World"s)

        var genName = Loc.get($"generator.{generatorName}");
        var metaStr = $"{genName} | seed: {seed} | {sizeStr} | {timeAgo}";

        var metaPos = new Vector2(bounds.X + textOffset.X * GUI.guiScale, bounds.Y + (bounds.Height - 8 * GUI.guiScale) - (textOffset.Y + 1) * GUI.guiScale);
        Game.gui.drawStringThin(metaStr, metaPos);
    }

    public override void click(MouseButton button) {
        if (button != MouseButton.Left) {
            base.click(button);
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var timeSinceLastClick = now - lastClickTime;

        if (timeSinceLastClick < DOUBLE_CLICK_MS && isSelected) {
            // double-click on selected entry - load world
            doClick();
            lastClickTime = 0;
        } else {
            // single-click - select (only invoke if not already selected)
            if (!isSelected) {
                isSelected = true;
                selected?.Invoke(this);
            }
            lastClickTime = now;
        }
    }

    private static string getTimeAgo(long unixMillis) {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var diff = now - unixMillis;
        var seconds = diff / 1000;
        var minutes = seconds / 60;
        var hours = minutes / 60;
        var days = hours / 24;

        if (days > 0) return $"{days}d ago";
        if (hours > 0) return $"{hours}h ago";
        if (minutes > 0) return $"{minutes}m ago";
        return $"{seconds}s ago";
    }
}