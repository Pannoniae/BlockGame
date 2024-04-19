using System.IO.Compression;
using SharpNBT;

namespace BlockGame;

public class WorldIO {
    public World world;

    public WorldIO(World world) {
        this.world = world;
    }

    public static void save(World world, string filename) {
        if (!Directory.Exists("world")) {
            Directory.CreateDirectory("world");
        }

        var tag = new TagBuilder("world");
        tag.AddDouble("posX", world.player.position.X);
        tag.AddDouble("posY", world.player.position.Y);
        tag.AddDouble("posZ", world.player.position.Z);
        tag.BeginList(TagType.Compound, "chunks");
        foreach (var chunk in world.chunks.Values) {
            tag.BeginCompound("chunk");
            tag.AddInt("posX", chunk.coord.x);
            tag.AddInt("posZ", chunk.coord.z);
            // blocks
            var my = Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT;
            var mx = Chunk.CHUNKSIZE;
            var mz = Chunk.CHUNKSIZE;
            //var blocks = new int[mx * my * mz];
            int index = 0;
            tag.BeginList(TagType.Short, "blocks");
            // using YXZ order
            for (int y = 0; y < my; y++) {
                for (int x = 0; x < mx; x++) {
                    for (int z = 0; z < mz; z++) {
                        //blocks[index] = chunk.blocks[x, y, z];
                        tag.AddShort(chunk.blocks[x, y, z]);
                        index++;
                    }
                }
            }
            tag.EndList();
            //tag.AddIntArray("blocks", blocks);
            tag.EndCompound();
        }

        tag.EndList();
        var fileTag = tag.Create();
        NbtFile.Write($"world/{filename}.nbt", fileTag, FormatOptions.LittleEndian, CompressionType.ZLib, CompressionLevel.Optimal);
    }

    public static World load(string filename) {
        CompoundTag tag = NbtFile.Read($"world/{filename}.nbt", FormatOptions.LittleEndian, CompressionType.ZLib);
        var seed = tag.Get<IntTag>("seed");
        var world = new World(seed);
        var chunkTags = tag.Get<ListTag>("chunks");
        foreach (var chunkTag in chunkTags) {
            var chunk = (CompoundTag)chunkTag;
            int chunkX = chunk.Get<IntTag>("posX").Value;
            int chunkZ = chunk.Get<IntTag>("posZ").Value;
            world.chunks[new ChunkCoord(chunkX, chunkZ)] = new Chunk(world, chunkX, chunkZ);
            var blocks = chunk.Get<ListTag>("blocks");
            int index = 0;
            for (int y = 0; y < Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                        world.chunks[new ChunkCoord(chunkX, chunkZ)].blocks[x, y, z] = (ShortTag)blocks[index];
                        index++;
                    }
                }
            }
        }

        world.player.position.X = tag.Get<DoubleTag>("posX");
        world.player.position.Y = tag.Get<DoubleTag>("posY");
        world.player.position.Z = tag.Get<DoubleTag>("posZ");
        world.player.prevPosition = world.player.position;

        //world.renderer.meshChunks();
        return world;
    }

}