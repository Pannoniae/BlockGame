using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGame.util.stuff;

public abstract class Registry {
    public static readonly BlockRegistry BLOCKS = new();
    public static readonly ItemRegistry ITEMS = new();
    public static readonly Registry<Recipe> RECIPES = new RecipeRegistry();
    public static readonly BlockEntityRegistry BLOCK_ENTITIES = new();
    public static readonly EntityRegistry ENTITIES = new();
    public static readonly EffectRegistry EFFECTS = new();
}

/**
 * I will do many pushups for this ;)) Enjoy!
 */
public abstract class Registry<T> : Registry {
    private int c = 0;

    public readonly XUList<T> values = [];

    /*
     * Maps string IDs to runtime int IDs.
     */
    public readonly Dictionary<string, int> nameToID = [];

    /*
     * Maps runtime int IDs to string IDs.
     */
    public readonly Dictionary<int, string> idToName = [];

    public readonly XUList<List> trackedLists = [];

    public Registry() {
    }

    /**
     * Register a type with a string ID.
     * Returns runtime int ID for fast lookups.
     */
    public virtual int register(string type, T value) {
        if (nameToID.ContainsKey(type)) {
            InputException.throwNew($"{typeof(T).Name} '{type}' is already registered!");
        }

        int itype = c++;
        nameToID[type] = itype;
        idToName[itype] = type;
        values.Add(value);

        foreach (var l in trackedLists) {
            l.add();
        }

        return itype;
    }

    public int count() {
        return c;
    }

    /**
     * Adds a property list to the registry with the given default value. When a value is added to the registry, a corresponding entry is added to the list.
     */
    public XUList<V> track<V>(V? dvalue = default) {
        var list = new XUList<V>();
        var l = new Lists<V>(list, dvalue!);
        trackedLists.Add(l);

        // fast-forward it
        for (int i = 0; i < c; i++) {
            l.add();
        }

        return list;
    }

    /**
     * Gets the runtime int ID for the given string ID, or -1 if not found.
     */
    public int getID(string id) {
        return nameToID.TryGetValue(id, out int iid) ? iid : -1;
    }

    /**
     * Gets the string ID for the given runtime int ID, or null if not found.
     */
    public string? getName(int id) {
        return idToName.TryGetValue(id, out string? str) ? str : null;
    }

    /**
     * Gets the entry for the given runtime int ID.
     */
    public T get(int id) {
        return values[id];
    }

    /**
     * Gets the entry for the given string ID.
     */
    public T get(string name) {
        int id = getID(name);
        if (id == -1) {
            InputException.throwNew($"{typeof(T).Name} '{name}' is not registered!");
        }

        return get(id);
    }

    /**
     * Gets the entry for the given string ID, or a fallback value if not found.
     */
    public T getOrDefault(string name, T defaultValue) {
        int id = getID(name);
        if (id == -1) {
            return defaultValue;
        }

        return get(id);
    }

    /**
     * Gets the entry for the given runtime int ID, or a fallback value if not found.
     */
    public T getOrDefault(int id, T defaultValue) {
        if (id < 0 || id >= values.Count) {
            return defaultValue;
        }

        return get(id);
    }

    public void clear() {
        nameToID.Clear();
        idToName.Clear();
        values.Clear();
        c = 0;
    }

    public bool contains(string name) {
        return nameToID.ContainsKey(name);
    }

    public bool contains(int id) {
        return idToName.ContainsKey(id);
    }

    public XUList<T> all() {
        return values;
    }

    public interface List {
        public void add();
    }

    public struct Lists<U> : List {
        public XUList<U> list;
        public U dvalue;

        public unsafe Lists(XUList<U> list, U defaultValue) {
            this.list = list;
            dvalue = defaultValue;
        }

        public void add() {
            list.Add(dvalue);
        }
    }
}

/**
 * Registry for objects that need to be instantiated via factories.
 * Used for entities, block entities, etc. where you can't construct them ahead of time.
 */
public class ObjectRegistry<T, TFactory> : Registry<TFactory> {
    /**
     * Get the factory function for the given ID.
     */
    public TFactory? factory(int id) {
        return getOrDefault(id, default!);
    }

    /**
     * Get the factory function for the given string ID.
     */
    public TFactory? factory(string id) {
        return getOrDefault(id, default!);
    }
}

public class EntityRegistry : ObjectRegistry<Entity, Func<World, Entity>> {
    public readonly XUList<SpawnType> spawnType;

    public EntityRegistry() {
        spawnType = track(SpawnType.NONE);
    }
}

public class BlockEntityRegistry : ObjectRegistry<BlockEntity, Func<BlockEntity>> {

    public readonly XUList<bool> hasRenderer;

    public BlockEntityRegistry() {
        hasRenderer = track(false);
    }
}