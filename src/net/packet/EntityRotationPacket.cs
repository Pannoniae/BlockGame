using System.Numerics;

namespace BlockGame.net.packet;

/** S→C: 0x25 - entity rotation update */
public struct EntityRotationPacket : Packet {
    public int entityID;
    public Vector3 rotation;
    public Vector3 bodyRotation;

    public byte channel => 1;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeVec3(rotation);
        buf.writeVec3(bodyRotation);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        rotation = buf.readVec3();
        bodyRotation = buf.readVec3();
    }
}