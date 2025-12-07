using Molten;

namespace BlockGame.net.packet;

/** S→C: 0x47 - sync furnace state (sent every 2 ticks to viewers) */
public class FurnaceSyncPacket : Packet {
    public Vector3I position;
    public int smeltProgress;
    public int smeltTime;
    public int fuelRemaining;
    public int fuelMax;
    public bool lit;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeVec3I(position);
        buf.writeInt(smeltProgress);
        buf.writeInt(smeltTime);
        buf.writeInt(fuelRemaining);
        buf.writeInt(fuelMax);
        buf.writeBool(lit);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3I();
        smeltProgress = buf.readInt();
        smeltTime = buf.readInt();
        fuelRemaining = buf.readInt();
        fuelMax = buf.readInt();
        lit = buf.readBool();
    }
}