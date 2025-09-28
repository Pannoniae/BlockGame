using BlockGame.GL;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

/** Renders an entity with its associated model. Optionally applies effects like being hit, etc. */
public interface EntityRenderer<out T> where T : Entity {

    /**
     * We originally had pos/rot here in addition to scale but I kinda realised you can just 1. use the matrix stack for that if you want to modify 2. get it from the entity itself
     */
    public virtual void render(MatrixStack mat, Entity e, float scale, double interp) {
        
    }
}

public class CowRenderer : EntityRenderer<Cow> {
    
    public void render(MatrixStack mat, Entity e, float scale, double interp) {
    }
}


public static class EntityRenderers {

    public static readonly EntityRenderer<Entity>[] renderers = new EntityRenderer<Entity>[Entities.ENTITYCOUNT];

    public static readonly InstantDrawEntity ide = new(2048);

    static EntityRenderers() {
        ide.setup();
        reloadAll();
    }

    /** hot reload all entity models by recreating them */
    public static void reloadAll() {
        renderers[Entities.COW] = new CowRenderer();
        renderers[Entities.PLAYER] = new PlayerRenderer();
        renderers[Entities.ITEM_ENTITY] = new ItemEntityRenderer();
    }
}