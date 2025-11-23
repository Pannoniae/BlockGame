using System.Numerics;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** S→C: 0x21 - spawn other player entity */
public struct SpawnPlayerPacket : Packet {
    public int entityID;
    public string username;
    public Vector3D position;
    public Vector3 rotation;
    public bool sneaking;
    public bool flying;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeString(username);
        buf.writeVec3D(position);
        buf.writeVec3(rotation);
        buf.writeBool(sneaking);
        buf.writeBool(flying);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        username = buf.readString();
        position = buf.readVec3D();
        rotation = buf.readVec3();
        sneaking = buf.readBool();
        flying = buf.readBool();
    }
}