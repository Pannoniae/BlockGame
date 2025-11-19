namespace BlockGame.util;

public class Metrics {
    public int renderedVerts;
    public int renderedChunks;
    public int renderedSubChunks;
    public int chunksUpdated;

    // network stats (/sec)
    public long bytesSent;
    public long bytesReceived;
    public int packetsSent;
    public int packetsReceived;
    public int ping;

    // packet type tracking (received/sec)
    public readonly Dictionary<Type, int> packets = new();

    public void clear() {
        renderedVerts = 0;
        renderedChunks = 0;
        renderedSubChunks = 0;
        // don't clear chunksUpdated here - it's cleared per debug display frame
    }

    public void clearChunkUpdates() {
        chunksUpdated = 0;
    }

    public void clearNet() {
        bytesSent = 0;
        bytesReceived = 0;
        packetsSent = 0;
        packetsReceived = 0;
        packets.Clear();
    }
}