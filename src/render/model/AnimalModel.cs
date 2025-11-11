using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

public class AnimalModel : EntityModel {

    public const int xs = 68;
    public const int ys = 40;
    public readonly Cube head;
    public readonly Cube body;
    public readonly Cube frontRightLeg;
    public readonly Cube frontLeftLeg;
    public readonly Cube backLeftLeg;
    public readonly Cube backRightLeg;

    public AnimalModel(int l) {

        head = new Cube().pos(0, l + 4, 13).off(-3, 0, -3).ext(6, 6, 6).tex(0, 0).gen(xs, ys);
        body = new Cube().pos(0, l + 8, 0).off(-5, -8, -10).ext(10, 8, 20).tex(8, 12).gen(xs, ys);

        frontRightLeg = new Cube().pos(-2.5f, l, 6.5f).off(-1.5f, -l, -1.5f).ext(3, l, 3).tex(0, 12).gen(xs, ys);
        frontLeftLeg = new Cube().pos(2.5f, l, 6.5f).off(-1.5f, -l, -1.5f).ext(3, l, 3).tex(0, 12).gen(xs, ys);
        backLeftLeg = new Cube().pos(2.5f, l, -8.5f).off(-1.5f, -l, -1.5f).ext(3, l, 3).tex(0, 12).gen(xs, ys);
        backRightLeg = new Cube().pos(-2.5f, l, -8.5f).off(-1.5f, -l, -1.5f).ext(3, l, 3).tex(0, 12).gen(xs, ys);
    }


    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        // set cow texture
        Game.graphics.tex(0, e.tex);

        // calculate interpolated rotations
        var interpRot = Vector3.Lerp(e.prevRotation, e.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(e.prevBodyRotation, e.bodyRotation, (float)interp);

        // calculate head rotation relative to body
        var headRotY = interpRot.Y - interpBodyRot.Y;

        float cs = Meth.clamp(aspeed, 0, 1);
        float lr = MathF.Sin(apos * 10) * 20f * cs * Meth.phiF;

        head.rotation = new Vector3(0, headRotY, 0);
        frontRightLeg.rotation = new Vector3(lr, 0, 0);
        frontLeftLeg.rotation = new Vector3(-lr, 0, 0);
        backRightLeg.rotation = new Vector3(-lr, 0, 0);
        backLeftLeg.rotation = new Vector3(lr, 0, 0);
        
        var ide = EntityRenderers.ide;
        body.xfrender(ide, mat, scale);
        head.xfrender(ide, mat, scale);
        frontRightLeg.xfrender(ide, mat, scale);
        frontLeftLeg.xfrender(ide, mat, scale);
        backRightLeg.xfrender(ide, mat, scale);
        backLeftLeg.xfrender(ide, mat, scale);
    }
}
