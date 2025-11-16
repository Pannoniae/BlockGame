using System.Numerics;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** C→S: 0x32 - combined position+rotation update */
public struct PlayerPositionRotationPacket : Packet {
    public Vector3D position;
    public Vector3 rotation;
    public bool onGround;

    public void write(PacketBuffer buf) {
        buf.writeVec3D(position);
        buf.writeVec3(rotation);
        buf.writeBool(onGround);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3D();
        rotation = buf.readVec3();
        onGround = buf.readBool();
    }
}