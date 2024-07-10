using BlockGame.util;
using BlockGame.util.xNBT;

namespace BlockGame;

public class WorldIO {

    //public static Dictionary<RegionCoord, CompoundTag> regionCache = new();

    private const int my = Chunk.CHUNKSIZE;
    private const int mx = Chunk.CHUNKSIZE;
    private const int mz = Chunk.CHUNKSIZE;
    public static FixedArrayPool<ushort> saveBlockPool = new(mx * my * mz);
    public static FixedArrayPool<byte> saveLightPool = new(mx * my * mz);

    public World world;

    public WorldIO(World world) {
        this.world = world;
    }

    public void save(World world, string filename) {
        // save metadata
        if (!Directory.Exists("level")) {
            Directory.CreateDirectory("level");
        }

        var tag = new NBTCompound("world");
        tag.addInt("seed", world.seed);
        tag.addDouble("posX", world.player.position.X);
        tag.addDouble("posY", world.player.position.Y);
        tag.addDouble("posZ", world.player.position.Z);
        NBT.writeFile(tag, $"level/{filename}.xnbt");

        // save chunks
        foreach (var chunk in world.chunks.Values) {
            //var regionCoord = World.getRegionPos(chunk.coord);
            saveChunk(chunk);
        }
        //regionCache.Clear();
    }

    public void saveChunk(Chunk chunk) {
        var nbt = serialiseChunkIntoNBT(chunk);
        NBT.writeFile(nbt, $"level/c{chunk.coord.x},{chunk.coord.z}.xnbt");
    }

    private NBTCompound serialiseChunkIntoNBT(Chunk chunk) {
        var chunkTag = new NBTCompound("chunk");
        chunkTag.addInt("posX", chunk.coord.x);
        chunkTag.addInt("posZ", chunk.coord.z);
        chunkTag.addByte("status", (byte)chunk.status);
        // using YXZ order
        var sectionsTag = new NBTList<NBTCompound>(NBTType.TAG_Compound, "sections");
        for (int sectionY = 0; sectionY < Chunk.CHUNKHEIGHT; sectionY++) {
            var section = new NBTCompound();
            // if empty, just write zeros
            if (chunk.subChunks[sectionY].blocks.inited) {
                section.addByte("inited", 1);

                // add the arrays
                section.addUShortArray("blocks", chunk.subChunks[sectionY].blocks.blocks);
                section.addByteArray("light", chunk.subChunks[sectionY].blocks.light);
            }
            else {
                section.addByte("inited", 0);
            }
            sectionsTag.add(section);
        }
        chunkTag.addListTag("sections", sectionsTag);
        return chunkTag;
    }

    private static Chunk loadChunkFromNBT(World world, NBTCompound nbt) {

        var posX = nbt.getInt("posX");
        var posZ = nbt.getInt("posZ");
        var status = nbt.getByte("status");
        var chunk = new Chunk(world, posX, posZ) {
            status = (ChunkStatus)status
        };
        var sections = nbt.getListTag<NBTCompound>("sections");
        for (int sectionY = 0; sectionY < Chunk.CHUNKHEIGHT; sectionY++) {
            var section = sections.get(sectionY);
            // if not initialised, leave it be
            if (section.getByte("inited") == 0) {
                chunk.subChunks[sectionY].blocks.inited = false;
                continue;
            }

            // init chunk section
            chunk.subChunks[sectionY].blocks.loadInit();

            // blocks
            chunk.subChunks[sectionY].blocks.blocks = section.getUShortArray("blocks");
            chunk.subChunks[sectionY].blocks.light = section.getByteArray("light");
            chunk.subChunks[sectionY].blocks.refreshCounts();
        }
        return chunk;
    }

    public static World load(string filename) {
        var tag = NBT.readFile($"level/{filename}.xnbt");
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
        var chunk = loadChunkFromNBT(world, nbt);


        // if meshed, cap the status so it's not meshed (otherwise VAO is not created -> crash)
        if (chunk.status >= ChunkStatus.MESHED) {
            chunk.status = ChunkStatus.LIGHTED;
        }
        world.addChunk(chunk.coord, chunk);
    }
}