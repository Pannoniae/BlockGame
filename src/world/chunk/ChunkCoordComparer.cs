using Molten;
using Silk.NET.Maths;

namespace BlockGame;

public class ChunkCoordComparer : IComparer<ChunkCoord> {
    public Molten.DoublePrecision.Vector3D position;

    public ChunkCoordComparer(Molten.DoublePrecision.Vector3D position) {
        this.position = position;
    }
    public int Compare(ChunkCoord x, ChunkCoord y) {
        return Vector2D.Distance(new Vector2D<int>(x.x * Chunk.CHUNKSIZE, x.z * Chunk.CHUNKSIZE), new Vector2D<int>((int)position.X, (int)position.Z)) -
               Vector2D.Distance(new Vector2D<int>(y.x * Chunk.CHUNKSIZE, y.z * Chunk.CHUNKSIZE), new Vector2D<int>((int)position.X, (int)position.Z));
    }
}

public class ChunkTicketComparer : IComparer<ChunkLoadTicket> {
    public Vector3I position;

    public ChunkTicketComparer(Vector3I position) {
        this.position = position;
    }
    public int Compare(ChunkLoadTicket x, ChunkLoadTicket y) {
        var comparison = Vector2D.Distance(new Vector2D<int>(x.chunkCoord.x * Chunk.CHUNKSIZE, x.chunkCoord.z * Chunk.CHUNKSIZE), new Vector2D<int>(position.X, position.Z)) -
                         Vector2D.Distance(new Vector2D<int>(y.chunkCoord.x * Chunk.CHUNKSIZE, y.chunkCoord.z * Chunk.CHUNKSIZE), new Vector2D<int>(position.X, position.Z));
        int statusDiff = (int)x.level - (int)y.level;
        // if statusDiff > 0, chunk2 is bigger
        //Console.Out.WriteLine($"{comparison} {statusDiff * 1000}");
        return comparison + statusDiff * 10000;
    }
}

public class ChunkTicketComparerReverse : IComparer<ChunkLoadTicket> {
    public Vector3I position;

    public ChunkTicketComparerReverse(Vector3I position) {
        this.position = position;
    }
    public int Compare(ChunkLoadTicket x, ChunkLoadTicket y) {
        var comparison = Vector2D.Distance(new Vector2D<int>(y.chunkCoord.x * Chunk.CHUNKSIZE, y.chunkCoord.z * Chunk.CHUNKSIZE), new Vector2D<int>(position.X, position.Z)) -
                         Vector2D.Distance(new Vector2D<int>(x.chunkCoord.x * Chunk.CHUNKSIZE, x.chunkCoord.z * Chunk.CHUNKSIZE), new Vector2D<int>(position.X, position.Z));
        int statusDiff = (int)y.level - (int)x.level;
        // if statusDiff > 0, chunk2 is bigger
        //Console.Out.WriteLine($"{comparison} {statusDiff * 1000}");
        return comparison + statusDiff * 10000;
    }
}