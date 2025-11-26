using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

/**
 * Base class for projectile entity renderers.
 * Consolidates common rendering patterns: lighting lookup, 3D voxelized item rendering
 */
public abstract class ProjectileEntityRenderer<T> : EntityRenderer<T> where T : ProjectileEntity {
    protected const float ITEM_SCALE = 0.4f;

    // unified lighting lookup
    public Color getLighting(T projectile) {
        var pos = projectile.position.toBlockPos();
        var l = projectile.world.getLightC(pos.X, pos.Y, pos.Z);
        return WorldRenderer.getLightColour((byte)(l & 15), (byte)(l >> 4));
    }

    protected void render(MatrixStack mat, T proj, ItemStack stack, Color tint, double interp) {
        mat.push();
        mat.scale(ITEM_SCALE, ITEM_SCALE, ITEM_SCALE);
        mat.translate(-0.5f, -0.5f, 0f);

        var idt = Game.graphics.idt;
        idt.begin(PrimitiveType.Quads);
        idt.setTexture(Game.textures.itemTexture);

        Game.player.handRenderer.renderItemInHand(stack, tint);

        idt.model(mat);
        idt.view(Game.camera.getViewMatrix(interp));
        idt.proj(Game.camera.getProjectionMatrix());
        idt.applyMat();
        idt.end();
        mat.pop();
    }
}