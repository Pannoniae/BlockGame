namespace BlockGame.net.packet;

/** S→C: 0x43 - confirm or reject inventory transaction */
public struct InventoryAckPacket : Packet {
    public int invID;
    public ushort actionID;
    public bool acc;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(invID);
        buf.writeUShort(actionID);
        buf.writeBool(acc);
    }

    public void read(PacketBuffer buf) {
        invID = buf.readInt();
        actionID = buf.readUShort();
        acc = buf.readBool();
    }
}