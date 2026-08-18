using BlockGame.net.packet;
using LiteNetLib;

namespace BlockGame.net.srv;

public sealed class MemServerConnection : ServerConnection {
    public readonly MemPipe pipe;

    public MemServerConnection(MemPipe pipe) : base(null) {
        this.pipe = pipe;
        isHost = true;
    }

    public override void send<T>(T packet, DeliveryMethod method) {
        pipe.receive(serialise(packet));
    }

    public override void disconnect(string reason) {
        send(new DisconnectPacket { reason = reason }, DeliveryMethod.ReliableOrdered);
        pipe.open = false;
    }
}
