using System.Numerics;
using BlockGame.world.chunk;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/**
 * S→C: 0x26 - combined entity position+rotation update
 */
public struct EntityPositionRotationPacket : Packet {
    public int entityID;
    public Vector3D position;
    public Vector3 rotation;

    public byte channel => 1;

    private const double OFF_SCALE = 4096.0;
    private const double Y_SCALE = 1024.0;
    private const float ROT_SCALE = 256.0f / 360.0f;

    public void write(PacketBuffer buf) {
        split(position.X, out var cx, out var offX);
        split(position.Z, out var cz, out var offZ);

        buf.writeInt(entityID);
        buf.writeInt(cx);
        buf.writeInt(cz);
        buf.writeUShort(offX);
        buf.writeUShort(offZ);
        buf.writeInt((int)Math.Round(position.Y * Y_SCALE));
        buf.writeSByte(encRot(rotation.X));
        buf.writeSByte(encRot(rotation.Y));
        buf.writeSByte(encRot(rotation.Z));
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();

        var cx = buf.readInt();
        var cz = buf.readInt();
        var offX = buf.readUShort();
        var offZ = buf.readUShort();
        var y = buf.readInt() / Y_SCALE;

        position = new Vector3D(join(cx, offX), y, join(cz, offZ));
        rotation = new Vector3(decRot(buf.readSByte()), decRot(buf.readSByte()), decRot(buf.readSByte()));
    }

    private static void split(double v, out int chunk, out ushort off) {
        var c = (int)Math.Floor(v * (1.0 / Chunk.CHUNKSIZE));
        var rel = v - (double)c * Chunk.CHUNKSIZE;

        var q = (int)Math.Round(rel * OFF_SCALE);
        chunk = c;
        off = (ushort)int.Clamp(q, 0, ushort.MaxValue);
    }

    private static double join(int chunk, ushort off) {
        return (double)chunk * Chunk.CHUNKSIZE + off / OFF_SCALE;
    }

    private static sbyte encRot(float deg) => unchecked((sbyte)(int)MathF.Round(deg * ROT_SCALE));
    private static float decRot(sbyte v) => v / ROT_SCALE;
}
