using TrippyGL;

namespace BlockGame.GL;

public class Graphics {
    public readonly TextureBatcher mainBatch;
    public readonly TextureBatcher immediateBatch;

    public Graphics() {
        mainBatch = new TextureBatcher(Game.GD);
        immediateBatch = new TextureBatcher(Game.GD);
    }
}