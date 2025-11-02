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
using Silk.NET.OpenGL.Legacy;

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

        // translate so it's not rendered at centre but the corner
        var o = 0.25f / 2f;
        mat.translate(0, o, 0);

        // apply hover animation
        var age = itemEntity.age + (float)interp;
        var ho =  float.Sin(age * (1 / 16f)) * (1 / 32f);
        mat.translate(0, ho, 0);

        // rotate slowly for visual appeal:tm:
        var rot = (itemEntity.age + (float)interp) * 2f;
        mat.rotate(rot, 0, 1, 0);

        // scale down the item
        float itemScale = item.isBlock() ? 0.25f : 0.4f;
        mat.scale(itemScale, itemScale, itemScale);



        if (item.isBlock() && !Block.renderItemLike[item.getBlock()!.id]) {
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
        var l = world.getLightC(pos.X, pos.Y, pos.Z);

        Game.graphics.tex(0, Game.textures.blockTexture);
        Game.blockRenderer.setupStandalone();

        // centre the block around origin
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

        var item = itemEntity.stack.getItem();
        var texUV = item.getTexture(itemEntity.stack);

        var world = itemEntity.world;
        var pos = itemEntity.position.toBlockPos();
        var l = world.getLightC(pos.X, pos.Y, pos.Z);



        Game.blockRenderer.setupStandalone();

        // offset -0.5 so it's centred on origin
        mat.push();
        // NOZ
        mat.translate(-0.5f, -0.5f, 0f);

        var idt = Game.graphics.idt;

        idt.begin(PrimitiveType.Quads);

        // itemLike blocks use block texture atlas, regular items use item atlas
        if (item.isBlock() && Block.renderItemLike[item.getBlock()!.id]) {
            idt.setTexture(Game.textures.blockTexture);
        } else {
            idt.setTexture(Game.textures.itemTexture);
        }

        Game.player.handRenderer.renderItemInHand(itemEntity.stack, WorldRenderer.getLightColour((byte)(l & 15), (byte)(l >> 4)));

        idt.model(mat);
        idt.view(Game.camera.getViewMatrix(interp));
        idt.proj(Game.camera.getProjectionMatrix());
        idt.applyMat();

        idt.end();

        mat.pop();
    }

    private void renderItem(UVPair texUV, byte light) {
        const float thickness = 1 / 16f;

        // unpack light and look up in lightmap
        var blocklight = (byte)((light >> 4) & 0xF);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);

        var u = UVPair.texCoordsi(texUV);
        var v = UVPair.texCoordsi(texUV + 1);
        var u0 = u.X;
        var v0 = v.Y;
        var u1 = u.Y;
        var v1 = v.X;

        // front face
        addQuad(0, 0, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0,
            u0, v1, u0, v0, u1, v0, u1, v1, lightVal.R, lightVal.G, lightVal.B);

        // back face
        addQuad(1, 0, thickness, 1, 1, thickness, 0, 1, thickness, 0, 0, thickness,
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