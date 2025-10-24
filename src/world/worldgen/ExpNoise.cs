using BlockGame.util.meth.noise;

namespace BlockGame.world.worldgen;

/**
 * Wrap a OpenSimplex2F-Exp noise generator to provide a more user-friendly interface. (that one is all static)
 */
public class ExpNoise {
    public long seed;


    public ExpNoise(long seed) {
        this.seed = seed;
    }

    public void setExp(long seed, float expBase, float expMin) {
        OpenSimplex2S_Exp.initExp(seed, expBase, expMin);
    }

    public float noise2(float x, float y) {
        return OpenSimplex2S_Exp.noise2(seed, x, y);
    }

    public float noise3_XZBeforeY(float x, float y, float z) {
        return OpenSimplex2S_Exp.noise3_XZBeforeY(seed, x, y, z);
    }

    public float noise3_XYBeforeZ(double x, double y, double z) {
        return OpenSimplex2S_Exp.noise3_XYBeforeZ(seed, x, y, z);
    }
}