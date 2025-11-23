using BlockGame.main;
using BlockGame.util;
using BlockGame.world.item;
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

    public override (Item? item, byte metadata, int count) getDrop(World world, int x, int y, int z, byte metadata, bool canBreak) {
        // oak: 1 in 10 chance to drop apple
        if (id == LEAVES.id && Game.random.Next(10) == 0) {
            return (Item.APPLE, 0, 1);
        }

        // oak: 1 in 15 chance to drop sapling
        if (id == LEAVES.id && Game.random.Next(15) == 0){
            return (OAK_SAPLING.item, 0, 1);
        }

        // maple leaves: 1 in 15 chance to drop sapling
        if (id == MAPLE_LEAVES.id && Game.random.Next(15) == 0) {
            return (MAPLE_SAPLING.item, 0, 1);
        }

        // maple leaves: 1 in 20 chance to drop maple syrup
        if (id == MAPLE_LEAVES.id && Game.random.Next(20) == 0) {
            return (Item.MAPLE_SYRUP, 0, 1);
        }

        // mahogany leaves: 1 in 15 chance to drop sapling
        if (id == MAHOGANY_LEAVES.id && Game.random.Next(15) == 0) {
            return (MAHOGANY_SAPLING.item, 0, 1);
        }

        // mahogany: 1 in 10 chance to drop apple
        if (id == MAHOGANY_LEAVES.id && Game.random.Next(10) == 0) {
            return (Item.APPLE, 0, 1);
        }


        return base.getDrop(world, x, y, z, metadata, canBreak);
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