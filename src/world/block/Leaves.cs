using BlockGame.util;
using Molten;

namespace BlockGame.world.block;

public class Leaves : Block {
    private static readonly Queue<(int x, int y, int z, int dist)> queue = [];
    private static readonly HashSet<Vector3I> visited = [];

    /** Max distance to search for logs */
    private const int DECAY_DIST = 5;


    public Leaves(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        transparency();
        tick();

        material(Material.ORGANIC);

        // only broken by a scythe!
        tool[id] = ToolType.SCYTHE;
        tier[id] = MaterialTier.WOOD;
    }

    public override void randomUpdate(World world, int x, int y, int z) {
        // check if connected to log within DECAY_DIST blocks
        if (!isConnectedToLog(world, x, y, z)) {
            // decay: drop nothing, just disappear
            world.setBlock(x, y, z, AIR.id);
        }
    }

    /** BFS to find if leaf is connected to a log within DECAY_DIST */
    private static bool isConnectedToLog(World world, int sx, int sy, int sz) {
        queue.Clear();
        visited.Clear();

        queue.Enqueue((sx, sy, sz, 0));
        visited.Add(new Vector3I(sx, sy, sz));

        while (queue.Count > 0) {
            var (x, y, z, dist) = queue.Dequeue();

            // reached max distance
            if (dist >= DECAY_DIST) continue;

            // check all 6 neighbours
            foreach (var dir in Direction.directions) {
                int nx = x + dir.X, ny = y + dir.Y, nz = z + dir.Z;

                if (!visited.Add(new Vector3I(nx, ny, nz))) {
                    continue;
                }

                var bid = world.getBlock(nx, ny, nz);

                // found log - connected!
                if (log[bid]) {
                    return true;
                }

                // found leaf - continue search
                if (leaves[bid]) {
                    queue.Enqueue((nx, ny, nz, dist + 1));
                }
            }
        }

        // no log found
        return false;
    }
}