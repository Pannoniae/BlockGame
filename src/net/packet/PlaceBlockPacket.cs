using Molten;

namespace BlockGame.net.packet;

public struct PlaceBlockPacket : Packet {
    public Vector3I position;  // where block is placed
    public byte face;          // which face was clicked

    public void write(PacketBuffer buffer) {
        buffer.writeVec3I(position);
        buffer.writeByte(face);
    }

    public void read(PacketBuffer buffer) {
        position = buffer.readVec3I();
        face = buffer.readByte();
    }
}