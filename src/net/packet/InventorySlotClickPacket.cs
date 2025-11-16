using BlockGame.util;

namespace BlockGame.net.packet;

/** C→S: 0x42 - client clicks inventory slot
 * Client does optimistic update locally, then sends expected result for server validation.
 */
public struct InventorySlotClickPacket : Packet {
    public byte invID;
    public ushort idx;
    public byte button;           // 0=left, 1=right, 2=middle
    public ushort actionID;       // transaction ID for ordering/desync detection
    public byte mode;             // 0=normal click, 1=shift-click, 2=drop
    public ItemStack expectedSlot; // what the clicked slot should contain after the operation

    public void write(PacketBuffer buf) {
        buf.writeByte(invID);
        buf.writeUShort(idx);
        buf.writeByte(button);
        buf.writeUShort(actionID);
        buf.writeByte(mode);
        buf.writeItemStack(expectedSlot);
    }

    public void read(PacketBuffer buf) {
        invID = buf.readByte();
        idx = buf.readUShort();
        button = buf.readByte();
        actionID = buf.readUShort();
        mode = buf.readByte();
        expectedSlot = buf.readItemStack();
    }
}