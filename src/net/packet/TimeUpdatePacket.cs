namespace BlockGame.net.packet;

/** S→C: 0x15 - world time synchronization */
public struct TimeUpdatePacket : Packet {
    public int worldTick;
    public bool snap; // true = manual /time set, snap colors/clear decay; false = periodic sync

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(worldTick);
        buf.writeBool(snap);
    }

    public void read(PacketBuffer buf) {
        worldTick = buf.readInt();
        snap = buf.readBool();
    }
}