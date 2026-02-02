using BlockGame.main;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class BoaModel : EntityModel {
    public const int xs = 64;
    public const int ys = 64;

    public readonly Cube bottomcoil = new Cube().pos(0, 4, 0).off(0, -4, 0).ext(16, 4, 16).tex(0, 0).gen(xs, ys);
    public readonly Cube topcoil = new Cube().pos(0, 7, 0).off(4, -3, 4).ext(8, 3, 8).tex(0, 21).gen(xs, ys);
    public readonly Cube head = new Cube().pos(0, 8, 0).off(-2, -2, 6).ext(6, 2, 3).tex(0, 40).gen(xs, ys);
    public readonly Cube tong = new Cube().pos(0, 7, 0).off(-6, -0, 6).ext(6, 0, 3).tex(23, 43).dsided().gen(xs, ys);
    //public readonly Cube topcoil = new Cube().pos(0, 12, 0).off(4, -4, 4).ext(8, 4, 8).tex(0, 40).gen(xs, ys);
    //public readonly Cube neck = new Cube().pos(0, 0, -16).off(-4, -4, -4).ext(8, 8, 8).tex(0, 24).gen(xs, ys);
    //public readonly Cube head = new Cube().pos(0, 0, -20).off(-4, -4, -4).ext(8, 8, 8).tex(16, 0).gen(xs, ys);

    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        Game.graphics.tex(0, Game.textures.boa);

        var ide = EntityRenderers.ide;
        bottomcoil.xfrender(ide, mat, scale);
        topcoil.xfrender(ide, mat, scale);
        head.xfrender(ide, mat, scale);
        tong.xfrender(ide, mat, scale);
        /*neck.xfrender(ide, mat, scale);
        head.xfrender(ide, mat, scale);*/
    }
}