using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.item;
using Molten;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.world;

public class PlayerRenderer {
    public Player player;
    public StreamingVAO<BlockVertexTinted> vao;
    private List<BlockVertexTinted> vertices = new();
    private List<ushort> indices = new();

    private ItemStack? handItem;
    private int handSlot;

    /// Lower block when switching
    public double prevLower;

    public double lower;

    private int uMVP;
    private int tex;

    // Water overlay renderer
    private InstantDrawTexture waterOverlayRenderer;

    public PlayerRenderer(Player player) {
        this.player = player;
        handItem = player.survivalInventory.getSelected();
        vao = new StreamingVAO<BlockVertexTinted>();
        vao.bind();
        vao.setSize(Face.MAX_FACES * 4);
        uMVP = main.Game.graphics.instantTextureShader.getUniformLocation("uMVP");
        tex = main.Game.graphics.instantTextureShader.getUniformLocation("tex");

        // Initialize water overlay renderer
        waterOverlayRenderer = new InstantDrawTexture(60);
        waterOverlayRenderer.setup();
    }

    public double getLower(double dt) {
        return double.Lerp(prevLower, lower, dt);
    }

    public void render(double dt, double interp) {
        if (handItem == null) {
            return;
        }

        var world = player.world;
        var pos = player.position.toBlockPos();
        main.Game.graphics.tex(0, main.Game.textures.blockTexture);
        var light = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;
        main.Game.blockRenderer.setupStandalone();

        if (handItem.getItem().isBlock()) {
            main.Game.blockRenderer.renderBlock(Item.get(handItem.id).getBlock(), (byte)handItem.metadata, Vector3I.Zero, vertices,
                lightOverride: (byte)world.getBrightness(light, (byte)world.getSkyDarkenFloat(world.worldTick)), cullFaces: false);
        }

        vao.bind();
        main.Game.renderer.bindQuad();
        vao.upload(CollectionsMarshal.AsSpan(vertices));

        var swingProgress = player.getSwingProgress(interp);
        // thx classicube? the description is bs with the matrices but it gives some ideas for the maths
        var sinSwing = Math.Sin(swingProgress * Math.PI);
        var sinSwingSqrt = Math.Sin(Math.Sqrt(swingProgress) * Math.PI);
        // we need something like a circle?
        var circleishThing = Math.Sin(Math.Sqrt(swingProgress) * Math.PI * 2);

        var mat = main.Game.graphics.modelView.reversed();
        mat.push();
        mat.loadIdentity();
        
        
        var pivot = new Vector3(0.5f, 0.5f, 0.5f);
        
        mat.rotate(45, 0, 1, 0, pivot);
        
        mat.rotate((float)(-sinSwing * 20), 0, 1, 0, pivot);
        mat.rotate((float)(sinSwingSqrt * 20), 0, 0, 1, pivot);
        mat.rotate((float)(sinSwingSqrt * 20), 1, 0, 0, pivot);
        
        mat.scale(0.6f, pivot);
        
        mat.translate(0.75f, (float)(-1.45f - (getLower(interp) * 0.35f)), 1f);
        
        mat.translate((float)(sinSwingSqrt * -0.7f), (float)(circleishThing * 0.35f), (float)(sinSwing * 0.6f));
        
        main.Game.graphics.instantTextureShader.use();
        main.Game.graphics.instantTextureShader.setUniform(uMVP,
            mat.top * main.Game.camera.getHandViewMatrix(interp) * main.Game.camera.getFixedProjectionMatrix());
        main.Game.graphics.instantTextureShader.setUniform(tex, 0);
        vao.render();
        
        mat.reversed().pop();

        // Render water overlay if player is underwater
        if (player.isUnderWater()) {
            renderWaterOverlay();
        }
    }

    private void renderWaterOverlay() {
        // Set the water overlay texture
        waterOverlayRenderer.setTexture(main.Game.textures.waterOverlay);

        // Set identity MVP matrix (screen space coordinates)
        var identityMVP = Matrix4x4.Identity;
        waterOverlayRenderer.setMVP(identityMVP);


        // Draw a full-screen quad with slightly blue tint
        float alpha = 0.5f;
        // multiply by lighting

        var world = main.Game.world;
        var blockPos = player.position.toBlockPos();

        var skylight = world.getSkyLight(blockPos.X, blockPos.Y, blockPos.Z);
        var blocklight = world.getBlockLight(blockPos.X, blockPos.Y, blockPos.Z);
        var tint = main.Game.renderer.getLightColourDarken(skylight, blocklight);

        var r = (byte)(tint.R * 255);
        var g = (byte)(tint.G * 255);
        var b = (byte)(tint.B * 255);

        waterOverlayRenderer.begin(PrimitiveType.Triangles);

        waterOverlayRenderer.addVertex(new BlockVertexTinted(-1, -1, 0, 0f, 0f, r, g, b, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(1, -1, 0, 1f, 0f, r, g, b, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(-1, 1, 0, 0f, 1f, r, g, b, (byte)(alpha * 255)));

        waterOverlayRenderer.addVertex(new BlockVertexTinted(-1, 1, 0, 0f, 1f, r, g, b, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(1, -1, 0, 1f, 0f, r, g, b, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(1, 1, 0, 1f, 1f, r, g, b, (byte)(alpha * 255)));

        // Render the overlay
        waterOverlayRenderer.end();
    }

    public void update(double dt) {
        prevLower = lower;
        // if the player has the same item, raise, else lower
        double target;

        var d = dt * 5;
        if (handSlot == player.survivalInventory.selected && handItem == player.survivalInventory.getSelected()) {
            target = 0;
        }
        else {
            target = 1;
        }

        if (lower < target) {
            lower += d;
        }
        else {
            lower -= d;
        }

        lower = Math.Clamp(lower, 0, 1);

        // lowering shit
        if (lower > 0.8f) {
            handSlot = player.survivalInventory.selected;
            handItem = player.survivalInventory.getSelected();
        }
    }
}