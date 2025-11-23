using Molten;

namespace BlockGame.net.packet;

/** S→C: 0x51 - update block entity data */
public class BlockEntityDataPacket : Packet {
    public Vector3I position;
    public byte[] data;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeVec3I(position);
        buf.writeInt(data.Length);
        buf.writeBytes(data);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3I();
        int length = buf.readInt();
        data = buf.readBytes(length);
    }
}