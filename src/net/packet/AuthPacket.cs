namespace BlockGame.net.packet;

/** C→S: 0x0B - client authentication response */
public struct AuthPacket : Packet {
    public string password;  // plaintext (hashed on server)

    public void write(PacketBuffer buf) {
        buf.writeString(password);
    }

    public void read(PacketBuffer buf) {
        password = buf.readString();
    }
}