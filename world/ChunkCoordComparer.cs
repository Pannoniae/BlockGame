using Silk.NET.Maths;

namespace BlockGame;

public class ChunkCoordComparer : IComparer<ChunkCoord> {
    public Vector3D<double> position;

    public ChunkCoordComparer(Vector3D<double> position) {
        this.position = position;
    }
    public int Compare(ChunkCoord x, ChunkCoord y) {
        return Vector2D.Distance(new Vector2D<int>(x.x * Chunk.CHUNKSIZE, x.z * Chunk.CHUNKSIZE), new Vector2D<int>((int)position.X, (int)position.Z)) -
               Vector2D.Distance(new Vector2D<int>(y.x * Chunk.CHUNKSIZE, y.z * Chunk.CHUNKSIZE), new Vector2D<int>((int)position.X, (int)position.Z));
    }
}