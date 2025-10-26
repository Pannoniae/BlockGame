using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class CowRenderer : EntityRenderer<Cow> {
    public CowModel model;

    public CowRenderer() {
        model = new CowModel();
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not Cow cow) return;

        // interpolate animation state
        var apos = float.Lerp(cow.papos, cow.apos, (float)interp);
        var aspeed = float.Lerp(cow.paspeed, cow.aspeed, (float)interp);

        // render the cow model with animation
        model.render(mat, cow, apos, aspeed, scale, interp);
    }
}
