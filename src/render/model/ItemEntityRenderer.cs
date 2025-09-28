using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.entity;
using BlockGame.world.item;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.render.model;

public class ItemEntityRenderer : EntityRenderer<ItemEntity> {
    private readonly List<BlockVertexTinted> vertices = [];
    private StreamingVAO<BlockVertexTinted>? vao;

    public ItemEntityRenderer() {
        // initialize VAO for rendering
        vao = new StreamingVAO<BlockVertexTinted>();
        vao.bind();
        vao.setSize(Face.MAX_FACES * 4);
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not ItemEntity itemEntity) {
            return;
        }

        if (itemEntity.stack == ItemStack.EMPTY) {
            return;
        }

        var item = itemEntity.stack.getItem();

        mat.push();

        // apply hover animation
        //var hoverOffset = itemEntity.hover;
        //mat.translate(0, hoverOffset, 0);

        // rotate slowly for visual appeal:tm:
        //var rotation = float.Sin((itemEntity.age + (float)interp) / 15f) * 20f;
        //mat.rotate(rotation, 0, 1, 0);

        // scale down the item
        var itemScale = scale;
        //mat.scale(itemScale, itemScale, itemScale);

        if (item.isBlock()) {
            // render as small block
            renderItemAsBlock(mat, itemEntity, interp);
        } else {
            // render as flat item texture (placeholder)
            renderItemAsTexture(mat, itemEntity, interp);
        }

        mat.pop();
    }

    private void renderItemAsBlock(MatrixStack mat, ItemEntity itemEntity, double interp) {

        vertices.Clear();

        var block = itemEntity.stack.getItem().getBlock();
        var metadata = (byte)itemEntity.stack.metadata;

        var world = itemEntity.world;

        var pos = itemEntity.position.toBlockPos();
        var l = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;

        var itemRenderer = Game.player.handRenderer.itemRenderer;

        Game.graphics.tex(0, Game.textures.blockTexture);
        Game.blockRenderer.setupStandalone();


        // render the block without face culling
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

    private void renderItemAsTexture(MatrixStack mat, ItemEntity itemEntity, double interp) {
        if (vao == null) return;

        vertices.Clear();

        var item = itemEntity.stack.getItem();
        var texUV = item.getTexture(itemEntity.stack);

        Game.graphics.tex(0, Game.textures.itemTexture);
        Game.blockRenderer.setupStandalone();

        // create 3D flat item card
        renderItemCard(texUV);

        if (vertices.Count > 0) {
            // upload and render vertices
            vao.bind();
            Game.renderer.bindQuad();
            vao.upload(CollectionsMarshal.AsSpan(vertices));

            var itemRenderer = Game.player.handRenderer.itemRenderer;
            Game.graphics.instantTextureShader.use();

            itemRenderer.model(mat);
            itemRenderer.view(Game.camera.getViewMatrix(interp));
            itemRenderer.proj(Game.camera.getProjectionMatrix());
            itemRenderer.applyMat();

            vao.render();
        }
    }

    private void renderItemCard(UVPair texUV) {
        const float thickness = 1 / 16f;
        const byte lightLevel = 255; // full brightness for floating items

        var u0 = UVPair.texU(texUV.u);
        var v0 = UVPair.texV(texUV.v);
        var u1 = UVPair.texU(texUV.u + 1);
        var v1 = UVPair.texV(texUV.v + 1);

        // front face
        addQuad(0, 0, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0,
            u0, v1, u0, v0, u1, v0, u1, v1, lightLevel);

        // back face
        addQuad(1, 0, thickness, 1, 1, thickness, 0, 1, thickness, 0, 0, thickness,
            u1, v1, u1, v0, u0, v0, u0, v1, lightLevel);
    }

    private void addQuad(float x1, float y1, float z1, float x2, float y2, float z2,
        float x3, float y3, float z3, float x4, float y4, float z4,
        float u1, float v1, float u2, float v2, float u3, float v3, float u4, float v4,
        byte lightLevel) {

        vertices.Add(new BlockVertexTinted(x1, y1, z1, u1, v1, lightLevel, lightLevel, lightLevel, 255));
        vertices.Add(new BlockVertexTinted(x2, y2, z2, u2, v2, lightLevel, lightLevel, lightLevel, 255));
        vertices.Add(new BlockVertexTinted(x3, y3, z3, u3, v3, lightLevel, lightLevel, lightLevel, 255));
        vertices.Add(new BlockVertexTinted(x4, y4, z4, u4, v4, lightLevel, lightLevel, lightLevel, 255));
    }
}