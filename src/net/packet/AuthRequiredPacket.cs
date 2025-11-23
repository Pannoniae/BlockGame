namespace BlockGame.net.packet;

/** S→C: 0x0A - server requires authentication */
public struct AuthRequiredPacket : Packet {
    public bool needsRegister;  // true = show register screen, false = show login screen

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeBool(needsRegister);
    }

    public void read(PacketBuffer buf) {
        needsRegister = buf.readBool();
    }
}