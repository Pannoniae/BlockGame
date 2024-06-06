using Silk.NET.Maths;

namespace BlockGame;

public class ChunkComparer : IComparer<Chunk> {
    public Player player;

    public ChunkComparer(Player player) {
        this.player = player;
    }
    public int Compare(Chunk x, Chunk y) {
        return Vector2D.Distance(x.worldPos, new Vector2D<int>((int)player.position.X, (int)player.position.Z)) -
               Vector2D.Distance(y.worldPos, new Vector2D<int>((int)player.position.X, (int)player.position.Z));
    }
}