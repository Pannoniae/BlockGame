using BlockGame.util;
using Silk.NET.Maths;

namespace BlockGame.ui;

public class LevelSelectMenu : Menu {

    public const int NUM_LEVELS = 5;

    public bool[] worldExists = new bool[NUM_LEVELS];

    public LevelSelectMenu() {
        /*var newLevelButton = new Button(this, "newLevel", true, "New Level");
        newLevelButton.setPosition(new Vector2D<int>(0, -64));
        newLevelButton.centreContents();
        newLevelButton.clicked += () => {
            if (!Directory.Exists("level")) {
                Directory.CreateDirectory("level");
            }
            // if existing world, delete files from level directory
            else {
                foreach (var file in Directory.GetFiles("level")) {
                    File.Delete(file);
                }
            }
            var seed = Random.Shared.Next(int.MaxValue);
            var world =  new World("level1", seed);
            Screen.GAME_SCREEN.setWorld(world);
            world.loadAroundPlayer();
            Game.instance.switchToScreen(Screen.GAME_SCREEN);
            Game.instance.lockMouse();
        };
        var loadLevelButton = new Button(this, "loadLevel", true, "Load Level");
        loadLevelButton.setPosition(new Vector2D<int>(0, -32));
        loadLevelButton.centreContents();
        loadLevelButton.clicked += () => {
            if (!WorldIO.worldExists("level1")) {
                return;
            }
            var world = WorldIO.load("level");
            Screen.GAME_SCREEN.setWorld(world);
            world.loadAroundPlayer();
            Game.instance.switchToScreen(Screen.GAME_SCREEN);
            Game.instance.lockMouse();
        };*/


        // create level buttons
        for (int i = 0; i < NUM_LEVELS; i++) {
            var levelIndex = i + 1;
            var levelButton = new LevelSelectButton(this, $"level{levelIndex}", levelIndex, $"Level {levelIndex}");
            levelButton.setPosition(new Vector2D<int>(0, 32 + i * 24));
            levelButton.topCentre();
            levelButton.clicked += loadLevel;
            addElement(levelButton);
        }


        var backButton = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        backButton.setPosition(new Vector2D<int>(2, -18));
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
        Game.gui.drawBG(Blocks.DIRT, 16);
        base.draw();
    }

    private static long getDirSize(string folderPath) {
        DirectoryInfo di = new DirectoryInfo(folderPath);
        return di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
    }
}