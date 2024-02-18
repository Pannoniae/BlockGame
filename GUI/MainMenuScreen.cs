using System.Drawing;
using TrippyGL;

namespace BlockGame;

public class MainMenuScreen : Screen {
    public MainMenuScreen(GUI gui, GraphicsDevice GD, TextureBatch tb) : base(gui, GD, tb) {
        var button = new Button(this, new Rectangle(gui.centreX, gui.centreY, 160, 40));
        button.clicked += () => {
            gui.screen = Screens.GAME_SCREEN;
            Game.instance.world = new World();
        };
        elements.Add(button);
    }
};