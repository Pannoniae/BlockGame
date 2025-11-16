namespace BlockGame.net.packet;

/** S→C: 0x43 - confirm or reject inventory transaction */
public struct InventoryAckPacket : Packet {
    public byte invID;
    public ushort actionID;
    public bool acc;

    public void write(PacketBuffer buf) {
        buf.writeByte(invID);
        buf.writeUShort(actionID);
        buf.writeBool(acc);
    }

    public void read(PacketBuffer buf) {
        invID = buf.readByte();
        actionID = buf.readUShort();
        acc = buf.readBool();
    }
}