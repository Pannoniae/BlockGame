using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using Molten;
using Silk.NET.Input;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu {
    public class PauseMenu : Menu {
        public PauseMenu() {
            var backToGame = new Button(this, "backToGame", false, "Back to the game");
            backToGame.setPosition(new Vector2I(0, -24));
            backToGame.centreContents();
            backToGame.clicked += _ => { Screen.GAME_SCREEN.backToGame(); };

            var browseAddons = new Button(this, "browseAddons", false, "Browse Add-ons!");
            browseAddons.setPosition(new Vector2I(0, 0));
            browseAddons.centreContents();
            browseAddons.clicked += _ => {
                // just open the texture pack screen lol
                // switch to settings screen
                Game.instance.executeOnMainThread(() => {
                    Screen.GAME_SCREEN.openSettings();
                    // then open the texture pack screen
                    var packMenu = new TexturePackMenu(Screen.SETTINGS_SCREEN);
                    SettingsScreen.SETTINGS_MENU.push(packMenu);
                });
            };

            var settings = new Button(this, "settings", false, "Settings");
            settings.setPosition(new Vector2I(0, 24));
            settings.centreContents();
            settings.clicked += _ => { Screen.GAME_SCREEN.openSettings(); };

            var mainMenu = new Button(this, "mainMenu", false, "Quit to Main Menu");
            mainMenu.setPosition(new Vector2I(0, 48));
            mainMenu.centreContents();
            mainMenu.clicked += returnToMainMenu;

            addElement(backToGame);
            addElement(browseAddons);
            addElement(settings);
            addElement(mainMenu);
        }

        public override void activate() {
            base.activate();
            // show/hide disconnect button based on connection status
            if (hasElement("disconnect")) {
                getElement("disconnect").active = Game.client?.connected ?? false;
            }
        }

        public static void returnToMainMenu(GUIElement guiElement) {
            Screen.GAME_SCREEN.trim(true);

            if (Net.mode.isMPC()) {
                // multiplayer: disconnect and return to menu
                Game.instance.executeOnMainThread(Game.disconnectAndReturnToMenu);
            }
            else {
                // singleplayer: exit world (with save) and return to menu
                Game.instance.executeOnMainThread(() => {
                    Game.exitWorld(save: true);
                    Game.instance.switchToScreen(Screen.MAIN_MENU_SCREEN);
                });
            }
        }

        public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
            base.onKeyDown(keyboard, key, scancode);
            if (key == Key.Escape) {
                Game.instance.executeOnMainThread(() => { Screen.GAME_SCREEN.backToGame(); });
            }
        }

        public override void update(double dt) {
            base.update(dt);
            // don't update ingame menu when paused
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