namespace BlockGame.net.packet;

/** S→C: 0x02 - connection rejected */
public struct LoginFailedPacket : Packet {
    public string reason;  // "Version mismatch", "Server full", etc.

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeString(reason);
    }

    public void read(PacketBuffer buf) {
        reason = buf.readString();
    }
}