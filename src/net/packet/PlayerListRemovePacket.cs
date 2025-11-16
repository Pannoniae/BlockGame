namespace BlockGame.net.packet;

/** S→C: 0x07 - remove player from tab list */
public struct PlayerListRemovePacket : Packet {
    public int entityID;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
    }
}