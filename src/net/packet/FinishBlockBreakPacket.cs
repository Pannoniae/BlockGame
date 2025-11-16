using Molten;

namespace BlockGame.net.packet;

public struct FinishBlockBreakPacket : Packet {
    public Vector3I position;

    public void write(PacketBuffer buffer) {
        buffer.writeVec3I(position);
    }

    public void read(PacketBuffer buffer) {
        position = buffer.readVec3I();
    }
}