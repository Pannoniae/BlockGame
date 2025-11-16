namespace BlockGame.net.packet;

/** C→S: 0x33 - client requests respawn after death */
public class RespawnRequestPacket : Packet {
    public void write(PacketBuffer buf) {
        // No data to write
    }

    public void read(PacketBuffer buf) {
        // No data to read
    }
}