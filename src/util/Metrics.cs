namespace BlockGame.util;

public class Metrics {
    public int renderedVerts;
    public int renderedChunks;
    public int renderedSubChunks;

    public void clear() {
        renderedVerts = 0;
        renderedChunks = 0;
        renderedSubChunks = 0;
    }
}