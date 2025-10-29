using Molten;

namespace BlockGame.world.block;

#pragma warning disable CS8618
public class FallingBlock(string name) : Block(name) {
    public override void update(World world, int x, int y, int z) {
        var ym = y - 1;
        bool isSupported = true;
        // if not supported, set flag
        while (world.getBlock(new Vector3I(x, ym, z)) == 0) {
            // decrement Y
            isSupported = false;
            ym--;
        }

        if (!isSupported) {
            world.setBlock(x, y, z, 0);
            world.setBlock(x, ym + 1, z, getID());
        }

        // if sand above, update
        if (world.getBlock(new Vector3I(x, y + 1, z)) == getID()) {
            // if you do an update immediately, it will cause a stack overflow lol
            world.scheduleBlockUpdate(new Vector3I(x, y + 1, z), 1);
        }
    }

    public override void scheduledUpdate(World world, int x, int y, int z) {
        // run a normal update
        update(world, x, y, z);
    }
}