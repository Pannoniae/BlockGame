using Molten;

namespace BlockGame.ui {
    public class PauseMenu : Menu {
        public PauseMenu() {
            var backToGame = new Button(this, "backToGame", false, "Back to the game");
            backToGame.setPosition(new Vector2I(0, 0));
            backToGame.centreContents();
            backToGame.clicked += _ => { Screen.GAME_SCREEN.backToGame(); };
            var settings = new Button(this, "settings", false, "Settings");
            settings.setPosition(new Vector2I(0, 24));
            settings.centreContents();
            settings.clicked += _ => { Screen.GAME_SCREEN.openSettings(); };
            var mainMenu = new Button(this, "mainMenu", false, "Quit to Main Menu");
            mainMenu.setPosition(new Vector2I(0, 48));
            mainMenu.centreContents();
            mainMenu.clicked += returnToMainMenu;
            addElement(backToGame);
            addElement(settings);
            addElement(mainMenu);
        }

        public static void returnToMainMenu(GUIElement guiElement) {
            // save world
            Screen.GAME_SCREEN.world.worldIO.save(Screen.GAME_SCREEN.world, Screen.GAME_SCREEN.world.name);

            Game.instance.executeOnMainThread(() => Game.instance.switchToScreen(Screen.MAIN_MENU_SCREEN));
        }

        public override void update(double dt) {
            base.update(dt);
            // update ingame too!
            if (!Screen.GAME_SCREEN.world.paused) {
                Screen.GAME_SCREEN.INGAME_MENU.update(dt);
            }
        }

        public override void draw() {
            base.draw();
            Screen.GAME_SCREEN.INGAME_MENU.draw();
        }

        public override void postDraw() {
            base.postDraw();
            Screen.GAME_SCREEN.INGAME_MENU.postDraw();
        }
    }
}