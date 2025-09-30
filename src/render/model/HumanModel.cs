using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;

namespace BlockGame.render.model;

public class HumanModel : EntityModel {

    public const int xs = 48;
    public const int ys = 32;


    public readonly Cube head = new Cube().pos(0, 20, 0).off(-4, 0, -4).ext(8, 8, 8).tex(0, 0).gen(xs, ys);
    public readonly Cube body = new Cube().pos(0, 20, 0f).off(-3, -10, -1.5f).ext(6, 10, 3).tex(12, 16).gen(xs, ys);
    // same with the arms lol
    // centre of the arm should be at the shoulder!!
    public readonly Cube rightArm = new Cube().pos(4.5f, 20, 0f).off(-1.5f, -14, -1.5f).ext(3, 12, 3).tex(32, 16).mirror().gen(xs, ys);
    public readonly Cube leftArm = new Cube().pos(-4.5f, 20, 0f).off(-1.5f, -14, -1.5f).ext(3, 12, 3).tex(32, 16).gen(xs, ys);
    // the legs should be positioned at the hips!! so the rotation works properly
    public readonly Cube rightLeg = new Cube().pos(1.5f, 10, 0f).off(-1.5f, -10, -1.5f).ext(3, 10, 3).tex(0, 16).mirror().gen(xs, ys);
    public readonly Cube leftLeg = new Cube().pos(-1.5f, 10, 0f).off(-1.5f, -10, -1.5f).ext(3, 10, 3).tex(0, 16).gen(xs, ys);

    public bool armRaise = false;
    public bool sneaking = false;

    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp, byte r, byte g, byte b) {
        // texture
        Game.graphics.tex(0, Game.textures.human);

        // calculate interpolated rotations
        var interpRot = Vector3.Lerp(e.prevRotation, e.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(e.prevBodyRotation, e.bodyRotation, (float)interp);

        // calculate head rotation relative to body (includes up/down look)
        var headRotX = interpRot.X - interpBodyRot.X; // pitch diff
        var headRotY = interpRot.Y - interpBodyRot.Y; // yaw diff


        // SNEAKING CODE
        if (sneaking) {
            head.position.Y = 17f;
            rightArm.position.Y = 19f;
            leftArm.position.Y = 19f;

            rightLeg.position.Y = 9;
            leftLeg.position.Y = 9;

            // move legs back
            rightLeg.position.Z = -4f;
            leftLeg.position.Z = -4f;

            body.position.Y = 18f;
            body.rotation.X = 20f;
        } else {
            head.position.Y = 20;
            rightArm.position.Y = 22;
            leftArm.position.Y = 22;


            rightLeg.position.Y = 10;
            leftLeg.position.Y = 10;

            rightLeg.position.Z = 0;
            leftLeg.position.Z = 0;

            body.position.Y = 20;
            body.rotation.X = 0;
        }




        // render head with additional rotation for up/down look
        head.rotation = new Vector3(-headRotX, headRotY, 0);
        head.render(mat, scale, r, g, b);

        float cs = Meth.clamp(aspeed, 0, 1);
        float ar = MathF.Sin(apos * 10) * 30f * cs * Meth.phiF;
        float lr = MathF.Sin(apos * 10) * 25f * cs * Meth.phiF;

        // get swing animation progress
        // TODO this is fucked
        // fuck it circle time
        var swingProgress = (float)e.getSwingProgress(interp);

        var sinSwing = float.Sin(swingProgress * MathF.PI * 2f);
        var sinSwingSqrt = -float.Sin(float.Sqrt(swingProgress) * MathF.PI * 2f);
        var cosSwing = -float.Sin(swingProgress * MathF.PI);
        var cosSwingSqrt = -float.Sin(float.Sqrt(swingProgress) * MathF.PI);

        // apply swing animation to right arm (main hand)
        const int n = 60;
        var rasX = cosSwingSqrt * n / 2f;
        var rasY = cosSwing * 360;
        var rasZ = sinSwingSqrt * n / -3f;

        var off = armRaise ? -10 : 0;
        var sneakArmRotX = sneaking ? 5f : 0f;

        // tilt body
        body.rotation = new Vector3(body.rotation.X, rasX / 2f, body.rotation.Z);
        body.render(mat, scale, r, g, b);


        // blend walking animation with swing animation for right arm
        rightArm.rotation = new Vector3(ar + rasX + off + sneakArmRotX, 0, rasZ);

        var lpos = leftArm.position;
        var rpos = rightArm.position;
        // offset arms to avoid clipping into body when swinging
        rightArm.position = new Vector3(rightArm.position.X, rightArm.position.Y, 0 + (rasX * (1 / 15f)));
        leftArm.position = new Vector3(leftArm.position.X, leftArm.position.Y, 0 + (rasX * 0.04f));
        leftArm.rotation = new Vector3(-ar + sneakArmRotX, rasX * 0.5f, 0);
        rightLeg.rotation = new Vector3(-lr, 0, 0);
        leftLeg.rotation = new Vector3(lr, 0, 0);

        rightArm.render(mat, scale, r, g, b);
        leftArm.render(mat, scale, r, g, b);
        rightLeg.render(mat, scale, r, g, b);
        leftLeg.render(mat, scale, r, g, b);

        leftArm.position = lpos;
        rightArm.position = rpos;
    }
}