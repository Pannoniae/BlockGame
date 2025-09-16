using Molten.DoublePrecision;

namespace BlockGame.world.chunk;

public class ChunkComparer : IComparer<Chunk> {
    public Player player;

    public ChunkComparer(Player player) {
        this.player = player;
    }
    public int Compare(Chunk x, Chunk y) {
        return (int)(Vector2D.Distance((Vector2D)x.worldPos, new Vector2D((int)player.position.X, (int)player.position.Z)) -
                     Vector2D.Distance((Vector2D)y.worldPos, new Vector2D((int)player.position.X, (int)player.position.Z)));
    }
}