using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class LoadingScreen : Screen {

    public double counter;
    /// <summary>
    /// 0 - 3
    /// </summary>
    public int dots = 2;

    private Text text;

    public LoadingScreen() {
        text = new Text(this, new Rectangle(0, 0, 160, 40), "Loading fonts...");
        elements.Add(text);
    }

    public override void activate() {
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