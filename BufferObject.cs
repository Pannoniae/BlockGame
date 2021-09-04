using System;
using Silk.NET.OpenGL;

namespace BlockGame {
    // sorry, this is totally not copy-pasted from somewhere ^^
    public class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged {
        private uint handle;
        private BufferTargetARB type;
        private GL GL;

        public unsafe BufferObject(GL GL, Span<TDataType> data, BufferTargetARB type) {
            this.GL = GL;
            this.type = type;

            handle = this.GL.GenBuffer();
            bind();
            fixed (void* d = data) {
                this.GL.BufferData(type, (nuint)(data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
            }
        }

        public void bind() {
            GL.BindBuffer(type, handle);
        }

        public void Dispose() {
            GL.DeleteBuffer(handle);
        }
    }
}