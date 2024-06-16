using BlockGame.util;
using BlockGame.util.xNBT;

namespace BlockGame;

public class WorldIO {

    //public static Dictionary<RegionCoord, CompoundTag> regionCache = new();

    private const int my = Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT;
    private const int mx = Chunk.CHUNKSIZE;
    private const int mz = Chunk.CHUNKSIZE;
    public static FixedArrayPool<ushort> saveBlockPool = new FixedArrayPool<ushort>(mx * my * mz);
    public static FixedArrayPool<byte> saveLightPool = new FixedArrayPool<byte>(mx * my * mz);

    public World world;

    public WorldIO(World world) {
        this.world = world;
    }

    public void save(World world, string filename) {
        // save metadata
        if (!Directory.Exists("level")) {
            Directory.CreateDirectory("level");
        }

        var tag = new NBTTagCompound("world");
        tag.addInt("seed", world.seed);
        tag.addDouble("posX", world.player.position.X);
        tag.addDouble("posY", world.player.position.Y);
        tag.addDouble("posZ", world.player.position.Z);
        NBT.writeFile(tag, $"level/{filename}.nbt");

        // save chunks
        foreach (var chunk in world.chunks.Values) {
            var regionCoord = World.getRegionPos(chunk.coord);
            saveChunk(chunk);
        }
        //regionCache.Clear();
    }

    public void saveChunk(Chunk chunk) {
        if (!Directory.Exists("level")) {
            Directory.CreateDirectory("level");
        }
        var nbt = serialiseChunkIntoNBT(chunk);
        NBT.writeFile(nbt, $"level/c{chunk.coord.x},{chunk.coord.z}.nbt");
    }

    private NBTTagCompound serialiseChunkIntoNBT(Chunk chunk) {
        var chunkTag = new NBTTagCompound("chunk");
        chunkTag.addInt("posX", chunk.coord.x);
        chunkTag.addInt("posZ", chunk.coord.z);
        chunkTag.addByte("status", (byte)chunk.status);
        // blocks
        var blocks = saveBlockPool.grab();
        var light = saveLightPool.grab();
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

        saveBlockPool.putBack(blocks);
        saveLightPool.putBack(light);
        return chunkTag;
    }

    private Chunk loadChunkFromNBT(NBTTagCompound nbt) {

        var posX = nbt.getInt("posX");
        var posZ = nbt.getInt("posZ");
        var status = nbt.getByte("status");
        var chunk = new Chunk(world, posX, posZ) {
            status = (ChunkStatus)status
        };
        // blocks
        var my = Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT;
        var mx = Chunk.CHUNKSIZE;
        var mz = Chunk.CHUNKSIZE;
        var blocks = nbt.getUShortArray("blocks");
        var light = nbt.getByteArray("light");

        int index = 0;
        // using YXZ order
        for (int y = 0; y < my; y++) {
            for (int z = 0; z < mz; z++) {
                for (int x = 0; x < mx; x++) {
                    chunk.setBlock(x, y, z, blocks[index]);
                    chunk.setLight(x, y, z, light[index]);
                    index++;
                }
            }
        }
        return chunk;
    }

    public static World load(string filename) {
        var tag = NBT.readFile($"level/{filename}.nbt");
        var seed = tag.getInt("seed");
        var world = new World(seed);
        world.player.position.X = tag.getDouble("posX");
        world.player.position.Y = tag.getDouble("posY");
        world.player.position.Z = tag.getDouble("posZ");

        // go over all chunks in the directory
        foreach (var file in Directory.EnumerateFiles("level")) {
            // it's a chunk file
            var name = Path.GetFileName(file);
            if (name.StartsWith('c')) {
                loadChunkFromFile(world, file);
            }
        }
        world.player.prevPosition = world.player.position;
        world.loadAroundPlayer();
        return world;
    }

    private static void loadChunkFromFile(World world, string file) {
        var nbt = NBT.readFile(file);
        var chunk = world.worldIO.loadChunkFromNBT(nbt);

        // if meshed, cap the status so it's not meshed (otherwise VAO is not created -> crash)
        if (chunk.status >= ChunkStatus.MESHED) {
            chunk.status = ChunkStatus.LIGHTED;
        }
        world.chunks[chunk.coord] = chunk;
    }
}