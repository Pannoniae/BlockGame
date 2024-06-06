using System.Drawing;

namespace BlockGame.GUI;

public class LoadingMenu : Menu {

    public double counter;
    /// <summary>
    /// 0 - 3
    /// </summary>
    public int dots = 2;

    private Text text;

    public override void activate() {
        text = new Text(this, "loadingText", "Loading fonts...");
        text.setPosition(new Rectangle(0, 0, 160, 40));
        text.centreContents();
        addElement(text);

        counter = 0;
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
    }

    public void sleep() {
        Task.Delay(2000).Wait();
    }
}