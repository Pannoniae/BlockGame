using BlockGame.net.packet;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using LiteNetLib;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.net.srv;

public class ServerConnection {
    public readonly NetPeer peer;
    public string username;
    public bool authenticated = false;
    public int entityID;
    public int ping;

    public ServerPacketHandler handler;

    /** player entity in world (server ticks this) */
    public ServerPlayer player;

    /** chunks this client has loaded */
    public readonly HashSet<ChunkCoord> loadedChunks = [];
    public int renderDistance = 8;

    // block breaking state
    public Vector3I? breakingBlock;
    public double breakProgress;

    // network stats
    public readonly Metrics metrics = new();

    public ServerConnection(NetPeer peer) {
        handler = new ServerPacketHandler(this);
        this.peer = peer;
    }

    public void send<T>(T packet, DeliveryMethod method) where T : Packet {
        var buf = PacketWriter.get();

        // write packet ID first
        int packetID = PacketRegistry.getID(packet.GetType());
        buf.writeInt(packetID);

        // write packet data
        packet.write(buf);

        // send to peer with packet's channel
        var bytes = PacketWriter.getBytesUnsafe();
        peer.Send(bytes, packet.channel, method);

        // track metrics
        metrics.bytesSent += bytes.Length;
        metrics.packetsSent++;
    }
    public void disconnect(string reason) {
        send(new DisconnectPacket { reason = reason }, DeliveryMethod.ReliableOrdered);
        peer.Disconnect();
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

    public void updateLoadedChunks() {
        if (player == null) {
            return;
        }

        // calculate which chunks should be loaded based on player position
        var playerChunk = new ChunkCoord(
            (int)player.position.X >> 4,
            (int)player.position.Z >> 4
        );

        var shouldLoad = new HashSet<ChunkCoord>();

        // gather chunks in render distance
        for (int dx = -renderDistance; dx <= renderDistance; dx++) {
            for (int dz = -renderDistance; dz <= renderDistance; dz++) {
                var coord = new ChunkCoord(playerChunk.x + dx, playerChunk.z + dz);

                // circle
                if (coord.distanceSq(playerChunk) <= renderDistance * renderDistance) {
                    shouldLoad.Add(coord);
                }
            }
        }

        //Console.Out.WriteLine($"[{username}] Player at {player.position}, chunk {playerChunk}, shouldLoad={shouldLoad.Count}, loadedChunks={loadedChunks.Count}");

        // unload chunks no longer in range
        var toUnload = new List<ChunkCoord>();
        foreach (var coord in loadedChunks) {
            if (!shouldLoad.Contains(coord)) {
                toUnload.Add(coord);
            }
        }
        foreach (var coord in toUnload) {
            unloadChunk(coord);
        }

        // load new chunks
        int sent = 0, failed = 0;
        foreach (var coord in shouldLoad) {
            if (!loadedChunks.Contains(coord)) {
                // only mark as loaded if send succeeded
                if (sendChunk(coord)) {
                    loadedChunks.Add(coord);
                    sent++;
                } else {
                    failed++;
                }
                // if chunk not ready yet, it'll retry next tick
            }
        }
        if (sent > 0 || failed > 0) {
            Console.Out.WriteLine($"[{username}] Sent {sent} chunks, failed {failed}");
        }
    }

    // determine if this client should receive updates for given position/entity
    public bool isInRange(Vector3D pos) {
        if (player == null) return false;
        double distSq = Vector3D.DistanceSquared(player.position, pos);
        double maxDist = renderDistance * 16.0;
        return distSq <= maxDist * maxDist;
    }

    public bool isInRange(ChunkCoord coord) {
        return loadedChunks.Contains(coord);
    }
}