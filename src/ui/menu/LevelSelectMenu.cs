using BlockGame.util;
using Silk.NET.Maths;

namespace BlockGame.ui;

public class LevelSelectMenu : Menu {
    public LevelSelectMenu() {
        var newLevelButton = new Button(this, "newLevel", true, "New Level");
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
            var world =  new World(seed);
            Screen.GAME_SCREEN.setWorld(world);
            world.loadAroundPlayer();
            Game.instance.switchToScreen(Screen.GAME_SCREEN);
            Game.instance.lockMouse();
        };
        var loadLevelButton = new Button(this, "loadLevel", true, "Load Level");
        loadLevelButton.setPosition(new Vector2D<int>(0, -32));
        loadLevelButton.centreContents();
        loadLevelButton.clicked += () => {
            if (!Directory.Exists("level") || !WorldIO.worldExists("level")) {
                return;
            }
            var world = WorldIO.load("level");
            Screen.GAME_SCREEN.setWorld(world);
            world.loadAroundPlayer();
            Game.instance.switchToScreen(Screen.GAME_SCREEN);
            Game.instance.lockMouse();
        };

        var backButton = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        backButton.setPosition(new Vector2D<int>(2, -18));
        backButton.clicked += () => {
            Game.instance.switchTo(MAIN_MENU);
        };

        addElement(newLevelButton);
        addElement(loadLevelButton);
        addElement(backButton);

    }

    public override void draw() {
        Game.gui.drawBG(Blocks.DIRT, 16);
        base.draw();
    }
}