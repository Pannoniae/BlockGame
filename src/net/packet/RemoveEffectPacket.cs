namespace BlockGame.net.packet;

/** S→C: Remove an effect from an entity */
public struct RemoveEffectPacket : Packet {
    public int entityID;
    public int effectID;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeInt(effectID);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        effectID = buf.readInt();
    }
}
