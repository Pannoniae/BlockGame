using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class DodoModel : EntityModel {

    public const int xs = 100;
    public const int ys = 100;

    public readonly Cube body = new Cube().pos(0, 24, 0).off(0, -15, 0).ext(15, 15, 10).tex(0, 0).gen(xs, ys);
    //public readonly Cube nest = new Cube().pos(-8, 27, 2).off(0, -16, 0).ext(8, 16, 6).tex(53, 0).gen(xs, ys);
    public readonly Cube nest = new Cube().pos(-8, 37, 2).off(0, -26, 0).ext(9, 26, 6).tex(53, 0).gen(xs, ys);
    public readonly Cube back = new Cube().pos(15, 23, 0).off(0, -12, 0).ext(6, 12, 10).tex(0, 26).gen(xs, ys);
    public readonly Cube tail= new Cube().pos(18, 26, 2.5f).off(0, -5, 0).ext(5, 5, 5).tex(0, 49).gen(xs, ys);
    public readonly Cube upperBeak = new Cube().pos(-16, 36, 3).off(0, -7, 0).ext(14, 7, 4).tex(38, 63).gen(xs, ys);
    public readonly Cube lowerBeak = new Cube().pos(-15, 29, 3).off(0, -2, 0).ext(13, 2, 4).tex(38, 48).gen(xs, ys);
    public readonly Cube rightLeg = new Cube().pos(5, 9, 0).off(0, -9, 0).ext(3, 9, 3).tex(23, 49).gen(xs, ys);
    public readonly Cube leftLeg = new Cube().pos(5, 9, 7).off(0, -9, 0).ext(3, 9, 3).tex(23, 49).gen(xs, ys);


    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        // set dodo texture
        Game.graphics.tex(0, Game.textures.get(e.tex));

        // calculate interpolated rotations
        //var interpRot = Vector3.Lerp(e.prevRotation, e.rotation, (float)interp);
        //var interpBodyRot = Vector3.Lerp(e.prevBodyRotation, e.bodyRotation, (float)interp);

        // calculate head rotation relative to body
        //var headRotY = interpRot.Y - interpBodyRot.Y;

        //float cs = Meth.clamp(aspeed, 0, 1);
        //float lr = MathF.Sin(apos * 10) * 20f * cs * Meth.phiF;

        //upperBeak.rotation = new Vector3(0, headRotY, 0);
        //rightLeg.rotation = new Vector3(lr, 0, 0);
        //leftLeg.rotation = new Vector3(-lr, 0, 0);

        // render dodo
        var ide = EntityRenderers.ide;

        upperBeak.xfrender(ide, mat, scale);
        lowerBeak.xfrender(ide, mat, scale);
        nest.xfrender(ide, mat, scale);
        body.xfrender(ide, mat, scale);
        back.xfrender(ide, mat, scale);
        tail.xfrender(ide, mat, scale);
        rightLeg.xfrender(ide, mat, scale);
        leftLeg.xfrender(ide, mat, scale);
        }
}