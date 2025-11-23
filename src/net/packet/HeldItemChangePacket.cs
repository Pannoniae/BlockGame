using BlockGame.util;

namespace BlockGame.net.packet;

/** Bidirectional: 0x45 - sync hotbar selection + held item */
public struct HeldItemChangePacket : Packet {
    public int entityID;        // which player (for S→C broadcast)
    public byte slotIndex;      // 0-8 hotbar slot
    public ItemStack heldItem; // item in that slot (null = empty)

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeByte(slotIndex);
        buf.writeItemStack(heldItem);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        slotIndex = buf.readByte();
        heldItem = buf.readItemStack();
    }
}