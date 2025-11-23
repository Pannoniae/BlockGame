namespace BlockGame.net.packet;

/** C→S: 0x39 - drop item from inventory */
public class DropItemPacket : Packet {
    public byte slotIndex;
    public byte quantity;  // how many to drop (ctrl+Q drops stack)

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeByte(slotIndex);
        buf.writeByte(quantity);
    }

    public void read(PacketBuffer buf) {
        slotIndex = buf.readByte();
        quantity = buf.readByte();
    }
}