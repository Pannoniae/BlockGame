using BlockGame.util;

namespace BlockGame.world.block;

public class Leaves : Block {

    /** Max distance to search for logs */
    private const int DECAY_DIST = 5;

    /** Neighbor offsets for BFS */
    private static readonly (int dx, int dy, int dz)[] dirs = [
        (-1, 0, 0), (1, 0, 0),
        (0, -1, 0), (0, 1, 0),
        (0, 0, -1), (0, 0, 1)
    ];


    public Leaves(ushort id, string name) : base(id, name) {
        transparency();
        tick();
    }

    public override void randomUpdate(World world, int x, int y, int z) {
        // check if connected to log within DECAY_DIST blocks
        if (!isConnectedToLog(world, x, y, z)) {
            // decay: drop nothing, just disappear
            world.setBlock(x, y, z, Blocks.AIR);
        }
    }

    /** BFS to find if leaf is connected to a log within DECAY_DIST */
    private bool isConnectedToLog(World world, int sx, int sy, int sz) {
        var queue = new Queue<(int x, int y, int z, int dist)>();
        var visited = new HashSet<(int, int, int)>();

        queue.Enqueue((sx, sy, sz, 0));
        visited.Add((sx, sy, sz));

        while (queue.Count > 0) {
            var (x, y, z, dist) = queue.Dequeue();

            // reached max distance
            if (dist >= DECAY_DIST) continue;

            // check all 6 neighbors
            foreach (var dir in Direction.directions) {
                int nx = x + dir.X, ny = y + dir.Y, nz = z + dir.Z;

                if (!visited.Add((nx, ny, nz))) continue;

                var bid = world.getBlock(nx, ny, nz);

                // found log - connected!
                if (isLog(bid)) return true;

                // found leaf - continue search
                if (isLeaf(bid)) {
                    queue.Enqueue((nx, ny, nz, dist + 1));
                }
            }
        }

        // no log found
        return false;
    }

    private static bool isLog(ushort block) {
        return block == Blocks.LOG || block == Blocks.MAPLE_LOG;
    }

    private static bool isLeaf(ushort block) {
        return block == Blocks.LEAVES || block == Blocks.MAPLE_LEAVES;
    }
}