using BlockGame.main;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.block.entity;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

public class FenceRenderer : BlockEntityRenderer<FenceBlockEntity> {

    // texture size
    public const int xs = 58;
    public const int ys = 17;

    // 2 duplex horizontal boards
    public readonly Cube board1;
    public readonly Cube board2;
    public readonly Cube board3;
    public readonly Cube board4;

    // 4 vertical pillars
    public readonly Cube pillar1;
    public readonly Cube pillar2;
    public readonly Cube pillar3;
    public readonly Cube pillar4;

    public FenceRenderer() {
        // model at close edge (z=14-16), rotation will place it on correct edge
        // pillars: 2 wide, 16 tall, 1 thick at 4 X positions
        pillar1 = new Cube().pos(1, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(34, 0).gen(xs, ys);
        pillar2 = new Cube().pos(5, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(40, 0).gen(xs, ys);
        pillar3 = new Cube().pos(9, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(46, 0).gen(xs, ys);
        pillar4 = new Cube().pos(13, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(52, 0).gen(xs, ys);

        // boards on pillars: 16 wide, 2 tall, 1 thick on both sides of pillars
        board1 = new Cube().pos(0, 14, 15).off(0, -2, 0).ext(16, 2, 1).tex(0, 0).gen(xs, ys);
        board2 = new Cube().pos(0, 5, 15).off(0, -2, 0).ext(16, 2, 1).tex(0, 0).gen(xs, ys);
        board3 = new Cube().pos(0, 14, 13).off(0, -2, 0).ext(16, 2, 1).tex(0, 0).gen(xs, ys);
        board4 = new Cube().pos(0, 5, 13).off(0, -2, 0).ext(16, 2, 1).tex(0, 0).gen(xs, ys);
    }

    public void render(MatrixStack mat, BlockEntity be, float scale, double interp) {
        var ide = EntityRenderers.ide;
        var world = Game.world;
        var pos = be.pos;

        // get lighting
        var light = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;
        var blocklight = (byte)((light >> 4) & 0xF);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);
        ide.setColour(new Color(lightVal.R, lightVal.G, lightVal.B, (byte)255));

        // bind fence texture
        Game.graphics.tex(0, Game.textures.fence);

        // get facing from block metadata
        var metadata = world.getBlockRaw(pos.X, pos.Y, pos.Z).getMetadata();
        var facing = metadata & 0b11;

        mat.push();

        // rotate based on facing: 0=W, 1=E, 2=S, 3=N
        mat.translate(0.5f, 0, 0.5f);
        float rot = facing switch {
            0 => 90, // west
            1 => -90,  // east
            2 => 180, // south
            _ => 0    // north
        };
        mat.rotate(rot, 0, 1, 0);
        mat.translate(-0.5f, 0, -0.5f);

        ide.model(mat);
        ide.view(Game.camera.getViewMatrix(interp));
        ide.proj(Game.camera.getProjectionMatrix());

        ide.begin(PrimitiveType.Quads);

        // render all cubes - scale 1/16 converts pixel units to blocks
        const float pixelScale = 1f / 16f;
        board1.xfrender(ide, mat, pixelScale);
        board2.xfrender(ide, mat, pixelScale);
        board3.xfrender(ide, mat, pixelScale);
        board4.xfrender(ide, mat, pixelScale);
        pillar1.xfrender(ide, mat, pixelScale);
        pillar2.xfrender(ide, mat, pixelScale);
        pillar3.xfrender(ide, mat, pixelScale);
        pillar4.xfrender(ide, mat, pixelScale);

        ide.end();

        mat.pop();

        ide.setColour(Color.White);
    }
}
