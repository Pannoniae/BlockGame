namespace BlockGame.ui;

public class LevelSelectButton : Button {
    public int levelIndex;

    public LevelSelectButton(Menu menu, string name, int index, string? text = default) : base(menu, name, true, text) {
        this.levelIndex = index;
    }
}