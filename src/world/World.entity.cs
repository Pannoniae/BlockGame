using BlockGame.render;
using BlockGame.util;
using BlockGame.world.chunk;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public partial class World {
    public readonly List<Entity> entities;

    /** Entities that are scheduled for removal at the end of the tick. They aren't removed yet!!! */
    public readonly List<Entity> removedEntities;

    public readonly Particles particles;
    public Player player;

    public void addEntity(Entity entity) {
        // search for matching chunk
        var pos = entity.position.toBlockPos();
        var success = getChunkMaybe(pos.X, pos.Z, out var chunk);
        if (success) {
            // add entity to chunk
            chunk!.addEntity(entity);
            entity.inWorld = true;
        }

        entities.Add(entity);
    }

    public void removeEntity(Entity entity) {
        if (entity.inWorld) {
            var pos = entity.prevPosition.toBlockPos();
            var success = getChunkMaybe(pos.X, pos.Z, out var chunk);
            if (success) {
                chunk!.removeEntity(entity);
            }

            entity.inWorld = false;
            entity.active = false;
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

    public void updateEntities(double dt) {
        // first, remove entities which don't belong
        foreach (Entity e in removedEntities) {
            removeEntity(e);
        }

        removedEntities.Clear();

        foreach (var entity in entities) {
            if (entity.active) {
                updateEntity(entity, dt);
            }
            else {
                removedEntities.Add(entity);
            }
        }

        // remove entities that are scheduled for removal *again* ??
        foreach (var entity in removedEntities) {
            removeEntity(entity);
        }
    }

    /**
     * This exists separately so the chunk tracking can't be missed!!
     */
    public void updateEntity(Entity e, double dt) {
        e.update(dt);

        // you handle chunk transfers AFTER the entity has updated itself because it might have moved!!
        // remove from old chunk, add to new chunk
        var pos = e.position.toBlockPos();
        var newChunkPos = getChunkSectionPos(pos.X, pos.Y, pos.Z);
        var oldChunkPos = e.subChunkCoord;

        // if it doesn't match, remove
        if (newChunkPos != oldChunkPos || !e.inWorld) {
            // has chunk at old?
            if (getChunkMaybe(oldChunkPos.toChunk(), out var oldChunk)) {
                oldChunk!.removeEntity(e);
            }


            // has chunk at new?
            if (getChunkMaybe(newChunkPos.toChunk(), out var newChunk)) {
                newChunk!.addEntity(e);
                e.inWorld = true;
            }
            else {
                e.inWorld = false;
            }

            // clamp
            newChunkPos = new SubChunkCoord(newChunkPos.x, Math.Clamp(newChunkPos.y, 0, Chunk.CHUNKHEIGHT - 1), newChunkPos.z);
            if (newChunkPos != oldChunkPos) {
                //Console.Out.WriteLine("asdasdasd!");
                e.onChunkChanged();
            }
        }
    }
}