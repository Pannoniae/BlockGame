using System.Drawing;

namespace BlockGame.ui;

public class LoadingMenu : Menu {

    public double counter;
    /// <summary>
    /// 0 - 3
    /// </summary>
    public int dots = 2;

    private Text text;
    private World world;

    public LoadingMenu() {
        text = new Text(this, "loadingText", "Loading world...");
        text.setPosition(new Rectangle(0, 0, 160, 40));
        text.centreContents();
        addElement(text);

        counter = 0;
    }
    
    public void load(World world) {
        this.world = world;
        Game.renderer.setWorld(world);
        Game.world.init();
    }

    public override void render(double dt, double interp) {
        base.render(dt, interp);
        counter += dt;
        if (counter > 0.5) {
            dots++;
            dots = dots % 3;
            counter = 0;
            text.text = "Loading fonts" + new string('.', dots + 1);
        }
        
        Game.instance.switchToScreen(Screen.GAME_SCREEN);
        world.loadAroundPlayer();
        Game.instance.lockMouse();
        
    }

    public void sleep() {
        Task.Delay(2000).Wait();
    }
}