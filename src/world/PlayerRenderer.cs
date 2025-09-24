using System.Numerics;
using BlockGame.render.model;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public class PlayerRenderer : EntityRenderer<Player> {
    private Player player;
    private HumanModel model;

    public PlayerRenderer(Player player) {
        this.player = player;
        model = new HumanModel();
    }

    public void render(MatrixStack mat, double interp) {
        // interpolate position and rotation
        var interpPos = Vector3D.Lerp(player.prevPosition, player.position, interp);
        var interpRot = Vector3.Lerp(player.prevRotation, player.rotation, (float)interp);

        mat.push();

        // translate to player position
        mat.translate((float)interpPos.X, (float)interpPos.Y, (float)interpPos.Z);

        // apply player rotation
        mat.rotate(interpRot.X, 1, 0, 0);
        mat.rotate(interpRot.Y, 0, 1, 0);
        mat.rotate(interpRot.Z, 0, 0, 1);

        // interpolate animation state
        var interpApos = float.Lerp(player.papos, player.apos, (float)interp);
        var interpAspeed = float.Lerp(player.paspeed, player.aspeed, (float)interp);

        // render the human model with animation
        model.render(mat, player, interpRot, 1.0f, interp);

        mat.pop();
    }
}