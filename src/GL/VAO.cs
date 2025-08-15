using BlockGame.GL.vertexformats;

namespace BlockGame.GL;

public interface VAO : IDisposable {
    public void upload(Span<BlockVertexPacked> data, Span<ushort> indices);

    public void format();

    public void bind();

    public uint render();
}