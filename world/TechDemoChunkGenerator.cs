namespace BlockGame;

public class TechDemoChunkGenerator : ChunkGenerator {

    public Chunk chunk;
    public World world;
    public TechDemoWorldGenerator generator;

    public TechDemoChunkGenerator(TechDemoWorldGenerator generator, Chunk chunk) {
        this.generator = generator;
        this.chunk = chunk;
        this.world = chunk.world;
    }

    public void generate() {
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = world.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                // -1 to 1
                // transform to the range 10 - 30
                var height = generator.noise.GetNoise(worldPos.X, worldPos.Z) * 20 + 20;
                for (int y = 0; y < height - 1; y++) {
                    chunk.setBlock(x, y, z, Blocks.DIRT.id, false);
                }
                chunk.setBlock(x, (int)height, z, Blocks.GRASS.id, false);
            }
        }
        chunk.status = ChunkStatus.GENERATED;
    }

    public void populate() {
        chunk.status = ChunkStatus.POPULATED;
    }}