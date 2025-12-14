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
        setHardness(0.25);
        optionalTool[id] = true;

        // only broken by a scythe!
        tool[id] = ToolType.SCYTHE;
        tier[id] = MaterialTier.WOOD;
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        // oak: 1 in 10 chance to drop apple
        if (id == LEAVES.id && Game.random.Next(10) == 0) {
            drops.Add(new ItemStack(Item.APPLE, 1, 0));
        }
        // oak: 1 in 15 chance to drop sapling
        if (id == LEAVES.id && Game.random.Next(15) == 0) {
            drops.Add(new ItemStack(OAK_SAPLING.item, 1, 0));
        }

        // maple leaves: 1 in 15 chance to drop sapling
        if (id == MAPLE_LEAVES.id && Game.random.Next(15) == 0) {
            drops.Add(new ItemStack(MAPLE_SAPLING.item, 1, 0));
        }
        // maple leaves: 1 in 20 chance to drop maple syrup
        if (id == MAPLE_LEAVES.id && Game.random.Next(20) == 0) {
            drops.Add(new ItemStack(Item.MAPLE_SYRUP, 1, 0));
        }

        // mahogany leaves: 1 in 15 chance to drop sapling
        if (id == MAHOGANY_LEAVES.id && Game.random.Next(15) == 0) {
            drops.Add(new ItemStack(MAHOGANY_SAPLING.item, 1, 0));
        }
        // mahogany: 1 in 10 chance to drop apple
        if (id == MAHOGANY_LEAVES.id && Game.random.Next(10) == 0) {
            drops.Add(new ItemStack(Item.PINEAPPLE, 1, 0));
        }

        // PINE: 1 in 15 chance to drop sapling
        if (id == PINE_LEAVES.id && Game.random.Next(15) == 0) {
            drops.Add(new ItemStack(PINE_SAPLING.item, 1, 0));
        }

        // palm: 1 in 10 chance to drop banana
        if (id == PALM_LEAVES.id && Game.random.Next(10) == 0) {
            drops.Add(new ItemStack(Item.BANANA, 1, 0));
        }
        // palm: 1 in 15 chance to drop sapling
        if (id == PALM_LEAVES.id && Game.random.Next(15) == 0) {
            drops.Add(new ItemStack(PALM_SAPLING.item, 1, 0));
        }

        if (canBreak) {
            drops.Add(new ItemStack(getItem(), 1, 0));
        }
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