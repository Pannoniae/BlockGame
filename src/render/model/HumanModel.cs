using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;

namespace BlockGame.render.model;

public class HumanModel : EntityModel {

    public const int xs = 48;
    public const int ys = 32;


    public readonly Cube head = new Cube().pos(0, 22, 0).off(-4, 0, -4).ext(8, 8, 8).tex(0, 0).gen(xs, ys);
    public readonly Cube body = new Cube().pos(0, 10, 0).off(-3, 0, -2).ext(6, 12, 3).tex(12, 16).gen(xs, ys);
    // same with the arms lol
    public readonly Cube rightArm = new Cube().pos(4.5f, 24, 0).off(-1.5f, -14, -2).ext(3, 12, 3).tex(32, 16).mirror().gen(xs, ys);
    public readonly Cube leftArm = new Cube().pos(-4.5f, 24, 0).off(-1.5f, -14, -2).ext(3, 12, 3).tex(32, 16).gen(xs, ys);
    // the legs should be positioned at the hips!! so the rotation works properly
    public readonly Cube rightLeg = new Cube().pos(1.5f, 10, 0).off(-1.5f, -10, -1.5f).ext(3, 10, 3).tex(0, 16).mirror().gen(xs, ys);
    public readonly Cube leftLeg = new Cube().pos(-1.5f, 10, 0).off(-1.5f, -10, -1.5f).ext(3, 10, 3).tex(0, 16).gen(xs, ys);

    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        // texture
        Game.graphics.tex(0, Game.textures.human);

        // calculate interpolated rotations
        var interpRot = Vector3.Lerp(e.prevRotation, e.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(e.prevBodyRotation, e.bodyRotation, (float)interp);

        // calculate head rotation relative to body (includes up/down look)
        var headRotX = interpRot.X - interpBodyRot.X; // pitch diff
        var headRotY = interpRot.Y - interpBodyRot.Y; // yaw diff

        // render head with additional rotation for up/down look
        head.rotation = new Vector3(-headRotX, headRotY, 0);
        head.render(mat, scale);
        body.render(mat, scale);

        float cs = Meth.clamp(aspeed, 0, 1);
        float ar = MathF.Sin(apos * 10) * 30f * cs * Meth.phiF;
        float lr = MathF.Sin(apos * 10) * 25f * cs * Meth.phiF;

        // get swing animation progress
        // TODO this is fucked
        var swingProgress = (float)e.getSwingProgress(interp);

        // calculate swing animation values (same as first-person)
        var sinSwing = MathF.Sin(swingProgress * MathF.PI);
        var sinSwingSqrt = MathF.Sin(MathF.Sqrt(swingProgress) * MathF.PI);

        // apply swing animation to right arm (main hand)
        var rightArmSwingX = -sinSwingSqrt * 60f * Meth.phiF; // convert to radians
        var rightArmSwingZ = -sinSwingSqrt * 20f * Meth.phiF;
        var rightArmSwingY = sinSwing * 10f * Meth.phiF;

        // blend walking animation with swing animation for right arm
        rightArm.rotation = new Vector3(ar + rightArmSwingX, rightArmSwingY, rightArmSwingZ);
        leftArm.rotation = new Vector3(-ar, 0, 0);
        rightLeg.rotation = new Vector3(-lr, 0, 0);
        leftLeg.rotation = new Vector3(lr, 0, 0);

        rightArm.render(mat, scale);
        leftArm.render(mat, scale);
        rightLeg.render(mat, scale);
        leftLeg.render(mat, scale);
    }
}