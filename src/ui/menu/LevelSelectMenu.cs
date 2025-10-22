using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.util.xNBT;
using BlockGame.world;
using Molten;
using Silk.NET.Input;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu;

public class LevelSelectMenu : ScrollableMenu {

    private bool load;
    private WorldEntry? selectedEntry;
    private Button createButton = null!;

    protected override int viewportY => 32;

    protected override int viewportMargin => 64; // top + bottom

    public LevelSelectMenu() {

        var backButton = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        backButton.setPosition(new Vector2I(8, -18));
        backButton.clicked += _ => { Game.instance.switchTo(MAIN_MENU); };
        addElement(backButton);

        var deleteButton = new Button(this, "deleteWorld", false, "Delete World") {
            horizontalAnchor = HorizontalAnchor.RIGHT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        deleteButton.setPosition(new Vector2I(-8 - deleteButton.guiPosition.Width, -18));
        deleteButton.clicked += _ => deleteSelectedWorld();
        addElement(deleteButton);

        // the order is important. no, I won't tell you why.
        createButton = new Button(this, "createWorld", true, "Create New World") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        createButton.setPosition(new Vector2I(0, -18));
        createButton.clicked += _ => {
            Game.instance.switchTo(CREATE_WORLD);
        };
        addElement(createButton);
    }

    private void loadWorld(GUIElement element) {
        if (load) return;
        load = true;

        var worldEntry = (WorldEntry)element;
        var world = WorldIO.load(worldEntry.folderName);
        Game.setWorld(world);
        Game.instance.switchTo(LOADING);
        LOADING.load(world, true);
    }

    private void deleteSelectedWorld() {
        if (selectedEntry == null) return;

        var worldEntry = selectedEntry;
        Game.instance.switchTo(CONFIRM_DIALOG);
        CONFIRM_DIALOG.show(
            $"Delete '{worldEntry.displayName}'?",
            () => {
                WorldIO.deleteLevel(worldEntry.folderName);
                Game.instance.switchTo(LEVEL_SELECT);
                refreshWorldList();
            },
            () => {
                Game.instance.switchTo(LEVEL_SELECT);
            }
        );
    }

    private void selectWorld(GUIElement element) {
        var entry = (WorldEntry)element;

        // deselect previous
        selectedEntry?.isSelected = false;

        selectedEntry = entry;
    }

    // when activated, refresh world list
    public override void activate() {
        load = false;
        refreshWorldList();
    }

    private void refreshWorldList() {
        // clear selection
        selectedEntry = null;

        // remove old world entries (keep fixed buttons)
        var toRemove = elements.Values.Where(e => e is WorldEntry).ToList();
        foreach (var e in toRemove) {
            removeScrollable(e);
        }

        // scan level directory for worlds
        const string levelDir = "level";
        if (!Directory.Exists(levelDir)) {
            Directory.CreateDirectory(levelDir);
            return;
        }

        var worlds = new List<WorldEntry>();

        // enumerate directories
        foreach (var dir in Directory.GetDirectories(levelDir)) {
            var folderName = Path.GetFileName(dir);
            var levelFile = $"{dir}/level.xnbt";
            if (!File.Exists(levelFile)) continue;

            try {
                var tag = NBT.readFile(levelFile);
                var displayName = tag.has("displayName") ? tag.getString("displayName") : folderName;
                var seed = tag.getInt("seed");
                var lastPlayed = tag.has("lastPlayed") ? tag.getLong("lastPlayed") : 0;
                var generatorName = tag.has("generator") ? tag.getString("generator") : "perlin";
                var size = getDirSize(dir);
                var entry = new WorldEntry(this, $"world_{folderName}", folderName, displayName, seed, size, lastPlayed, generatorName);
                worlds.Add(entry);
            }
            catch {
                // skip corrupted worlds
                Log.warn($"Failed to load world info for '{folderName}', skipping");
            }
        }

        // sort by lastPlayed descending
        worlds.Sort((a, b) => b.lastPlayed.CompareTo(a.lastPlayed));

        // add sorted entries to UI
        var y = 32;
        foreach (var entry in worlds) {
            // responsible width: 80% of screen, capped at 500px, min 200px
            var entryWidth = Math.Clamp((int)(Game.gui.uiWidth * 0.8f), 200, 500);
            entry.guiPosition = new Rectangle(0, y, entryWidth, 32);
            entry.setPosition(entry.guiPosition);
            entry.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
            entry.verticalAnchor = VerticalAnchor.TOP;

            // single-click selects
            entry.selected += selectWorld;
            // double-click loads
            entry.clicked += loadWorld;

            addScrollable(entry);
            y += 36;
        }
    }

    public override void deactivate() {
        base.deactivate();
        load = false;
    }

    public override void resize(Vector2I newSize) {
        base.resize(newSize);

        // update world entry widths
        var entryWidth = Math.Clamp((int)(Game.gui.uiWidth * 0.8f), 200, 500);
        foreach (var entry in elements.Values) {
            if (entry is WorldEntry) {
                entry.guiPosition = new Rectangle(entry.guiPosition.X, entry.guiPosition.Y, entryWidth,
                    entry.guiPosition.Height);
                entry.setPosition(entry.guiPosition);
            }
        }

        // toggle create button width based on available space
        var wide = Game.gui.uiWidth >= 480;
        if (createButton.wide != wide) {
            createButton.wide = wide;
            createButton.guiPosition.Width = wide ? 192 : 128;
            createButton.setPosition(createButton.guiPosition);
        }
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }

    // esc handling
    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);
        if (key == Key.Escape) {
            Game.instance.switchTo(MAIN_MENU);
        }
    }

    private static long getDirSize(string folderPath) {
        var di = new DirectoryInfo(folderPath);
        return di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
    }
}