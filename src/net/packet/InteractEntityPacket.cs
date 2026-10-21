namespace BlockGame.net.packet;

/** C→S: 0x3F - right-click on an entity with the held item (milking etc.) */
public struct InteractEntityPacket : Packet {
    public int entityID;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
    }
}
