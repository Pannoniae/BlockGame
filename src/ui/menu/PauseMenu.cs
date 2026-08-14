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
            backToGame.setPosition(new Vector2I(0, 0));
            backToGame.centreContents();
            backToGame.clicked += _ => { Screen.GAME_SCREEN.backToGame(); };

            var settings = new Button(this, "settings", false, "Settings");
            settings.setPosition(new Vector2I(0, 24));
            settings.centreContents();
            settings.clicked += _ => { Screen.GAME_SCREEN.openSettings(); };

            var openToLAN = new Button(this, "openToLAN", false, "Open to LAN");
            openToLAN.setPosition(new Vector2I(0, 48));
            openToLAN.centreContents();
            openToLAN.clicked += openToLANClicked;

            var mainMenu = new Button(this, "mainMenu", false, "Quit to Main Menu");
            mainMenu.setPosition(new Vector2I(0, 72));
            mainMenu.centreContents();
            mainMenu.clicked += returnToMainMenu;

            addElement(backToGame);
            addElement(settings);
            addElement(openToLAN);
            addElement(mainMenu);
        }

        public override void activate() {
            base.activate();
            // show/hide disconnect button based on connection status
            if (hasElement("disconnect")) {
                getElement("disconnect").active = Game.client?.connected ?? false;
            }

            updateLANButton();
        }

        private void updateLANButton() {
            var btn = getElement("openToLAN");
            btn.active = Game.server != null;
        }

        private void openToLANClicked(GUIElement e) {
            var sv = Game.server;
            if (sv == null) {
                return;
            }

            if (sv.isOpenToLAN) {
                Screen.GAME_SCREEN.CHAT.addMessage($"&eAlready open on port &f{sv.lanPort}");
                Screen.GAME_SCREEN.backToGame();
                return;
            }

            var port = sv.openToLAN();
            Screen.GAME_SCREEN.CHAT.addMessage(port > 0
                ? $"&aWorld opened to LAN on port &f{port}"
                : "&cCouldn't open the world to LAN");

            // back to the game so the message is actually visible
            Screen.GAME_SCREEN.backToGame();
        }

        public static void returnToMainMenu(GUIElement guiElement) {
            Screen.GAME_SCREEN.trim(true);

            if (Game.server != null) {
                // integrated singleplayer: disconnect, stop the server, wait for the save
                Game.instance.executeOnMainThread(Game.quitIntegratedToMenu);
            }
            else if (Net.mode.isMPC()) {
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