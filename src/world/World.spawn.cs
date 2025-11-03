using BlockGame.util;
using BlockGame.util.log;
using BlockGame.world.block;
using Molten.DoublePrecision;

namespace BlockGame.world;

public partial class World {
    /** spawn attempt every N ticks */
    private const int SPAWN_INTERVAL = 60;

    /** how many spawn attempts per cycle */
    private const int SPAWN_ATTEMPTS = 24;

    /** max distance from player for spawning */
    private const int SPAWN_RADIUS = 128;
    private const int SPAWN_RADIUS_Y = 24;

    /** Y distance weight multiplier (so the mobcap isn't wasted on random cave mobs and shit) */
    private const double Y_WEIGHT = 3.0;

    /** mob caps */
    private const int MAX_PASSIVE = 20;
    private const int MAX_HOSTILE = 70;

    /** min distance from player to spawn */
    private const int MIN_SPAWN_DIST = 16;


    /** check if position is valid for spawning */
    private bool spawnAt(int x, int y, int z, bool needsLight) {
        if (y is <= 0 or >= WORLDHEIGHT - 2) {
            return false;
        }

        if (!inWorld(x, y, z)) {
            return false;
        }

        var below = getBlock(x, y - 1, z);
        if (!Block.isFullBlock(below)) {
            return false;
        }

        // needs air (or grass) at spawn pos and above
        var at = getBlock(x, y, z);
        var above = getBlock(x, y + 1, z);

        bool atClear = at == Block.AIR.id || at == Block.TALL_GRASS.id || at == Block.SHORT_GRASS.id;
        bool aboveClear = above == Block.AIR.id || above == Block.TALL_GRASS.id || above == Block.SHORT_GRASS.id;

        if (!atClear || !aboveClear) {
            return false;
        }

        if (!getChunkMaybe(x, z, out var chunk)) {
            return false;
        }

        // skillcheck
        if (needsLight) {
            // animals need light (skylight > 8)
            var skylight = chunk.getSkyLight(x & 15, y, z & 15);
            if (skylight <= 8) {
                return false;
            }
        } else {
            // hostiles need darkness (block light < 4)
            var blocklight = chunk.getBlockLight(x & 15, y, z & 15);
            if (blocklight >= 4) {
                return false;
            }
        }

        return true;
    }

    /** check if mob can spawn on this block */
    private static bool spawnOn(ushort blockId, bool passive) {
        if (passive) {
            // animals spawn on grass/dirt
            return blockId == Block.GRASS.id || blockId == Block.DIRT.id;
        } else {
            // hostiles spawn on any solid block
            return Block.isFullBlock(blockId);
        }
    }

    /** attempt to spawn one mob */
    private bool spawnMob(SpawnType type) {
        var px = (int)player.position.X;
        var py = (int)player.position.Y;
        var pz = (int)player.position.Z;

        // try a few positions, hope it works out
        for (int a = 0; a < 8; a++) {
            var xo = random.Next(-SPAWN_RADIUS, SPAWN_RADIUS + 1);
            var yo = random.Next(-SPAWN_RADIUS_Y, SPAWN_RADIUS_Y + 1);
            var zo = random.Next(-SPAWN_RADIUS, SPAWN_RADIUS + 1);

            var x = px + xo;
            var y = py + yo;
            var z = pz + zo;

            // check weighted distance (Y matters more, biases against vertical spawns)
            var dx = x - player.position.X;
            var dy = (y - player.position.Y) * Y_WEIGHT;
            var dz = z - player.position.Z;
            var dsq = dx * dx + dy * dy + dz * dz;

            // too close?
            if (dsq < MIN_SPAWN_DIST * MIN_SPAWN_DIST) {
                continue;
            }

            // too far?
            if (dsq > SPAWN_RADIUS * SPAWN_RADIUS) {
                continue;
            }

            // valid spawn pos?
            bool needsLight = type == SpawnType.PASSIVE;
            if (!spawnAt(x, y, z, needsLight)) {
                continue;
            }

            // valid surface?
            var below = getBlock(x, y - 1, z);
            if (!spawnOn(below, needsLight)) {
                continue;
            }

            // pick random mob of this type from registry
            var types = Entities.spawnType;
            var mobs = new XUList<int>();
            for (int i = 0; i < types.Count; i++) {
                if (types[i] == type) {
                    mobs.Add(i);
                }
            }

            if (mobs.Count == 0) {
                Log.warn($"No candidates for spawn type {type}!");
                return false;
            }

            var mobType = mobs[random.Next(mobs.Count)];

            // determine pack size (passives spawn in groups of 2-4, hostiles spawn solo)
            int packSize = type == SpawnType.PASSIVE ? random.Next(2, 5) : 1;
            int spawned = 0;

            // spawn pack around initial position
            for (int p = 0; p < packSize; p++) {

                // offset for pack members (first one at exact spot)
                int nx = x, ny = y, nz = z;
                if (p > 0) {
                    nx += random.Next(-4, 5);
                    nz += random.Next(-4, 5);
                    ny += random.Next(-1, 2);

                    if (!spawnAt(nx, ny, nz, type == SpawnType.PASSIVE)) {
                        continue;
                    }

                    var nbelow = getBlock(nx, ny - 1, nz);
                    if (!spawnOn(nbelow, type == SpawnType.PASSIVE)) {
                        continue;
                    }
                }

                var mob = Entities.create(this, mobType);
                if (mob == null) {
                    if (p == 0) Log.warn($"Failed to create mob of type ID {mobType} for spawning.");
                    continue;
                }

                mob.position = new Vector3D(nx + 0.5, ny, nz + 0.5);
                addEntity(mob);
                spawned++;
            }

            return spawned > 0;
        }

        return false;
    }

    public void updateSpawning() {
        // only run every SPAWN_INTERVAL ticks
        if (worldTick % SPAWN_INTERVAL != 0) {
            return;
        }

        // count loaded mobs
        int passives = 0;
        int hostiles = 0;
        foreach (var e in entities) {
            if (e is not Mob || !e.inWorld) {
                continue;
            }

            var type = Entities.spawnType[Entities.getID(e.type)];
            if (type == SpawnType.PASSIVE) passives++;
            else if (type == SpawnType.HOSTILE) hostiles++;
        }

        // spawn attempts
        for (int i = 0; i < SPAWN_ATTEMPTS; i++) {
            // decide passive vs hostile (75% hostile, 25% passive)
            var type = random.Next(4) == 0 ? SpawnType.PASSIVE : SpawnType.HOSTILE;

            // check caps
            if (type == SpawnType.PASSIVE && passives >= MAX_PASSIVE) {
                continue;
            }
            if (type == SpawnType.HOSTILE && hostiles >= MAX_HOSTILE) {
                continue;
            }

            spawnMob(type);
        }
    }
}