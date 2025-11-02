using BlockGame.world;

namespace BlockGame.render.model;

/** The geometry of an entity. */
public abstract class EntityModel {

    public virtual void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
    }
}