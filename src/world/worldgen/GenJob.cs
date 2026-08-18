using BlockGame.util;
using BlockGame.world.chunk;
using BlockGame.world.worldgen.generator;

namespace BlockGame.world.worldgen;

public sealed class GenJob : XJob {
    public static readonly XJobPool pool = new("Worldgen", XJobPool.defaultWorkers());

    private readonly WorldGenerator gen;
    public Chunk? chunk;

    public GenJob(World world) {
        gen = WorldGenerators.create(world, world.generatorName);
        gen.setup(new XRandom(world.seed), world.seed);
    }

    public override void run() {
        gen.generate(chunk!);
        chunk!.recalc();
    }
}
