using BlockGame.main;
using BlockGame.render.model;
using BlockGame.util;
using Molten;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.world;

public class PlayerRenderer : EntityRenderer<Player> {
    public HumanModel model;

    public PlayerRenderer() {
        model = new HumanModel();
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        render(mat, e, scale, interp, forceRender: false);
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp, bool forceRender) {
        if (e is not Player player) return;

        // don't render player in first person (unless actually)
        if (!forceRender && Game.camera.mode == CameraMode.FirstPerson) {
            return;
        }

        var item = player.inventory.getSelected();
        model.armRaise = item != ItemStack.EMPTY;

        model.sneaking = player.sneaking;

        // interpolate animation state
        var apos = float.Lerp(player.papos, player.apos, (float)interp);
        var aspeed = float.Lerp(player.paspeed, player.aspeed, (float)interp);

        var ide = EntityRenderers.ide;

        // render the human model with animation
        ide.begin(PrimitiveType.Quads);
        model.render(mat, player, apos, aspeed, scale, interp);
        ide.end();

        // render damage tint overlay if player is taking damage
        if (player.dmgTime > 0) {
            const float t = 1;
            var tint = new Color((byte)255, (byte)0, (byte)0, (byte)(192 * t));
            ide.setColour(tint);
            ide.begin(PrimitiveType.Quads);
            model.render(mat, player, apos, aspeed, scale, interp);
            ide.end();
            ide.setColour(Color.White);
        }

        // Render hand item in third person!
        // Position at right arm location and render item
        // BETTER IDEA, we just also steal the rotation from the arm too!
        mat.push();
        const float sc = 1f / 16f;
        mat.translate(model.rightArm.position.X * sc, model.rightArm.position.Y * sc, model.rightArm.position.Z * sc);
        mat.rotate(model.rightArm.rotation.X, 1, 0, 0);
        mat.rotate(model.rightArm.rotation.Y, 0, 1, 0);
        mat.rotate(model.rightArm.rotation.Z, 0, 0, 1);

        // adjust to arm rotation
        //mat.translate((cosSwingSqrt) / 2f, 0, (sinSwingSqrt) / -3f);

        //player.handRenderer.render(interp);
        player.handRenderer.renderThirdPerson(mat, interp);
        mat.pop();
    }
}