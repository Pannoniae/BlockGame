using System.Diagnostics.CodeAnalysis;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace BlockGame.world;

/**
 * Sadly can't do this like blocks because if you init an entity, it should exist in the world, etc.
 * Now of course there could be special "template" entities (like how items work in terraria) but
 * that's too much hassle.
 * Alternative: we just store the classes here and have a create function or something.
 */

[SuppressMessage("Compiler", "CS8618:Non-nullable field must contain a non-null value when exiting constructor. Consider adding the \'required\' modifier or declaring as nullable.")]
public class Entities {
    public const int ENTITYCOUNT = 1;
    
    
    public static Entity[] entities = new Entity[ENTITYCOUNT];
    
    public static Entity? get(int id) {
        if (id is < 0 or >= ENTITYCOUNT) {
            return null;
        }
        return entities[id];
    }
    
    public static void init() {
        
    }
}