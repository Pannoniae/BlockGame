using System.Numerics;

namespace BlockGame.net.packet;

/** C→S: 0x31 - client rotation update */
public struct PlayerRotationPacket : Packet {
    public Vector3 rotation;

    public void write(PacketBuffer buf) {
        buf.writeVec3(rotation);
    }

    public void read(PacketBuffer buf) {
        rotation = buf.readVec3();
    }
}