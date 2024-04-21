using Silk.NET.Maths;

namespace BlockGame;

public class ChunkCoordComparer : IComparer<ChunkCoord> {
    public Player player;

    public ChunkCoordComparer(Player player) {
        this.player = player;
    }
    public int Compare(ChunkCoord x, ChunkCoord y) {
        return Vector2D.Distance(new Vector2D<int>(x.x * Chunk.CHUNKSIZE, x.z * Chunk.CHUNKSIZE), new Vector2D<int>((int)player.position.X, (int)player.position.Z)) -
               Vector2D.Distance(new Vector2D<int>(y.x * Chunk.CHUNKSIZE, y.z * Chunk.CHUNKSIZE), new Vector2D<int>((int)player.position.X, (int)player.position.Z));
    }
}