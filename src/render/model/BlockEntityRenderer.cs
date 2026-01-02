using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world;
using BlockGame.world.block;

namespace BlockGame.render.model;

/** Renders an entity with its associated model. Optionally applies effects like being hit, etc. */
public interface BlockEntityRenderer<out T> where T : BlockEntity {

    /**
     * We originally had pos/rot here in addition to scale but I kinda realised you can just 1. use the matrix stack for that if you want to modify 2. get it from the entity itself
     */
    public virtual void render(MatrixStack mat, BlockEntity be, float scale, double interp) {

    }
}

public static class BlockEntityRenderers {
    private static readonly XUList<BlockEntityRenderer<BlockEntity>> renderers = Registry.BLOCK_ENTITIES.track<BlockEntityRenderer<BlockEntity>>();

    static BlockEntityRenderers() {

    }

    public static void preLoad() {

    }

    /** Register a renderer for a block entity type */
    public static void register(int entityID, BlockEntityRenderer<BlockEntity> renderer) {
        renderers[entityID] = renderer;
    }

    /** Get renderer for a block entity type */
    public static BlockEntityRenderer<BlockEntity> get(int entityID) {
        return renderers[entityID];
    }

    /** hot reload all block entity models by recreating them */
    public static void reloadAll() {
        register(BlockEntity.SIGN, new SignRenderer());
        register(BlockEntity.FENCE, new FenceRenderer());
    }
}