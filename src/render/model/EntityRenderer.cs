using BlockGame.entity;

namespace BlockGame.model;

/** Renders an entity with its associated model. Optionally applies effects like being hit, etc. */
public interface EntityRenderer<out T> where T : Entity {
    
    public virtual void render(Entity entity, double t, double yaw, double pitch, double scale) {
        
    }
}

public class CowRenderer : EntityRenderer<Cow> {
    
    public void render(Entity cow, double t, double yaw, double pitch, double scale) {
    }
}


public static class EntityRenderers {

    public const int ENTITY_RENDERER_COUNT = 16;
    public static EntityRenderer<Entity>[] renderers = new EntityRenderer<Entity>[ENTITY_RENDERER_COUNT];

    static EntityRenderers() {
        renderers[0] = new CowRenderer();
    }
}