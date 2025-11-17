using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** S→C: 0x24 - entity velocity update */
public struct EntityVelocityPacket : Packet {
    public int entityID;
    public Vector3D velocity;

    public byte channel => 1;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeVec3D(velocity);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        velocity = buf.readVec3D();
    }
}