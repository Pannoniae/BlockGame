namespace BlockGame.net.packet;

/** remove entity from client */
public struct DespawnEntityPacket : Packet {
    public int entityID;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
    }
}