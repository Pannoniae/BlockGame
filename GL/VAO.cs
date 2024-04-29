namespace BlockGame;

public interface VAO {

    public void upload(Span<BlockVertex> data, Span<ushort> indices);

    public void format();

    public void bind();

    public uint render();
}