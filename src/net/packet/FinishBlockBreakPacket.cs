using Molten;

namespace BlockGame.net.packet;

public struct FinishBlockBreakPacket : Packet {
    public Vector3I position;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeVec3I(position);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3I();
    }
}