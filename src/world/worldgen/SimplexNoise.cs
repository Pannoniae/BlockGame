namespace BlockGame.world.worldgen;

public class SimplexNoise {
    public long seed;

    public SimplexNoise(long seed) {
        this.seed = seed;
    }

    public float noise3_XZBeforeY(double x, double y, double z) {
        // idk I like my gradients fucked!
        return OpenSimplex2.Noise3_Fallback(seed, x, y, z);
    }

    /*public void noise3_XZBeforeY(float[] buffer, double x, double y, double z, double xs, double ys, double zs) {
        OpenSimplex2.Noise3_ImproveXZ(seed, buffer, x, y, z, xs, ys, zs);
    }*/

    public float noise3_XYBeforeZ(double x, double y, double z) {
        return OpenSimplex2.Noise3_Fallback(seed, x, y, z);
    }

    public float noise2(double x, double y) {
        return OpenSimplex2.Noise2(seed, x, y);
    }
}