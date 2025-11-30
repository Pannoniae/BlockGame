using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** C→S: 0x3E - player velocity update */
public struct PlayerVelocityPacket : Packet {
    public Vector3D velocity;

    public byte channel => 1;

    public void write(PacketBuffer buf) {
        buf.writeVec3D(velocity);
    }

    public void read(PacketBuffer buf) {
        velocity = buf.readVec3D();
    }
}