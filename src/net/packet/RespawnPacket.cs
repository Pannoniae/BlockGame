using System.Numerics;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** S→C: 0x04 - respawn player */
public class RespawnPacket : Packet {
    public Vector3D spawnPosition;
    public Vector3 rotation;

    public void write(PacketBuffer buf) {
        buf.writeVec3D(spawnPosition);
        buf.writeVec3(rotation);
    }

    public void read(PacketBuffer buf) {
        spawnPosition = buf.readVec3D();
        rotation = buf.readVec3();
    }
}