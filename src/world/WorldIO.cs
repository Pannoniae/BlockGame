using System.Runtime.CompilerServices;
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

    public void save(World world, string filename, bool saveChunks = true) {
        // save metadata
        // create level folder
        if (!Directory.Exists($"level/{filename}")) {
            Directory.CreateDirectory($"level/{filename}");
        }

        saveWorldData();

        // save chunks
        if (saveChunks) {
            foreach (var chunk in world.chunks.Values) {
                //var regionCoord = World.getRegionPos(chunk.coord);
                saveChunk(world, chunk);
            }
        }
        //regionCache.Clear();
    }

    public void saveWorldData() {
        var tag = new NBTCompound("");
        tag.addInt("seed", world.seed);
        tag.addDouble("posX", world.player.position.X);
        tag.addDouble("posY", world.player.position.Y);
        tag.addDouble("posZ", world.player.position.Z);
        tag.addInt("time", world.worldTick);
        NBT.writeFile(tag, $"level/{world.name}/level.xnbt");
        Console.Out.WriteLine($"Saved world data to level/{world.name}/level.xnbt");
    }

    public void saveChunk(World world, Chunk chunk) {
        chunk.lastSaved = (ulong)Game.permanentStopwatch.ElapsedMilliseconds;
        var nbt = serialiseChunkIntoNBT(chunk);
        // ensure directory is created
        var pathStr = getChunkString(world.name, chunk.coord);
        Directory.CreateDirectory(Path.GetDirectoryName(pathStr) ?? string.Empty);
        NBT.writeFile(nbt, pathStr);
    }

    public static void deleteLevel(string level) {
        Directory.Delete($"level/{level}", true);
    }

    private NBTCompound serialiseChunkIntoNBT(Chunk chunk) {
        var chunkTag = new NBTCompound();
        chunkTag.addInt("posX", chunk.coord.x);
        chunkTag.addInt("posZ", chunk.coord.z);
        chunkTag.addByte("status", (byte)chunk.status);
        chunkTag.addULong("lastSaved", chunk.lastSaved);
        // using YXZ order
        var sectionsTag = new NBTList<NBTCompound>(NBTType.TAG_Compound, "sections");
        for (int sectionY = 0; sectionY < Chunk.CHUNKHEIGHT; sectionY++) {
            var section = new NBTCompound();
            // if empty, just write zeros
            if (chunk.blocks[sectionY].inited) {
                section.addByte("inited", 1);

                // add the arrays
                section.addUShortArray("blocks", chunk.blocks[sectionY].blocks);
                section.addByteArray("light", chunk.blocks[sectionY].light);
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
        var lastSaved = nbt.getULong("lastSaved");
        var chunk = new Chunk(world, posX, posZ) {
            status = (ChunkStatus)status,
            lastSaved = lastSaved
        };
        var sections = nbt.getListTag<NBTCompound>("sections");
        for (int sectionY = 0; sectionY < Chunk.CHUNKHEIGHT; sectionY++) {
            var section = sections.get(sectionY);
            var blocks = chunk.blocks[sectionY];
            // if not initialised, leave it be
            if (section.getByte("inited") == 0) {
                blocks.inited = false;
                continue;
            }

            // init chunk section
            blocks.loadInit();

            // blocks
            blocks.blocks = section.getUShortArray("blocks");
            blocks.light = section.getByteArray("light");
            blocks.refreshCounts();
        }

        /*var file = "chunk.xnbt";
        if (File.Exists(file)) {
            File.Delete(file);
        }

        SNBT.writeToFile(nbt, file, prettyPrint: true);*/
        return chunk;
    }

    public static World load(string filename) {
        Console.Out.WriteLine($"Loaded data from level/{filename}/level.xnbt");
        var tag = NBT.readFile($"level/{filename}/level.xnbt");
        var seed = tag.getInt("seed");
        var world = new World(filename, seed);
        world.init(true);
        Console.Out.WriteLine(tag.getDouble("posX"));
        Console.Out.WriteLine(tag.getDouble("posY"));
        Console.Out.WriteLine(tag.getDouble("posZ"));
        world.player.position.X = tag.getDouble("posX");
        world.player.position.Y = tag.getDouble("posY");
        world.player.position.Z = tag.getDouble("posZ");
        world.worldTick = tag.has("time") ? tag.getInt("time") : 0;

        world.player.prevPosition = world.player.position;

        // dump nbt into file
        /*var file = "dump.xnbt";
        if (File.Exists(file)) {
            File.Delete(file);
        }

        SNBT.writeToFile(tag, file, prettyPrint: true);*/

        return world;
    }


    /// <summary>
    /// Gets the path for saving/loading a chunk.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string getChunkString(string levelname, ChunkCoord coord) {
        // Organises chunks into directories (chunk coord divided by 32, converted into base 64)
        var x = coord.x >> 5;
        var z = coord.z >> 5;
        var xDir = x < 0 ? $"-{-x:X}" : x.ToString("X");
        var zDir = z < 0 ? $"-{-z:X}" : z.ToString("X");
        return $"level/{levelname}/{xDir}/{zDir}/c{coord.x},{coord.z}.xnbt";
    }

    public static bool chunkFileExists(string levelname, ChunkCoord coord) {
        return File.Exists(getChunkString(levelname, coord));
    }

    public static bool worldExists(string level) {
        return File.Exists($"level/{level}/level.xnbt");
    }

    public static Chunk loadChunkFromFile(World world, ChunkCoord coord) {
        return loadChunkFromFile(world, getChunkString(world.name, coord));
    }

    public static Chunk loadChunkFromFile(World world, string file) {
        var nbt = NBT.readFile(file);
        var chunk = loadChunkFromNBT(world, nbt);


        // if meshed, cap the status so it's not meshed (otherwise VAO is not created -> crash)
        if (chunk.status >= ChunkStatus.MESHED) {
            chunk.status = ChunkStatus.LIGHTED;
        }

        return chunk;
    }
}