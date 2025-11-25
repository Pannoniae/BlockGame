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
using BlockGame.world.entity;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public class WorldIO {
    public static readonly FixedArrayPool<uint> saveBlockPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);
    public static readonly FixedArrayPool<byte> saveLightPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);

    // palette building
    private static readonly XUList<string> paletteList = new(256);
    private static readonly XUList<byte> lightPaletteList = new(256);

    // blocks can change at runtime though? maybe, idk, but don't assume that plz


    public World world;

    private readonly ChunkSaveThread chunkSaveThread;
    private readonly ChunkLoadThread chunkLoadThread;

    // Background saving and loading
    public readonly ManualResetEvent shutdownEvent = new(false);
    private volatile bool isDisposed;

    // lock file to prevent multiple instances
    public FileStream? lockFile;

    public WorldIO(World world) {
        this.world = world;
        if (!world.isMP) {
            chunkSaveThread = new ChunkSaveThread(this);
            chunkLoadThread = new ChunkLoadThread(this);
        }
    }

    public void save(World world, string filename, bool saveChunks = true) {
        // save metadata
        // create level folder

        if (world.isMP) {
            SkillIssueException.throwNew("fix your fucking game");
        }

        if (Net.mode.isDed()) {
            if (!Directory.Exists($"{filename}")) {
                Directory.CreateDirectory($"{filename}");
            }
        }
        else {
            if (!Directory.Exists($"level/{filename}")) {
                Directory.CreateDirectory($"level/{filename}");
            }
        }

        try {
            saveWorldData();

            // save chunks
            if (saveChunks) {
                foreach (var chunk in world.chunks) {
                    //var regionCoord = World.getRegionPos(chunk.coord);
                    saveChunk(world, chunk);
                }
            }
        }
        catch (Exception e) {
            Log.error("Error saving world:");
            Log.error(e);
        }
        //regionCache.Clear();
    }

    public void saveWorldData() {
        var tag = new NBTCompound("");
        tag.addInt("seed", world.seed);
        tag.addInt("time", world.worldTick);
        tag.addString("displayName", world.displayName);

        // in singleplayer we add it from the player
        if (Net.mode.isSP()) {
            tag.addString("gamemode", world.player.gameMode == GameMode.survival ? "survival" : "creative");
        }

        tag.addLong("lastPlayed", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        tag.addString("generator", world.generatorName);

        // add player spawn
        tag.addDouble("spawnX", world.spawn.X);
        tag.addDouble("spawnY", world.spawn.Y);
        tag.addDouble("spawnZ", world.spawn.Z);

        if (Net.mode.isSP()) {
            // save full player entity data
            var playerData = new NBTCompound("player");
            world.player.write(playerData);
            tag.add(playerData);
        }

        // save lighting queues
        saveLightingQueues(tag);

        if (Net.mode.isDed()) {
            NBT.writeFile(tag, $"{world.name}/level.xnbt");
            Log.info($"Saved world data to {world.name}/level.xnbt");
        }
        else {
            NBT.writeFile(tag, $"level/{world.name}/level.xnbt");
            Log.info($"Saved world data to level/{world.name}/level.xnbt");
        }
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
        // check for lock file
        var lockPath = getLockFilePath(filename);
        if (File.Exists(lockPath)) {
            throw new IOException($"World '{filename}' is already open in another instance! Close the other instance first.");
        }

        NBTCompound tag;
        if (Net.mode.isDed()) {
            Log.info($"Loaded data from {filename}/level.xnbt");
            tag = NBT.readFile($"{filename}/level.xnbt");
        }
        else {
            Log.info($"Loaded data from level/{filename}/level.xnbt");
            tag = NBT.readFile($"level/{filename}/level.xnbt");
        }

        var seed = tag.getInt("seed");
        var displayName = tag.has("displayName") ? tag.getString("displayName") : filename;
        var generatorName = tag.has("generator") ? tag.getString("generator") : "perlin";
        var world = new World(filename, seed, displayName, generatorName);
        world.toBeLoadedNBT = tag;

        // create lock file
        try {
            world.worldIO.lockFile = File.Open(lockPath, FileMode.Create, FileAccess.Write, FileShare.None);
            Log.info($"Created lock file for world '{filename}'");
        }
        catch (Exception e) {
            throw new IOException($"Failed to create lock file for world '{filename}': {e.Message}", e);
        }

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

        // we can return the pooled arrays now
        // todo is this safe?
        /*for (int sectionY = 0; sectionY < Chunk.CHUNKHEIGHT; sectionY++) {
            var freshBlocks = nbt.getListTag<NBTCompound>("sections").get(sectionY).getUIntArray("blocks");
            if (freshBlocks != null) {
                saveBlockPool.putBack(freshBlocks);
            }

            var freshLight = nbt.getListTag<NBTCompound>("sections").get(sectionY).getByteArray("light");
            if (freshLight != null) {
                saveLightPool.putBack(freshLight);
            }
        }*/
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

        releaseLock();
    }

    public void releaseLock() {
        // release and delete lock file
        if (lockFile != null) {
            var lockPath = getLockFilePath(world.name);
            try {
                lockFile.Close();
                lockFile.Dispose();
                if (File.Exists(lockPath)) {
                    File.Delete(lockPath);
                    Log.info($"Removed lock file for world '{world.name}'");
                }
            }
            catch (Exception e) {
                Log.error("Failed to remove lock file:");
                Log.error(e);
            }

            lockFile = null;
        }
    }

    public static void deleteLevel(string level) {
        Directory.Delete(Net.mode.isDed() ? $"{level}" : $"level/{level}", true);
    }

    public static string getLockFilePath(string worldName) {
        return Net.mode.isDed() ? $"{worldName}/world.lock" : $"level/{worldName}/world.lock";
    }

    public static NBTCompound serialiseChunkIntoNBT(Chunk chunk) {
        var chunkTag = new NBTCompound();
        chunkTag.addInt("posX", chunk.coord.x);
        chunkTag.addInt("posZ", chunk.coord.z);
        chunkTag.addByte("status", (byte)chunk.status);
        chunkTag.addULong("lastSaved", chunk.lastSaved);
        // YXZ
        var sectionsTag = new NBTList<NBTCompound>(NBTType.TAG_Compound, "sections");
        for (int sectionY = 0; sectionY < Chunk.CHUNKHEIGHT; sectionY++) {
            var section = new NBTCompound();
            // if empty, just write zeros
            if (chunk.blocks[sectionY].inited) {
                section.addByte("inited", 1);

                var blockData = chunk.blocks[sectionY];

                // build NBT block palette directly from internal palette (no unpacking)
                paletteList.Clear();
                var internalVerts = blockData.skillIssueVertices();
                var internalRefs = blockData.skillIssueBlockRefs();
                var internalVertCount = blockData.skillIssueVertCount();

                // map: internal palette index -> NBT palette index
                Span<int> indexRemap = stackalloc int[internalVertCount];

                for (int i = 0; i < internalVertCount; i++) {
                    if (internalRefs[i] > 0) {
                        uint bl = internalVerts[i];
                        ushort blockID = bl.getID();
                        string stringID = Registry.BLOCKS.getName(blockID) ?? "air";
                        indexRemap[i] = paletteList.Count;
                        paletteList.Add(stringID);
                    }
                }

                // remap internal block indices to NBT indices
                var paletteIndices = saveBlockPool.grab();
                for (int i = 0; i < Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE; i++) {
                    int idx = blockData.skillIssueIndexRaw(i);
                    uint bl = internalVerts[idx];
                    byte metadata = bl.getMetadata();
                    int nbtidx = indexRemap[idx];
                    paletteIndices[i] = ((uint)metadata << 24) | (uint)nbtidx;
                }

                // build NBT light palette from internal light palette
                var internalLightVerts = blockData.skillIssueLightVertices();
                var internalLightRefs = blockData.skillIssueLightRefs();
                var internalLightVertCount = blockData.skillIssueLightVertCount();

                // todo we could move the allocation outside the loop and just resize it if it's bigger than the current one...
                Span<int> lightIndexRemap = stackalloc int[internalLightVertCount];
                lightPaletteList.Clear();

                for (int i = 0; i < internalLightVertCount; i++) {
                    if (internalLightRefs[i] > 0) {
                        lightIndexRemap[i] = lightPaletteList.Count;
                        lightPaletteList.Add(internalLightVerts[i]);
                    }
                }

                // remap internal light indices to NBT indices
                var lightIndices = saveLightPool.grab();
                for (int i = 0; i < Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE; i++) {
                    int idx = blockData.skillIssueLightIndexRaw(i);
                    int nbtidx = lightIndexRemap[idx];
                    lightIndices[i] = (byte)nbtidx;
                }

                // save palettes
                section.addStringListUnsafe("palette", paletteList);
                section.addByteListUnsafe("lightPalette", lightPaletteList);
                section.addUIntArray("blocks", paletteIndices);
                section.addByteArray("lightIndices", lightIndices);
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

        // save biome data
        chunkTag.addSByteArray("biomeTemp", chunk.biomeData.temp);
        chunkTag.addSByteArray("biomeHum", chunk.biomeData.hum);
        chunkTag.addSByteArray("biomeAge", chunk.biomeData.age);
        chunkTag.addSByteArray("biomeW", chunk.biomeData.w);

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

        loadChunkSections(chunk, chunkTag);

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
                    entity.inWorld = true;
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

        // load biome data
        if (chunkTag.has("biomeTemp")) {
            chunk.biomeData.temp = chunkTag.getSByteArray("biomeTemp");
            chunk.biomeData.hum = chunkTag.getSByteArray("biomeHum");
            chunk.biomeData.age = chunkTag.getSByteArray("biomeAge");
            chunk.biomeData.w = chunkTag.getSByteArray("biomeW");
        }

        chunk.biomeData.setChunk(chunk);

        /*var file = "chunk.xnbt";
        if (File.Exists(file)) {
            File.Delete(file);
        }

        SNBT.writeToFile(nbt, file, prettyPrint: true);*/
        return chunk;
    }

    /** Shared section loading - deduped from loadChunkFromNBT and loadChunkDataFromNBT */
    private static void loadChunkSections(Chunk chunk, NBTCompound chunkTag) {
        var sections = chunkTag.getListTag<NBTCompound>("sections");
        for (int sy = 0; sy < Chunk.CHUNKHEIGHT; sy++) {
            var section = sections.get(sy);
            var blocks = chunk.blocks[sy];

            if (section.getByte("inited") == 0) {
                blocks.inited = false;
                continue;
            }

            blocks.loadInit();

            // load blocks - must unpack palette because NBT format strips metadata
            // todo this could be fixed? store the metadata in the palette too on disk and avoid unpacking?
            // instead of storing just block IDs in the palette, store full uint values
            // jess, please don't be lazy next time
            uint[] runtimeBlocks;
            if (section.has("palette")) {
                var paletteTag = section.getListTag<NBTString>("palette");
                var paletteIndices = section.getUIntArray("blocks");

                // unpack: NBT palette only has block IDs, metadata is packed in indices
                runtimeBlocks = new uint[paletteIndices.Length];
                for (int i = 0; i < paletteIndices.Length; i++) {
                    uint packed = paletteIndices[i];
                    int paletteIdx = (int)(packed & 0xFFFFFF);
                    byte metadata = (byte)(packed >> 24);

                    string stringID = paletteTag.get(paletteIdx).data;
                    int runtimeID = Registry.BLOCKS.getID(stringID);
                    runtimeBlocks[i] = runtimeID == -1 ? 0 : ((uint)metadata << 24) | (uint)runtimeID;
                }
            } else {
                // old format - flat array
                runtimeBlocks = section.getUIntArray("blocks");
            }

            // load light - also must unpack
            byte[] runtimeLight;
            if (section.has("lightPalette")) {
                var lightPaletteTag = section.getListTag<NBTByte>("lightPalette");
                var lightIndices = section.getByteArray("lightIndices");

                runtimeLight = new byte[lightIndices.Length];
                for (int i = 0; i < lightIndices.Length; i++) {
                    runtimeLight[i] = lightPaletteTag.get(lightIndices[i]).data;
                }
            } else {
                // old format - flat array
                runtimeLight = section.getByteArray("light");
            }

            blocks.setSerializationData(runtimeBlocks, runtimeLight);
        }
    }

    /**
     * Apply NBT data to an existing empty chunk (for async loading)
     */
    public static void loadChunkDataFromNBT(Chunk chunk, NBTCompound nbt) {
        var status = nbt.getByte("status");
        var lastSaved = nbt.getULong("lastSaved");

        chunk.status = (ChunkStatus)status;
        chunk.lastSaved = lastSaved;

        loadChunkSections(chunk, nbt);

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
                    entity.inWorld = true;
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

        // load biome data
        if (nbt.has("biomeTemp")) {
            chunk.biomeData.temp = nbt.getSByteArray("biomeTemp");
            chunk.biomeData.hum = nbt.getSByteArray("biomeHum");
            chunk.biomeData.age = nbt.getSByteArray("biomeAge");
            chunk.biomeData.w = nbt.getSByteArray("biomeW");
        }

        chunk.biomeData.setChunk(chunk);

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
        return Net.mode.isDed() ? $"{levelname}/{xDir}/{zDir}/c{coord.x},{coord.z}.xnbt" : $"level/{levelname}/{xDir}/{zDir}/c{coord.x},{coord.z}.xnbt";
    }

    public static bool chunkFileExists(string levelname, ChunkCoord coord) {
        return File.Exists(getChunkString(levelname, coord));
    }

    public static bool worldExists(string level) {
        return File.Exists(Net.mode.isDed() ? $"{level}/level.xnbt" : $"level/{level}/level.xnbt");
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
            Name = "ChunkSaveThread",
            Priority = ThreadPriority.BelowNormal
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
                            // put back the arrays into the pool, but only if they exist (i.e. section was inited)
                            if (section.getByte("inited") != 0) {
                                var blocks = section.getUIntArray("blocks");
                                var lightIndices = section.getByteArray("lightIndices");

                                if (blocks != null) {
                                    WorldIO.saveBlockPool.putBack(blocks);
                                }
                                else {
                                    Log.warn("ChunkSaveThread: blocks array was null when returning to pool!");
                                }
                                if (lightIndices != null) {
                                    WorldIO.saveLightPool.putBack(lightIndices);
                                }
                                else {
                                    Log.warn("ChunkSaveThread: lightIndices array was null when returning to pool!");
                                }
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
            Name = "ChunkLoadThread",
            Priority = ThreadPriority.BelowNormal
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