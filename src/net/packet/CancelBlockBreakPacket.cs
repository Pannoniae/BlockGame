namespace BlockGame.net.packet;

public struct CancelBlockBreakPacket : Packet {

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        // no data
    }

    public void read(PacketBuffer buf) {
        // no data
    }
}