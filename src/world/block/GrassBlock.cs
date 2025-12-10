using BlockGame.util;

namespace BlockGame.world.block;

#pragma warning disable CS8618
public class GrassBlock(string name) : Block(name) {
    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        // grass drops dirt
        drops.Add(new ItemStack(DIRT.getItem(), 1, 0));
    }

    public override void randomUpdate(World world, int x, int y, int z) {
        // turn to dirt if full block above
        if (y < World.WORLDHEIGHT - 1 && isFullBlock(world.getBlock(x, y + 1, z))) {
            world.setBlock(x, y, z, DIRT.id);
            return;
        }

        // spread grass to nearby dirt
        // try 3 times!
        for (int i = 0; i < 3; i++) {
            var r = world.random.Next(27); // 3x3x3
            int dx = (r % 3) - 1;
            int dy = ((r / 3) % 3) - 1;
            int dz = (r / 9) - 1;

            int nx = x + dx;
            int ny = y + dy;
            int nz = z + dz;

            // spread to dirt or unseeded farmland (with air above)
            var targetBlock = world.getBlock(nx, ny, nz);
            if (targetBlock == DIRT.id || targetBlock == FARMLAND.id) {
                if (ny < World.WORLDHEIGHT - 1 && world.getBlock(nx, ny + 1, nz) == AIR.id) {
                    // only spreads if air above (crops block spreading to seeded farmland)
                    world.setBlock(nx, ny, nz, id);
                }
            }
        }
    }

    public override UVPair getTexture(int faceIdx, int metadata) {
        return faceIdx switch {
            // top: uv[0], bottom: uv[1], sides: uv[2]
            5 => uvs[0],
            4 => uvs[2],
            _ => uvs[1]
        };
    }
}