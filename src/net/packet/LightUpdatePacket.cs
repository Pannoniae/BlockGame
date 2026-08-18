using BlockGame.world.chunk;

namespace BlockGame.net.packet;

/**
 * S→C: 0x14 - light values for one subchunk.
 */
public class LightUpdatePacket : Packet {
    public ChunkCoord coord;
    public byte y;
    public byte uniformSky;
    public byte uniformBlock;
    public byte[]? skyPlane;
    public byte[]? blockPlane;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(coord.x);
        buf.writeInt(coord.z);
        buf.writeByte(y);
        buf.writeByte(uniformSky);
        buf.writeByte(uniformBlock);

        buf.writeBool(skyPlane != null);
        if (skyPlane != null) {
            buf.writeRawBytes(skyPlane, 0, ChunkDataPacket.LIGHT_PLANE_BYTES);
        }

        buf.writeBool(blockPlane != null);
        if (blockPlane != null) {
            buf.writeRawBytes(blockPlane, 0, ChunkDataPacket.LIGHT_PLANE_BYTES);
        }
    }

    public void read(PacketBuffer buf) {
        coord = new ChunkCoord(buf.readInt(), buf.readInt());
        y = buf.readByte();
        uniformSky = buf.readByte();
        uniformBlock = buf.readByte();

        if (buf.readBool()) {
            skyPlane = buf.readBytes(ChunkDataPacket.LIGHT_PLANE_BYTES);
        }

        if (buf.readBool()) {
            blockPlane = buf.readBytes(ChunkDataPacket.LIGHT_PLANE_BYTES);
        }
    }
}
