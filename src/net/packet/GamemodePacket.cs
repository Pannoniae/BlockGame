using BlockGame.logic;

namespace BlockGame.net.packet;

/** C→S: 0x05 - client changes gamemode */
public class GamemodePacket : Packet {
    public GameModeID gamemode;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeByte((byte)gamemode);
    }

    public void read(PacketBuffer buf) {
        gamemode = (GameModeID)buf.readByte();
    }
}