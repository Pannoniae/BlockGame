using Molten;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

public struct PlaceBlockPacket : Packet {
    public Vector3I position;      // where block is placed
    public Placement info;

    public byte channel => 0;

    public void write(PacketBuffer buf) {
        buf.writeVec3I(position);
        buf.writeByte((byte)info.face);
        buf.writeByte((byte)info.facing);
        buf.writeByte((byte)info.hfacing);
        buf.writeVec3D(info.hitPoint);
    }

    public void read(PacketBuffer buf) {
        position = buf.readVec3I();
        var face = (RawDirection)buf.readByte();
        var facing = (RawDirection)buf.readByte();
        var hfacing = (RawDirectionH)buf.readByte();
        Vector3D hitPoint = buf.readVec3D();
        info = new Placement(face, facing, hfacing, hitPoint);
    }
}