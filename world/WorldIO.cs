using System.Buffers;
using System.IO.Compression;
using BlockGame.NBT;
using SharpNBT;

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
            if (false && !File.Exists($"level/r{regionCoord.x},{regionCoord.z}.nbt")) {
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
            //var region =
                // open region file, get from cache if it exists
            //    regionCache.TryGetValue(regionCoord, out var _region) ? _region : NbtFile.Read($"level/r{regionCoord.x},{regionCoord.z}.nbt", FormatOptions.LittleEndian, CompressionType.ZLib);
            // write the chunk into the region
            var nbt = serialiseChunkIntoNBT(chunk);
            NBT.NBT.writeFile(nbt, $"level/r{chunk.coord.x},{chunk.coord.z}.nbt");
            // save it back into the file
            //regionCache[regionCoord] = region;
            //NbtFile.Write($"level/r{regionCoord.x},{regionCoord.z}.nbt", region, FormatOptions.LittleEndian, CompressionType.ZLib, CompressionLevel.Optimal);
        }
        regionCache.Clear();
    }

    private static NBTTagCompound serialiseChunkIntoNBT(Chunk chunk) {
        var chunkTag = new NBTTagCompound("chunk");
        chunkTag.addInt("posX", chunk.coord.x);
        chunkTag.addInt("posZ", chunk.coord.z);
        chunkTag.addByte("status", (byte)chunk.status);
        // blocks
        var my = Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT;
        var mx = Chunk.CHUNKSIZE;
        var mz = Chunk.CHUNKSIZE;
        var blocks = ArrayPool<ushort>.Shared.Rent(mx * my * mz);
        var light = ArrayPool<byte>.Shared.Rent(mx * my * mz);
        int index = 0;
        // using YXZ order
        for (int y = 0; y < my; y++) {
            for (int z = 0; z < mz; z++) {
                for (int x = 0; x < mx; x++) {
                    blocks[index] = chunk.getBlock(x, y, z);
                    light[index] = chunk.getLight(x, y, z);
                    index++;
                }
            }
        }
        chunkTag.addUShortArray("blocks", blocks);
        chunkTag.addByteArray("light", light);

        ArrayPool<ushort>.Shared.Return(blocks);
        ArrayPool<byte>.Shared.Return(light);
        return chunkTag;
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