using System.Buffers;
using BlockGame.xNBT;
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

        var tag = new NBTTagCompound("world");
        tag.addDouble("posX", world.player.position.X);
        tag.addDouble("posY", world.player.position.Y);
        tag.addDouble("posZ", world.player.position.Z);
        NBT.writeFile(tag, $"level/{filename}.nbt");

        // save regions
        foreach (var chunk in world.chunks.Values) {
            var regionCoord = World.getRegionPos(chunk.coord);
            var nbt = serialiseChunkIntoNBT(chunk);
            NBT.writeFile(nbt, $"level/r{chunk.coord.x},{chunk.coord.z}.nbt");
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