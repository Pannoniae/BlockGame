using BlockGame.util;

namespace BlockGame.net.packet;

/** S→C: 0x41 - sync entire inventory window */
public struct InventorySyncPacket : Packet {
    public byte windowID;
    public ItemStack[] items;

    public void write(PacketBuffer buf) {
        buf.writeByte(windowID);
        buf.writeUShort((ushort)items.Length);
        foreach (var item in items) {
            buf.writeItemStack(item);
        }
    }

    public void read(PacketBuffer buf) {
        windowID = buf.readByte();
        var count = buf.readUShort();
        items = new ItemStack[count];
        for (int i = 0; i < count; i++) {
            items[i] = buf.readItemStack();
        }
    }
}