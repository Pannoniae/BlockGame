using Molten;

namespace BlockGame.net.packet;

public struct BlockBreakProgressPacket : Packet {
    public int playerEntityID;
    public Vector3I position;
    public double progress;  // 0.0 - 1.0

    public byte channel => 0;

    public void write(PacketBuffer buffer) {
        buffer.writeInt(playerEntityID);
        buffer.writeVec3I(position);
        buffer.writeDouble(progress);
    }

    public void read(PacketBuffer buffer) {
        playerEntityID = buffer.readInt();
        position = buffer.readVec3I();
        progress = buffer.readDouble();
    }
}