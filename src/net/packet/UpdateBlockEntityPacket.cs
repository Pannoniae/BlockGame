using BlockGame.util;
using Molten;

namespace BlockGame.net.packet;

/** S→C: 0x50 - send block entity data using NBT
 * TODO type is unused for now
 */
public struct UpdateBlockEntityPacket : Packet {
    public Vector3I position;
    public byte type;
    public byte[] nbt;

    public void write(PacketBuffer buf) {
        buf.writeVec3I(position);
        buf.writeByte(type);
        buf.writeBytes(nbt); // writeBytes handles length automatically
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3I();
        type = buf.readByte();
        nbt = buf.readBytes(); // readBytes handles length automatically
    }
}