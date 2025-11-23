using System.Numerics;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** S→C: 0x20 - spawn non-player entities (mobs, items, falling blocks)
 */
public struct SpawnEntityPacket : Packet {
    public int entityID;
    public int entityType;  // runtime int ID
    public Vector3D position;
    public Vector3 rotation;
    public Vector3D velocity;
    public byte[] extraData;  // entity-specific data

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeInt(entityType);
        buf.writeVec3D(position);
        buf.writeVec3(rotation);
        buf.writeVec3D(velocity);
        buf.writeBytes(extraData);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        entityType = buf.readInt();
        position = buf.readVec3D();
        rotation = buf.readVec3();
        velocity = buf.readVec3D();
        extraData = buf.readBytes();
    }
}