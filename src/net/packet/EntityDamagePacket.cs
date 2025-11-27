using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** S→C: 0x3B - broadcast damage event to clients for visual effects */
public struct EntityDamagePacket : Packet {
    public int entityID;
    public int attackerID; // -1 if environmental (fall, fire, etc.)
    public double damage;
    public Vector3D knockback;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeInt(attackerID);
        buf.writeDouble(damage);
        buf.writeVec3D(knockback);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        attackerID = buf.readInt();
        damage = buf.readDouble();
        knockback = buf.readVec3D();
    }
}
