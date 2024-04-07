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
        foreach (var chunk in world.chunks) {
            tag.BeginCompound("chunk");
            tag.AddInt("posX", chunk.x);
            tag.AddInt("posZ", chunk.z);
            // blocks
            var my = ChunkSection.CHUNKSIZE * Chunk.CHUNKHEIGHT;
            var mx = ChunkSection.CHUNKSIZE;
            var mz = ChunkSection.CHUNKSIZE;
            //var blocks = new int[mx * my * mz];
            int index = 0;
            tag.BeginList(TagType.Short, "blocks");
            // using YXZ order
            for (int y = 0; y < my; y++) {
                for (int x = 0; x < mx; x++) {
                    for (int z = 0; z < mz; z++) {
                        //blocks[index] = chunk.block[x, y, z];
                        tag.AddShort(chunk.block[x, y, z]);
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
        var world = new World(true);
        var chunkTags = tag.Get<ListTag>("chunks");
        foreach (var chunkTag in chunkTags) {
            var chunk = (CompoundTag)chunkTag;
            int chunkX = chunk.Get<IntTag>("posX").Value;
            int chunkZ = chunk.Get<IntTag>("posZ").Value;
            world.chunks[chunkX, chunkZ] = new Chunk(world, chunkX, chunkZ);
            var blocks = chunk.Get<ListTag>("blocks");
            int index = 0;
            for (int y = 0; y < ChunkSection.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
                for (int x = 0; x < ChunkSection.CHUNKSIZE; x++) {
                    for (int z = 0; z < ChunkSection.CHUNKSIZE; z++) {
                        world.chunks[chunkX, chunkZ].block[x, y, z] = (ShortTag)blocks[index];
                        index++;
                    }
                }
            }
        }

        world.player.position.X = tag.Get<DoubleTag>("posX");
        world.player.position.Y = tag.Get<DoubleTag>("posY");
        world.player.position.Z = tag.Get<DoubleTag>("posZ");
        world.player.prevPosition = world.player.position;

        world.renderer.meshChunks();
        return world;
    }

}