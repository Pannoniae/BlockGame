using BlockGame.ui.element;
using Molten;

namespace BlockGame.ui.menu {
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
            main.Game.instance.executeOnMainThread(() => {
                
                // save world
                main.Game.world.worldIO.save(main.Game.world, main.Game.world.name);
                
                // dispose world
                main.Game.setWorld(null);
                main.Game.instance.switchToScreen(Screen.MAIN_MENU_SCREEN);
            });
        }

        public override void update(double dt) {
            base.update(dt);
            // update ingame too!
            if (!main.Game.world.paused) {
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