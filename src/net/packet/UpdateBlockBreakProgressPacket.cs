using Molten;

namespace BlockGame.net.packet;

/**
 * C→S: client sends break progress update
 * TODO this is sloppy, we should use fixed point here..
 */
public struct UpdateBlockBreakProgressPacket : Packet {
    public Vector3I position;
    public double progress;  // 0.0 - 1.0

    public byte channel => 0;

    public void write(PacketBuffer buffer) {
        buffer.writeVec3I(position);
        buffer.writeDouble(progress);
    }

    public void read(PacketBuffer buffer) {
        position = buffer.readVec3I();
        progress = buffer.readDouble();
    }
}
