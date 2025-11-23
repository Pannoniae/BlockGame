namespace BlockGame.net.packet;

public struct CancelBlockBreakPacket : Packet {

    public byte channel => 0;

    public void write(PacketBuffer buffer) {
        // no data
    }

    public void read(PacketBuffer buffer) {
        // no data
    }
}