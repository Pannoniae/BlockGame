using System.Numerics;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/** S→C: 0x01 - successful login response */
public struct LoginSuccessPacket : Packet {
    public int entityID;
    public Vector3D spawnPos;
    public Vector3 rotation;
    public int worldTick;
    public bool creative;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeVec3D(spawnPos);
        buf.writeVec3(rotation);
        buf.writeInt(worldTick);
        buf.writeBool(creative);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        spawnPos = buf.readVec3D();
        rotation = buf.readVec3();
        worldTick = buf.readInt();
        creative = buf.readBool();
    }
}