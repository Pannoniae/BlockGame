using BlockGame.util;

namespace BlockGame.world.worldgen.feature;

public abstract class Feature {
    public abstract void place(World world, XRandom random, int x, int y, int z);
    
}
