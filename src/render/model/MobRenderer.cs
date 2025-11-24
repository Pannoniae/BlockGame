using BlockGame.world;
using BlockGame.world.entity;
using Silk.NET.OpenGL.Legacy;
using Entity = BlockGame.world.entity.Entity;

namespace BlockGame.render.model;

public class MobRenderer<T> : EntityRenderer<T> where T : Mob {
    public EntityModel model;

    public MobRenderer() {

    }
    public MobRenderer(EntityModel model) {
        this.model = model;
    }

    public virtual void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not Mob mob) return;

        // interpolate animation state
        var apos = float.Lerp(mob.papos, mob.apos, (float)interp);
        var aspeed = float.Lerp(mob.paspeed, mob.aspeed, (float)interp);



        
        var ide = EntityRenderers.ide;

        // we need to set the matrixstack here again to dirty it! stupid system I know but shhh
        ide.model(mat);

        ide.begin(PrimitiveType.Quads);
        model.render(mat, mob, apos, aspeed, scale, interp);


        // render damage tint overlay if player is taking damage
        if (false && mob.dmgTime > 0) {
            const float t = 1;
            var tint = new Color((byte)255, (byte)0, (byte)0, (byte)(128 * t));
            ide.setColour(tint);
            model.render(mat, mob, apos, aspeed, scale, interp);

        }
        ide.end();
        ide.setColour(Color.White);
    }

}