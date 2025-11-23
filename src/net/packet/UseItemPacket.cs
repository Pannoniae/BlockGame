namespace BlockGame.net.packet;

/** use held item (eat food, throw, etc - no target position) */
public struct UseItemPacket : Packet {

    public byte channel => 0;

    public void write(PacketBuffer buffer) {
        // no data needed - server knows selected slot
    }

    public void read(PacketBuffer buffer) {
        // no data
    }
}