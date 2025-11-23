namespace BlockGame.net.packet;

/** C→S: 0x48 - client changes held item slot */
public class PlayerHeldItemChangePacket : Packet {
    public byte slot;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeByte(slot);
    }

    public void read(PacketBuffer buf) {
        slot = buf.readByte();
    }
}