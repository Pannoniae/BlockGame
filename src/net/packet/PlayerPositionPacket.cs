using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** C→S: 0x30 - client position update */
public struct PlayerPositionPacket : Packet {
    public Vector3D position;
    public bool onGround;

    public void write(PacketBuffer buf) {
        buf.writeVec3D(position);
        buf.writeBool(onGround);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3D();
        onGround = buf.readBool();
    }
}