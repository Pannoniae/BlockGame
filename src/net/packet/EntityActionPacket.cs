namespace BlockGame.net.packet;

/** C→S and S→C: 0x29 - one-shot entity actions (swing, damage, death, eat, crit) */
public struct EntityActionPacket : Packet {
    public int entityID;
    public Action action;

    public enum Action : byte {
        SWING = 0,
        TAKE_DAMAGE = 1,
        DEATH = 2,
        EAT = 3,
        CRITICAL_HIT = 4
    }

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeByte((byte)action);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        action = (Action)buf.readByte();
    }
}