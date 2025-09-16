namespace BlockGame.world;

/**
 * Sadly can't do this like blocks because if you init an entity, it should exist in the world, etc.
 * Now of course there could be special "template" entities (like how items work in terraria) but
 * that's too much hassle.
 * Alternative: we just store the classes here and have a create function or something.
 */
public class Entities {
    public const int ENTITYCOUNT = 1;
    
    
    public static Entity[] entities = new Entity[ENTITYCOUNT];
    
    public static Entity get(int id) {
        if (id is < 0 or >= ENTITYCOUNT) {
            return null;
        }
        return entities[id];
    }
    
    public static void init() {
        
    }
}