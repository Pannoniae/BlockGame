namespace BlockGame.net.packet;

/** use held item in air (eat food, throw, shoot bow, etc - no target position) */
public struct UseItemPacket : Packet {
    public byte channel => 0;

    public void write(PacketBuffer buf) {
        // no data to write
    }

    public void read(PacketBuffer buf) {
        // no data to read
    }
}