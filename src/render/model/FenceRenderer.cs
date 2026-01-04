using BlockGame.main;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.block.entity;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

public class FenceRenderer : BlockEntityRenderer<FenceBlockEntity> {

    public const int typeHeight = 17; // height per fence type in atlas

    // cubes per fence type [fenceType]
    private readonly Cube[] board1;
    private readonly Cube[] board2;
    private readonly Cube[] board3;
    private readonly Cube[] board4;
    private readonly Cube[] pillar1;
    private readonly Cube[] pillar2;
    private readonly Cube[] pillar3;
    private readonly Cube[] pillar4;

    public FenceRenderer() {
        var tex = Game.textures.fence;
        int xs = tex.width;
        int ys = tex.height;
        int numTypes = ys / typeHeight;

        board1 = new Cube[numTypes];
        board2 = new Cube[numTypes];
        board3 = new Cube[numTypes];
        board4 = new Cube[numTypes];
        pillar1 = new Cube[numTypes];
        pillar2 = new Cube[numTypes];
        pillar3 = new Cube[numTypes];
        pillar4 = new Cube[numTypes];

        for (int i = 0; i < numTypes; i++) {
            int yOff = i * typeHeight;
            // pillars: 2 wide, 16 tall, 1 thick at 4 X positions
            pillar1[i] = new Cube().pos(1, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(34, yOff).gen(xs, ys);
            pillar2[i] = new Cube().pos(5, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(40, yOff).gen(xs, ys);
            pillar3[i] = new Cube().pos(9, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(46, yOff).gen(xs, ys);
            pillar4[i] = new Cube().pos(13, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(52, yOff).gen(xs, ys);
            // boards: 16 wide, 2 tall, 1 thick
            board1[i] = new Cube().pos(0, 14, 15).off(0, -2, 0).ext(16, 2, 1).tex(0, yOff).gen(xs, ys);
            board2[i] = new Cube().pos(0, 3, 15).off(0, -2, 0).ext(16, 2, 1).tex(0, yOff).gen(xs, ys);
            board3[i] = new Cube().pos(0, 14, 13).off(0, -2, 0).ext(16, 2, 1).tex(0, yOff).gen(xs, ys);
            board4[i] = new Cube().pos(0, 3, 13).off(0, -2, 0).ext(16, 2, 1).tex(0, yOff).gen(xs, ys);
        }
    }

    public void render(MatrixStack mat, BlockEntity be, float scale, double interp) {
        var ide = EntityRenderers.ide;
        var world = Game.world;
        var pos = be.pos;

        // get fence type from block
        var blockId = world.getBlock(pos.X, pos.Y, pos.Z);
        var fence = Block.blocks[blockId] as Fence;
        int ft = fence?.fenceType ?? 0;
        if (ft >= board1.Length) ft = 0; // fallback if type exceeds atlas

        // get lighting
        var light = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;
        var blocklight = (byte)((light >> 4) & 0xF);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);
        ide.setColour(new Color(lightVal.R, lightVal.G, lightVal.B, (byte)255));

        // bind fence atlas
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

        const float pixelScale = 1f / 16f;
        board1[ft].xfrender(ide, mat, pixelScale);
        board2[ft].xfrender(ide, mat, pixelScale);
        board3[ft].xfrender(ide, mat, pixelScale);
        board4[ft].xfrender(ide, mat, pixelScale);
        pillar1[ft].xfrender(ide, mat, pixelScale);
        pillar2[ft].xfrender(ide, mat, pixelScale);
        pillar3[ft].xfrender(ide, mat, pixelScale);
        pillar4[ft].xfrender(ide, mat, pixelScale);

        ide.end();

        mat.pop();

        ide.setColour(Color.White);
    }
}