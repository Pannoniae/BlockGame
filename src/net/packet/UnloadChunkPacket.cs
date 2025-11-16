using BlockGame.world.chunk;

namespace BlockGame.net.packet;

/** S→C: 0x11 - tells client to unload a chunk */
public class UnloadChunkPacket : Packet {
    public ChunkCoord coord;

    public void write(PacketBuffer buf) {
        buf.writeInt(coord.x);
        buf.writeInt(coord.z);
    }

    public void read(PacketBuffer buf) {
        coord = new ChunkCoord(buf.readInt(), buf.readInt());
    }
}