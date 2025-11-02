using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BlockGame.logic;
using BlockGame.main;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.util.stuff;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public class WorldIO {
    public static readonly FixedArrayPool<uint> saveBlockPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);
    public static readonly FixedArrayPool<byte> saveLightPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);

    // palette building
    private static readonly XMap<ushort, int> paletteDict = new(256);
    private static readonly XUList<string> paletteList = new(256);

    // blocks can change at runtime though? maybe, idk, but don't assume that plz


    public World world;

    private readonly ChunkSaveThread chunkSaveThread;
    private readonly ChunkLoadThread chunkLoadThread;

    // Background saving and loading
    public readonly ManualResetEvent shutdownEvent = new(false);
    private volatile bool isDisposed;

    public WorldIO(World world) {
        this.world = world;
        chunkSaveThread = new ChunkSaveThread(this);
        chunkLoadThread = new ChunkLoadThread(this);
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
            foreach (var chunk in world.chunks) {
                //var regionCoord = World.getRegionPos(chunk.coord);
                saveChunk(world, chunk);
            }
        }
        //regionCache.Clear();
    }

    public void saveWorldData() {
        var tag = new NBTCompound("");
        tag.addInt("seed", world.seed);
        tag.addInt("time", world.worldTick);
        tag.addString("displayName", world.displayName);
        tag.addString("gamemode", Game.gamemode == GameMode.survival ? "survival" : "creative");
        tag.addLong("lastPlayed", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        tag.addString("generator", world.generatorName);

        // add player spawn
        tag.addDouble("spawnX", world.spawn.X);
        tag.addDouble("spawnY", world.spawn.Y);
        tag.addDouble("spawnZ", world.spawn.Z);

        // save full player entity data
        var playerData = new NBTCompound("player");
        world.player.write(playerData);
        tag.add(playerData);

        // save lighting queues
        saveLightingQueues(tag);

        NBT.writeFile(tag, $"level/{world.name}/level.xnbt");
        Log.info($"Saved world data to level/{world.name}/level.xnbt");
    }

    private void saveLightingQueues(NBTCompound tag) {
        // skylight queue - just save world coords
        var skyLightList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "skyLightQueue");
        foreach (var node in world.skyLightQueue) {
            var nodeTag = new NBTCompound();
            nodeTag.addInt("x", node.x);
            nodeTag.addInt("y", node.y);
            nodeTag.addInt("z", node.z);
            skyLightList.add(nodeTag);
        }

        tag.addListTag("skyLightQueue", skyLightList);

        // skylight removal queue
        var skyLightRemovalList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "skyLightRemovalQueue");
        foreach (var node in world.skyLightRemovalQueue) {
            var nodeTag = new NBTCompound();
            nodeTag.addInt("x", node.x);
            nodeTag.addInt("y", node.y);
            nodeTag.addInt("z", node.z);
            nodeTag.addByte("value", node.value);
            skyLightRemovalList.add(nodeTag);
        }

        tag.addListTag("skyLightRemovalQueue", skyLightRemovalList);

        // blocklight queue
        var blockLightList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "blockLightQueue");
        foreach (var node in world.blockLightQueue) {
            var nodeTag = new NBTCompound();
            nodeTag.addInt("x", node.x);
            nodeTag.addInt("y", node.y);
            nodeTag.addInt("z", node.z);
            blockLightList.add(nodeTag);
        }

        tag.addListTag("blockLightQueue", blockLightList);

        // blocklight removal queue
        var blockLightRemovalList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "blockLightRemovalQueue");
        foreach (var node in world.blockLightRemovalQueue) {
            var nodeTag = new NBTCompound();
            nodeTag.addInt("x", node.x);
            nodeTag.addInt("y", node.y);
            nodeTag.addInt("z", node.z);
            nodeTag.addByte("value", node.value);
            blockLightRemovalList.add(nodeTag);
        }

        tag.addListTag("blockLightRemovalQueue", blockLightRemovalList);

        // block update queue (scheduled block updates)
        var blockUpdateList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "blockUpdateQueue");
        foreach (var update in world.blockUpdateQueue) {
            var updateTag = new NBTCompound();
            updateTag.addInt("x", update.position.X);
            updateTag.addInt("y", update.position.Y);
            updateTag.addInt("z", update.position.Z);
            updateTag.addInt("tick", update.tick);
            blockUpdateList.add(updateTag);
        }

        tag.addListTag("blockUpdateQueue", blockUpdateList);
    }

    public static World load(string filename) {
        Log.info($"Loaded data from level/{filename}/level.xnbt");
        var tag = NBT.readFile($"level/{filename}/level.xnbt");
        var seed = tag.getInt("seed");
        var displayName = tag.has("displayName") ? tag.getString("displayName") : filename;
        var generatorName = tag.has("generator") ? tag.getString("generator") : "perlin";
        var world = new World(filename, seed, displayName, generatorName);
        world.toBeLoadedNBT = tag;

        // todo load the player spawn!
        world.spawn = new Vector3D(
            tag.getDouble("spawnX"),
            tag.getDouble("spawnY"),
            tag.getDouble("spawnZ")
        );

        // dump nbt into file
        /*var file = "dump.xnbt";
        if (File.Exists(file)) {
            File.Delete(file);
        }

        SNBT.writeToFile(tag, file, prettyPrint: true);*/

        return world;
    }

    /** Load lighting queues from NBT - stores world coords with null chunk (loaded on demand) */
    public static void loadLightingQueues(World world, NBTCompound tag) {
        // skylight queue - store world coords with null chunk
        if (tag.has("skyLightQueue")) {
            var skyLightList = tag.getListTag<NBTCompound>("skyLightQueue");
            for (int i = 0; i < skyLightList.count(); i++) {
                var nodeTag = skyLightList.get(i);
                var x = nodeTag.getInt("x");
                var y = nodeTag.getInt("y");
                var z = nodeTag.getInt("z");

                world.skyLightQueue.Enqueue(new LightNode(x, y, z, null));
            }
        }

        // skylight removal queue
        if (tag.has("skyLightRemovalQueue")) {
            var skyLightRemovalList = tag.getListTag<NBTCompound>("skyLightRemovalQueue");
            for (int i = 0; i < skyLightRemovalList.count(); i++) {
                var nodeTag = skyLightRemovalList.get(i);
                var x = nodeTag.getInt("x");
                var y = nodeTag.getInt("y");
                var z = nodeTag.getInt("z");
                var value = nodeTag.getByte("value");

                world.skyLightRemovalQueue.Enqueue(new LightRemovalNode(x, y, z, value, null));
            }
        }

        // blocklight queue
        if (tag.has("blockLightQueue")) {
            var blockLightList = tag.getListTag<NBTCompound>("blockLightQueue");
            for (int i = 0; i < blockLightList.count(); i++) {
                var nodeTag = blockLightList.get(i);
                var x = nodeTag.getInt("x");
                var y = nodeTag.getInt("y");
                var z = nodeTag.getInt("z");

                world.blockLightQueue.Enqueue(new LightNode(x, y, z, null));
            }
        }

        // blocklight removal queue
        if (tag.has("blockLightRemovalQueue")) {
            var blockLightRemovalList = tag.getListTag<NBTCompound>("blockLightRemovalQueue");
            for (int i = 0; i < blockLightRemovalList.count(); i++) {
                var nodeTag = blockLightRemovalList.get(i);
                var x = nodeTag.getInt("x");
                var y = nodeTag.getInt("y");
                var z = nodeTag.getInt("z");
                var value = nodeTag.getByte("value");

                world.blockLightRemovalQueue.Enqueue(new LightRemovalNode(x, y, z, value, null));
            }
        }

        // block update queue (scheduled block updates)
        if (tag.has("blockUpdateQueue")) {
            var blockUpdateList = tag.getListTag<NBTCompound>("blockUpdateQueue");
            for (int i = 0; i < blockUpdateList.count(); i++) {
                var updateTag = blockUpdateList.get(i);
                var x = updateTag.getInt("x");
                var y = updateTag.getInt("y");
                var z = updateTag.getInt("z");
                var tick = updateTag.getInt("tick");

                world.blockUpdateQueue.Add(new BlockUpdate(new Vector3I(x, y, z), tick));
            }
        }
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

    public void loadChunkAsync(ChunkCoord coord, ChunkStatus targetStatus) {
        if (isDisposed) {
            // can't load async if disposed
            return;
        }

        chunkLoadThread.queueLoad(new ChunkLoadRequest(world, world.name, coord, targetStatus));
    }

    public bool hasChunkLoadResult() {
        return chunkLoadThread.hasResult();
    }

    public ChunkLoadResult? getChunkLoadResult() {
        return chunkLoadThread.getResult();
    }

    public void Dispose() {
        if (isDisposed) return;
        isDisposed = true;

        // wait for the save and load threads to finish
        chunkSaveThread.Dispose();
        chunkLoadThread.Dispose();
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

                // get block and light data
                var freshBlocks = saveBlockPool.grab();
                var freshLight = saveLightPool.grab();
                chunk.blocks[sectionY].getSerializationBlocks(freshBlocks);
                chunk.blocks[sectionY].getSerializationLight(freshLight);

                // build palette: collect unique block IDs
                paletteDict.Clear();
                paletteList.Clear();

                foreach (var b in freshBlocks) {
                    ushort blockID = b.getID();

                    if (!paletteDict.ContainsKey(blockID)) {
                        string stringID = Registry.BLOCKS.getName(blockID) ?? "air";
                        paletteDict.Set(blockID, paletteList.Count);
                        paletteList.Add(stringID);
                    }
                }

                // convert blocks to palette indices
                var paletteIndices = saveBlockPool.grab();
                for (int i = 0; i < freshBlocks.Length; i++) {
                    ushort blockID = freshBlocks[i].getID();
                    byte metadata = freshBlocks[i].getMetadata();
                    int paletteIdx = paletteDict[blockID];
                    paletteIndices[i] = ((uint)metadata << 24) | (uint)paletteIdx;
                }

                // save palette
                var paletteTag = new NBTList<NBTString>(NBTType.TAG_String, "palette");
                foreach (var stringID in paletteList) {
                    paletteTag.add(new NBTString(null, stringID));
                }

                section.addListTag("palette", paletteTag);
                section.addUIntArray("blocks", paletteIndices);
                section.addByteArray("light", freshLight);

                // return freshBlocks to pool (we're done with it)
                saveBlockPool.putBack(freshBlocks);
            }
            else {
                section.addByte("inited", 0);
            }

            sectionsTag.add(section);
        }

        chunkTag.addListTag("sections", sectionsTag);

        // save entities (skip players - they're saved with world data)
        var entitiesTag = new NBTList<NBTCompound>(NBTType.TAG_Compound, "entities");
        for (int sectionY = 0; sectionY < Chunk.CHUNKHEIGHT; sectionY++) {
            foreach (var entity in chunk.entities[sectionY]) {
                // skip players
                if (entity.type == "player") {
                    continue;
                }

                var entityData = new NBTCompound();
                entityData.addString("type", entity.type);
                var data = new NBTCompound("data");
                entity.write(data);
                entityData.add(data);
                entitiesTag.add(entityData);
            }
        }

        chunkTag.addListTag("entities", entitiesTag);

        // save block entities
        var blockEntitiesTag = new NBTList<NBTCompound>(NBTType.TAG_Compound, "blockEntities");
        foreach (var (pos, be) in chunk.blockEntities) {
            var beData = new NBTCompound();
            be.write(beData);
            blockEntitiesTag.add(beData);
        }
        chunkTag.addListTag("blockEntities", blockEntitiesTag);

        return chunkTag;
    }

    public static Chunk loadChunkFromNBT(World world, NBTCompound chunkTag) {
        var posX = chunkTag.getInt("posX");
        var posZ = chunkTag.getInt("posZ");
        var status = chunkTag.getByte("status");
        var lastSaved = chunkTag.getULong("lastSaved");
        var chunk = new Chunk(world, posX, posZ) {
            status = (ChunkStatus)status,
            lastSaved = lastSaved
        };
        var sections = chunkTag.getListTag<NBTCompound>("sections");
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

            // load palette if it exists (new format)
            uint[] runtimeBlocks;
            if (section.has("palette")) {
                var paletteTag = section.getListTag<NBTString>("palette");
                var paletteIndices = section.getUIntArray("blocks");

                // convert palette to runtime IDs
                runtimeBlocks = new uint[paletteIndices.Length];
                for (int i = 0; i < paletteIndices.Length; i++) {
                    uint packedValue = paletteIndices[i];
                    int paletteIdx = (int)(packedValue & 0xFFFFFF);
                    byte metadata = (byte)(packedValue >> 24);

                    string stringID = paletteTag.get(paletteIdx).data;
                    int runtimeID = Registry.BLOCKS.getID(stringID);

                    if (runtimeID == -1) {
                        // Block removed from game -> use air
                        runtimeBlocks[i] = 0;
                    }
                    else {
                        runtimeBlocks[i] = ((uint)metadata << 24) | (uint)runtimeID;
                    }
                }
            }
            else {
                // old format without palette - assume IDs are still valid?
                runtimeBlocks = section.getUIntArray("blocks");
            }

            // blocks
            blocks.setSerializationData(runtimeBlocks, section.getByteArray("light"));
        }

        // load entities (skip players - they're saved with world data)
        if (chunkTag.has("entities")) {
            var entitiesTag = chunkTag.getListTag<NBTCompound>("entities");
            for (int i = 0; i < entitiesTag.count(); i++) {
                var entityData = entitiesTag.get(i);
                var type = entityData.getString("type");

                // skip players
                if (type == "player") {
                    continue;
                }

                var data = entityData.getCompoundTag("data");
                var entity = Entities.create(world, type);
                if (entity != null) {
                    entity.read(data);
                    // update global entity ID counter to prevent duplicates
                    World.ec = Math.Max(World.ec, entity.id + 1);

                    // add directly to chunk and world entity list
                    // (chunk not in world.chunks yet, so world.addEntity() would fail to add to chunk)
                    // calculate subchunk coord from pos
                    var pos = entity.position.toBlockPos();
                    int chunkX = pos.X >> 4;
                    int chunkZ = pos.Z >> 4;
                    int subY = pos.Y >> 4;
                    entity.subChunkCoord = new SubChunkCoord(chunkX, subY, chunkZ);
                    chunk.addEntity(entity);
                    //entity.inWorld = true;
                    world.entities.Add(entity);
                }
                else {
                    Log.warn($"loadChunkFromNBT: Failed to create entity of type {type} in chunk ({posX},{posZ})");
                }
            }
        }

        // load block entities
        if (chunkTag.has("blockEntities")) {
            var blockEntitiesTag = chunkTag.getListTag<NBTCompound>("blockEntities");
            for (int i = 0; i < blockEntitiesTag.count(); i++) {
                var beData = blockEntitiesTag.get(i);
                var pos = new Vector3I(
                    beData.getInt("x"),
                    beData.getInt("y"),
                    beData.getInt("z")
                );

                // get block type at position to determine BE type
                var blockValue = chunk.getBlock(pos.X & 15, pos.Y, pos.Z & 15);
                var blockID = ((uint)blockValue).getID();
                var block = Block.get(blockID);

                if (block is EntityBlock eb) {
                    var be = eb.get();
                    be.read(beData);
                    chunk.blockEntities[new Vector3I(pos.X & 15, pos.Y, pos.Z & 15)] = be;
                    world.blockEntities.Add(be);
                }
            }
        }

        /*var file = "chunk.xnbt";
        if (File.Exists(file)) {
            File.Delete(file);
        }

        SNBT.writeToFile(nbt, file, prettyPrint: true);*/
        return chunk;
    }

    /**
     * Apply NBT data to an existing empty chunk (for async loading)
     */
    // todo merge this with WorldIO.loadChunkFromNBT
    public static void loadChunkDataFromNBT(Chunk chunk, NBTCompound nbt) {
        var status = nbt.getByte("status");
        var lastSaved = nbt.getULong("lastSaved");

        chunk.status = (ChunkStatus)status;
        chunk.lastSaved = lastSaved;

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

            // load palette if it exists (new format)
            uint[] runtimeBlocks;
            if (section.has("palette")) {
                var paletteTag = section.getListTag<NBTString>("palette");
                var paletteIndices = section.getUIntArray("blocks");

                // convert palette to runtime IDs
                runtimeBlocks = new uint[paletteIndices.Length];
                for (int i = 0; i < paletteIndices.Length; i++) {
                    uint packedValue = paletteIndices[i];
                    int paletteIdx = (int)(packedValue & 0xFFFFFF);
                    byte metadata = (byte)(packedValue >> 24);

                    string stringID = paletteTag.get(paletteIdx).data;
                    int runtimeID = Registry.BLOCKS.getID(stringID);

                    if (runtimeID == -1) {
                        // Block removed from game -> use air
                        runtimeBlocks[i] = 0;
                    }
                    else {
                        runtimeBlocks[i] = ((uint)metadata << 24) | (uint)runtimeID;
                    }
                }
            }
            else {
                // old format without palette - assume IDs are still valid?
                runtimeBlocks = section.getUIntArray("blocks");
            }

            // blocks
            blocks.setSerializationData(runtimeBlocks, section.getByteArray("light"));
        }

        // load entities (skip players - they're saved with world data)
        if (nbt.has("entities")) {
            var entitiesTag = nbt.getListTag<NBTCompound>("entities");
            for (int i = 0; i < entitiesTag.count(); i++) {
                var entityData = entitiesTag.get(i);
                var type = entityData.getString("type");

                // skip players
                if (type == "player") {
                    continue;
                }

                var data = entityData.getCompoundTag("data");
                var entity = Entities.create(chunk.world, type);
                if (entity != null) {
                    entity.read(data);
                    // update global entity ID counter to prevent duplicates
                    World.ec = Math.Max(World.ec, entity.id + 1);
                    // calculate subchunk coord from pos
                    var pos = entity.position.toBlockPos();
                    int chunkX = pos.X >> 4;
                    int chunkZ = pos.Z >> 4;
                    int subY = pos.Y >> 4;
                    entity.subChunkCoord = new SubChunkCoord(chunkX, subY, chunkZ);

                    // add directly to chunk and world entity list
                    // (chunk might not be in world.chunks yet, so world.addEntity() could fail to add to chunk)
                    chunk.addEntity(entity);
                    //entity.inWorld = true;
                    chunk.world.entities.Add(entity);
                }
            }
        }

        // load block entities
        if (nbt.has("blockEntities")) {
            var blockEntitiesTag = nbt.getListTag<NBTCompound>("blockEntities");
            for (int i = 0; i < blockEntitiesTag.count(); i++) {
                var beData = blockEntitiesTag.get(i);
                var pos = new Vector3I(
                    beData.getInt("x"),
                    beData.getInt("y"),
                    beData.getInt("z")
                );

                // get block type at position to determine BE type
                var blockValue = chunk.getBlock(pos.X & 15, pos.Y, pos.Z & 15);
                var blockID = ((uint)blockValue).getID();
                var block = Block.get(blockID);

                if (block is EntityBlock eb) {
                    var be = eb.get();
                    be.read(beData);
                    chunk.blockEntities[new Vector3I(pos.X & 15, pos.Y, pos.Z & 15)] = be;
                    chunk.world.blockEntities.Add(be);
                }
            }
        }

        // if meshed, cap the status so it's not meshed (otherwise VAO is not created -> crash)
        if (chunk.status >= ChunkStatus.MESHED) {
            chunk.status = ChunkStatus.LIGHTED;
        }
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

public struct ChunkLoadRequest(World world, string worldName, ChunkCoord coord, ChunkStatus targetStatus) {
    public readonly World world = world;
    public readonly string worldName = worldName;
    public readonly ChunkCoord coord = coord;
    public readonly ChunkStatus targetStatus = targetStatus;
}

public struct ChunkLoadResult(ChunkCoord coord, NBTCompound? nbtData, ChunkStatus targetStatus, Exception? error = null) {
    public readonly ChunkCoord coord = coord;
    public readonly NBTCompound? nbtData = nbtData;
    public readonly ChunkStatus targetStatus = targetStatus;
    public readonly Exception? error = error;
}

public sealed class ChunkSaveThread : IDisposable {
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

                        // if we're being polite, we can put the arrays back after it's done
                        var sections = saveData.nbt.getListTag<NBTCompound>("sections");
                        foreach (var section in sections.list) {
                            //print what it has

                            //Log.info($"Saved chunk section, inited: {section.getByte("inited")}, blocks length: {section.getUIntArray("blocks")?.Length}, light length: {section.getByteArray("light")?.Length}");

                            // put back the arrays into the pool, but only if they exist (i.e. section was inited)
                            if (section.getByte("inited") != 0) {
                                WorldIO.saveBlockPool.putBack(section.getUIntArray("blocks"));
                                WorldIO.saveLightPool.putBack(section.getByteArray("light"));
                            }
                        }
                    }
                    catch (Exception ex) {
                        Log.warn($"Failed to save chunk to {saveData.path}:", ex);
                    }
                }
                else {
                    // no chunks to save, wait a bit or until shutdown
                    io.shutdownEvent.WaitOne(10);
                }
            }
        }
        catch (Exception ex) {
            Log.error("Background save loop error", ex);
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
            Log.error("Error waiting for save thread to complete", ex);
        }

        // process remaining saves synchronously
        while (saveQueue.TryDequeue(out var saveData)) {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(saveData.path) ?? string.Empty);
                NBT.writeFile(saveData.nbt, saveData.path);
            }
            catch (Exception ex) {
                Log.error("Failed to save chunk during dispose", ex);
            }
        }
    }

    public void add(ChunkSaveData chunk) {
        saveQueue.Enqueue(chunk);
    }
}

public sealed class ChunkLoadThread : IDisposable {
    private readonly WorldIO io;
    private readonly Thread loadThread;

    private volatile bool isDisposed;

    private readonly ConcurrentQueue<ChunkLoadRequest> loadQueue = new();
    private readonly ConcurrentQueue<ChunkLoadResult> resultQueue = new();

    public ChunkLoadThread(WorldIO io) {
        this.io = io;
        loadThread = new Thread(loadLoop) {
            IsBackground = true,
            Name = "ChunkLoadThread"
        };
        loadThread.Start();
    }

    public void Join() {
        loadThread.Join();
    }

    private void loadLoop() {
        try {
            while (!io.shutdownEvent.WaitOne(0)) {
                if (loadQueue.TryDequeue(out var loadRequest)) {
                    try {
                        // Background thread: File I/O + NBT parsing only
                        var pathStr = WorldIO.getChunkString(loadRequest.worldName, loadRequest.coord);
                        var nbt = NBT.readFile(pathStr);

                        resultQueue.Enqueue(new ChunkLoadResult(loadRequest.coord, nbt, loadRequest.targetStatus));
                    }
                    catch (Exception ex) {
                        // Queue the error result
                        resultQueue.Enqueue(new ChunkLoadResult(loadRequest.coord, null, loadRequest.targetStatus, ex));
                    }
                }
                else {
                    // no chunks to load, wait a bit or until shutdown
                    io.shutdownEvent.WaitOne(10);
                }
            }
        }
        catch (Exception ex) {
            Log.error("Background load loop error", ex);
        }
    }

    public void Dispose() {
        if (isDisposed) return;
        isDisposed = true;

        // wait for the thread to finish
        io.shutdownEvent.Set();
        try {
            loadThread.Join(5000); // wait up to 5 seconds
        }
        catch (Exception ex) {
            Log.error("Error waiting for load thread to complete", ex);
        }

        // clear remaining queues
        while (loadQueue.TryDequeue(out _)) {
        }

        while (resultQueue.TryDequeue(out _)) {
        }
    }

    public void queueLoad(ChunkLoadRequest request) {
        if (!isDisposed) {
            loadQueue.Enqueue(request);
        }
    }

    public bool hasResult() {
        return !resultQueue.IsEmpty;
    }

    public ChunkLoadResult? getResult() {
        return resultQueue.TryDequeue(out var result) ? result : null;
    }
}