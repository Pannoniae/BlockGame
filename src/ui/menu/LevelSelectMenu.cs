using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world;
using Molten;
using Silk.NET.Input;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu;

public class LevelSelectMenu : ScrollableMenu {

    private bool load;
    private WorldEntry? selectedEntry;

    protected override int viewportY => 32;

    protected override int viewportMargin => 64; // top + bottom

    public LevelSelectMenu() {
        // create new world button (fixed at bottom)
        var createButton = new Button(this, "createWorld", true, "Create New World") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        createButton.setPosition(new Vector2I(0, -18));
        createButton.clicked += _ => {
            Game.instance.switchTo(CREATE_WORLD);
        };
        addElement(createButton);

        // back button (fixed at bottom)
        var backButton = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        backButton.setPosition(new Vector2I(2, -18));
        backButton.clicked += _ => { Game.instance.switchTo(MAIN_MENU); };
        addElement(backButton);

        // delete world button (fixed at bottom right)
        var deleteButton = new Button(this, "deleteWorld", false, "Delete World") {
            horizontalAnchor = HorizontalAnchor.RIGHT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        deleteButton.setPosition(new Vector2I(-16 - deleteButton.guiPosition.Width, -18));
        deleteButton.clicked += _ => deleteSelectedWorld();
        addElement(deleteButton);
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
        if (selectedEntry != null) {
            selectedEntry.isSelected = false;
        }

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
        var levelDir = "level";
        if (!Directory.Exists(levelDir)) {
            Directory.CreateDirectory(levelDir);
            return;
        }

        var worlds = new List<(string folderName, string displayName, int seed, long size, long lastPlayed)>();

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
                var size = getDirSize(dir);
                worlds.Add((folderName, displayName, seed, size, lastPlayed));
            }
            catch {
                // skip corrupted worlds
            }
        }

        // sort by lastPlayed descending
        worlds.Sort((a, b) => b.lastPlayed.CompareTo(a.lastPlayed));

        // create WorldEntry elements
        var y = 32;
        foreach (var (folderName, displayName, seed, size, lastPlayed) in worlds) {
            var entry = new WorldEntry(this, $"world_{folderName}", folderName, displayName, seed, size, lastPlayed);
            entry.guiPosition = new Rectangle(0, y, 400, 32);
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