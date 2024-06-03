namespace BlockGame;

public class Metrics {
    public int renderedVerts;
    public int renderedChunks;

    public void clear() {
        renderedVerts = 0;
        renderedChunks = 0;
    }
}