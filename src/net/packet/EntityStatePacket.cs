namespace BlockGame.net.packet;

/** S→C: 0x27 - delta-encoded entity state (sneaking, on fire, flying, etc.) */
public struct EntityStatePacket : Packet {
    public int entityID;
    public byte[] data; // serialized state stream with terminator

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeBytes(data);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        data = buf.readBytes();
    }
}