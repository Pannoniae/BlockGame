namespace BlockGame.net.packet;

/**
 * C→S: syncs the client render distance to the server.
 */
public struct RenderDistancePacket : Packet {
    public byte dist;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeByte(dist);
    }

    public void read(PacketBuffer buf) {
        dist = buf.readByte();
    }
}
