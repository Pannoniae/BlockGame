using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGame.render.model;

/**
 * Renders grenades using 3D voxelized item rendering with flashing effect
 */
public class GrenadeEntityRenderer : ProjectileEntityRenderer<GrenadeEntity> {
    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not GrenadeEntity grenade) return;

        var lightVal = getLighting(grenade);

        // grenade-specific flashing effect
        var fuseRatio = (float)grenade.fuseTime / GrenadeEntity.FUSE_LENGTH;
        var flashSpeed = 5.0f + (1.0f - fuseRatio) * 20.0f; // speeds up near explosion
        var flash = MathF.Sin((float)Game.permanentStopwatch.Elapsed.TotalSeconds * flashSpeed);

        // tint red when flashing
        Color tintColor = flash > 0
            ? new Color((byte)255, (byte)(lightVal.G * 0.5f), (byte)(lightVal.B * 0.5f))
            : lightVal;

        var stack = new ItemStack(Item.HAND_GRENADE, 1);
        render(mat, grenade, stack, tintColor, interp);
    }
}
