using BlockGame.net.packet;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;
using LiteNetLib;
using Molten;

namespace BlockGame.net.srv;

/**
 * tracks which players are subscribed to a chunk and batches block changes
 */ 
public class ChunkTracker {
    public readonly ChunkCoord coord;
    public readonly GameServer server;

    /** players subscribed to this chunk (receive block updates) */ 
    public readonly HashSet<ServerConnection> subscribers = [];

    /** accumulated dirty blocks (local coords within chunk) */
    private readonly List<Vector3I> dirtyBlocks = [];

    /** bounding box of dirty region (for large updates) */
    private Vector3I dirtyMin;
    private Vector3I dirtyMax;
    private bool hasDirtyBounds = false;

    private const int MAX_INDIVIDUAL_CHANGES = 16;

    public ChunkTracker(ChunkCoord coord, GameServer server) {
        this.coord = coord;
        this.server = server;
    }

    /** mark a block as dirty (world coords) */
    public void markDirty(int x, int y, int z) {
        if (subscribers.Count == 0) {
            return; // no one subscribed, don't track
        }

        // convert to local chunk coords for dedup
        int localX = x & 15;
        int localZ = z & 15;
        var localPos = new Vector3I(localX, y, localZ);

        // if we're already in region mode, just expand bounds
        if (dirtyBlocks.Count >= MAX_INDIVIDUAL_CHANGES) {
            if (!hasDirtyBounds) {
                dirtyMin = new Vector3I(x, y, z);
                dirtyMax = new Vector3I(x, y, z);
                hasDirtyBounds = true;
            } else {
                dirtyMin = new Vector3I(
                    int.Min(dirtyMin.X, x),
                    int.Min(dirtyMin.Y, y),
                    int.Min(dirtyMin.Z, z)
                );
                dirtyMax = new Vector3I(
                    int.Max(dirtyMax.X, x),
                    int.Max(dirtyMax.Y, y),
                    int.Max(dirtyMax.Z, z)
                );
            }
            return;
        }

        // check if already in list (dedup)
        foreach (var existing in dirtyBlocks) {
            if (existing.X == localX && existing.Y == y && existing.Z == localZ) {
                return; // already tracked
            }
        }

        // add to list
        dirtyBlocks.Add(localPos);

        // if we just hit the limit, switch to region mode
        if (dirtyBlocks.Count >= MAX_INDIVIDUAL_CHANGES) {
            // calculate initial bounds from existing dirty blocks
            int wx = coord.x << 4;
            int wz = coord.z << 4;
            dirtyMin = new Vector3I(wx + dirtyBlocks[0].X, dirtyBlocks[0].Y, wz + dirtyBlocks[0].Z);
            dirtyMax = dirtyMin;

            foreach (var local in dirtyBlocks) {
                int worldX = wx + local.X;
                int worldZ = wz + local.Z;
                dirtyMin = new Vector3I(
                    int.Min(dirtyMin.X, worldX),
                    int.Min(dirtyMin.Y, local.Y),
                    int.Min(dirtyMin.Z, worldZ)
                );
                dirtyMax = new Vector3I(
                    int.Max(dirtyMax.X, worldX),
                    int.Max(dirtyMax.Y, local.Y),
                    int.Max(dirtyMax.Z, worldZ)
                );
            }
            hasDirtyBounds = true;
        }
    }

    /** flush accumulated changes to all subscribers */
    public void flush() {
        if (dirtyBlocks.Count == 0) {
            return; // nothing to send
        }

        if (subscribers.Count == 0) {
            // no subscribers, just clear
            dirtyBlocks.Clear();
            hasDirtyBounds = false;
            return;
        }

        int wx = coord.x << 4;
        int wz = coord.z << 4;

        if (dirtyBlocks.Count == 1) {
            // single block change - use individual packet
            var local = dirtyBlocks[0];
            int worldX = wx + local.X;
            int worldZ = wz + local.Z;

            var block = server.world.getBlockRaw(worldX, local.Y, worldZ);
            var packet = new BlockChangePacket {
                position = new Vector3I(worldX, local.Y, worldZ),
                blockID = block.getID(),
                metadata = block.getMetadata()
            };

            foreach (var conn in subscribers) {
                conn.send(packet, DeliveryMethod.ReliableOrdered);
            }
        }
        else if (dirtyBlocks.Count < MAX_INDIVIDUAL_CHANGES) {
            // 2-9 blocks - use multi-block-change packet
            var pos = new Vector3I[dirtyBlocks.Count];
            var blockIDs = new ushort[dirtyBlocks.Count];
            var metadata = new byte[dirtyBlocks.Count];

            for (int i = 0; i < dirtyBlocks.Count; i++) {
                var local = dirtyBlocks[i];
                int worldX = wx + local.X;
                int worldZ = wz + local.Z;

                pos[i] = new Vector3I(worldX, local.Y, worldZ);
                var block = server.world.getBlockRaw(worldX, local.Y, worldZ);
                blockIDs[i] = block.getID();
                metadata[i] = block.getMetadata();
            }

            var packet = new MultiBlockChangePacket {
                pos = pos,
                blockIDs = blockIDs,
                metadata = metadata
            };

            foreach (var conn in subscribers) {
                conn.send(packet, DeliveryMethod.ReliableOrdered);
            }
        }
        else {
            // 10+ blocks - resend entire chunk
            // this is more efficient than sending many individual packets
            // todo have a more efficient chunk resend packet? or a box chunksend packet? idk
            foreach (var conn in subscribers) {
                conn.sendChunk(coord);
            }
        }

        // clear dirty state
        dirtyBlocks.Clear();
        hasDirtyBounds = false;
    }

    /** add a player subscription to this chunk */
    public void addSubscriber(ServerConnection conn) {
        subscribers.Add(conn);
    }

    /** remove a player subscription from this chunk */
    public void removeSubscriber(ServerConnection conn) {
        subscribers.Remove(conn);
    }
}