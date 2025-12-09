using Molten;

namespace BlockGame.net.packet;

/** S→C: 0x13 - notifies client of multiple block changes */
public struct MultiBlockChangePacket : Packet {
    public Vector3I[] pos;
    public ushort[] blockIDs;
    public byte[] metadata;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        // write count
        int count = pos.Length;
        buf.writeByte((byte)count);

        // write positions
        for (int i = 0; i < count; i++) {
            buf.writeVec3I(pos[i]);
        }

        // write block IDs
        for (int i = 0; i < count; i++) {
            buf.writeUShort(blockIDs[i]);
        }

        // write metadata
        for (int i = 0; i < count; i++) {
            buf.writeByte(metadata[i]);
        }
    }

    public void read(PacketBuffer buf) {
        // read count
        int count = buf.readByte();

        // allocate arrays
        pos = new Vector3I[count];
        blockIDs = new ushort[count];
        metadata = new byte[count];

        // read positions
        for (int i = 0; i < count; i++) {
            pos[i] = buf.readVec3I();
        }

        // read block IDs
        for (int i = 0; i < count; i++) {
            blockIDs[i] = buf.readUShort();
        }

        // read metadata
        for (int i = 0; i < count; i++) {
            metadata[i] = buf.readByte();
        }
    }
}