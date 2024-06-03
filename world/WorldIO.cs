using System.IO.Compression;
using SharpNBT;
using Silk.NET.SDL;

namespace BlockGame;

public class WorldIO {

    public static Dictionary<RegionCoord, CompoundTag> regionCache = new();

    public World world;

    public WorldIO(World world) {
        this.world = world;
    }

    public static void save(World world, string filename) {
        // save metadata
        if (!Directory.Exists("level")) {
            Directory.CreateDirectory("level");
        }

        var tag = new TagBuilder("world");
        tag.AddDouble("posX", world.player.position.X);
        tag.AddDouble("posY", world.player.position.Y);
        tag.AddDouble("posZ", world.player.position.Z);

        var fileTag = tag.Create();
        NbtFile.Write($"level/{filename}.nbt", fileTag, FormatOptions.LittleEndian, CompressionType.ZLib, CompressionLevel.Optimal);

        // save regions
        foreach (var chunk in world.chunks.Values) {
            // if region file does not exist, write
            var regionCoord = World.getRegionPos(chunk.coord);
            if (!File.Exists($"level/r{regionCoord.x},{regionCoord.z}.nbt")) {
                // create file
                var regionTag = new TagBuilder("chunk");
                regionTag.BeginList(TagType.Compound, "chunks");
                regionTag.BeginCompound("test");
                regionTag.AddByte("test", 5);
                regionTag.EndCompound();
                regionTag.EndList();
                var regionToWrite = regionTag.Create();
                regionCache[regionCoord] = regionToWrite;
                NbtFile.Write($"level/r{regionCoord.x},{regionCoord.z}.nbt", regionToWrite, FormatOptions.LittleEndian, CompressionType.ZLib, CompressionLevel.Optimal);
            }
            //Console.Out.WriteLine(regionCoord);
            //Console.Out.WriteLine($"level/r{regionCoord.x},{regionCoord.z}.nbt");
            var region =
                // open region file, get from cache if it exists
                regionCache.TryGetValue(regionCoord, out var _region) ? _region : NbtFile.Read($"level/r{regionCoord.x},{regionCoord.z}.nbt", FormatOptions.LittleEndian, CompressionType.ZLib);
            var chunks = region.Get<ListTag>("chunks");
            // write the chunk into the region
            writeChunkIntoTag(chunks, chunk);

            //Console.Out.WriteLine(region);
            //Console.Out.WriteLine(region.Get<ListTag>("chunks"));
            // save it back into the file
            regionCache[regionCoord] = region;
            NbtFile.Write($"level/r{regionCoord.x},{regionCoord.z}.nbt", region, FormatOptions.LittleEndian, CompressionType.ZLib, CompressionLevel.Optimal);
        }
        regionCache.Clear();
    }

    private static void writeChunkIntoTag(ListTag chunks, Chunk chunk) {
        var chunkTag = new TagBuilder("chunk");
        chunkTag.AddInt("posX", chunk.coord.x);
        chunkTag.AddInt("posZ", chunk.coord.z);
        chunkTag.AddByte("status", (byte)chunk.status);
        // blocks
        var my = Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT;
        var mx = Chunk.CHUNKSIZE;
        var mz = Chunk.CHUNKSIZE;
        var blocks = new int[mx * my * mz];
        int index = 0;
        // using YXZ order
        for (int y = 0; y < my; y++) {
            for (int z = 0; z < mz; z++) {
                for (int x = 0; x < mx; x++) {
                    blocks[index] = chunk.getBlock(x, y, z) | chunk.getLight(x, y, z) << 16;
                    //tag.AddShort(chunk.getBlock(x, y, z));
                    index++;
                }
            }
        }
        chunkTag.AddIntArray("blocks", blocks);
        chunkTag.EndCompound();
        chunks.Add(chunkTag.Create());
    }

    public static World load(string filename) {
        CompoundTag tag = NbtFile.Read($"level/{filename}.nbt", FormatOptions.LittleEndian, CompressionType.ZLib);
        var seed = tag.Get<IntTag>("seed");
        var world = new World(seed);
        var chunkTags = tag.Get<ListTag>("chunks");
        foreach (var chunkTag in chunkTags) {
            var chunk = (CompoundTag)chunkTag;
            int chunkX = chunk.Get<IntTag>("posX").Value;
            int chunkZ = chunk.Get<IntTag>("posZ").Value;
            var status = chunk.Get<ByteTag>("status").Value;
            world.chunks[new ChunkCoord(chunkX, chunkZ)] = new Chunk(world, chunkX, chunkZ);
            var blocks = chunk.Get<IntArrayTag>("blocks");
            int index = 0;
            for (int y = 0; y < Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
                for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                    for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                        var value = blocks[index];
                        world.chunks[new ChunkCoord(chunkX, chunkZ)].setBlock(x, y, z, (ushort)(value & 0xFFFF));
                        world.chunks[new ChunkCoord(chunkX, chunkZ)].setLight(x, y, z, (byte)((value >> 16) & 0xFF));
                        index++;
                    }
                }
            }
            world.chunks[new ChunkCoord(chunkX, chunkZ)].status = (ChunkStatus)status;
        }

        world.player.position.X = tag.Get<DoubleTag>("posX");
        world.player.position.Y = tag.Get<DoubleTag>("posY");
        world.player.position.Z = tag.Get<DoubleTag>("posZ");
        world.player.prevPosition = world.player.position;

        //world.renderer.meshChunks();
        return world;
    }

}