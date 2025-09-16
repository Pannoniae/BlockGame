using BlockGame.render;
using BlockGame.util;
using BlockGame.world.chunk;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public partial class World {
    public readonly List<Entity> entities;
    public readonly Particles particles;
    public Player player;
    
    public void addEntity(Entity entity) {
        
        // search for matching chunk
        var success = getChunkMaybe((int)entity.position.X, (int)entity.position.Z, out var chunk);
        if (success && entity.position.Y is >= 0 and < WORLDHEIGHT) {
            // add entity to chunk
            chunk!.addEntity(entity);
            entity.inWorld = true;
        }
        else {
            entity.inWorld = false;
        }

        entities.Add(entity);
    }
    
    public void removeEntity(Entity entity) {
        if (entity.inWorld) {
            var success = getChunkMaybe((int)entity.position.X, (int)entity.position.Z, out var chunk);
            if (success) {
                chunk!.removeEntity(entity);
            }
            entity.inWorld = false;
        }
        entities.Remove(entity);
    }
    
    public void getEntitiesInBox(List<Entity> result, Vector3I min, Vector3I max) {
        result.Clear();
        var minChunk = getChunkPos(min.X, min.Z);
        var maxChunk = getChunkPos(max.X, max.Z);
        
        // create query AABB from min/max bounds
        var queryAABB = new AABB(new Vector3D(min.X, min.Y, min.Z), new Vector3D(max.X + 1, max.Y + 1, max.Z + 1));
        
        for (int chunkX = minChunk.x; chunkX <= maxChunk.x; chunkX++) {
            for (int chunkZ = minChunk.z; chunkZ <= maxChunk.z; chunkZ++) {
                if (getChunkMaybe(new ChunkCoord(chunkX, chunkZ), out var chunk)) {
                    int minY = Math.Max(0, min.Y >> 4);
                    int maxY = Math.Min(Chunk.CHUNKHEIGHT - 1, max.Y >> 4);
                    
                    for (int y = minY; y <= maxY; y++) {
                        foreach (var entity in chunk!.entities[y]) {
                            // get entity's AABB and check intersection with the box
                            if (AABB.isCollision(queryAABB, entity.aabb)) {
                                result.Add(entity);
                            }
                        }
                    }
                }
            }
        }
    }

    public List<Entity> getEntitiesInBox(Vector3I min, Vector3I max) {
        var result = new List<Entity>();
        getEntitiesInBox(result, min, max);
        return result;
    }
    
    public void loadEntitiesIntoChunk(ChunkCoord chunkCoord) {
        // check all entities not currently in world to see if they belong in this newly loaded chunk
        foreach (var entity in entities) {
            if (!entity.inWorld) {
                var entityChunkCoord = getChunkPos(entity.position.X.toBlockPos(), entity.position.Z.toBlockPos());
                if (entityChunkCoord.Equals(chunkCoord) && entity.position.Y is >= 0 and < WORLDHEIGHT) {
                    var chunk = chunks[chunkCoord];
                    chunk.addEntity(entity);
                    entity.inWorld = true;
                }
            }
        }
    }
}