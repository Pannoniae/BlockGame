using BlockGame.main;
using BlockGame.net.srv;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using BlockGame.world.item;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public partial class World {
    public readonly XUList<Entity> entities;

    public readonly Particles particles;

    /** This only exists on the a client side world!! */
    public Player player;
    public Vector3D spawn;

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

        // if player add to the players list
        if (entity is Player p) {
            // only set world.player for local player (ClientPlayer), not remote players (Humanoid)
            if (p is ClientPlayer) {
                player = p;
            }
            players.Add(p);
        }

        // notify entity tracker on server
        if (Net.mode.isDed()) {
            GameServer.instance.entityTracker.trackEntity(entity);
        }
    }

    public void removeEntity(Entity entity, int i) {
        // notify entity tracker on server BEFORE removal
        if (Net.mode.isDed()) {
            GameServer.instance.entityTracker.untrackEntity(entity.id);
        }

        doRemove(entity);
        entities.RemoveAt(i);

        // if player remove from players list
        if (entity is Player p) {
            players.Remove(p);
            if (player == p) {
                player = null!;
            }
        }
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
        // notify entity tracker on server BEFORE removal
        if (Net.mode.isDed()) {
            GameServer.instance.entityTracker.untrackEntity(entity.id);
        }

        doRemove(entity);
        entities.Remove(entity);

        // if player remove from players list
        if (entity is Player p) {
            players.Remove(p);
            if (player == p) {
                player = null!;
            }
        }
    }

    /** spawn block drop as item entity with randomised position and velocity */
    public void spawnBlockDrop(int x, int y, int z, Item item, int count, int metadata) {

        if (Net.mode.isMPC()) {
            return;
        }

        if (count <= 0 || item == null) {
            return;
        }

        var itemEntity = new ItemEntity(this);
        itemEntity.stack = new ItemStack(item, count, metadata);
        itemEntity.position = new Vector3D(x + 0.5, y + 0.5, z + 0.5);

        // randomise pos
        itemEntity.position.X += (Game.clientRandom.NextSingle() - 0.5) * 0.25;
        itemEntity.position.Z += (Game.clientRandom.NextSingle() - 0.5) * 0.25;
        itemEntity.position.Y += Game.clientRandom.NextSingle() * 0.15;

        // add some random velocity
        var random = Game.clientRandom;
        itemEntity.velocity = new Vector3D(
            (random.NextSingle() - 0.5) * 0.3,
            random.NextSingle() * 0.3 + 0.1,
            (random.NextSingle() - 0.5) * 0.3
        );

        addEntity(itemEntity);
    }

    public void getPlayersInBox(List<Player> result, Vector3I min, Vector3I max) {
        result.Clear();

        // create query AABB from min/max bounds
        var queryAABB = new AABB(new Vector3D(min.X, min.Y, min.Z), new Vector3D(max.X + 1, max.Y + 1, max.Z + 1));

        // don't query, check players array because there are usually few players
        foreach (var entity in players) {
            // get entity's AABB and check intersection with the box
            if (AABB.isCollision(queryAABB, entity.aabb)) {
                result.Add(entity);
            }
        }
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

    public void getEntitiesInBox(List<Entity> result, AABB box) {
        getEntitiesInBox(result, box.min.toBlockPos(), box.max.toBlockPos());
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
                    var chunk = chunks[chunkCoord.toLong()];
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