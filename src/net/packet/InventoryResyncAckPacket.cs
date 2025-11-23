namespace BlockGame.net.packet;

/** C→S: client confirms resync received */
public struct InventoryResyncAckPacket : Packet {
    public ushort actionID;

    public void write(PacketBuffer buf) {
        buf.writeUShort(actionID);
    }

    public void read(PacketBuffer buf) {
        actionID = buf.readUShort();
    }
}
