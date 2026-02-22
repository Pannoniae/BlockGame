using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class DodoModel : EntityModel {

    public const int xs = 96;
    public const int ys = 72;

    public readonly Cube body = new Cube().pos(0, 21, -17).off(0, -15, 0).ext(15, 15, 17).tex(0, 0).gen(xs, ys);
    public readonly Cube nest = new Cube().pos(4, 34, -2).off(0, -23, 0).ext(7, 23, 7).tex(0, 32).gen(xs, ys);
    public readonly Cube back = new Cube().pos(3, 18, -23).off(0, -11, 0).ext(9, 11, 6).tex(66, 0).gen(xs, ys);
    public readonly Cube tail= new Cube().pos(5, 21, -25).off(0, -5, 0).ext(5, 5, 5).tex(32, 32).gen(xs, ys);
    public readonly Cube upperBeak = new Cube().pos(5.5f, 32, 0).off(0, -4, 0).ext(4, 4, 17).tex(54, 32).gen(xs, ys);
    public readonly Cube lowerBeak = new Cube().pos(5.5f, 27.5f, 0).off(0, -2, 0).ext(4, 2, 16).tex(54, 54).gen(xs, ys);
    public readonly Cube rightLeg = new Cube().pos(0, 6, -9).off(0, -5, -2).ext(2, 5, 2).tex(32, 48).gen(xs, ys);
    public readonly Cube leftLeg = new Cube().pos(13, 6, -9).off(0, -5, -2).ext(2, 5, 2).tex(32, 48).gen(xs, ys);
    public readonly Cube rightfoot = new Cube().pos(0, 6, -9).off(-1, -6, -3).ext(4, 1, 7).tex(32, 56).gen(xs, ys);
    public readonly Cube leftfoot = new Cube().pos(13, 6, -9).off(-1, -6, -3).ext(4, 1, 7).tex(32, 56).gen(xs, ys);

    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        // set dodo texture
        Game.graphics.tex(0, Game.textures.dodo);

        // calculate interpolated rotations
        var interpRot = Vector3.Lerp(e.prevRotation, e.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(e.prevBodyRotation, e.bodyRotation, (float)interp);

        // calculate head rotation relative to body
        var headRotY = interpRot.Y - interpBodyRot.Y;
        nest.rotation = new Vector3(0, headRotY, 0);
        upperBeak.rotation = new Vector3(0, headRotY, 0);
        lowerBeak.rotation = new Vector3(0, headRotY, 0);

        // set leg movement
        float cs = Meth.clamp(aspeed, 0, 1);
        float lr = MathF.Sin(apos * 10) * 20f * cs * Meth.phiF;
        rightLeg.rotation = new Vector3(lr, 0, 0);
        leftLeg.rotation = new Vector3(-lr, 0, 0);
        rightfoot.rotation = new Vector3(lr, 0, 0);
        leftfoot.rotation = new Vector3(-lr, 0, 0);

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
        rightfoot.xfrender(ide, mat, scale);
        leftfoot.xfrender(ide, mat, scale);
        }
}