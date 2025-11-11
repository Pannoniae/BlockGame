using BlockGame.world.entity;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world.block;

public class FallingBlock(string name) : Block(name) {
    public override void update(World world, int x, int y, int z) {
        // check if block below is air or non-solid
        var blockBelow = world.getBlock(new Vector3I(x, y - 1, z));

        if (blockBelow == 0 || !collision[blockBelow]) {
            // spawn falling block entity
            var meta = world.getBlockMetadata(x, y, z);
            var entity = new FallingBlockEntity(world) {
                blockID = getID(),
                blockMeta = meta,
                position = new Vector3D(x + 0.5, y, z + 0.5),
                velocity = Vector3D.Zero
            };

            world.addEntity(entity);

            world.setBlock(x, y, z, 0);
        }

        // delayed to prevent stack overflow
        world.scheduleBlockUpdate(new Vector3I(x, y + 1, z), 1);
    }

    public override void scheduledUpdate(World world, int x, int y, int z) {
        // run a normal update
        update(world, x, y, z);
    }
}