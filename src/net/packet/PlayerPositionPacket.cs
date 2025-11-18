using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** C→S: 0x30 - client position update */
public struct PlayerPositionPacket : Packet {
    public Vector3D position;

    public byte channel => 1;

    public void write(PacketBuffer buf) {
        buf.writeVec3D(position);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3D();
    }
}