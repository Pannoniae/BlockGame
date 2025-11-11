using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.entity;
using Molten;

namespace BlockGame.render.model;

public class FallingBlockEntityRenderer : EntityRenderer<FallingBlockEntity> {
    private readonly List<BlockVertexTinted> vertices = [];
    private readonly StreamingVAO<BlockVertexTinted>? vao;

    public FallingBlockEntityRenderer() {
        // initialize VAO for rendering
        vao = new StreamingVAO<BlockVertexTinted>();
        vao.bind();
        vao.setSize(Face.MAX_FACES * 4);
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not FallingBlockEntity fallingBlock) {
            return;
        }

        var block = Block.get(fallingBlock.blockID);
        if (block == null) {
            return;
        }

        mat.push();

        // centre the block at the entity position
        mat.translate(-0.5f, 0, -0.5f);

        renderBlock(mat, fallingBlock, block, interp);

        mat.pop();
    }

    private void renderBlock(MatrixStack mat, FallingBlockEntity fallingBlock, Block block, double interp) {
        var itemRenderer = Game.graphics.idt;

        vertices.Clear();

        var metadata = fallingBlock.blockMeta;
        var world = fallingBlock.world;

        var pos = fallingBlock.position.toBlockPos();
        var l = world.getLightC(pos.X, pos.Y, pos.Z);

        Game.graphics.tex(0, Game.textures.blockTexture);
        Game.blockRenderer.setupStandalone();

        Game.blockRenderer.renderBlock(block, metadata, Vector3I.Zero, vertices,
            lightOverride: l, cullFaces: false);

        if (vertices.Count > 0) {
            // upload and render vertices using our own VAO
            vao.bind();
            Game.renderer.bindQuad();
            vao.upload(CollectionsMarshal.AsSpan(vertices));

            Game.graphics.instantTextureShader.use();

            itemRenderer.model(mat);
            itemRenderer.view(Game.camera.getViewMatrix(interp));
            itemRenderer.proj(Game.camera.getProjectionMatrix());

            // actually apply uniform (it's not automatic here because we don't use instantdraw!)
            itemRenderer.applyMat();

            vao.render();
        }
    }
}