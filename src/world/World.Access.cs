using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame;

public partial class World {
    public static bool isMisalignedBlock(Vector3I position) {
        return position.X < 0 || position.X > 15 || position.Z < 0 || position.Z > 15;
    }

    public ushort getMisalignedBlock(Vector3I position, Chunk chunk, out Chunk actualChunk) {
        var pos = toWorldPos(chunk.worldX, chunk.worldZ, position.X, position.Y, position.Z);
        var blockPos = getPosInChunk(pos);
        var success = getChunkMaybe(pos.X, pos.Z, out actualChunk);
        return success ? actualChunk!.getBlock(blockPos.X, pos.Y, blockPos.Z) : (ushort)0;
    }

    public Vector3I alignBlock(Vector3I position, Chunk chunk, out Chunk actualChunk) {
        var pos = toWorldPos(chunk.worldX, chunk.worldZ, position.X, position.Y, position.Z);
        var blockPos = getPosInChunk(pos);
        var success = getChunkMaybe(pos.X, pos.Z, out actualChunk);
        return blockPos;
    }

    public bool isBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return chunk.getBlock(blockPos.X, y, blockPos.Z) != 0;
    }

    public bool isSelectableBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return Block.selection[chunk.getBlock(blockPos.X, y, blockPos.Z)];
    }

    public ushort getBlock(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return 0;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getBlock(blockPos.X, y, blockPos.Z) : (ushort)0;
    }

    public byte getLight(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return 0;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getLight(blockPos.X, blockPos.Y, blockPos.Z) : (byte)0;
    }

    public byte getSkyLight(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return 0;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getSkyLight(blockPos.X, blockPos.Y, blockPos.Z) : (byte)0;
    }

    public byte getBlockLight(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return 0;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getBlockLight(blockPos.X, blockPos.Y, blockPos.Z) : (byte)0;
    }

    public void setSkyLight(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setSkyLight(blockPos.X, blockPos.Y, blockPos.Z, level);
        }
    }

    public void setSkyLightRemesh(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setSkyLightRemesh(blockPos.X, blockPos.Y, blockPos.Z, level);
        }
    }

    public void setSkyLightAndPropagate(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setSkyLight(blockPos.X, blockPos.Y, blockPos.Z, level);
            skyLightQueue.Add(new LightNode(x, y, z, chunk));
            //processSkyLightQueue();
        }
    }

    public void removeSkyLightAndPropagate(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            var value = getSkyLight(x, y, z);
            skyLightRemovalQueue.Add(new LightRemovalNode(x, y, z, value, chunk!));
            chunk!.setSkyLight(blockPos.X, blockPos.Y, blockPos.Z, 0);
        }
    }

    public void setBlockLight(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setBlockLight(blockPos.X, blockPos.Y, blockPos.Z, level);
        }
    }

    public void setBlockLightRemesh(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setBlockLightRemesh(blockPos.X, blockPos.Y, blockPos.Z, level);
        }
    }

    public void removeBlockLightAndPropagate(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            var value = getBlockLight(x, y, z);
            blockLightRemovalQueue.Add(new LightRemovalNode(x, y, z, value, chunk!));
            chunk!.setBlockLight(blockPos.X, blockPos.Y, blockPos.Z, 0);
        }
    }

    /// <summary>
    /// getBlock but returns -1 if OOB
    /// </summary>
    public int getBlockUnsafe(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return -1;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getBlock(blockPos.X, y, blockPos.Z) : -1;
    }

    public ushort getBlock(Vector3I pos) {
        return getBlock(pos.X, pos.Y, pos.Z);
    }

    public AABB? getAABB(int x, int y, int z, Block block) {
        var aabb = Block.AABB[block.id];
        if (aabb == null) {
            return null;
        }

        return new AABB(new Vector3D(x + aabb.Value.minX, y + aabb.Value.minY, z + aabb.Value.minZ),
            new Vector3D(x + aabb.Value.maxX, y + aabb.Value.maxY, z + aabb.Value.maxZ));
    }

    public AABB? getAABB(int x, int y, int z, ushort id) {
        if (id == 0) {
            return null;
        }

        var block = Block.get(id);
        return getAABB(x, y, z, block);
    }

    public AABB? getSelectionAABB(int x, int y, int z, ushort id) {
        if (id == 0) {
            return null;
        }

        var block = Block.get(id);
        return getSelectionAABB(x, y, z, block);
    }

    public AABB? getSelectionAABB(int x, int y, int z, Block block) {
        var aabb = Block.selectionAABB[block.id];
        if (aabb == null) {
            return null;
        }

        return new AABB(new Vector3D(x + aabb.Value.minX, y + aabb.Value.minY, z + aabb.Value.minZ),
            new Vector3D(x + aabb.Value.maxX, y + aabb.Value.maxY, z + aabb.Value.maxZ));
    }

    public void setBlock(int x, int y, int z, ushort block) {
        if (!inWorld(x, y, z)) {
            //Console.Out.WriteLine($"was? {x} {y} {z} {getChunkPos(x, z)} {chunks[getChunkPos(x, z)]}");
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        chunk.setBlock(blockPos.X, blockPos.Y, blockPos.Z, block);
    }

    public void setBlockRemesh(int x, int y, int z, ushort block) {
        if (!inWorld(x, y, z)) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        chunk.setBlockRemesh(blockPos.X, blockPos.Y, blockPos.Z, block);
    }

    /// <summary>
    /// This checks whether it's at least generated.
    /// </summary>
    public bool inWorld(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return false;
        }
        var chunkpos = getChunkPos(x, z);
        return chunks.ContainsKey(chunkpos);
    }

    public static bool inWorldY(int x, int y, int z) {
        return y is >= 0 and < WORLDHEIGHT;
    }

    public static SubChunkCoord getChunkSectionPos(Vector3I pos) {
        return new SubChunkCoord(
            pos.X >> 4,
            pos.Y >> 4,
            pos.Z >> 4);
    }

    public static SubChunkCoord getChunkSectionPos(int x, int y, int z) {
        return new SubChunkCoord(
            x >> 4,
            y >> 4,
            z >> 4);
    }

    public static ChunkCoord getChunkPos(Vector2I pos) {
        return new ChunkCoord(
            pos.X >> 4,
            pos.Y >> 4);
    }

    public static ChunkCoord getChunkPos(int x, int z) {
        return new ChunkCoord(
            x >> 4,
            z >> 4);
    }

    public static RegionCoord getRegionPos(ChunkCoord pos) {
        return new RegionCoord(
            pos.x >> 4,
            pos.z >> 4);
    }

    public static Vector3I getPosInChunk(int x, int y, int z) {
        return new Vector3I(
            x & 0xF,
            y,
            z & 0xF);
    }
    
    public static Vector2I getPosInChunk(int x, int z) {
        return new Vector2I(
            x & 0xF,
            z & 0xF);
    }

    public static Vector3I getPosInChunk(Vector3I pos) {
        return new Vector3I(
            pos.X & 0xF,
            pos.Y,
            pos.Z & 0xF);
    }

    public static Vector3I getPosInChunkSection(int x, int y, int z) {
        return new Vector3I(
            x & 0xF,
            y & 0xF,
            z & 0xF);
    }

    public static Vector3I getPosInChunkSection(Vector3I pos) {
        return new Vector3I(
            pos.X & 0xF,
            pos.Y & 0xF,
            pos.Z & 0xF);
    }

    public bool isChunkSectionInWorld(SubChunkCoord pos) {
        return chunks.ContainsKey(new ChunkCoord(pos.x, pos.z)) && pos.y >= 0 && pos.y < Chunk.CHUNKHEIGHT;
    }
    
    public bool isChunkInWorld(ChunkCoord pos) {
        return chunks.ContainsKey(pos);
    }

    public Chunk getChunk(int x, int z) {
        var pos = getChunkPos(x, z);
        return chunks[pos];
    }

    public bool getChunkMaybe(int x, int z, out Chunk? chunk) {
        var pos = getChunkPos(x, z);
        var c = chunks.TryGetValue(pos, out chunk);
        return c;
    }

    public bool getChunkMaybe(ChunkCoord coord, out Chunk? chunk) {
        var c = chunks.TryGetValue(coord, out chunk);
        return c;
    }

    public SubChunk getChunkSection(int x, int y, int z) {
        var pos = getChunkSectionPos(new Vector3I(x, y, z));
        return chunks[new ChunkCoord(pos.x, pos.z)].subChunks[pos.y];
    }

    public bool getChunkSectionMaybe(int x, int y, int z, out SubChunk? section) {
        var pos = getChunkSectionPos(x, y, z);
        var c = chunks.TryGetValue(new ChunkCoord(pos.x, pos.z), out var chunk);
        if (!c || y is < 0 or >= WORLDHEIGHT) {
            section = null;
            return false;
        }
        section = chunk!.subChunks[pos.y];
        return true;
    }

    public SubChunk getChunkSection(Vector3I coord) {
        var pos = getChunkSectionPos(coord);
        return chunks[new ChunkCoord(pos.x, pos.z)].subChunks[pos.y];
    }

    public SubChunk getChunkSection(SubChunkCoord coord) {
        return chunks[new ChunkCoord(coord.x, coord.z)].subChunks[coord.y];
    }

    public bool getChunkSectionMaybe(SubChunkCoord pos, out SubChunk? section) {
        var c = chunks.TryGetValue(new ChunkCoord(pos.x, pos.z), out var chunk);
        if (!c || pos.y is < 0 or >= Chunk.CHUNKHEIGHT) {
            section = null;
            return false;
        }
        section = chunk!.subChunks[pos.y];
        return true;
    }

    public SubChunk? getChunkSectionUnsafe(SubChunkCoord pos) {
        if (pos.y is < 0 or >= Chunk.CHUNKHEIGHT) {
            return null;
        }
        bool c = chunks.TryGetValue(new ChunkCoord(pos.x, pos.z), out var chunk);
        return !c ? null : chunk!.subChunks[pos.y];
    }

    public Chunk getChunk(Vector2I position) {
        var pos = getChunkPos(position);
        return chunks[pos];
    }

    public Chunk getChunk(ChunkCoord position) {
        return chunks[position];
    }

    /// <summary>
    /// For sections
    /// </summary>
    public static Vector3I toWorldPos(int chunkX, int chunkY, int chunkZ, int x, int y, int z) {
        return new Vector3I(chunkX * Chunk.CHUNKSIZE + x,
            chunkY * Chunk.CHUNKSIZE + y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }

    public static Vector3I toWorldPos(SubChunkCoord coord, int x, int y, int z) {
        return new Vector3I(coord.x * Chunk.CHUNKSIZE + x,
            coord.y * Chunk.CHUNKSIZE + y,
            coord.z * Chunk.CHUNKSIZE + z);
    }

    public static Vector3I toWorldPos(SubChunkCoord coord, Vector3I c) {
        return new Vector3I(coord.x * Chunk.CHUNKSIZE + c.X,
            coord.y * Chunk.CHUNKSIZE + c.Y,
            coord.z * Chunk.CHUNKSIZE + c.Z);
    }

    /// <summary>
    /// For chunks
    /// </summary>
    public static Vector3I toWorldPos(int chunkX, int chunkZ, int x, int y, int z) {
        return new Vector3I(chunkX * Chunk.CHUNKSIZE + x,
            y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }

    public static Vector3I toWorldPos(ChunkCoord coord, int x, int y, int z) {
        return new Vector3I(coord.x * Chunk.CHUNKSIZE + x,
            y,
            coord.z * Chunk.CHUNKSIZE + z);
    }

    public static Vector3I toWorldPos(ChunkCoord coord, Vector3I c) {
        return new Vector3I(coord.x * Chunk.CHUNKSIZE + c.X,
            c.Y,
            coord.z * Chunk.CHUNKSIZE + c.Z);
    }

    public int getHeight(int x, int z) {
        var pos = getChunkPos(x, z);
        if (!chunks.TryGetValue(pos, out var chunk)) {
            return 0;
        }
        
        var blockPos = getPosInChunk(x, z);
        return chunk.heightMap.get(blockPos.X, blockPos.Y);
    }

    public bool anyWater(int x0, int y0, int z0, int x1, int y1, int z1) {
        for (int x = x0; x <= x1; x++) {
            for (int y = y0; y <= y1; y++) {
                for (int z = z0; z <= z1; z++) {
                    if (getBlock(x, y, z) == Block.WATER.id) {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    
    /** This is like <see cref="anyWater"/> except that we know it's all in one chunk. Less lookups needed. */
    public bool anyWaterInChunk(int x0, int y0, int z0, int x1, int y1, int z1) {
        
        var chunk = getChunk(x0, z0);
        for (int x = x0; x <= x1; x++) {
            for (int y = y0; y <= y1; y++) {
                for (int z = z0; z <= z1; z++) {
                    if (chunk.getBlock(x & 0xF, y, z & 0xF) == Block.WATER.id) {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}