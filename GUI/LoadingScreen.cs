using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class LoadingScreen : Screen {

    public LoadingScreen() {
        var text = new Text(this, new Rectangle(0, 0, 160, 40), "Loading fonts...");
        elements.Add(text);
    }

    public void sleep() {
        Task.Delay(2000).Wait();
    }
}