using Silk.NET.Maths;
using TrippyGL;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class MainMenuScreen : Screen {
    public MainMenuScreen(GUI gui, GraphicsDevice GD, TextureBatch tb) : base(gui, GD, tb) {
        var button = new Button(this, new Rectangle(0, 0, 160, 40));
        button.clicked += () => {
            Console.Out.WriteLine("CLICKED");
            Game.instance.screen = GAME_SCREEN;
            GameScreen.world = new World();
            Game.instance.resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
            Game.instance.lockMouse();
        };
        elements.Add(button);
    }
};