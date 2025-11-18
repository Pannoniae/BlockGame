using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** S→C: 0x23 - entity position update */
public struct EntityPositionPacket : Packet {
    public int entityID;
    public Vector3D position;

    public byte channel => 1;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeVec3D(position);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        position = buf.readVec3D();
    }
}