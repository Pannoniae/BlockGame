using System.Collections.Concurrent;
using System.Net.Sockets;
using BlockGame.main;
using BlockGame.net.packet;
using BlockGame.ui.menu;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using LiteNetLib;
using Molten;

namespace BlockGame.net;

/** tracks another player's block break progress */
public struct OtherPlayerBreak {
    public int entityID;
    public double progress;
    public int lastUpdateTick;
}

/**
 * client network manager (singleton, integrated into Game class)
 * yes this isn't symmetric with the ServerConnection, RIP.
 * this one is singleton and handles the whole client networking,
 * while ServerConnection represents a single client on the server side and there are multiple of those, and GameServer handles the server networking.
 */
public class ClientConnection : INetEventListener {
    public static ClientConnection? instance;

    private NetManager netManager;

    public NetPeer peer;
    public int ourEntityID = -1;
    public bool authenticated = false;
    public int ping;

    public int entityID = -1;  // our entity ID
    public bool connected = false;
    public string username = "";

    // track if we received a custom disconnect packet (to avoid showing generic LiteNetLib reason)
    public bool receivedDisconnectPacket = false;

    // inventory transaction tracking
    public ushort nextActionID = 0;
    public bool waitingForResync = false;

    // chunk loading tracking
    public int minLoadRadius = 7;  // minimum chunk radius around player before we can load in
    public bool initialChunksLoaded = false;  // true once minimum chunks are loaded

    // player list (tab menu)
    public readonly Dictionary<int, PlayerListEntry> playerList = new();

    // track other players' block break progress
    public readonly XMap<Vector3I, OtherPlayerBreak> breaks = [];

    public readonly ClientPacketHandler handler;

    private readonly ConcurrentQueue<Packet> incomingPackets = new();
    private readonly XUList<Vector3I> toRemove = [];

    public ClientConnection() {
        instance = this;
        handler = new ClientPacketHandler(this);
        netManager = new NetManager(this);
        netManager.ChannelsCount = 4;  // 0-3 for different packet priorities
        netManager.Start();
        Log.info("GameClient initialized");
    }

    public void connect(string address, int port, string username) {
        this.username = username;
        Log.info($"Connecting to {address}:{port} as {username}...");
        netManager.Connect(address, port, Constants.connectionKey);
    }

    public void disconnect() {
        peer?.Disconnect();
        connected = false;
        entityID = -1;
        receivedDisconnectPacket = false; // reset for next connection
    }

    public void update() {
        // poll network events
        netManager.PollEvents();

        // process all incoming packets on game thread
        while (incomingPackets.TryDequeue(out var packet)) {
            try {
                handler.handle(packet);
            }
            catch (Exception e) {
                Log.error($"Error handling packet {packet.GetType().Name}:");
                Log.error(e);
            }
        }

        // decay other players' break progress
        updateOtherPlayersBreaking();
    }

    /** decay other players' block break progress over time (this prevents block breaking being stuck if the cancel packet is lost) */
    private void updateOtherPlayersBreaking() {
        if (Game.world == null) {
            return;
        }

        int currentTick = Game.world.worldTick;
        toRemove.Clear();

        foreach (var pos in breaks.Keys) {
            var entry = breaks[pos];
            int ticks = currentTick - entry.lastUpdateTick;
            double dt = ticks / 60.0;
            entry.progress -= Player.decayRate * dt;

            if (entry.progress <= 0.01) {
                toRemove.Add(pos);
            }
            else {
                entry.lastUpdateTick = currentTick;
                breaks.Set(pos, entry);
            }
        }

        foreach (var pos in toRemove) {
            breaks.Remove(pos);
        }
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
        Game.metrics.bytesSent += bytes.Length;
        Game.metrics.packetsSent++;
    }

    public void stop() {
        netManager.Stop();
    }

    /**
     * check if chunks around player are loaded and meshed.
     * edge chunks only need to be LIGHTED
     * but inner chunks must be MESHED.
     */
    public bool hasMinimumChunks() {
        if (Game.world == null || Game.player == null) {
            return false;
        }

        var playerChunk = new ChunkCoord(
            (int)Game.player.position.X >> 4,
            (int)Game.player.position.Z >> 4
        );

        // check chunks in radius
        for (int dx = -minLoadRadius; dx <= minLoadRadius; dx++) {
            for (int dz = -minLoadRadius; dz <= minLoadRadius; dz++) {
                var coord = new ChunkCoord(playerChunk.x + dx, playerChunk.z + dz);

                // circle
                if (coord.distanceSq(playerChunk) >= minLoadRadius * minLoadRadius) {
                    continue;
                }

                // edge chunks only need to be LIGHTED (may not have all neighbours to mesh)
                bool isEdge = dx == -minLoadRadius || dx == minLoadRadius ||
                             dz == -minLoadRadius || dz == minLoadRadius;

                if (isEdge) {
                    // edge chunks just need to exist and be lighted
                    if (!Game.world.getChunkMaybe(coord, out var chunk) || chunk.status < ChunkStatus.LIGHTED) {
                        return false;
                    }
                } else {
                    if (!Game.world.getChunkMaybe(coord, out var chunk) || chunk.status < ChunkStatus.LIGHTED) {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    // LiteNetLib callbacks (network thread)

    public void OnPeerConnected(NetPeer peer) {
        Log.info($"Connected to server: {peer.Address}");
        connected = true;
        this.peer = peer;
        Net.mode = NetMode.MPC;

        // send hug
        send(new HugPacket {
            netVersion = Constants.netVersion,
            username = username,
            version = Constants.VERSION
        }, DeliveryMethod.ReliableOrdered);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
        Log.info($"Disconnected from server: {disconnectInfo.Reason} {disconnectInfo.SocketErrorCode}");
        connected = false;
        entityID = -1;

        // unlock the mouse!
        Game.instance.executeOnMainThread(() => {
            Game.instance.unlockMouse();
        });

        // if we already received a DisconnectPacket with custom reason, don't show generic message :(
        if (receivedDisconnectPacket) {
            receivedDisconnectPacket = false;
            return;
        }

        // build disconnect reason from LiteNetLib
        string reason = disconnectInfo.Reason switch {
            DisconnectReason.ConnectionFailed => "Connection failed",
            DisconnectReason.Timeout => "Connection timed out",
            DisconnectReason.HostUnreachable => "Server unreachable",
            DisconnectReason.NetworkUnreachable => "Network unreachable",
            DisconnectReason.RemoteConnectionClose => "Server closed connection",
            DisconnectReason.DisconnectPeerCalled => "Disconnected",
            DisconnectReason.ConnectionRejected => "Connection rejected",
            DisconnectReason.InvalidProtocol => "Invalid protocol",
            DisconnectReason.UnknownHost => "Unknown host",
            DisconnectReason.Reconnect => "Reconnecting",
            DisconnectReason.PeerToPeerConnection => "Peer to peer connection",
            _ => $"Disconnected: {disconnectInfo.Reason}"
        };

        if (disconnectInfo.SocketErrorCode != SocketError.Success) {
            reason += $" ({disconnectInfo.SocketErrorCode})";
        }

        // show disconnect menu
        Game.instance.executeOnMainThread(() => {
            Game.disconnectAndReturnToMenu();
            Menu.DISCONNECTED_MENU.show(reason, kicked: false);
            Game.instance.switchTo(Menu.DISCONNECTED_MENU);
        });
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
        try {
            var bytes = reader.GetRemainingBytes();

            // track metrics
            Game.metrics.bytesReceived += bytes.Length;
            Game.metrics.packetsReceived++;

            // read packet ID
            using var br = new BinaryReader(new MemoryStream(bytes));
            var buf = new PacketBuffer(br);
            int packetID = buf.readInt();

            // if this is a disconnect packet, set the flag immediately (before OnPeerDisconnected fires)
            if (packetID == 0x03) {
                receivedDisconnectPacket = true;
            }

            // create packet instance
            var type = PacketRegistry.getType(packetID);
            var packet = (Packet)Activator.CreateInstance(type)!;
            packet.read(buf);

            // track by packet type
            Game.metrics.packets.TryAdd(type, 0);
            Game.metrics.packets[type]++;

            // queue for game thread processing
            incomingPackets.Enqueue(packet);
        }
        catch (Exception e) {
            Log.error($"Error processing packet: {e}");
        }
        finally {
            reader.Recycle();
        }
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, SocketError socketError) {
        Log.error($"Network error: {socketError}");
    }

    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
        reader.Recycle();
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
        ping = latency;
    }

    public void OnConnectionRequest(ConnectionRequest request) {
        // client doesn't accept connections
        request.Reject();
    }
}

/** entry in player list (for tab menu) */
public class PlayerListEntry {
    public int entityID;
    public string username;
    public int ping;

    public PlayerListEntry(int entityID, string username, int ping) {
        this.entityID = entityID;
        this.username = username;
        this.ping = ping;
    }
}