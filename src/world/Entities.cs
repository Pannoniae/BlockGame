using BlockGame.util;
using BlockGame.world.entity;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace BlockGame.world;

/**
 * Entity registry using string IDs.
 * Runtime uses int IDs for performance, string IDs map to sequential ints.
 * Added this so we can officially claim to be a factory game. :D
 *
 * TODO should we return the runtime int ID on create()? Or just the entity instance? (But then you create entities without adding them to the world...)
 */
public class Entities {
    private static readonly Dictionary<string, int> stringToID = new();
    private static readonly Dictionary<int, string> idToString = new();
    private static readonly List<Func<World, Entity>> factories = [];
    private static int c = 0;

    public static readonly int COW = register("cow", w => new Cow(w));
    public static readonly int PLAYER = register("player", w => new Player(w,0, 0, 0));
    public static readonly int ITEM_ENTITY = register("item", w => new ItemEntity(w));

    /**
     * Register an entity type with a string ID.
     * Returns runtime int ID for fast lookups.
     */
    public static int register(string type, Func<World, Entity> factory) {
        if (stringToID.ContainsKey(type)) {
            InputException.throwNew($"Entity '{type}' is already registered!");
        }

        int itype = c++;
        stringToID[type] = itype;
        idToString[itype] = type;
        factories.Add(factory);
        return itype;
    }

    /**
     * Create an entity instance by runtime int ID.
     */
    public static Entity? create(World world, int type) {
        if (type < 0 || type >= factories.Count) return null;
        return factories[type](world);
    }

    /**
     * Create an entity instance by string ID (used for loading saves).
     */
    public static Entity? create(World world, string type) {
        if (stringToID.TryGetValue(type, out int id)) {
            return create(world, id);
        }
        return null;
    }

    /**
     * Get runtime int ID from string ID.
     */
    public static int getID(string id) {
        return stringToID.TryGetValue(id, out int iid) ? iid : -1;
    }

    /**
     * Get string ID from runtime int ID.
     */
    public static string? getStringID(int id) {
        return idToString.TryGetValue(id, out string? str) ? str : null;
    }

    /**
     * Get total number of registered entities.
     */
    public static int count() => factories.Count;
}