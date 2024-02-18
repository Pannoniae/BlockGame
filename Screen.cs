using TrippyGL;

namespace BlockGame;

public class Screen {

    public GUI gui;
    public GraphicsDevice GD;
    public TextureBatch tb;

    public Screen(GUI gui, GraphicsDevice GD, TextureBatch tb) {
        this.gui = gui;
        this.GD = GD;
        this.tb = tb;
    }

    public virtual void draw() {

    }

    public virtual void imGuiDraw() {

    }
}


public static class Screens {
    public static Screen MAIN_MENU = null!;
    public static Screen GAME_SCREEN = null!;

    public static void initScreens(GUI gui) {
        MAIN_MENU = new MainMenuScreen(gui, gui.GD, gui.tb);
        GAME_SCREEN = new GameScreen(gui, gui.GD, gui.tb);
    }
}