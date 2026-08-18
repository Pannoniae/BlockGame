using Molten;
using Silk.NET.Maths;
using Vector3D = Molten.DoublePrecision.Vector3D;

namespace BlockGame.world.chunk;

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

public sealed class ChunkTicketComparerReverse : IComparer<ChunkLoadTicket> {
    private readonly List<Vector3D> anchors;

    public ChunkTicketComparerReverse(List<Vector3D> anchors) {
        this.anchors = anchors;
    }

    private double distSq(ChunkCoord c) {
        double cx = (c.x << 4) + 8;
        double cz = (c.z << 4) + 8;
        var best = double.MaxValue;
        for (var i = 0; i < anchors.Count; i++) {
            var dx = anchors[i].X - cx;
            var dz = anchors[i].Z - cz;
            var d = dx * dx + dz * dz;
            if (d < best) {
                best = d;
            }
        }
        return best;
    }

    public int Compare(ChunkLoadTicket x, ChunkLoadTicket y) {
        var lvl = y.level.CompareTo(x.level);
        if (lvl != 0) {
            return lvl;
        }
        return distSq(y.chunkCoord).CompareTo(distSq(x.chunkCoord));
    }
}

public class ChunkTicketComparerSegregated : IComparer<ChunkLoadTicket> {
    public Vector3I position;

    public ChunkTicketComparerSegregated(Vector3I position) {
        this.position = position;
    }
    public int Compare(ChunkLoadTicket x, ChunkLoadTicket y) {
        var comparison = Vector2D.Distance(new Vector2D<int>(y.chunkCoord.x * Chunk.CHUNKSIZE, y.chunkCoord.z * Chunk.CHUNKSIZE), new Vector2D<int>(position.X, position.Z)) -
                         Vector2D.Distance(new Vector2D<int>(x.chunkCoord.x * Chunk.CHUNKSIZE, x.chunkCoord.z * Chunk.CHUNKSIZE), new Vector2D<int>(position.X, position.Z));

        // sort by distance, but merge same level together
        if (x.level == y.level) {
            return comparison;
        }
        return (int)y.level - (int)x.level;
    }
}