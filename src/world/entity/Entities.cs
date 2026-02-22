using BlockGame.main;
using BlockGame.render.model;
using BlockGame.util;
using BlockGame.util.stuff;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace BlockGame.world.entity;

public enum SpawnType {
    NONE,      // doesn't spawn naturally
    PASSIVE,   // animals
    HOSTILE,    // monsters
    CAVE,       // hostile but only spawns in caves
    JUNGLE     // spawns only in jungle biomes
}

public static class SpawnTypeExt {
    public static bool isHostile(this SpawnType type) => type is SpawnType.HOSTILE or SpawnType.CAVE or SpawnType.JUNGLE;
}

/**
 * Entity registry using string IDs.
 * Runtime uses int IDs for performance, string IDs map to sequential ints.
 * Added this so we can officially claim to be a factory game. :D
 */
public class Entities {
    public static int PLAYER;
    public static int ITEM_ENTITY;
    public static int FALLING_BLOCK;
    public static int ARROW;
    public static int SNOWBALL;
    public static int GRENADE;

    public static int COW;
    public static int PIG;
    public static int ZOMBIE;
    public static int EYE;
    public static int MUMMY;
    public static int DODO;
    public static int BIGEYE;
    public static int BOA;

    /** spawn metadata for each entity type */
    public static XUList<SpawnType> spawnType => Registry.ENTITIES.spawnType;

    public static void preLoad() {

        if (!Net.mode.isDed()) {
            EntityRenderers.preLoad();
        }

        PLAYER = register("player", w => new Player(w, 0, 0, 0));
        ITEM_ENTITY = register("item", w => new ItemEntity(w));
        FALLING_BLOCK = register("fallingBlock", w => new FallingBlockEntity(w));
        ARROW = register("arrow", w => new ArrowEntity(w));
        SNOWBALL = register("snowball", w => new SnowballEntity(w));
        GRENADE = register("grenade", w => new GrenadeEntity(w));
        COW = register("cow", w => new Cow(w));
        PIG = register("pig", w => new Pig(w));
        ZOMBIE = register("zombie", w => new Zombie(w));
        EYE = register("eye", w => new DemonEye(w));
        MUMMY = register("mummy", w => new Mummy(w));
        DODO = register("dodo", w => new Dodo(w));
        BIGEYE = register("BigEye", w => new BigEye(w));
        BOA = register("boa", w => new Boa(w));

        if (!Net.mode.isDed()) {
            EntityRenderers.reloadAll();
        }

        // set spawn types
        spawnType[COW] = SpawnType.PASSIVE;
        spawnType[PIG] = SpawnType.PASSIVE;
        spawnType[ZOMBIE] = SpawnType.HOSTILE;
        spawnType[EYE] = SpawnType.HOSTILE;
        spawnType[MUMMY] = SpawnType.CAVE;
        spawnType[DODO] = SpawnType.PASSIVE;
        spawnType[BIGEYE] = SpawnType.HOSTILE;
        spawnType[BOA] = SpawnType.JUNGLE;
    }

    /**
     * Register an entity type with a string ID.
     * Returns runtime int ID for fast lookups.
     */
    public static int register(string type, Func<World, Entity> factory) {
        return Registry.ENTITIES.register(type, factory);
    }

    /**
     * Create an entity instance by runtime int ID.
     */
    public static Entity? create(World world, int type) {
        var factory = Registry.ENTITIES.factory(type);
        return factory?.Invoke(world);
    }

    /**
     * Create an entity instance by string ID (used for loading saves).
     */
    public static Entity? create(World world, string type) {
        var factory = Registry.ENTITIES.factory(type);
        return factory?.Invoke(world);
    }

    /**
     * Get runtime int ID from string ID.
     */
    public static int getID(string id) {
        return Registry.ENTITIES.getID(id);
    }

    /**
     * Get string ID from runtime int ID.
     */
    public static string? getName(int id) {
        return Registry.ENTITIES.getName(id);
    }

    /**
     * Get total number of registered entities.
     */
    public static int count() => Registry.ENTITIES.count();
}