using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;

namespace BlockGame.render.model;

public class CowModel : EntityModel {

    public const int xs = 68;
    public const int ys = 40;
    public readonly Cube head = new Cube().pos(0, 12, 13).off(-3, 0, -3).ext(6, 6, 6).tex(0, 0).gen(xs, ys);

    public readonly Cube body = new Cube().pos(0, 16, 0).off(-5, -8, -10).ext(10, 8, 20).tex(8, 12).gen(xs, ys);

    public readonly Cube frontRightLeg = new Cube().pos(-2.5f, 8, 6.5f).off(-1.5f, -8, -1.5f).ext(3, 8, 3).tex(0, 12).gen(xs, ys);
    public readonly Cube frontLeftLeg = new Cube().pos(2.5f, 8, 6.5f).off(-1.5f, -8, -1.5f).ext(3, 8, 3).tex(0, 12).gen(xs, ys);
    public readonly Cube backLeftLeg = new Cube().pos(2.5f, 8, -8.5f).off(-1.5f, -8, -1.5f).ext(3, 8, 3).tex(0, 12).gen(xs, ys);
    public readonly Cube backRightLeg = new Cube().pos(-2.5f, 8, -8.5f).off(-1.5f, -8, -1.5f).ext(3, 8, 3).tex(0, 12).gen(xs, ys);


    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        // set cow texture
        Game.graphics.tex(0, Game.textures.cow);

        // calculate interpolated rotations
        var interpRot = Vector3.Lerp(e.prevRotation, e.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(e.prevBodyRotation, e.bodyRotation, (float)interp);

        // calculate head rotation relative to body
        var headRotY = interpRot.Y - interpBodyRot.Y;

        // render body (no animation)
        body.render(mat, scale);

        head.rotation = new Vector3(0, headRotY, 0);
        head.render(mat, scale);

        float cs = Meth.clamp(aspeed, 0, 1);
        float lr = MathF.Sin(apos * 10) * 25f * cs * Meth.phiF;

        frontRightLeg.rotation = new Vector3(lr, 0, 0);
        frontLeftLeg.rotation = new Vector3(-lr, 0, 0);
        backRightLeg.rotation = new Vector3(-lr, 0, 0);
        backLeftLeg.rotation = new Vector3(lr, 0, 0);

        frontRightLeg.render(mat, scale);
        frontLeftLeg.render(mat, scale);
        backRightLeg.render(mat, scale);
        backLeftLeg.render(mat, scale);
    }
}
