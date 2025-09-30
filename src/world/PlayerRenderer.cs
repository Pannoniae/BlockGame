using BlockGame.main;
using BlockGame.render.model;
using BlockGame.util;

namespace BlockGame.world;

public class PlayerRenderer : EntityRenderer<Player> {
    public HumanModel model;

    public PlayerRenderer() {
        model = new HumanModel();
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not Player player) return;

        // don't render player in first person
        if (Game.camera.mode == CameraMode.FirstPerson) {
            return;
        }

        var item = player.survivalInventory.getSelected();
        model.armRaise = item != ItemStack.EMPTY;

        model.sneaking = player.sneaking;

        // interpolate animation state
        var apos = float.Lerp(player.papos, player.apos, (float)interp);
        var aspeed = float.Lerp(player.paspeed, player.aspeed, (float)interp);

        // get light level at player position and look up in lightmap
        var pos = player.position.toBlockPos();
        var light = player.world.inWorld(pos.X, pos.Y, pos.Z) ? player.world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;
        var blocklight = (byte)((light >> 4) & 0xF);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);

        // render the human model with animation
        model.render(mat, player, apos, aspeed, scale, interp, lightVal.R, lightVal.G, lightVal.B);

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