using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;

namespace BlockGame.render.model;

public class CowModel : EntityModel {

    public const int xs = 56;
    public const int ys = 46;

    // head at front, 8x8x8 pixels
    public readonly Cube head = new Cube().pos(0, 19, 0).off(-4, -8, -4).ext(8, 8, 8).tex(0, 0).gen(xs, ys);

    // body horizontal, 8x14x16 pixels
    public readonly Cube body = new Cube().pos(0, 18, -12).off(-4, -14, -8).ext(8, 14, 16).tex(8, 16).gen(xs, ys);

    // four legs at corners, 2x4x2 pixels each
    public readonly Cube frontRightLeg = new Cube().pos(0, 4, 0).off(-4, -4, -6).ext(2, 4, 2).tex(0, 16).gen(xs, ys);
    public readonly Cube frontLeftLeg = new Cube().pos(0, 4, 0).off(2, -4, -6).ext(2, 4, 2).tex(0, 16).gen(xs, ys);
    public readonly Cube backLeftLeg = new Cube().pos(0, 4, 0).off(2, -4, -20).ext(2, 4, 2).tex(0, 16).gen(xs, ys);
    public readonly Cube backRightLeg = new Cube().pos(0, 4, 0).off(-4, -4, -20).ext(2, 4, 2).tex(0, 16).gen(xs, ys);

    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        // set cow texture
        Game.graphics.tex(0, Game.textures.cow);

        // render body (no animation)
        body.render(mat, scale);

        // render head (could add head bobbing later)
        head.render(mat, scale);

        // animate legs - walking motion
        float cs = Meth.clamp(aspeed, 0, 1);
        float legSwing = MathF.Sin(apos * 8) * 20f * cs * Meth.phiF;

        // front legs swing opposite to back legs
        frontRightLeg.rotation = new Vector3(legSwing, 0, 0);
        frontLeftLeg.rotation = new Vector3(-legSwing, 0, 0);
        backRightLeg.rotation = new Vector3(-legSwing, 0, 0);
        backLeftLeg.rotation = new Vector3(legSwing, 0, 0);

        frontRightLeg.render(mat, scale);
        frontLeftLeg.render(mat, scale);
        backRightLeg.render(mat, scale);
        backLeftLeg.render(mat, scale);
    }
}
