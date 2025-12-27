using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class DodoModel : EntityModel {

    public const int xs = 96;
    public const int ys = 64;

    /*public readonly Cube body = new Cube().pos(0, 22, 0).off(0, -15, 0).ext(23, 15, 15).tex(0, 0).gen(xs, ys);
    public readonly Cube nest = new Cube().pos(1, 37, 4).off(0, -15, 0).ext(7, 15, 7).tex(0, 32).gen(xs, ys);
    public readonly Cube back = new Cube().pos(23, 20, 2.5f).off(0, -11, 0).ext(6, 11, 11).tex(32, 32).gen(xs, ys);
    public readonly Cube tail= new Cube().pos(27, 23, 5.5f).off(0, -5, 0).ext(5, 5, 5).tex(0, 56).gen(xs, ys);
    public readonly Cube upperBeak = new Cube().pos(-11, 35, 5.5f).off(0, -5, 0).ext(17, 5, 4).tex(54, 69).gen(xs, ys);
    public readonly Cube lowerBeak = new Cube().pos(-10, 29.9f, 5.5f).off(0, -2, 0).ext(16, 2, 4).tex(13, 74).gen(xs, ys);
    public readonly Cube rightLeg = new Cube().pos(13, 7, 0).off(0, -7, 0).ext(2, 7, 2).tex(0, 68).gen(xs, ys);
    public readonly Cube leftLeg = new Cube().pos(13, 7, 12).off(0, -7, 0).ext(2, 7, 2).tex(0, 68).gen(xs, ys);
    */

    public readonly Cube body = new Cube().pos(0, 21, 0).off(0, -15, 0).ext(17, 15, 15).tex(0, 0).gen(xs, ys);
    public readonly Cube nest = new Cube().pos(-5, 34, 4).off(0, -23, 0).ext(7, 23, 7).tex(0, 32).gen(xs, ys);
    public readonly Cube back = new Cube().pos(17, 18, 3).off(0, -11, 0).ext(6, 11, 9).tex(66, 0).gen(xs, ys);
    public readonly Cube tail= new Cube().pos(20, 21, 5).off(0, -5, 0).ext(5, 5, 5).tex(32, 32).gen(xs, ys);
    public readonly Cube upperBeak = new Cube().pos(-17, 32, 5.5f).off(0, -4, 0).ext(17, 4, 4).tex(54, 32).gen(xs, ys);
    public readonly Cube lowerBeak = new Cube().pos(-16, 27.5f, 5.5f).off(0, -2, 0).ext(16, 2, 4).tex(54, 48).gen(xs, ys);
    public readonly Cube rightLeg = new Cube().pos(9, 6, 0).off(0, -5, 0).ext(2, 5, 2).tex(32, 48).gen(xs, ys);
    public readonly Cube leftLeg = new Cube().pos(9, 6, 13).off(0, -5, 0).ext(2, 5, 2).tex(32, 48).gen(xs, ys);
    public readonly Cube rightfoot = new Cube().pos(5, 1, -1).off(0, -1, 0).ext(7, 1, 4).tex(32, 59).gen(xs, ys);
    public readonly Cube leftfoot = new Cube().pos(5, 1, 12).off(0, -1, 0).ext(7, 1, 4).tex(32, 59).gen(xs, ys);

    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        // set dodo texture
        Game.graphics.tex(0, Game.textures.get(e.tex));

        // calculate interpolated rotations
        var interpRot = Vector3.Lerp(e.prevRotation, e.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(e.prevBodyRotation, e.bodyRotation, (float)interp);

        // calculate head rotation relative to body
        var headRotZ = interpRot.Z - interpBodyRot.Z;
        nest.rotation = new Vector3(0, 0, headRotZ);
        upperBeak.rotation = new Vector3(0, 0, headRotZ);
        lowerBeak.rotation = new Vector3(0, 0, headRotZ);

        // set leg movement
        float cs = Meth.clamp(aspeed, 0, 1);
        float lr = MathF.Sin(apos * 10) * 20f * cs * Meth.phiF;
        rightLeg.rotation = new Vector3(0, 0, lr);
        leftLeg.rotation = new Vector3(0, 0, -lr);
        //rightfoot.position = new Vector3(0, 0, cs);
        //leftfoot.position = new Vector3(0, 0, -cs);



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