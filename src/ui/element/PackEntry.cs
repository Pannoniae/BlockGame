using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.render.texpack;
using BlockGame.ui.menu;
using Molten;

namespace BlockGame.ui.element;

/**
 * UI element for a texture pack entry in the pack list.
 */
public class PackEntry : GUIElement {
    public readonly TexturePack pack;
    public bool isSelected;

    BTexture2D? iconTexture;

    public PackEntry(Menu menu, string name, TexturePack pack) : base(menu, name) {
        this.pack = pack;
        iconTexture = pack.getIconTexture();
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
            Game.gui.drawBorderUI(GUIbounds.X, GUIbounds.Y, GUIbounds.Width, GUIbounds.Height, 2, new Color(100, 150, 255, 255));
        }

        const int iconSize = 16;
        const int padding = 4;

        // draw icon
        if (iconTexture != null) {
            var iconX = GUIbounds.X + padding;
            var iconY = GUIbounds.Y + (GUIbounds.Height - iconSize) / 2;
            Game.gui.drawUI(iconTexture,
                new Vector2(iconX, iconY),
                new Vector2(iconSize / (float)iconTexture.width),
                null,
                new Color(255, 255, 255, 255));
        }

        // pack name
        var textX = bounds.X + (iconTexture != null ? (iconSize + padding * 2) : padding) * GUI.guiScale;
        var textY = bounds.Y + 3 * GUI.guiScale;
        Game.gui.drawString(pack.name, new Vector2(textX, textY));

        // author/version
        if (pack.author != null || pack.version != null) {
            var subtitle = pack.author ?? "";
            if (pack.version != null) {
                if (subtitle.Length > 0) {
                    subtitle += " · ";
                }

                subtitle += "v" + pack.version;
            }
            var subtitleY = bounds.Y + 13 * GUI.guiScale;
            Game.gui.drawStringThin(subtitle, new Vector2(textX, subtitleY));
        }
    }
}
