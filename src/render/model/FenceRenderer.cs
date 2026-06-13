using BlockGame.main;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.block.entity;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

public class FenceRenderer : BlockEntityRenderer<FenceBlockEntity> {

    public const int typeHeight = 17; // height per fence type in atlas

    // boards[fenceType][variant][boardIdx]
    // variant: 0=full, 1=shortL, 2=shortR, 3=shortLR
    // boardIdx: 0-3 (outer top, outer bot, inner top, inner bot)
    private readonly Cube[][][] boards;

    // pillars[fenceType][pillarIdx] — 0=x1, 1=x5, 2=x9, 3=x13
    private readonly Cube[][] pillars;

    // board layout: (posY, posZ) for each of the 4 boards
    private static readonly (int py, int pz)[] boardLayout = [(14, 15), (3, 15), (14, 13), (3, 13)];

    // how many pixels to trim at a corner end (clears both inner+outer boards of the perpendicular panel)
    private const int trim = 3;

    public FenceRenderer() {
        var tex = Game.textures.fence;
        int xs = tex.width;
        int ys = tex.height;
        int numTypes = ys / typeHeight;

        boards = new Cube[numTypes][][];
        pillars = new Cube[numTypes][];

        for (int i = 0; i < numTypes; i++) {
            int yOff = i * typeHeight;

            // build 4 board variants
            boards[i] = new Cube[4][];
            for (int v = 0; v < 4; v++) {
                bool shortL = (v & 1) != 0;
                bool shortR = (v & 2) != 0;
                int xOff = shortL ? trim : 0;
                int width = 16 - (shortL ? trim : 0) - (shortR ? trim : 0);

                boards[i][v] = new Cube[4];
                for (int b = 0; b < 4; b++) {
                    var (py, pz) = boardLayout[b];
                    boards[i][v][b] = new Cube()
                        .pos(xOff, py, pz).off(0, -2, 0)
                        .ext(width, 2, 1).tex(0, yOff).gen(xs, ys);
                }
            }

            // pillars
            pillars[i] = [
                new Cube().pos(1, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(34, yOff).gen(xs, ys),
                new Cube().pos(5, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(40, yOff).gen(xs, ys),
                new Cube().pos(9, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(46, yOff).gen(xs, ys),
                new Cube().pos(13, 16, 14).off(0, -16, 0).ext(2, 16, 1).tex(52, yOff).gen(xs, ys),
            ];
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
        if (ft >= boards.Length) ft = 0;

        // get lighting
        var light = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;
        var blocklight = (byte)((light >> 4) & 0xF);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);
        ide.setColour(new Color(lightVal.R, lightVal.G, lightVal.B, (byte)255));

        // bind fence atlas
        Game.graphics.tex(0, Game.textures.fence);

        // metadata is a bitmask: bit 0=east, 1=west, 2=south, 3=north
        var metadata = world.getBlockRaw(pos.X, pos.Y, pos.Z).getMetadata();

        ide.view(Game.camera.getViewMatrix(interp));
        ide.proj(Game.camera.getProjectionMatrix());

        const float pixelScale = 1f / 16f;

        // render a panel for each active edge
        for (int edge = 0; edge < 4; edge++) {
            if ((metadata & (1 << edge)) == 0) continue;

            // east/west (edges 0,1) always render full; north/south (2,3) shorten at perpendicular corners
            int variant = 0;
            bool shortL = false, shortR = false;
            if (edge >= 2) {
                // edge 2 (south): left end=east, right end=west
                // edge 3 (north): left end=west, right end=east
                int leftAdj = edge == 2 ? 0 : 1;
                int rightAdj = edge == 2 ? 1 : 0;
                shortL = (metadata & (1 << leftAdj)) != 0;
                shortR = (metadata & (1 << rightAdj)) != 0;
                variant = (shortL ? 1 : 0) | (shortR ? 2 : 0);
            }

            mat.push();
            mat.translate(0.5f, 0, 0.5f);
            mat.rotate(Fence.edgeRot[edge], 0, 1, 0);
            mat.translate(-0.5f, 0, -0.5f);

            ide.model(mat);
            ide.begin(PrimitiveType.Quads);

            // boards (shortened at corners for north/south only)
            var bds = boards[ft][variant];
            for (int b = 0; b < 4; b++)
                bds[b].xfrender(ide, mat, pixelScale);

            // pillars (skip end pillars at corners)
            var pls = pillars[ft];
            if (!shortL) pls[0].xfrender(ide, mat, pixelScale);
            pls[1].xfrender(ide, mat, pixelScale);
            pls[2].xfrender(ide, mat, pixelScale);
            if (!shortR) pls[3].xfrender(ide, mat, pixelScale);

            ide.end();
            mat.pop();
        }

        ide.setColour(Color.White);
    }
}