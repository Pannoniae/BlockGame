using System.Numerics;
using BlockGame.main;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.block.entity;
using FontStashSharp;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

/**
 * Renders text on signs. Geometry is handled by SignBlock's custom block renderer.
 */
public class SignRenderer : BlockEntityRenderer<SignBlockEntity> {
    // scale: font is 16px tall, we want TEXT_HEIGHT_WORLD (2 worldpx)
    // 2 world pixels = 2/16 blocks = 0.125 blocks
    private const float FONT_SCALE = SignBlockEntity.TEXT_HEIGHT_WORLD / 16f / 16f; // = 1/128
    private const float LINE_SPACING_BLOCKS = SignBlockEntity.LINE_SPACING_WORLD / 16f; // in blocks

    public void render(MatrixStack mat, BlockEntity be, float scale, double interp) {
        var sign = (SignBlockEntity)be;
        var font = Game.fontLoader.fontSystemThin.GetFont(16);
        var renderer = Game.fontLoader.rendererBlockEntity;
        var ide = EntityRenderers.ide;

        renderer.ide = ide;

        // get lighting at sign position
        var world = Game.world;
        var pos = sign.pos;
        var light = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;
        var blocklight = (byte)((light >> 4) & 0xF);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);
        ide.setColour(new Color(lightVal.R, lightVal.G, lightVal.B, (byte)255));

        mat.push();

        const float epsilon = 0.005f; // prevent z-fighting with the sign..

        var metadata = world.getBlockRaw(pos.X, pos.Y, pos.Z).getMetadata();

        mat.translate(0.5f, 0f, 0.5f); // move to centre of block

        if (SignBlock.isWall(metadata)) {
            var rot = SignBlock.rotw(metadata);
            mat.rotate(rot.Y, 0, 1, 0);

            const float topPadding = SignBlockEntity.TEXT_PADDING_TOP_WORLD / 16f - 3 * FONT_SCALE;
            mat.translate(0, -topPadding, 0f);

            //mat.translate(0f, -0.1875f, 0.4375f); // wall offset
            mat.translate(0f, 13 / 16f, 7f / 16f - epsilon); // wall offset
        }
        else {
            const float topPadding = SignBlockEntity.TEXT_PADDING_TOP_WORLD / 16f - 3 * FONT_SCALE;

            // ascender is 3px

            var rot = SignBlock.rots(metadata);
            //mat.translate(0f, 16 / 16f, 0f);
            mat.rotate(rot.Y, 0, 1, 0);
            mat.translate(0, -topPadding, 0f);
            mat.translate(0f, 1f, -0.5f / 16f - epsilon); // standing sign offset

        }


        mat.scale(FONT_SCALE, -FONT_SCALE, FONT_SCALE);

        ide.model(mat);
        ide.view(Game.camera.getViewMatrix(interp));
        ide.proj(Game.camera.getProjectionMatrix());

        ide.begin(PrimitiveType.Quads);

        for (int i = 0; i < 4; i++) {
            var line = sign.lines[i];
            if (string.IsNullOrEmpty(line)) {
                continue;
            }

            var s = font.MeasureString(line);
            s.X *= Game.fontLoader.thinFontAspectRatio;

            var lineY = i * (LINE_SPACING_BLOCKS / FONT_SCALE); // block to font pixels
            var textPos = new Vector2(-s.X / 2, lineY);

            var sc = new Vector2(1f, 1f);

            var asc = new Vector2(sc.X * Game.fontLoader.thinFontAspectRatio, sc.Y);

            // draw
            var dummyMatrix = Matrix4x4.Identity;
            font.DrawText(renderer, line, textPos, FSColor.Black, ref dummyMatrix, scale: asc);
        }

        ide.end();
        mat.pop();
    }
}