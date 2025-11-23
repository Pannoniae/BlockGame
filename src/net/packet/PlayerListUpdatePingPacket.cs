namespace BlockGame.net.packet;

/** S→C: 0x08 - update player ping in tab list */
public struct PlayerListUpdatePingPacket : Packet {
    public int entityID;
    public int ping;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeInt(ping);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        ping = buf.readInt();
    }
}