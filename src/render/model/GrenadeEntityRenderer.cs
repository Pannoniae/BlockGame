using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;
using BlockGame.world.item;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

public class GrenadeEntityRenderer : EntityRenderer<GrenadeEntity> {
    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not GrenadeEntity grenade) {
            return;
        }

        mat.push();

        const float itemScale = 0.4f;
        mat.scale(itemScale, itemScale, itemScale);

        mat.translate(-0.5f, -0.5f, 0f);

        // get lighting
        var pos = grenade.position.toBlockPos();
        var l = grenade.world.getLightC(pos.X, pos.Y, pos.Z);
        var lightVal = WorldRenderer.getLightColour((byte)(l & 15), (byte)(l >> 4));

        // todo fix this up
        var fuseRatio = (float)grenade.fuseTime / GrenadeEntity.FUSE_LENGTH;
        var flashSpeed = 5.0f + (1.0f - fuseRatio) * 20.0f; // speeds up near explosion
        var flash = MathF.Sin((float)Game.permanentStopwatch.Elapsed.TotalSeconds * flashSpeed);

        // tint red when flashing
        Color tint =
            // red flash
            flash > 0 ? new Color((byte)255, (byte)(lightVal.G * 0.5f), (byte)(lightVal.B * 0.5f)) :
                // normal lighting
                lightVal;

        // create temporary ItemStack
        var stack = new ItemStack(Item.HAND_GRENADE, 1);


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