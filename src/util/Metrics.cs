namespace BlockGame.util;

public class Metrics {
    public int renderedVerts;
    public int renderedChunks;
    public int renderedSubChunks;
    public int chunksUpdated;

    public void clear() {
        renderedVerts = 0;
        renderedChunks = 0;
        renderedSubChunks = 0;
        // don't clear chunksUpdated here - it's cleared per debug display frame
    }
    
    public void clearChunkUpdates() {
        chunksUpdated = 0;
    }
}