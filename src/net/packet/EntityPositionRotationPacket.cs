using System.Numerics;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** S→C: 0x26 - combined entity position+rotation update */
public struct EntityPositionRotationPacket : Packet {
    public int entityID;
    public Vector3D position;
    public Vector3 rotation;

    public byte channel => 1;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeVec3D(position);
        buf.writeVec3(rotation);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        position = buf.readVec3D();
        rotation = buf.readVec3();
    }
}