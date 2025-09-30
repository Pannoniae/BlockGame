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
        var hoverOffset = itemEntity.hover;
        mat.translate(0, hoverOffset, 0);

        // rotate slowly for visual appeal:tm:
        var rotation = (itemEntity.age + (float)interp) * 2f;
        mat.rotate(rotation, 0, 1, 0);

        // scale down the item
        const float itemScale = 0.25f;
        mat.scale(itemScale, itemScale, itemScale);

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

        var itemRenderer = Game.graphics.idt;

        vertices.Clear();

        var block = itemEntity.stack.getItem().getBlock();
        var metadata = (byte)itemEntity.stack.metadata;

        var world = itemEntity.world;

        var pos = itemEntity.position.toBlockPos();
        var l = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;

        Game.graphics.tex(0, Game.textures.blockTexture);
        Game.blockRenderer.setupStandalone();

        // center the block around origin
        mat.push();
        mat.translate(-0.5f, -0.5f, -0.5f);

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

        mat.pop();
    }

    private void renderItemAsTexture(MatrixStack mat, ItemEntity itemEntity, double interp) {
        if (vao == null) return;

        vertices.Clear();

        var item = itemEntity.stack.getItem();
        var texUV = item.getTexture(itemEntity.stack);

        var world = itemEntity.world;
        var pos = itemEntity.position.toBlockPos();
        var l = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;

        Game.graphics.tex(0, Game.textures.itemTexture);
        Game.blockRenderer.setupStandalone();

        // create 3D flat item card
        renderItemCard(texUV, l);

        if (vertices.Count > 0) {

            var itemRenderer = Game.graphics.idt;

            // upload and render vertices
            vao.bind();
            Game.renderer.bindQuad();
            vao.upload(CollectionsMarshal.AsSpan(vertices));

            Game.graphics.instantTextureShader.use();

            itemRenderer.model(mat);
            itemRenderer.view(Game.camera.getViewMatrix(interp));
            itemRenderer.proj(Game.camera.getProjectionMatrix());
            itemRenderer.applyMat();

            vao.render();
        }
    }

    private void renderItemCard(UVPair texUV, byte light) {
        const float thickness = 1 / 16f;

        // unpack light and look up in lightmap
        var blocklight = (byte)((light >> 4) & 0xF);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);

        var u0 = UVPair.texU(texUV.u);
        var v0 = UVPair.texV(texUV.v);
        var u1 = UVPair.texU(texUV.u + 1);
        var v1 = UVPair.texV(texUV.v + 1);

        // center the card around origin by offsetting from -0.5 to 0.5
        const float halfSize = 0.5f;
        const float halfThickness = thickness / 2f;

        // front face
        addQuad(-halfSize, -halfSize, -halfThickness, -halfSize, halfSize, -halfThickness,
            halfSize, halfSize, -halfThickness, halfSize, -halfSize, -halfThickness,
            u0, v1, u0, v0, u1, v0, u1, v1, lightVal.R, lightVal.G, lightVal.B);

        // back face
        addQuad(halfSize, -halfSize, halfThickness, halfSize, halfSize, halfThickness,
            -halfSize, halfSize, halfThickness, -halfSize, -halfSize, halfThickness,
            u1, v1, u1, v0, u0, v0, u0, v1, lightVal.R, lightVal.G, lightVal.B);
    }

    private void addQuad(float x1, float y1, float z1, float x2, float y2, float z2,
        float x3, float y3, float z3, float x4, float y4, float z4,
        float u1, float v1, float u2, float v2, float u3, float v3, float u4, float v4,
        byte r, byte g, byte b) {

        vertices.Add(new BlockVertexTinted(x1, y1, z1, u1, v1, r, g, b, 255));
        vertices.Add(new BlockVertexTinted(x2, y2, z2, u2, v2, r, g, b, 255));
        vertices.Add(new BlockVertexTinted(x3, y3, z3, u3, v3, r, g, b, 255));
        vertices.Add(new BlockVertexTinted(x4, y4, z4, u4, v4, r, g, b, 255));
    }
}