using BlockGame.main;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class BoaModel : EntityModel {
    public const int xs = 64;
    public const int ys = 64;

    public readonly Cube level1 = new Cube().pos(0, 4, 0).off(0, -4, 0).ext(16, 4, 16).tex(0, 0).gen(xs, ys);
    public readonly Cube level2 = new Cube().pos(3, 6, 3).off(0, -2, 0).ext(10, 2, 10).tex(0, 22).gen(xs, ys);
    public readonly Cube level3 = new Cube().pos(5, 7, 5).off(0, -1, 0).ext(6, 1, 6).tex(0, 36).gen(xs, ys);
    public readonly Cube head = new Cube().pos(6, 8, 5).off(0, -1, 0).ext(4, 1, 3).tex(0, 59).gen(xs, ys);
    //public readonly Cube level3 = new Cube().pos(5, 7, 8).off(0, -1, 0).ext(6, 1, 3).tex(0, 36).gen(xs, ys);
    //public readonly Cube head = new Cube().pos(6, 8, 5).off(0, -2, 0).ext(5, 2, 3).tex(0, 59).gen(xs, ys);
    public readonly Cube tong = new Cube().pos(11, 7.01f, 5).off(0, 0, 0).ext(4, 0, 3).tex(23, 60).dsided().gen(xs, ys);

    /* The boa is made of 4 cubes, the bottom coil, the top coil, the head and the tongue. The tongue is doublesided.
    public readonly Cube bottomcoil = new Cube().pos(0, 4, 0).off(0, -4, 0).ext(16, 4, 16).tex(0, 0).gen(xs, ys);
    public readonly Cube topcoil = new Cube().pos(0, 7, 0).off(4, -3, 4).ext(8, 3, 8).tex(0, 21).gen(xs, ys);
    public readonly Cube head = new Cube().pos(0, 8, 0).off(-2, -2, 6).ext(6, 2, 3).tex(0, 40).gen(xs, ys);
    public readonly Cube tong = new Cube().pos(0, 7, 0).off(-6, -0, 6).ext(6, 0, 3).tex(23, 43).dsided().gen(xs, ys);
    */
    //public readonly Cube topcoil = new Cube().pos(0, 12, 0).off(4, -4, 4).ext(8, 4, 8).tex(0, 40).gen(xs, ys);
    //public readonly Cube neck = new Cube().pos(0, 0, -16).off(-4, -4, -4).ext(8, 8, 8).tex(0, 24).gen(xs, ys);
    //public readonly Cube head = new Cube().pos(0, 0, -20).off(-4, -4, -4).ext(8, 8, 8).tex(16, 0).gen(xs, ys);

    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        Game.graphics.tex(0, Game.textures.boa);

        var ide = EntityRenderers.ide;
        level1.xfrender(ide, mat, scale);
        level2.xfrender(ide, mat, scale);
        level3.xfrender(ide, mat, scale);
        head.xfrender(ide, mat, scale);
        tong.xfrender(ide, mat, scale);
        /*neck.xfrender(ide, mat, scale);
        head.xfrender(ide, mat, scale);*/
    }
}