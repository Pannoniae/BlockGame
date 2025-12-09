namespace BlockGame.net.packet;

public interface Packet {
    void write(PacketBuffer buf);

    void read(PacketBuffer buf);

    /** which channel to send this packet on (default 0) */
    byte channel => 0;
}