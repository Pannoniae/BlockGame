namespace BlockGame.net.packet;

/** S→C/C→S: 0x09 - sync player skin (PNG bytes, empty = default) */
public struct PlayerSkinPacket : Packet {
    public int entityID;
    public byte[] skinData; // PNG file bytes, or empty array for default skin

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeBytes(skinData);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        skinData = buf.readBytes();
    }
}
