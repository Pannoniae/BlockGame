using Molten;

namespace BlockGame.net.packet;

/**
 * S→C: 0x44 - open inventory window
 */
public class InventoryOpenPacket : Packet {
    public byte invID;
    public byte invType;  // inventory type ID
    public string title;
    public byte slotCount;
    public Vector3I? position;       // for block entities

    public void write(PacketBuffer buf) {
        buf.writeByte(invID);
        buf.writeByte(invType);
        buf.writeString(title);
        buf.writeByte(slotCount);
        buf.writeBool(position.HasValue);
        if (position.HasValue) {
            buf.writeVec3I(position.Value);
        }
    }

    public void read(PacketBuffer buf) {
        invID = buf.readByte();
        invType = buf.readByte();
        title = buf.readString();
        slotCount = buf.readByte();
        if (buf.readBool()) {
            position = buf.readVec3I();
        } else {
            position = null;
        }
    }
}