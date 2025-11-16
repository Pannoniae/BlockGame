namespace BlockGame.net.packet;

/** S→C: 0x15 - world time synchronization */
public struct TimeUpdatePacket : Packet {
    public int worldTick;

    public void write(PacketBuffer buf) {
        buf.writeInt(worldTick);
    }

    public void read(PacketBuffer buf) {
        worldTick = buf.readInt();
    }
}