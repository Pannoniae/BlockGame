namespace BlockGame.net.packet;

/** S→C: 0x28 - player's own health update
 * TODO wire this up properly..
 */
public struct PlayerHealthPacket : Packet {
    public double health;
    public int damageTime;

    public void write(PacketBuffer buf) {
        buf.writeDouble(health);
        buf.writeInt(damageTime);
    }

    public void read(PacketBuffer buf) {
        health = buf.readDouble();
        damageTime = buf.readInt();
    }
}