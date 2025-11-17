namespace BlockGame.net.packet;

public interface Packet {
    void write(PacketBuffer buffer);

    void read(PacketBuffer buffer);

    /** which channel to send this packet on (default 0) */
    byte channel => 0;
}