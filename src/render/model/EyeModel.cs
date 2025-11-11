using BlockGame.main;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class EyeModel : EntityModel {
    public const int xs = 32;
    public const int ys = 32;

    public readonly Cube eye = new Cube().pos(0, 0, 0).off(-4, -4, -4).ext(8, 8, 8).tex(0, 0).gen(xs, ys);
    public readonly Cube tail = new Cube().pos(0, 0, -8).off(-4, -4, -4).ext(8, 8, 8).tex(0, 16).dsided().gen(xs, ys);


    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        Game.graphics.tex(0, Game.textures.eye);

        var ide = EntityRenderers.ide;
        eye.xfrender(ide, mat, scale);
        tail.xfrender(ide, mat, scale);
    }
}