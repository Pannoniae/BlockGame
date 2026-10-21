using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/**
 * S→C: 0x24 - knockback for players
 */
public struct EntityImpulsePacket : Packet {
    public int entityID;
    public Vector3D impulse;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeVec3D(impulse);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        impulse = buf.readVec3D();
    }
}
