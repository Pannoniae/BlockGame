using System.Numerics;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** S→C: 0x2A - teleport player to position */
public struct TeleportPacket : Packet {
    public Vector3D position;
    public Vector3 rotation;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeVec3D(position);
        buf.writeVec3(rotation);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3D();
        rotation = buf.readVec3();
    }
}