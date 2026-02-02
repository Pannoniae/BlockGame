using System.Numerics;
using BlockGame.main;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class CowModel : AnimalModel {

    public Cube hornYRight;
    public Cube hornXRight;
    public Cube hornYLeft;
    public Cube hornXLeft;

    public CowModel() : base(10) {
        hornYRight =  new Cube().pos(0, 15, 13).off(-6, 5, 2).ext(1, 1, 1).tex(24, 0).gen(xs, ys);
        hornXRight =  new Cube().pos(0, 15, 13).off(-6, 4, 2).ext(3, 1, 1).tex(29, 0).gen(xs, ys);
        hornYLeft =  new Cube().pos(0, 15, 13).off(5, 5, 2).ext(1, 1, 1).tex(24, 0).gen(xs, ys);
        hornXLeft =  new Cube().pos(0, 15, 13).off(3, 4, 2).ext(3, 1, 1).tex(29, 0).gen(xs, ys);
    }

    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        base.render(mat, e, apos, aspeed, scale, interp);


        var interpRot = Vector3.Lerp(e.prevRotation, e.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(e.prevBodyRotation, e.bodyRotation, (float)interp);
        var headRotY = interpRot.Y - interpBodyRot.Y;

        hornYRight.rotation = new Vector3(0, headRotY, 0);
        hornXRight.rotation = new Vector3(0, headRotY, 0);
        hornYLeft.rotation = new Vector3(0, headRotY, 0);
        hornXLeft.rotation = new Vector3(0, headRotY, 0);

        var ide = EntityRenderers.ide;
        hornYRight.xfrender(ide, mat, scale);
        hornXRight.xfrender(ide, mat, scale);
        hornYLeft.xfrender(ide, mat, scale);
        hornXLeft.xfrender(ide, mat, scale);
    }
}