using System.Numerics;
using BlockGame.util;
using BlockGame.world.block;
using Molten;

namespace BlockGame.world.worldgen.feature;

public class OreFeature : Feature {

    public ushort block;
    public int minCount;
    public int maxCount;

    public OreFeature(ushort block, int minCount, int maxCount) {
        this.block = block;
        this.minCount = minCount;
        this.maxCount = maxCount;
    }

    public override void place(World world, XRandom random, int x, int y, int z) {
        // so we need a somewhat coherent shape

        /*// the size of the shape is a base sigma + slightly bigger if we have more ores
        var s = 1f + count / 8f;

        // get a random offset, biased towards the original
        for (int i = 0; i < count; i++) {
            var xo = random.ApproxGaussian(s);
            var yo = random.ApproxGaussian(s);
            var zo = random.ApproxGaussian(s);
            // cap
            xo = Math.Clamp(xo, -8, 8);
            yo = Math.Clamp(yo, -8, 8);
            zo = Math.Clamp(zo, -8, 8);

            var x1 = x + (int)xo;
            var y1 = y + (int)yo;
            var z1 = z + (int)zo;

            // check if in bounds
            if (y1 < 0 || y1 >= World.WORLDHEIGHT) {
                continue;
            }
            if (world.getBlock(x1, y1, z1) == Block.STONE.id) {
                world.setBlockDumb(x1, y1, z1, block);
            }
        }*/
        
        var count = random.Next(minCount, maxCount + 1);


        // we have *count* ores, we need to distribute them somehow
        if (world.getBlock(x, y, z) != Blocks.STONE) {
            return; // Only start in stone
        }

        // Place first ore block at origin point
        world.setBlockDumb(x, y, z, block);
        
        Queue<Vector3I> expansionQueue = new(count * 2);
        expansionQueue.Enqueue(new Vector3I(x, y, z));

        int placedCount = 1;

        // Continue until we've placed enough ore blocks or run out of valid positions
        while (placedCount < count && expansionQueue.Count > 0) {
            Vector3I current = expansionQueue.Dequeue();

            foreach (var dir in Direction.directions) {
                Vector3I newPos = current + dir;

                // Check world bounds
                if (newPos.Y is < 0 or >= World.WORLDHEIGHT) {
                    continue;
                }

                // Check if it's stone
                if (world.getBlock(newPos.X, newPos.Y, newPos.Z) != Blocks.STONE) {
                    continue;
                }
                
                
                float chance = 0.7f - (Vector3.Distance(new Vector3(x, y, z), new Vector3(newPos.X, newPos.Y, newPos.Z)) * 0.05f);
                chance = Math.Max(0.1f, chance);

                if (random.NextSingle() < chance) {
                    // Place ore (may place over existing ore, but we don't care)
                    world.setBlockDumb(newPos.X, newPos.Y, newPos.Z, block);
                    placedCount++;

                    // Add to expansion queue
                    expansionQueue.Enqueue(newPos);

                    if (placedCount >= count) {
                        break;
                    }
                }
            }
        }
    }
}
