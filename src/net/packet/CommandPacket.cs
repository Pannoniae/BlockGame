namespace BlockGame.net.packet;

/**
 * C→S: Client sends command to server for execution
 * Server executes with player as CommandSource, responses via ChatMessagePacket
 */
public struct CommandPacket : Packet {
    public string command;  // full command with args (without leading /)

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeString(command);
    }

    public void read(PacketBuffer buf) {
        command = buf.readString();
    }
}