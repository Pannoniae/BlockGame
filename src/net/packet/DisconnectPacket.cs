namespace BlockGame.net.packet;

/** S→C: 0x03 - server disconnects client with reason */
public struct DisconnectPacket : Packet {
    public string reason;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeString(reason);
    }

    public void read(PacketBuffer buf) {
        reason = buf.readString();
    }
}