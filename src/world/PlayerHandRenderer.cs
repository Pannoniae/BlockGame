using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.item;
using Molten;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.world;

public class PlayerHandRenderer {
    public Player player;
    public StreamingVAO<BlockVertexTinted> vao;
    private List<BlockVertexTinted> vertices = [];

    private ItemStack handItem;
    private int handSlot;

    /// Lower block when switching
    public double prevLower;

    public double lower;

    private int uMVP;
    private int tex;

    // Water overlay renderer
    private InstantDrawTexture waterOverlayRenderer;

    // Item renderer for hand
    private InstantDrawTexture itemRenderer;

    public PlayerHandRenderer(Player player) {
        this.player = player;
        handItem = player.survivalInventory.getSelected();
        vao = new StreamingVAO<BlockVertexTinted>();
        vao.bind();
        vao.setSize(Face.MAX_FACES * 4);
        uMVP = Game.graphics.instantTextureShader.getUniformLocation("uMVP");
        tex = Game.graphics.instantTextureShader.getUniformLocation("tex");

        // Initialize water overlay renderer
        waterOverlayRenderer = new InstantDrawTexture(60);
        waterOverlayRenderer.setup();

        // Initialize item renderer
        itemRenderer = new InstantDrawTexture(100);
        itemRenderer.setup();
    }

    public double getLower(double dt) {
        return double.Lerp(prevLower, lower, dt);
    }

    // This method is held together with duct tape and prayers, send help
    public void render(double dt, double interp) {
        if (handItem == ItemStack.EMPTY) {
            return;
        }

        var a = handItem.getItem().isBlock();

        var world = player.world;
        var pos = player.position.toBlockPos();
        Game.graphics.tex(0, Game.textures.blockTexture);
        Game.blockRenderer.setupStandalone();

        var l = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;

        if (a) {
            Game.blockRenderer.renderBlock(Item.get(handItem.id).getBlock(), (byte)handItem.metadata, Vector3I.Zero,
                vertices,
                lightOverride: l,
                cullFaces: false);

            vao.bind();
            Game.renderer.bindQuad();
            vao.upload(CollectionsMarshal.AsSpan(vertices));
        }
        else {
            // render item as flat card using InstantDrawTexture
            itemRenderer.begin(PrimitiveType.Quads);

        }


        var swingProgress = (float)player.getSwingProgress(interp);

        //swingProgress = (world.worldTick % 360 + interp) / 360.0;

        //swingProgress = 0.0005;
        // thx classicube? the description is bs with the matrices but it gives some ideas for the maths
        var sinSwing = MathF.Sin(swingProgress * MathF.PI);
        var sinSwingSqrt = MathF.Sin(MathF.Sqrt(swingProgress) * MathF.PI);
        // we need something like a circle?
        var circleishThing = MathF.Sin(MathF.Sqrt(swingProgress) * MathF.PI * 2f);
        var circle = MathF.Sin(swingProgress * MathF.PI * 2f);

        var mat = Game.graphics.model;

        //Console.Out.WriteLine(mat.stack.Count);
        mat.push();
        mat.loadIdentity();

        //Console.Out.WriteLine(mat.print());


        //var pivot = new Vector3(0.5f, 0.5f, 0.5f);
        // the swing code (common)

        mat.translate(sinSwingSqrt * -0.7f, ((float)-getLower(interp) + circleishThing) * 0.35f, sinSwing * 0.5f);

        // the lowering
        //mat.translate(0,  * 0.35f, 0);
        mat.translate(0.65f, -1.45f, 1f);



        if (a) {

            mat.translate(0.5f, 0.5f, 0.5f);

            mat.scale(0.6f);

            mat.rotate(sinSwingSqrt * 90, 1, 0, 0);
            mat.rotate(sinSwingSqrt * 20, 0, 0, 1);

            // only rotate a *bit*
            mat.rotate(sinSwing * 10, 0, 1, 0);

            // show the sunny side!
            mat.rotate(-45, 0, 1, 0);

            // rotate around the centre point
            mat.translate(-0.5f, -0.5f, -0.5f);

            Game.graphics.instantTextureShader.use();
            Game.graphics.instantTextureShader.setUniform(uMVP,
                mat.top * Game.camera.getHandViewMatrix(interp) * Game.camera.getFixedProjectionMatrix());
            Game.graphics.instantTextureShader.setUniform(tex, 0);
            vao.render();
        }

        else {

            mat.push();


            // we need to fixup the rotation a bit because items don't rotate somehow??

            mat.translate(0, 0, sinSwing * -0.3f);

            mat.translate(0.5f, 0.5f, 0.5f);

            mat.rotate(sinSwingSqrt * 30, 0, 0, 1);
            mat.rotate(sinSwing * 20, 0, 1, 0);

            mat.rotate(sinSwingSqrt * 90, 1, 0, 0);
            mat.rotate(sinSwingSqrt * 20, 0, 0, 1);

            // only rotate a *bit*
            mat.rotate(sinSwing * 10, 0, 1, 0);


            // it's too much to the left..
            mat.translate(0.5f, 0.2f, 0f);

            // rotate into direction
            // overrotate a bit so it's not as "harsh" into the distance
            mat.rotate(80, 0, 1, 0);

            // we rotate the item "into place"
            mat.rotate(20.0F, 0.0F, 0.0F, 1.0F);

            // do shit around the centre point
            mat.translate(-0.5f, -0.5f, -0.5f);

            // we can't just shrink here because it messes the swing animation up..
            //mat.scale(0.5f);



            itemRenderer.setTexture(Game.textures.itemTexture);


            //Console.Out.WriteLine((mat.top).print());
            itemRenderer.setMVP(mat.top * Game.camera.getHandViewMatrix(interp) * Game.camera.getFixedProjectionMatrix());
            itemRenderer.setMV(mat.top * Game.camera.getHandViewMatrix(interp));


            renderItemInHand(handItem, WorldRenderer.getLightColour((byte)(l >> 4), (byte)(l & 15)));

            //Game.GL.Disable(EnableCap.CullFace);
            //Game.GL.FrontFace(FrontFaceDirection.CW);
            itemRenderer.end();

            mat.pop();
        }

        mat.pop();

        // Render water overlay if player is underwater
        if (player.isUnderWater()) {
            renderWaterOverlay();
        }
    }

    private void renderItemInHand(ItemStack itemStack, Color4b lightOverride) {
        var item = Item.get(itemStack.id);
        var texUV = item.getTexture(itemStack);

        //Console.Out.WriteLine(lightOverride);

        Span<Color4b> shade = [
            new(0.8f, 0.8f, 0.8f, 1f),
            new(0.8f, 0.8f, 0.8f, 1f),
            new(0.6f, 0.6f, 0.6f, 1f),
            new(0.6f, 0.6f, 0.6f, 1f),
            new(0.6f, 0.6f, 0.6f, 1f),
            new(1f, 1f, 1f, 1f)
        ];


        const float thickness = 1 / 16f;
        // todo make this dynamic based on atlas size later!
        const int strips = UVPair.ATLASSIZE;

        // if you don't it z-fights?? i dont fully get why tho, its probably because of pixel boundary shit in the uv but idk
        const float epsilon = 1 / 4096f;

        var u0 = UVPair.texU(texUV.u);
        var v0 = UVPair.texV(texUV.v);
        var u1 = UVPair.texU(texUV.u + 1);
        var v1 = UVPair.texV(texUV.v + 1);


        // Front face
        Color4b frontShade = lightOverride * shade[5];
        addQuad(0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, u0, v0, u0, v1, u1, v1, u1, v0, frontShade);

        //return;

        // Back face
        Color4b backShade = lightOverride * shade[4];
        addQuad(0, 0, thickness, 0, 1, thickness, 1, 1, thickness, 1, 0, thickness, u0, v1, u0, v0, u1, v0, u1, v1,
            backShade);

        // Left face - slices from x=0 to x=15/16
        Color4b leftShade = lightOverride * shade[0];
        float u;
        float v;
        for (int i = 0; i < strips; i++) {
            float r = (float)i / strips;
            float x = r;
            u = u0 + (u1 - u0) * r + epsilon;

            addQuad(x, 1, thickness, x, 0, thickness, x, 0, 0, x, 1, 0,
                u, v0, u, v1, u, v1, u, v0, leftShade);
        }

        // Right face - slices from x=1/16 to x=1
        Color4b rightShade = lightOverride * shade[1];
        for (int i = 0; i < strips; i++) {
            float r = (float)i / strips;
            float x = r + thickness;
            u = u0 + (u1 - u0) * r + epsilon;

            addQuad(x, 0, thickness, x, 1, thickness, x, 1, 0, x, 0, 0,
                u, v1, u, v0, u, v0, u, v1, rightShade);
        }

        // Top face - slices from y=1 to y=1/16
        Color4b topShade = lightOverride * shade[2];
        for (int i = 0; i < strips; i++) {
            float r = (float)i / strips;
            float y = 1 - r;
            v = v0 + (v1 - v0) * r + epsilon;

            addQuad(0, y, thickness, 0, y, 0, 1, y, 0, 1, y, thickness,
                u0, v, u0, v, u1, v, u1, v, topShade);
        }

        // Bottom face - slices from y=15/16 to y=0
        Color4b bottomShade = lightOverride * shade[3];
        for (int i = 0; i < strips; i++) {
            float r = (float)i / strips;
            float y = (1 - thickness) - r;
            v = v0 + (v1 - v0) * r + epsilon;

            addQuad(1, y, thickness, 1, y, 0, 0, y, 0, 0, y, thickness,
                u1, v, u1, v, u0, v, u0, v, bottomShade);
        }
    }

    private void addQuad(float x1, float y1, float z1, float x2, float y2, float z2,
        float x3, float y3, float z3, float x4, float y4, float z4,
        float u1, float v1, float u2, float v2, float u3, float v3, float u4, float v4, Color4b shade) {
        // Add 4 vertices for quad

        //itemRenderer.begin(PrimitiveType.Quads);

        itemRenderer.addVertex(new BlockVertexTinted(x1, y1, z1, u1, v1, shade.R, shade.G, shade.B, shade.A));
        itemRenderer.addVertex(new BlockVertexTinted(x2, y2, z2, u2, v2, shade.R, shade.G, shade.B, shade.A));
        itemRenderer.addVertex(new BlockVertexTinted(x3, y3, z3, u3, v3, shade.R, shade.G, shade.B, shade.A));
        itemRenderer.addVertex(new BlockVertexTinted(x4, y4, z4, u4, v4, shade.R, shade.G, shade.B, shade.A));

        //itemRenderer.end();
    }

    private void renderWaterOverlay() {
        // Set the water overlay texture
        waterOverlayRenderer.setTexture(Game.textures.waterOverlay);

        // Set identity MVP matrix (screen space coordinates)
        var identityMVP = Matrix4x4.Identity;
        waterOverlayRenderer.setMVP(identityMVP);


        // Draw a full-screen quad with slightly blue tint
        const float alpha = 0.5f;
        // multiply by lighting

        var world = Game.world;
        var blockPos = player.position.toBlockPos();

        var skylight = world.getSkyLight(blockPos.X, blockPos.Y, blockPos.Z);
        var blocklight = world.getBlockLight(blockPos.X, blockPos.Y, blockPos.Z);
        var tint = Game.renderer.getLightColourDarken(blocklight, skylight);

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