using BlockGame.util;
using Molten;

namespace BlockGame.ui;

public class LevelSelectMenu : Menu {

    public const int NUM_LEVELS = 5;

    public bool[] worldExists = new bool[NUM_LEVELS];

    public LevelSelectMenu() {
        // create level buttons
        for (int i = 0; i < NUM_LEVELS; i++) {
            var levelIndex = i + 1;
            var levelButton = new LevelSelectButton(this, $"level{levelIndex}", levelIndex, $"Level {levelIndex}");
            levelButton.setPosition(new Vector2I(0, 32 + i * 24));
            levelButton.topCentre();
            levelButton.clicked += loadLevel;
            addElement(levelButton);
        }

        // delete levels
        var deleteButton = new Button(this, $"deleteButton", false, "Delete levels") {
            horizontalAnchor = HorizontalAnchor.RIGHT,
            verticalAnchor = VerticalAnchor.TOP
        };
        deleteButton.setPosition(new Vector2I(0, 32 + NUM_LEVELS * 24 + 16));
        deleteButton.topCentre();
        deleteButton.clicked += _ => {
            for (int i = 0; i < NUM_LEVELS; i++) {
                var levelIndex = i + 1;
                if (worldExists[i]) {
                    worldExists[i] = false;
                    elements[$"level{levelIndex}"].text = "-empty-";
                    WorldIO.deleteLevel($"level{levelIndex}");

                    // refresh elements
                    activate();
                }
            }
        };
        addElement(deleteButton);

        var backButton = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        backButton.setPosition(new Vector2I(2, -18));
        backButton.clicked += _ => { Game.instance.switchTo(MAIN_MENU); };

        addElement(backButton);
    }

    private void loadLevel(GUIElement element) {
        var levelSelect = (LevelSelectButton)element;

        // if exists, load
        // it doesn't, create
        World world;
        if (worldExists[levelSelect.levelIndex - 1]) {
            world = WorldIO.load($"level{levelSelect.levelIndex}");
        }
        else {
            world = new World($"level{levelSelect.levelIndex}", Random.Shared.Next(int.MaxValue));
        }
        Screen.GAME_SCREEN.setWorld(world);
        Game.instance.switchToScreen(Screen.GAME_SCREEN);
        world.loadAroundPlayer();
        Game.instance.lockMouse();
    }

    // when activated, refresh the button states/texts
    public override void activate() {
        for (int i = 0; i < NUM_LEVELS; i++) {
            var levelIndex = i + 1;
            worldExists[i] = WorldIO.worldExists($"level{levelIndex}");
            var levelButton = (Button)getElement($"level{levelIndex}");
            // get the size of the level
            if (worldExists[i]) {
                var levelSize = getDirSize($"level/level{levelIndex}");
                levelButton.text = $"Level {levelIndex} ({levelSize / (double)Constants.MEGABYTES:0.###}MB)\n";
            }
            else {
                levelButton.text = "-empty-";
            }

        }
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }

    private static long getDirSize(string folderPath) {
        DirectoryInfo di = new DirectoryInfo(folderPath);
        return di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
    }
}