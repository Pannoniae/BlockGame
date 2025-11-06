using BlockGame.world;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

public class MobRenderer<T> : EntityRenderer<T> where T : Mob {
    public EntityModel model;

    public MobRenderer(EntityModel model) {
        this.model = model;
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not Mob mob) return;

        // interpolate animation state
        var apos = float.Lerp(mob.papos, mob.apos, (float)interp);
        var aspeed = float.Lerp(mob.paspeed, mob.aspeed, (float)interp);
        
        var ide = EntityRenderers.ide;
        ide.begin(PrimitiveType.Quads);
        model.render(mat, mob, apos, aspeed, scale, interp);
        ide.end();
    }

}