namespace BlockGame;

public interface VAO : IDisposable {
    public void upload(BlockVertex[] data, ushort[] indices);

    public void upload(Span<BlockVertex> data, Span<ushort> indices);

    public void format();

    public void bind();

    public uint render();
}