namespace BlockGame.net.packet;

/** S→C: Add an effect to an entity */
public struct AddEffectPacket : Packet {
    public int entityID;
    public int effectID;
    public int duration;
    public int amplifier;
    public double value;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeInt(effectID);
        buf.writeInt(duration);
        buf.writeInt(amplifier);
        buf.writeDouble(value);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        effectID = buf.readInt();
        duration = buf.readInt();
        amplifier = buf.readInt();
        value = buf.readDouble();
    }
}
