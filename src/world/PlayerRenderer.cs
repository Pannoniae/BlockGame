using BlockGame.main;
using BlockGame.render.model;

namespace BlockGame.world;

public class PlayerRenderer : EntityRenderer<Player> {
    private HumanModel model;

    public PlayerRenderer() {
        model = new HumanModel();
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not Player player) return;

        // don't render player in first person
        if (Game.camera.mode == CameraMode.FirstPerson) {
            return;
        }

        // interpolate animation state
        var apos = float.Lerp(player.papos, player.apos, (float)interp);
        var aspeed = float.Lerp(player.paspeed, player.aspeed, (float)interp);

        // render the human model with animation
        model.render(mat, player, apos, aspeed, scale, interp);
    }
}