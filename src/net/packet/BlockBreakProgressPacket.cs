using Molten;

namespace BlockGame.net.packet;

public struct BlockBreakProgressPacket : Packet {
    public int playerEntityID;
    public Vector3I position;
    public double progress;  // 0.0 - 1.0

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(playerEntityID);
        buf.writeVec3I(position);
        buf.writeDouble(progress);
    }

    public void read(PacketBuffer buf) {
        playerEntityID = buf.readInt();
        position = buf.readVec3I();
        progress = buf.readDouble();
    }
}