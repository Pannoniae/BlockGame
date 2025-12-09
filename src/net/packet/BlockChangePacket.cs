using Molten;

namespace BlockGame.net.packet;

/** S→C: 0x12 - notifies client of a single block change */
public struct BlockChangePacket : Packet {
    public Vector3I position;
    public ushort blockID;
    public byte metadata;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeVec3I(position);
        buf.writeUShort(blockID);
        buf.writeByte(metadata);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3I();
        blockID = buf.readUShort();
        metadata = buf.readByte();
    }
}