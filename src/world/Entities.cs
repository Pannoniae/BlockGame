using BlockGame.world.entity;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace BlockGame.world;

/**
 * Sadly can't do this like blocks because if you init an entity, it should exist in the world, etc.
 * Now of course there could be special "template" entities (like how items work in terraria) but
 * that's too much hassle.
 * Alternative: we just store the classes here and have a create function or something.
 */
public class Entities {
    public const int COW = 0;
    public const int PLAYER = 1;
    public const int ITEM_ENTITY = 2;

    public const int ENTITYCOUNT = 3;

    public static Type[] entityTypes = new Type[ENTITYCOUNT];

    public static Type? getType(int id) {
        if (id is < 0 or >= ENTITYCOUNT) {
            return null;
        }

        return entityTypes[id];
    }

    static Entities() {
        entityTypes[COW] = typeof(Cow);
        entityTypes[PLAYER] = typeof(Player);
        entityTypes[ITEM_ENTITY] = typeof(ItemEntity);
    }
}