using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class ZombieModel : HumanModel {
    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        Game.graphics.tex(0, Game.textures.zombie);

        var interpRot = Vector3.Lerp(e.prevRotation, e.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(e.prevBodyRotation, e.bodyRotation, (float)interp);

        var headRotX = interpRot.X - interpBodyRot.X;
        var headRotY = interpRot.Y - interpBodyRot.Y;

        head.position.Y = 20;
        rightArm.position.Y = 22;
        leftArm.position.Y = 22;
        rightLeg.position.Y = 10;
        leftLeg.position.Y = 10;
        rightLeg.position.Z = 0;
        leftLeg.position.Z = 0;
        body.position.Y = 20;
        body.rotation.X = 0;

        float cs = Meth.clamp(aspeed, 0, 1);
        float ar = MathF.Sin(apos * 10) * 30f * cs * Meth.phiF;
        float lr = MathF.Sin(apos * 10) * 25f * cs * Meth.phiF;

        const float armFlail = -45f;

        head.rotation = new Vector3(headRotX, headRotY, 0);
        body.rotation = new Vector3(body.rotation.X, 0, body.rotation.Z);

        // arms raised forward + walking animation
        rightArm.rotation = new Vector3(ar + armFlail, 0, 0);
        leftArm.rotation = new Vector3(-ar + armFlail, 0, 0);

        rightLeg.rotation = new Vector3(-lr, 0, 0);
        leftLeg.rotation = new Vector3(lr, 0, 0);

        var ide = EntityRenderers.ide;
        head.xfrender(ide, mat, scale);
        body.xfrender(ide, mat, scale);
        rightArm.xfrender(ide, mat, scale);
        leftArm.xfrender(ide, mat, scale);
        rightLeg.xfrender(ide, mat, scale);
        leftLeg.xfrender(ide, mat, scale);
    }
}