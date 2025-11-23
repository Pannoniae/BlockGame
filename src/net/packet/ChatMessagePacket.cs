namespace BlockGame.net.packet;

/**
 * bidirectional chat packet
 * C→S: client sends raw message
 * S→C: server sends formatted message with username
 */
public struct ChatMessagePacket : Packet {
    public string message;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeString(message);
    }

    public void read(PacketBuffer buf) {
        message = buf.readString();
    }
}