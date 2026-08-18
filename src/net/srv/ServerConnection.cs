using BlockGame.net.packet;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using LiteNetLib;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.net.srv;

public class ServerConnection {
    public readonly NetPeer? peer;
    public string username;
    public bool authenticated = false;

    /** are we the local player of an integrated server? false on ded */
    public bool isHost;
    public int entityID;
    public int ping;

    /** player skin data (PNG bytes, empty = default) */
    public byte[] skinData = [];

    public ServerPacketHandler handler;

    /** player entity in world (server ticks this) */
    public ServerPlayer player;

    /** chunks this client has loaded */
    public readonly HashSet<ChunkCoord> loadedChunks = [];
    public int renderDistance = 8;

    // block breaking state
    public Vector3I? breakingBlock;
    public double breakProgress;
    public int lastProgressBroadcastTick;

    // inventory sync state
    public bool outOfSync = false;

    // network stats
    public readonly Metrics metrics = new();

    public ServerConnection(NetPeer? peer) {
        handler = new ServerPacketHandler(this);
        this.peer = peer;
    }

    public virtual void send<T>(T packet, DeliveryMethod method) where T : Packet {
        var bytes = serialise(packet);
        peer!.Send(bytes, packet.channel, method);
    }

    protected Span<byte> serialise<T>(T packet) where T : Packet {
        var buf = PacketWriter.get();
        buf.writeInt(PacketRegistry.getID(packet.GetType()));
        packet.write(buf);

        var bytes = PacketWriter.getBytesUnsafe();

        metrics.bytesSent += bytes.Length;
        metrics.packetsSent++;

        return bytes;
    }

    public virtual void disconnect(string reason) {
        send(new DisconnectPacket { reason = reason }, DeliveryMethod.ReliableOrdered);
        peer!.Disconnect();
    }

    // chunk loading
    public bool sendChunk(ChunkCoord coord) {
        var succ = GameServer.instance.world.getChunkMaybe(coord, out var chunk);
        if (!succ || chunk == null || chunk.status < ChunkStatus.LIGHTED) {
            //Console.Out.WriteLine("not ready!");
            return false; // chunk not ready yet
        }

        var nonEmptySubs = new List<ChunkDataPacket.SubChunkData>();
        for (int y = 0; y < Chunk.CHUNKHEIGHT; y++) {
            var paletteData = chunk.blocks[y];
            nonEmptySubs.Add(paletteData.write((byte)y));
        }

        var packet = new ChunkDataPacket {
            coord = coord,
            subChunks = nonEmptySubs.ToArray()
        };

        send(packet, DeliveryMethod.ReliableOrdered);

        // sync all block entities in this chunk
        foreach (var (pos, be) in chunk.blockEntities) {
            var nbt = new NBTCompound();
            be.write(nbt);
            var nbtBytes = NBT.write(nbt);

            send(new UpdateBlockEntityPacket {
                position = be.pos,
                type = 0, // unused for now
                nbt = nbtBytes
            }, DeliveryMethod.ReliableOrdered);
        }

        // subscribe to chunk tracker for block update notifications
        var tracker = GameServer.instance.get(coord);
        tracker.addSubscriber(this);

        return true;
    }

    public void unloadChunk(ChunkCoord coord) {
        if (loadedChunks.Remove(coord)) {
            var packet = new UnloadChunkPacket { coord = coord };
            send(packet, DeliveryMethod.ReliableOrdered);

            // unsubscribe from chunk tracker
            long key = coord.toLong();
            if (GameServer.instance.chunkTrackers.TryGetValue(key, out var tracker)) {
                tracker.removeSubscriber(this);
            }
        }
    }


    private readonly List<ChunkCoord> pending = [];
    private readonly List<ChunkCoord> toUnload = [];

    private ChunkCoord? lastChunk;

    public void invalidateChunks() {
        lastChunk = null;
    }

    /**
     * Chunk packets are fat.
     */
    private const int MAX_SENDS_PER_TICK = 8;

    /** ...but the first load is behind a loading screen with nothing else to do, so get it over with */
    private const int INITIAL_SENDS_PER_TICK = 64;
    private const int INITIAL_THRESHOLD = 256;

    public void updateLoadedChunks() {
        if (player == null) {
            return;
        }
        var playerChunk = new ChunkCoord(
            (int)player.position.X >> 4,
            (int)player.position.Z >> 4
        );

        // only recompute the wanted set when we actually moved to another chunk
        if (lastChunk != playerChunk) {
            lastChunk = playerChunk;
            rebuildPending(playerChunk);
        }

        // I'm not 100% we're ordering these right, might be checkerboarding?
        var cap = loadedChunks.Count < INITIAL_THRESHOLD ? INITIAL_SENDS_PER_TICK : MAX_SENDS_PER_TICK;
        var sent = 0;
        var w = 0;
        for (var r = 0; r < pending.Count; r++) {
            var coord = pending[r];

            if (loadedChunks.Contains(coord)) {
                continue; // already got it
            }

            if (sent < cap && sendChunk(coord)) {
                loadedChunks.Add(coord);
                sent++;
                continue;
            }

            pending[w++] = coord;
        }
        pending.RemoveRange(w, pending.Count - w);
    }

    private void rebuildPending(ChunkCoord playerChunk) {
        var rdSq = renderDistance * renderDistance;

        // drop anything that fell out of range
        toUnload.Clear();
        foreach (var coord in loadedChunks) {
            if (coord.distanceSq(playerChunk) > rdSq) {
                toUnload.Add(coord);
            }
        }
        foreach (var coord in toUnload) {
            unloadChunk(coord);
        }

        pending.Clear();
        for (var dx = -renderDistance; dx <= renderDistance; dx++) {
            for (var dz = -renderDistance; dz <= renderDistance; dz++) {
                var coord = new ChunkCoord(playerChunk.x + dx, playerChunk.z + dz);

                // circle
                if (coord.distanceSq(playerChunk) <= rdSq && !loadedChunks.Contains(coord)) {
                    pending.Add(coord);
                }
            }
        }

        pending.Sort((a, b) => a.distanceSq(playerChunk).CompareTo(b.distanceSq(playerChunk)));
    }

    // determine if this client should receive updates for given position/entity
    public bool isInRange(Vector3D pos, double margin = 0) {
        if (player == null) {
            return false;
        }

        double distSq = Vector3D.DistanceSquared(player.position, pos);
        double maxDist = renderDistance * 16.0 + margin;
        return distSq <= maxDist * maxDist;
    }

    public bool isInRange(ChunkCoord coord) {
        return loadedChunks.Contains(coord);
    }
}