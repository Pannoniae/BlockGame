namespace BlockGame.net.packet;

/** S→C: 0x06 - add player to tab list */
public struct PlayerListAddPacket : Packet {
    public int entityID;
    public string username;
    public int ping;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeString(username);
        buf.writeInt(ping);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        username = buf.readString();
        ping = buf.readInt();
    }
}