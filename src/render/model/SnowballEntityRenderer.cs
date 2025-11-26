using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGame.render.model;

/**
 * Renders snowballs using 3D voxelized item rendering
 */
public class SnowballEntityRenderer : ProjectileEntityRenderer<SnowballEntity> {
    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not SnowballEntity snowball) return;

        var lightVal = getLighting(snowball);
        var stack = new ItemStack(Item.SNOWBALL, 1);
        render(mat, snowball, stack, lightVal, interp);
    }
}
