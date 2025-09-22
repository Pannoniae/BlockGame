namespace BlockGame.util;

public interface ProgressUpdater {
    public void start(string stage);
    public void stage(string stage);
    public void update(float progress);
}
