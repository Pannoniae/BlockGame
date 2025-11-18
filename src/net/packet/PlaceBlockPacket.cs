using Molten;
using System.Numerics;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

public struct PlaceBlockPacket : Packet {
    public Vector3I position;      // where block is placed
    public Placement info;

    public byte channel => 0;

    public void write(PacketBuffer buffer) {
        buffer.writeVec3I(position);
        buffer.writeByte((byte)info.face);
        buffer.writeByte((byte)info.facing);
        buffer.writeByte((byte)info.hfacing);
        buffer.writeVec3D(info.hitPoint);
    }

    public void read(PacketBuffer buffer) {
        position = buffer.readVec3I();
        var face = (RawDirection)buffer.readByte();
        var facing = (RawDirection)buffer.readByte();
        var hfacing = (RawDirectionH)buffer.readByte();
        Vector3D hitPoint = buffer.readVec3D();
        info = new Placement(face, facing, hfacing, hitPoint);
    }
}