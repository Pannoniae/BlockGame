namespace BlockGame.net.packet;

public interface Packet {
    void write(PacketBuffer buffer);

    void read(PacketBuffer buffer);
}