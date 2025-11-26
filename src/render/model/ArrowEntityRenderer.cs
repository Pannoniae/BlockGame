using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.util.meth;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.entity;
using BlockGame.world.item;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render.model;

/**
 * Renders arrows as two crossed quads (plus shape) for 3D effect.
 */
public class ArrowEntityRenderer : EntityRenderer<ArrowEntity> {
    private readonly List<BlockVertexTinted> vertices = [];
    private readonly StreamingVAO<BlockVertexTinted>? vao;

    public ArrowEntityRenderer() {
        vao = new StreamingVAO<BlockVertexTinted>();
        vao.bind();
        vao.setSize(8);
    }

    public void render(MatrixStack mat, Entity e, float scale, double interp) {
        if (e is not ArrowEntity arrow) {
            return;
        }

        mat.push();

        var pitch = Meth.lerp(arrow.prevRotation.X, arrow.rotation.X, (float)interp);
        var yaw = Meth.lerp(arrow.prevRotation.Y, arrow.rotation.Y, (float)interp);

        mat.rotate(yaw, 0, 1, 0);
        mat.rotate(pitch, 1, 0, 0);
        mat.translate(0, 0, -0.5f); // move to tail of arrow

        vertices.Clear();

        var texUV = Item.ARROW_WOOD.tex;
        var u0 = UVPair.texCoordsi(texUV).X;
        var v0 = UVPair.texCoordsi(texUV).Y;
        var u1 = UVPair.texCoordsi(texUV + 1).X;
        var v1 = UVPair.texCoordsi(texUV + 1).Y;

        // get lighting
        var pos = arrow.position.toBlockPos();
        var l = arrow.world.getLightC(pos.X, pos.Y, pos.Z);
        var lightVal = WorldRenderer.getLightColour((byte)(l & 15), (byte)(l >> 4));

        const float length = 1f;
        const float width = 1 / 4f;

        addQuad(
            -width, 0, 0,  width, 0, 0,  width, 0, length,  -width, 0, length,
            u0, v1, u1, v1, u1, v0, u0, v0,
            lightVal.R, lightVal.G, lightVal.B
        );

        addQuad(
            0, -width, 0,  0, width, 0,  0, width, length,  0, -width, length,
            u0, v1, u1, v1, u1, v0, u0, v0,
            lightVal.R, lightVal.G, lightVal.B
        );

        if (vertices.Count > 0) {
            vao.bind();
            Game.renderer.bindQuad();
            vao.upload(CollectionsMarshal.AsSpan(vertices));

            Game.graphics.tex(0, Game.textures.itemTexture);
            Game.graphics.instantTextureShader.use();

            var idt = Game.graphics.idt;
            idt.model(mat);
            idt.view(Game.camera.getViewMatrix(interp));
            idt.proj(Game.camera.getProjectionMatrix());
            idt.applyMat();

            // disable backface culling
            // todo emit proper double-sided quads instead...
            Game.GL.Disable(EnableCap.CullFace);
            vao.render();
            Game.GL.Enable(EnableCap.CullFace);
        }

        mat.pop();
    }

    private void addQuad(
        float x1, float y1, float z1, float x2, float y2, float z2,
        float x3, float y3, float z3, float x4, float y4, float z4,
        float u1, float v1, float u2, float v2, float u3, float v3, float u4, float v4,
        byte r, byte g, byte b) {

        vertices.Add(new BlockVertexTinted(x1, y1, z1, u1, v1, r, g, b, 255));
        vertices.Add(new BlockVertexTinted(x2, y2, z2, u2, v2, r, g, b, 255));
        vertices.Add(new BlockVertexTinted(x3, y3, z3, u3, v3, r, g, b, 255));
        vertices.Add(new BlockVertexTinted(x4, y4, z4, u4, v4, r, g, b, 255));
    }
}
