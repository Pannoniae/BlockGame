using BlockGame.render.model;
using BlockGame.util.stuff;
using BlockGame.world.entity;
using Core.world.entity;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace BlockGame.world;

/**
 * Entity registry using string IDs.
 * Runtime uses int IDs for performance, string IDs map to sequential ints.
 * Added this so we can officially claim to be a factory game. :D
 */
public class Entities {

    public static int PLAYER;
    public static int ITEM_ENTITY;

    public static int COW;
    public static int PIG;

    public static void preLoad() {
        EntityRenderers.preLoad();
        PLAYER = register("player", w => new Player(w, 0, 0, 0));
        ITEM_ENTITY = register("item", w => new ItemEntity(w));
        COW = register("cow", w => new Cow(w));
        PIG = register("pig", w => new Pig(w));
        EntityRenderers.reloadAll();

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