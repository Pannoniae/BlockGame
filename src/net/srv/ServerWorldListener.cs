using BlockGame.world;
using BlockGame.world.chunk;
using Molten;

namespace BlockGame.net.srv;

/** broadcasts block changes to clients */
public class ServerWorldListener : WorldListener {
    private readonly GameServer server;

    public ServerWorldListener(GameServer server) {
        this.server = server;
    }

    public void onWorldLoad() {
    }

    public void onWorldUnload() {
    }

    public void onWorldTick(float delta) {
    }

    public void onWorldRender(float delta) {
    }

    public void onChunkLoad(ChunkCoord coord) {
    }

    public void onChunkUnload(ChunkCoord coord) {
        // notify all connections to unload this chunk
        foreach (var conn in server.connections.Values) {
            conn.unloadChunk(coord);
        }
    }

    public void onDirtyChunk(SubChunkCoord coord) {
    }

    public void onDirtyChunksBatch(ReadOnlySpan<SubChunkCoord> coords) {
    }

    public void onDirtyArea(Vector3I min, Vector3I max) {
        // skip if nosend flag is set (worldgen, chunk loading, etc.)
        if (server.world.nosend) {
            return;
        }

        // mark all affected blocks as dirty in their respective chunk trackers
        // changes will be batched and flushed at end of tick
        for (int x = min.X; x <= max.X; x++) {
            for (int y = min.Y; y <= max.Y; y++) {
                for (int z = min.Z; z <= max.Z; z++) {
                    var chunkCoord = new ChunkCoord(x >> 4, z >> 4);
                    var tracker = server.get(chunkCoord);
                    tracker.markDirty(x, y, z);
                }
            }
        }
    }
}