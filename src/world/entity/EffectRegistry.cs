using BlockGame.util;

namespace BlockGame.world.entity;

/**
 * Registry for effect types.
 * Maps int IDs <-> string IDs for networking.
 *
 * todo migrate to a normal registry
 */
public class EffectRegistry {
    private int c = 0;

    public readonly Dictionary<string, int> nameToID = [];
    public readonly Dictionary<int, string> idToName = [];

    public static int REGEN;

    /**
     * Register an effect type with a string ID.
     * Returns runtime int ID for fast lookups.
     */
    public int register(string name) {
        if (nameToID.ContainsKey(name)) {
            InputException.throwNew($"Effect '{name}' is already registered!");
        }

        int id = c++;
        nameToID[name] = id;
        idToName[id] = name;
        return id;
    }

    /**
     * Gets the runtime int ID for the given string ID, or -1 if not found.
     */
    public int getID(string name) {
        return nameToID.TryGetValue(name, out int id) ? id : -1;
    }

    /**
     * Gets the string ID for the given runtime int ID, or null if not found.
     */
    public string? getName(int id) {
        return idToName.TryGetValue(id, out string? name) ? name : null;
    }

    /**
     * Initialize all effect types. Called on game startup.
     */
    public static void init(EffectRegistry reg) {
        REGEN = reg.register("regen");
    }
}
