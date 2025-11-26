using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;
using BlockGame.world.item;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

// todo reuse ItemEntityRenderer somehow, with rot disabled? the copypaste is grating me
public class SnowballEntityRenderer : EntityRenderer<SnowballEntity> {
    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not SnowballEntity snowball) {
            return;
        }

        mat.push();

        const float itemScale = 0.4f;
        mat.scale(itemScale, itemScale, itemScale);

        mat.translate(-0.5f, -0.5f, 0f);

        // get lighting
        var pos = snowball.position.toBlockPos();
        var l = snowball.world.getLightC(pos.X, pos.Y, pos.Z);
        var lightVal = WorldRenderer.getLightColour((byte)(l & 15), (byte)(l >> 4));

        // create temporary ItemStack
        var stack = new ItemStack(Item.SNOWBALL, 1);

        var idt = Game.graphics.idt;
        idt.begin(PrimitiveType.Quads);
        idt.setTexture(Game.textures.itemTexture);

        Game.player.handRenderer.renderItemInHand(stack, lightVal);

        idt.model(mat);
        idt.view(Game.camera.getViewMatrix(interp));
        idt.proj(Game.camera.getProjectionMatrix());
        idt.applyMat();
        idt.end();

        mat.pop();
    }
}
