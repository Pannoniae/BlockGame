namespace BlockGame.net.packet;

/** Bidirectional: 0x45 - close inventory window
 * C→S: when player closes an inventory
 * S→C: when server forces inventory to close
 * TODO on the server, handle the close logic (dropping items if needed, etc.) and send this packet to the client and set the server player's currentCtx to their inventory.
 * On the client we close the GUI, send the packet to the server, and set the player's currentCtx to their inventory.
 * Also don't let shit remain in the cursor.
 */
public class InventoryClosePacket : Packet {
    public int invID;

    public void write(PacketBuffer buf) {
        buf.writeInt(invID);
    }

    public void read(PacketBuffer buf) {
        invID = buf.readInt();
    }
}