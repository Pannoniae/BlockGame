namespace BlockGame.net.packet;

/** C→S: 0x3A - client attacks entity */
public struct AttackEntityPacket : Packet {
    public int targetEntityID;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(targetEntityID);
    }

    public void read(PacketBuffer buf) {
        targetEntityID = buf.readInt();
    }
}