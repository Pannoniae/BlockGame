namespace BlockGame;

public interface VAO : IDisposable {
    public void upload(Span<BlockVertexPacked> data, Span<ushort> indices);

    public void format();

    public void bind();

    public uint render();
}