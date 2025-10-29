using LiteNetLib;
using BlockGame.util.log;

namespace BlockGame.main;

public class Server {
    private readonly bool devMode;
    private bool running;
    private NetManager netManager;

    private const int port = 31337;
    private const string connectionKey = "BlockGame";

    public Server(bool devMode) {
        this.devMode = devMode;

        Log.info("Server initialized");
        Log.info($"Dev mode: {devMode}");

        var listener = new EventBasedNetListener();
        listener.ConnectionRequestEvent += onConnectionRequest;
        listener.PeerConnectedEvent += onPeerConnected;
        listener.PeerDisconnectedEvent += onPeerDisconnected;
        listener.NetworkReceiveEvent += onNetworkReceive;

        netManager = new NetManager(listener);
        netManager.Start(port);

        Log.info($"Server listening on port {port}");

        running = true;
        run();
    }

    private void run() {
        while (running) {
            netManager.PollEvents();
            Thread.Sleep(15);
        }

        netManager.Stop();
        Log.info("Server stopped");
    }

    private void onConnectionRequest(ConnectionRequest request) {
        Log.info($"Connection request from {request.RemoteEndPoint}");
        request.Accept();
    }

    private void onPeerConnected(NetPeer peer) {
        Log.info($"Peer connected: {peer}");
    }

    private void onPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
        Log.info($"Peer disconnected: {peer}, reason: {disconnectInfo.Reason}");
    }

    private void onNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
        // dummy server does nothing with packets
        reader.Recycle();
    }

    public void stop() {
        running = false;
    }
}