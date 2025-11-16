using Molten;

namespace BlockGame.net.packet;

/** S→C: 0x12 - notifies client of a single block change */
public struct BlockChangePacket : Packet {
    public Vector3I position;
    public ushort blockID;
    public byte metadata;

    public void write(PacketBuffer buffer) {
        buffer.writeVec3I(position);
        buffer.writeUShort(blockID);
        buffer.writeByte(metadata);
    }

    public void read(PacketBuffer buffer) {
        position = buffer.readVec3I();
        blockID = buffer.readUShort();
        metadata = buffer.readByte();
    }
}