using System.Runtime.InteropServices;

namespace BlockGame.GL;

[StructLayout(LayoutKind.Sequential, Size = 12)]
public struct BlockVertexPacked {
    public ushort x;
    public ushort y;
    public ushort z;
    public ushort u;
    public ushort v;

    /// <summary>
    /// from least significant:
    /// second byte (8-16) is lighting
    /// first 3 bits are side (see Direction enum)
    /// next 2 bits are AO
    /// </summary>
    public ushort d;

    public BlockVertexPacked(float x, float y, float z, float u, float v, ushort d) {
        this.x = (ushort)((x + 16) * 256);
        this.y = (ushort)((y + 16) * 256);
        this.z = (ushort)((z + 16) * 256);
        this.u = (ushort)(u * 32768);
        this.v = (ushort)(v * 32768);
        this.d = d;
    }

    public BlockVertexPacked(ushort x, ushort y, ushort z, ushort u, ushort v, ushort d) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.d = d;
    }
}