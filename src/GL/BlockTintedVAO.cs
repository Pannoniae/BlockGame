using Silk.NET.OpenGL;

namespace BlockGame.GL;

public class BlockTintedVAO : IDisposable {
    public uint handle;
    public uint vbo;
    public uint ibo;
    public uint count;

    public Silk.NET.OpenGL.GL GL;

    public BlockTintedVAO() {
        GL = Game.GL;
        handle = GL.GenVertexArray();
        vbo = GL.GenBuffer();
        ibo = GL.GenBuffer();
    }

    public void upload(float[] data) {
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)data.Length;
            fixed (float* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(float)), d,
                    BufferUsageARB.DynamicDraw);
            }
        }

        format();
    }

    public void upload(Span<float> data) {
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)data.Length;
            fixed (float* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(float)), d,
                    BufferUsageARB.DynamicDraw);
            }
        }

        format();
    }

    public void upload(BlockVertexTinted[] data, ushort[] indices) {
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)indices.Length;
            fixed (BlockVertexTinted* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(BlockVertexTinted)), d,
                    BufferUsageARB.DynamicDraw);
            }

            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);
            fixed (ushort* d = indices) {
                GL.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Length * sizeof(ushort)), d,
                    BufferUsageARB.DynamicDraw);
            }
        }

        format();
    }

    public void upload(Span<BlockVertexTinted> data, Span<ushort> indices) {
        unsafe {

            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)indices.Length;
            fixed (BlockVertexTinted* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(BlockVertexTinted)), d,
                    BufferUsageARB.DynamicDraw);
            }


            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);
            fixed (ushort* d = indices) {
                GL.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Length * sizeof(ushort)), d,
                    BufferUsageARB.DynamicDraw);
            }
        }

        format();
    }

    public void format() {
        unsafe {
            // 20 bytes in total, 3*4 for pos, 2*2 for uv, 4 bytes for color
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(BlockVertexTinted), (void*)0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.HalfFloat, false, (uint)sizeof(BlockVertexTinted), (void*)(0 + 3 * sizeof(float)));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, (uint)sizeof(BlockVertexTinted), (void*)(0 + 4 * sizeof(float)));
            GL.EnableVertexAttribArray(2);
        }
    }

    public void bind() {
        GL.BindVertexArray(handle);
    }

    // rendering
    public uint render() {
        unsafe {
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedShort, (void*)0);
            return count;
        }
    }

    public void Dispose() {
        GL.DeleteBuffer(vbo);
        GL.DeleteBuffer(ibo);
        GL.DeleteVertexArray(handle);
    }
}