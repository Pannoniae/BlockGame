using Molten;

namespace BlockGame;

public partial class World {
    public readonly List<Entity> entities;
    public readonly Particles particles;
    public Player player;
    
    public void addEntity(Entity entity) {
        
        // search for matching chunk
        var success = getChunkMaybe((int)entity.position.X, (int)entity.position.Z, out var chunk);
        if (success && entity.position.Y is >= 0 and < WORLDHEIGHT) {
            // add entity to chunk
            chunk!.addEntity(entity);
            entity.inWorld = true;
        }
        else {
            entity.inWorld = false;
        }

        entities.Add(entity);
    }

    public static void getEntitiesInBox(List<Entity> result, Vector3I min, Vector3I max) {
        // fill
    }

    public List<Entity> getEntitiesInBox(Vector3I min, Vector3I max) {
        var result = new List<Entity>();
        getEntitiesInBox(result, min, max);
        return result;
    }
}