using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BlockGame.util;
using BlockGame.util.xNBT;

namespace BlockGame;

public class WorldIO {
    //public static Dictionary<RegionCoord, CompoundTag> regionCache = new();

    private const int my = Chunk.CHUNKSIZE;
    private const int mx = Chunk.CHUNKSIZE;
    private const int mz = Chunk.CHUNKSIZE;

    public World world;
    
    private readonly ChunkSaveThread chunkSaveThread;
    
    // Background saving
    public readonly ManualResetEvent shutdownEvent = new(false);
    private volatile bool isDisposed;

    public WorldIO(World world) {
        this.world = world;
        chunkSaveThread = new ChunkSaveThread(this);
        
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

    public void saveChunkAsync(World world, Chunk chunk) {
        if (isDisposed) {
            // fallback to sync save if disposed
            saveChunk(world, chunk);
            return;
        }
        
        chunk.lastSaved = (ulong)Game.permanentStopwatch.ElapsedMilliseconds;
        var nbt = serialiseChunkIntoNBT(chunk);
        var pathStr = getChunkString(world.name, chunk.coord);
        
        chunkSaveThread.add(new ChunkSaveData(nbt, pathStr, chunk.lastSaved));
    }

    public void Dispose() {
        if (isDisposed) return;
        isDisposed = true;
        
        // wait for the save thread to finish
        chunkSaveThread.Dispose();
        shutdownEvent.Dispose();
    }

    public static void deleteLevel(string level) {
        Directory.Delete($"level/{level}", true);
    }

    public static NBTCompound serialiseChunkIntoNBT(Chunk chunk) {
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
                section.addUIntArray("blocks", chunk.blocks[sectionY].blocks);
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

    public static Chunk loadChunkFromNBT(World world, NBTCompound nbt) {
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
            blocks.blocks = section.getUIntArray("blocks");
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


public struct ChunkSaveData(NBTCompound nbt, string path, ulong lastSave) {
    public readonly NBTCompound nbt = nbt;
    public readonly string path = path;
    public ulong lastSave = lastSave;
}

public class ChunkSaveThread : IDisposable {
    private readonly WorldIO io;
    private readonly Thread saveThread;
    
    private volatile bool isDisposed;
    
    private readonly ConcurrentQueue<ChunkSaveData> saveQueue = new();

    public ChunkSaveThread(WorldIO io) {
        this.io = io;
        saveThread = new Thread(saveLoop) {
            IsBackground = true,
            Name = "ChunkSaveThread"
        };
        saveThread.Start();
    }

    public void Join() {
        saveThread.Join();
    }
    
    private void saveLoop() {
        try {
            while (!io.shutdownEvent.WaitOne(0)) {
                if (saveQueue.TryDequeue(out var saveData)) {
                    try {
                        // ensure directory is created
                        Directory.CreateDirectory(Path.GetDirectoryName(saveData.path) ?? string.Empty);
                        NBT.writeFile(saveData.nbt, saveData.path);
                    }
                    catch (Exception ex) {
                        Console.Error.WriteLine($"Failed to save chunk to {saveData.path}: {ex}");
                    }
                }
                else {
                    // no chunks to save, wait a bit or until shutdown
                    io.shutdownEvent.WaitOne(10);
                }
            }
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Background save loop error: {ex}");
        }
    }

    public void Dispose() {
        if (isDisposed) return;
        isDisposed = true;
        
        // wait for the thread to finish
        // we first do this to ensure no new saves are added
        io.shutdownEvent.Set();
        try {
            saveThread.Join(5000); // wait up to 5 seconds
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"Error waiting for save thread to complete: {ex}");
        }
        
        // process remaining saves synchronously
        while (saveQueue.TryDequeue(out var saveData)) {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(saveData.path) ?? string.Empty);
                NBT.writeFile(saveData.nbt, saveData.path);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Failed to save chunk during dispose: {ex}");
            }
        }
    }

    public void add(ChunkSaveData chunk) {
        saveQueue.Enqueue(chunk);
    }
}