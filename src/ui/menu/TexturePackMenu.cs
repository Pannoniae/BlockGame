using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.render.texpack;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using FontStashSharp.RichText;
using Molten;
using Silk.NET.Input;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu;

/**
 * Menu for selecting texture packs
 */
public class TexturePackMenu : Menu {
    public readonly SettingsScreen parentScreen;

    public readonly List<PackEntry> packEntries = [];
    public PackEntry? selectedEntry;

    public BTexture2D? previewIcon;
    public string previewName = "";
    public string previewAuthor = "";
    public string previewVersion = "";
    public string previewDesc = "";

    public const int LIST_WIDTH = 135;
    public const int PREVIEW_WIDTH = 155;
    public const int GAP = 10; // gap between list and preview
    public const int MARGIN_Y = 20;

    // calculate centred pos
    public static int listx() {
        const int totalWidth = LIST_WIDTH + GAP + PREVIEW_WIDTH;
        return (GUI.instance.uiWidth - totalWidth) / 2;
    }

    public static int prevx() {
        return listx() + LIST_WIDTH + GAP;
    }

    public TexturePackMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initialiseUI();
    }

    public void initialiseUI() {
        // back button
        var back = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.setPosition(new Vector2I(6, -18));
        back.clicked += _ => {
            deactivate();
            parentScreen.returnToPrevScreen();
        };
        addElement(back);

        // apply button
        var apply = new Button(this, "apply", false, "Apply") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        apply.setPosition(new Vector2I(0, -18));
        apply.clicked += _ => applySelectedPack();
        addElement(apply);

        // open folder button
        var openFolder = new Button(this, "openFolder", false, "Open Folder") {
            horizontalAnchor = HorizontalAnchor.RIGHT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        openFolder.setPosition(new Vector2I(-6 - openFolder.guiPosition.Width, -18));
        openFolder.clicked += _ => TexturePackManager.openPackFolder();
        addElement(openFolder);
    }

    public override void activate() {
        base.activate();
        refreshPackList();
    }

    public void refreshPackList() {
        // clear the old entries
        foreach (var entry in packEntries) {
            removeElement(entry.name);
        }

        packEntries.Clear();
        selectedEntry = null;

        // discover packs
        var packs = TexturePackManager.discoverPacks();

        // create entries
        int listX = listx();
        var y = MARGIN_Y;
        foreach (var pack in packs) {
            var entry = new PackEntry(this, $"pack_{pack.name}", pack) {
                guiPosition = new Rectangle(listX, y, LIST_WIDTH, 24),
                horizontalAnchor = HorizontalAnchor.LEFT,
                verticalAnchor = VerticalAnchor.TOP
            };
            entry.clicked += e => selectPack((PackEntry)e);
            entry.setPosition(entry.guiPosition);

            // check if this is the current pack
            if (pack.name == Settings.instance.texturePack) {
                entry.isSelected = true;
                selectedEntry = entry;
                updatePreview(entry);
            }

            packEntries.Add(entry);
            addElement(entry);
            y += 28; // 24px height + 4px margin
        }
    }

    public void selectPack(PackEntry entry) {
        // deselect previous
        selectedEntry?.isSelected = false;

        // select new
        selectedEntry = entry;
        entry.isSelected = true;

        // update preview
        updatePreview(entry);
    }

    public void updatePreview(PackEntry entry) {
        var pack = entry.pack;

        previewIcon = pack.getIconTexture();
        previewName = pack.name;
        previewAuthor = pack.author ?? "";
        previewVersion = pack.version ?? "";
        previewDesc = pack.description ?? "";
    }

    public void applySelectedPack() {
        if (selectedEntry == null) return;

        Settings.instance.texturePack = selectedEntry.pack.name;
        Settings.instance.save();

        // load the pack
        TexturePackManager.loadPack(selectedEntry.pack);
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();

        // draw preview panel if a pack is selected
        if (selectedEntry != null) {
            drawPreviewPanel();
        }
    }

    public void drawPreviewPanel() {
        int panelX = prevx();
        const int panelY = MARGIN_Y;
        const int panelW = PREVIEW_WIDTH;
        const int panelH = 220;

        //panelX *= GUI.guiScale;

        // panel background with border
        Game.gui.draw9PatchUI(panelX, panelY, panelW, panelH,
            Button.button, 4, 4, 4, 4);
        Game.gui.drawBorderUI(panelX, panelY, panelW, panelH, 2,
            new Color(56, 62, 69, 255));

        const int iconSize = 32;
        const int padding = 6;

        int contentX = panelX + padding;
        const int contentY = panelY + padding;

        // draw pack icon centered at top
        if (previewIcon != null) {
            float targetSize = iconSize;
            int iconX = panelX + (panelW - (int)targetSize) / 2;
            Game.gui.drawUI(previewIcon,
                new RectangleF(iconX, contentY, targetSize, targetSize),
                null,
                new Color(255, 255, 255, 255));
        }

        // pack name centered below icon
        const int titleY = contentY + (iconSize + 6);
        var nameSize = Game.gui.measureStringUI(previewName);
        int titleX = (int)(panelX + (panelW - nameSize.X) / 2);
        Game.gui.drawStringUI(previewName, new Vector2(titleX, titleY));

        // author/version subtitle centered
        var subtitle = previewAuthor;
        if (!string.IsNullOrEmpty(previewVersion)) {
            if (!string.IsNullOrEmpty(subtitle)) {
                subtitle += " · ";
            }

            subtitle += "v" + previewVersion;
        }

        if (!string.IsNullOrEmpty(subtitle)) {
            const int subtitleY = titleY + 12;
            var subSize = Game.gui.measureStringUIThin(subtitle);
            int subX = panelX + (panelW - (int)subSize.X) / 2;
            Game.gui.drawStringUIThin(subtitle, new Vector2(subX, subtitleY));
        }

        // description box
        const int descY = contentY + (iconSize + 32);
        const int descW = (PREVIEW_WIDTH - padding * 2);
        const int descH = 140;

        // description box background (darker inset)
        Game.gui.draw9PatchUI(contentX, descY, descW, descH,
            Button.button, 3, 3, 8, 8);
        Game.gui.drawBorderUI(contentX, descY, descW, descH, 2,
            new Color(56, 62, 69, 255));

        // description text
        if (!string.IsNullOrEmpty(previewDesc)) {
            int textX = contentX + 4;
            const int textY = descY + 4;

            // todo idk why we have to make it wider than the inner box to make the wrapping work!! investigate :D
            //  maybe we have the scale mixed up somewhere between ui and screen..
            var l = new RichTextLayout {
                //Width = (descW - 8) * GUI.TEXTSCALE,
                //Height = (descH - 8) * GUI.TEXTSCALE,
                Width = (PREVIEW_WIDTH) * GUI.TEXTSCALE,
                Height = (PREVIEW_WIDTH) * GUI.TEXTSCALE,
                Font = Game.gui.guiFontThin,
                Text = previewDesc
            };
            Game.gui.drawRStringUI(l, new Vector2(textX, textY), TextHorizontalAlignment.Left);
        }
    }

    public override void deactivate() {
        base.deactivate();
        ui.Settings.instance.save();
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            deactivate();
            parentScreen.returnToPrevScreen();
        }
    }
}