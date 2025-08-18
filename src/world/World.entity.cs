using BlockGame.util;
using Molten;

namespace BlockGame;

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

    public void getEntitiesInBox(List<Entity> result, Vector3I min, Vector3I max) {
        var minChunk = getChunkPos(min.X, min.Z);
        var maxChunk = getChunkPos(max.X, max.Z);
        
        for (int chunkX = minChunk.x; chunkX <= maxChunk.x; chunkX++) {
            for (int chunkZ = minChunk.z; chunkZ <= maxChunk.z; chunkZ++) {
                if (getChunkMaybe(new ChunkCoord(chunkX, chunkZ), out var chunk)) {
                    int minY = Math.Max(0, min.Y >> 4);
                    int maxY = Math.Min(Chunk.CHUNKHEIGHT - 1, max.Y >> 4);
                    
                    for (int y = minY; y <= maxY; y++) {
                        foreach (var entity in chunk!.entities[y]) {
                            var pos = entity.position.toBlockPos();
                            if (pos.X >= min.X && pos.X <= max.X &&
                                pos.Y >= min.Y && pos.Y <= max.Y &&
                                pos.Z >= min.Z && pos.Z <= max.Z) {
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
}