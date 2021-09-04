using System;
using Silk.NET.OpenGL;

namespace BlockGame {
    public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
        where TVertexType : unmanaged
        where TIndexType : unmanaged {
        private uint handle;
        private GL GL;

        public VertexArrayObject(GL GL, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo) {
            this.GL = GL;

            handle = this.GL.GenVertexArray();
            bind();
            vbo.bind();
            ebo.bind();
        }

        public unsafe void vertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize,
            int offset) {
            GL.VertexAttribPointer(index, count, type, false, vertexSize * (uint)sizeof(TVertexType),
                (void*)(offset * sizeof(TVertexType)));
            GL.EnableVertexAttribArray(index);
        }

        public void bind() {
            GL.BindVertexArray(handle);
        }

        public void Dispose() {
            GL.DeleteVertexArray(handle);
        }
    }
}