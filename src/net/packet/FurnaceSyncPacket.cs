using Molten;

namespace BlockGame.net.packet;

/** S→C: 0x46 - sync furnace state
 * TODO we have to send this every frame, no excuses. Maybe a bit less frequently but still VERY often because this is directly used for the furnace GUI state. We currently don't send this!!
 */
public class FurnaceSyncPacket : Packet {
    public Vector3I position;
    public int smeltProgress;
    public int fuelRemaining;
    public int fuelMax;
    public bool lit;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeVec3I(position);
        buf.writeInt(smeltProgress);
        buf.writeInt(fuelRemaining);
        buf.writeInt(fuelMax);
        buf.writeBool(lit);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3I();
        smeltProgress = buf.readInt();
        fuelRemaining = buf.readInt();
        fuelMax = buf.readInt();
        lit = buf.readBool();
    }
}