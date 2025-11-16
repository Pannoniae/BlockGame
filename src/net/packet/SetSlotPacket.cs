using BlockGame.util;

namespace BlockGame.net.packet;

/** S→C: 0x40 - update single inventory slot */
// TODO we also need a state tracker for inventories just like we have for entities. Otherwise, this is vulnerable to desyncs. :(
//  Changes happen, and we broadcast the changes to all interested clients. The server keeps track of the true state of the inventory ofc.
//  We could probably get rid of all the explicit sending of this packet type from other places in the code, and just have the inventory system handle it automatically when changes occur....
//
public struct SetSlotPacket : Packet {
    public byte invID;
    public ushort slotIndex;
    public ItemStack stack;

    public void write(PacketBuffer buf) {
        buf.writeByte(invID);
        buf.writeUShort(slotIndex);
        buf.writeItemStack(stack);
    }

    public void read(PacketBuffer buf) {
        invID = buf.readByte();
        slotIndex = buf.readUShort();
        stack = buf.readItemStack();
    }
}