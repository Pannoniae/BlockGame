using BlockGame.main;
using BlockGame.world;
using BlockGame.world.entity;

namespace BlockGame.render.model;

public class BigEyeModel : EntityModel {
    public const int xs = 64;
    public const int ys = 64;

    public readonly Cube eye = new Cube().pos(0, 0, 0).off(-8, -8, -8).ext(16, 16, 16).tex(0, 0).gen(xs, ys);
    public readonly Cube tail = new Cube().pos(0, 0, -16).off(-8, -8, -8).ext(16, 16, 16).tex(0, 32).dsided().gen(xs, ys);


    public override void render(MatrixStack mat, Entity e, float apos, float aspeed, float scale, double interp) {
        Game.graphics.tex(0, Game.textures.bigeye);

        var ide = EntityRenderers.ide;
        eye.xfrender(ide, mat, scale);
        tail.xfrender(ide, mat, scale);
    }
}