namespace BlockGame.net.packet;

/** use held item (eat food, throw, etc - no target position) */
public struct UseItemPacket : Packet {
    public float chargeRatio; // 0-1, for bows and other chargeable items

    public byte channel => 0;

    public void write(PacketBuffer buffer) {
        buffer.writeFloat(chargeRatio);
    }

    public void read(PacketBuffer buffer) {
        chargeRatio = buffer.readFloat();
    }
}