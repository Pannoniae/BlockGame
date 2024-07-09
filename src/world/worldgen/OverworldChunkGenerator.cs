using BlockGame.util;
using Silk.NET.Maths;

namespace BlockGame;

public class OverworldChunkGenerator : ChunkGenerator {

    public const int WATER_LEVEL = 64;

    public OverworldWorldGenerator generator;

    public OverworldChunkGenerator(OverworldWorldGenerator generator) {
        this.generator = generator;
    }

    public void generate(ChunkCoord coord) {
        var world = generator.world;
        var chunk = world.getChunk(coord);
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {

                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                // -1 to 1
                // transform to the range -25 to 25, add 80 for 50 - 105
                var aux = generator.getNoise(generator.auxNoise, worldPos.X, worldPos.Z, 1, 0.5f);
                var mountainness = MathF.Pow((generator.getNoise(generator.terrainNoise2, worldPos.X, worldPos.Z, 1, 0.5f) + 1) / 2f, 2);
                var flatNoise = generator.getNoise(generator.terrainNoise, worldPos.X / 3f + aux * 5 + 1, worldPos.Z / 3f + aux * 5 + 1, 5, 0.5f);
                //Console.Out.WriteLine(flatNoise);
                // \sin\left(x\right)\cdot\ 0.8+\operatorname{sign}\left(x\right)\cdot x\cdot0.3
                // sin(x) * 0.8 + sign(x) * x * 0.3
                // this rescales the noise so there's more above than below
                flatNoise = MathF.Sin(flatNoise) * 0.8f + MathF.Sign(flatNoise) * flatNoise * 0.3f;
                //Console.Out.WriteLine(mountainness);
                flatNoise *= mountainness * 64;
                flatNoise += 64;
                //Console.Out.WriteLine(flatNoise);
                
                chunk.setBlockFast(x, 0, z, Blocks.HELLSTONE.id);
                // hack until we can propagate them properly AND cheaply
                chunk.setBlockLight(x, 0, z, Blocks.HELLSTONE.lightLevel);

                for (int y = 1; y < World.WORLDHEIGHT * Chunk.CHUNKSIZE; y++) {
                    if (y < flatNoise) {
                        chunk.setBlockFast(x, y, z, Blocks.STONE.id);
                        // set heightmap
                        chunk.addToHeightMap(x, y, z);
                    }
                    else {
                        break;
                    }
                }
                int height = chunk.heightMap.get(x, z);
                // replace top layers with dirt
                var amt = generator.getNoise(generator.auxNoise2, worldPos.X, worldPos.Z, 1, 0.5f) + 2.5;
                for (int yy = height - 1; yy > height - 1 - amt; yy--) {
                    chunk.setBlockFast(x, yy, z, Blocks.DIRT.id);
                }

                // water if low
                if (height < WATER_LEVEL - 1) {
                    for (int y2 = height; y2 < WATER_LEVEL; y2++) {
                        chunk.setBlockFast(x, y2, z, Blocks.WATER.id);
                    }
                    // put sand on the lake floors
                    chunk.setBlockFast(x, height, z, generator.getNoise2(worldPos.X, worldPos.Z) > 0 ? Blocks.SAND.id : Blocks.DIRT.id);
                }
                else {
                    chunk.setBlockFast(x, height, z, Blocks.GRASS.id);
                }
            }
        }
        foreach (var subChunk in chunk.subChunks) {
            if (subChunk.blocks.inited) {
                subChunk.blocks.refreshCounts();
            }
        }
        chunk.status = ChunkStatus.GENERATED;
    }

    public void populate(ChunkCoord coord) {
        var random = getRandom(coord);
        var world = generator.world;
        var chunk = world.getChunk(coord);

        // TREES
        var treeCount = Math.Pow(generator.treenoise.GetNoise(chunk.worldX / 16f, chunk.worldZ / 16f), 3) * 4;
        for (int i = 0; i < treeCount; i++) {
            var randomPos = random.Next(16 * 16);
            var x = randomPos >> 4;
            var z = randomPos & 0xF;
            var height = chunk.heightMap.get(x, z);
            var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, (int)(height + 1), z);
            if ((height < 64 && random.NextSingle() < 0.25) || !(height < 64)) {
                placeTree(worldPos.X, worldPos.Y, worldPos.Z);
            }
        }
        chunk.status = ChunkStatus.POPULATED;
    }

    public Random getRandom(ChunkCoord coord) {
        return new Random(coord.GetHashCode());
    }

    // Can place in neighbouring chunks, so they must be loaded first
    private void placeTree(int x, int y, int z) {
        var world = generator.world;
        // tree
        for (int i = 0; i < 7; i++) {
            world.setBlock(x, y + i, z, Blocks.LOG.id);
        }
        // leaves, thick
        for (int x1 = -2; x1 <= 2; x1++) {
            for (int z1 = -2; z1 <= 2; z1++) {
                // don't overwrite the trunk
                if (x1 == 0 && z1 == 0) {
                    continue;
                }
                for (int y1 = 4; y1 < 6; y1++) {
                    world.setBlock(x + x1, y + y1, z + z1, Blocks.LEAVES.id);
                }
            }
        }
        // leaves, thin on top
        for (int x2 = -1; x2 <= 1; x2++) {
            for (int z2 = -1; z2 <= 1; z2++) {
                for (int y2 = 6; y2 <= 7; y2++) {
                    // don't overwrite the trunk
                    if (x2 == 0 && z2 == 0 && y2 == 6) {
                        continue;
                    }
                    world.setBlock(x + x2, y + y2, z + z2, Blocks.LEAVES.id);
                }
            }
        }
    }
}