using BlockGame.net.packet;

namespace BlockGame.net;

public interface PacketHandler {
    void handle(Packet packet);
}