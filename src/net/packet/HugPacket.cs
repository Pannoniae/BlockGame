namespace BlockGame.net.packet;

/** C→S: 0x00 - initial connection request
 * We used to handshake but we hug now!
 */
public struct HugPacket : Packet {
    public int netVersion;
    public string username;
    public string version;

    public void write(PacketBuffer buf) {
        buf.writeInt(netVersion);
        buf.writeString(username);
        buf.writeString(version);
    }

    public void read(PacketBuffer buf) {
        netVersion = buf.readInt();
        username = buf.readString();
        version = buf.readString();
    }
}