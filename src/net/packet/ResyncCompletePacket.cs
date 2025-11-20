namespace BlockGame.net.packet;

/** S→C: signals end of inventory resync after desync */
public struct ResyncCompletePacket : Packet {
    public ushort actionID;

    public void write(PacketBuffer buf) {
        buf.writeUShort(actionID);
    }

    public void read(PacketBuffer buf) {
        actionID = buf.readUShort();
    }
}
