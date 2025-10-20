using BlockGame.render;
using BlockGame.util;
using BlockGame.world.chunk;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public partial class World {
    public readonly XUList<Entity> entities;

    public readonly Particles particles;
    public Player player;

    public static int ec = 1;

    public void addEntity(Entity entity) {
        // assign id if not already set!!
        if (entity.id == 0) {
            entity.id = ec++;
        }

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

    public void removeEntity(Entity entity, int i) {
        doRemove(entity);
        entities.RemoveAt(i);
    }

    private void doRemove(Entity entity) {
        if (entity.inWorld) {
            var success = getChunkMaybe(entity.subChunkCoord.toChunk(), out var chunk);
            if (success) {
                chunk!.removeEntity(entity);
            }

            entity.inWorld = false;
            entity.active = false;
        }
    }

    public void removeEntity(Entity entity) {
        doRemove(entity);
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
            if (!entity.inWorld && entity.subChunkCoord.toChunk().Equals(chunkCoord)) {
                var y = entity.subChunkCoord.y;
                if (y >= 0 && y < Chunk.CHUNKHEIGHT) {
                    var chunk = chunks[chunkCoord];
                    chunk.addEntity(entity);
                    entity.inWorld = true;
                }
            }
        }
    }

    public void updateEntities(double dt) {

        //Console.Out.WriteLine(entities.Count);

        // player chunk entities
        //getChunkMaybe(player.subChunkCoord.toChunk(), out var pc);
        //Console.Out.WriteLine("Player chunk entities: " + (pc != null ? pc.entities[player.subChunkCoord.y].Count : 0));

        for (int i = entities.Count - 1; i >= 0; i--) {
            Entity entity = entities[i];
            if (entity.active) {
                updateEntity(entity, dt);
            }

            // we check again because entity might have suicided in update
            if (!entity.active) {
                removeEntity(entity, i);
            }
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

        //Console.Out.WriteLine("hi1! " + oldChunkPos + " -> " + newChunkPos);

        // clamp
        oldChunkPos = new SubChunkCoord(oldChunkPos.x, Math.Clamp(oldChunkPos.y, 0, Chunk.CHUNKHEIGHT - 1), oldChunkPos.z);
        newChunkPos = new SubChunkCoord(newChunkPos.x, Math.Clamp(newChunkPos.y, 0, Chunk.CHUNKHEIGHT - 1), newChunkPos.z);

        // if it doesn't match, remove
        if (newChunkPos != oldChunkPos || !e.inWorld) {


            // has chunk at old?
            if (getChunkMaybe(oldChunkPos.toChunk(), out var oldChunk)) {
                //Console.Out.WriteLine("yes? " + oldChunkPos + " -> " + newChunkPos);
                oldChunk!.removeEntity(e);
            }


            // has chunk at new?
            if (getChunkMaybe(newChunkPos.toChunk(), out var newChunk)) {
                //Console.Out.WriteLine("yes2? " + oldChunkPos + " -> " + newChunkPos);
                newChunk!.addEntity(e);
                e.inWorld = true;
            }
            else {
                e.inWorld = false;
            }


            if (newChunkPos != oldChunkPos) {
                //Console.Out.WriteLine("hi! " + oldChunkPos + " -> " + newChunkPos);
                //Console.Out.WriteLine("asdasdasd!");
                e.onChunkChanged();
            }
        }
    }
}