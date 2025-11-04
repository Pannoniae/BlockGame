using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class FireBlock(string name) : Block(name) {

    public override void update(World world, int x, int y, int z) {

    }

    public override void randomUpdate(World world, int x, int y, int z) {

    }

    public override (Item item, byte metadata, int count) getDrop(World world, int x, int y, int z, byte metadata) {
        return (null!, 0, 0); // fire doesn't drop anything
    }
}