using BlockGame.util;
using BlockGame.world.worldgen.surface;

namespace BlockGame.world.worldgen.generator;

/**
 * Do I look like I know what I'm doing? If you think so, go to the optician plz
 */
public partial class NewWorldGenerator : WorldGenerator {
    public World world;

    public SurfaceGenerator surfacegen;

    public SimplexNoise tn;
    public SimplexNoise t2n;
    public SimplexNoise sn;
    public ExpNoise en;
    public ExpNoise fn;
    public SimplexNoise esn;
    public SimplexNoise fsn;
    public SimplexNoise gn;
    public SimplexNoise mn;
    public SimplexNoise on;
    public SimplexNoise auxn;


    public SimplexNoise tempn;
    public SimplexNoise humn;
    public SimplexNoise agen;
    public SimplexNoise wn;
    public SimplexNoise detailn;

    public XRandom random;
    private readonly int version;

    public NewWorldGenerator(World world, int version) {
        this.world = world;
        surfacegen = new NewSurfaceGenerator(this, world, version);

        this.version = version;
    }

    public void setup(XRandom random, int seed) {
        this.random = random;
        tn = new SimplexNoise(seed);
        t2n = new SimplexNoise(random.Next(seed));
        sn = new SimplexNoise(random.Next(seed));
        var s = random.Next(seed);

        // a noobtrap is making the exp too high, so it's endless plains lol

        // V2
        en = new ExpNoise(s);
        en.setExp(s, float.E, 0.1f);
        s = random.Next(seed);
        fn = new ExpNoise(s);
        fn.setExp(s, Meth.phiF, 0.1f);

        // V3
        esn = new SimplexNoise(s);
        fsn = new SimplexNoise(random.Next(seed));

        gn = new SimplexNoise(random.Next(seed));
        mn = new SimplexNoise(random.Next(seed));

        on = new SimplexNoise(random.Next(seed));

        auxn = new SimplexNoise(random.Next(seed));
        tempn = new SimplexNoise(random.Next(seed));
        humn = new SimplexNoise(random.Next(seed));

        agen = new SimplexNoise(random.Next(seed));
        wn = new SimplexNoise(random.Next(seed));
        detailn = new SimplexNoise(random.Next(seed));

        surfacegen.setup(random, seed);
    }
}