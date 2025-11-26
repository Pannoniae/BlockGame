using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render;
using BlockGame.render.model;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.world;

public class PlayerHandRenderer {
    public Player player;
    public StreamingVAO<BlockVertexTinted> vao;
    private readonly List<BlockVertexTinted> vertices = [];

    private ItemStack handItem;
    private int handSlot;

    /// Lower block when switching
    public double prevLower;

    public double lower;

    // Recoil animation for weapons
    private float recoilProgress;
    private float prevRecoilProgress;

    // Water overlay renderer
    public readonly InstantDrawTexture waterOverlayRenderer;

    public PlayerHandRenderer(Player player) {
        this.player = player;
        handItem = player.inventory.getSelected();
        vao = new StreamingVAO<BlockVertexTinted>();
        vao.bind();
        vao.setSize(Face.MAX_FACES * 4);

        // Initialize water overlay renderer
        waterOverlayRenderer = new InstantDrawTexture(60);
        waterOverlayRenderer.setup();
    }

    public double getLower(double dt) {
        return double.Lerp(prevLower, lower, dt);
    }

    // This method is held together with duct tape and prayers, send help
    public void render(double interp) {
        var handItem = this.handItem ?? ItemStack.EMPTY;
        var renderHand = handItem == ItemStack.EMPTY;

        var world = player.world;
        var pos = player.position.toBlockPos();
        var l = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;

        // If rendering empty hand, skip item-specific shit
        if (renderHand) {
            renderEmptyHand(interp, l);
        }
        else {
            var itemRenderer = Game.graphics.idt;

            var item = handItem.getItem();
            var a = handItem.getItem().isBlock() && !Block.renderItemLike[handItem.getItem().getBlock()!.id];

            Game.graphics.tex(0, Game.textures.blockTexture);
            Game.blockRenderer.setupStandalone();

            if (a) {
                Game.blockRenderer.renderBlock(handItem.getItem().getBlock()!, (byte)handItem.metadata, Vector3I.Zero,
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

            //swingProgress = (world.worldTick % 360f + (float)interp) / 360.0f;

            //swingProgress = 0.9f;
            // thx classicube? the description is bs with the matrices but it gives some ideas for the maths
            var sinSwing = MathF.Sin(swingProgress * MathF.PI);
            var sinSwingSqrt = MathF.Sin(MathF.Sqrt(swingProgress) * MathF.PI);
            var sinSwingSqrtish = MathF.Sin(float.Pow(swingProgress, 0.75f) * MathF.PI);
            // we need something like a circle?
            var circleishThing = MathF.Sin(MathF.Sqrt(swingProgress) * MathF.PI * 2f);
            var circleindThing = MathF.Sin(float.Pow(swingProgress, 0.25f) * MathF.PI * 2f);
            var circleimpThing = MathF.Sin(swingProgress * swingProgress * MathF.PI * 2f);
            var circleishishThing = MathF.Sin(float.Pow(swingProgress, 0.75f) * MathF.PI * 2f);
            var circle = MathF.Sin(swingProgress * MathF.PI * 2f);

            // recoil animation
            var recoilProg = float.Lerp(prevRecoilProgress, recoilProgress, (float)interp);
            var recoilKick = recoilProg * 0.3f;

            var mat = Game.graphics.model;

            //Console.Out.WriteLine(mat.stack.Count);
            mat.push();
            //mat.loadIdentity();

            //Console.Out.WriteLine(mat.print());


            //var pivot = new Vector3(0.5f, 0.5f, 0.5f);
            // the swing code (common)

            mat.translate(sinSwingSqrt * -0.7f, ((float)-getLower(interp) + circleishThing) * 0.35f, sinSwing * 0.5f);
            // cheat a bit, lower in second half
            // cut positive out
            var prog = float.Min(0, circleishThing) * 0.2f;
            mat.translate(0, prog, 0);

            // the lowering
            //mat.translate(0,  * 0.35f, 0);
            mat.translate(0.65f, -1.45f, 1f);


            if (a) {
                mat.translate(0.5f, 0.5f, 0.5f);

                mat.scale(0.6f);

                mat.rotate(sinSwingSqrt * 60, 1, 0, 0);
                mat.rotate(sinSwingSqrt * 20, 0, 0, 1);

                // only rotate a *bit*
                mat.rotate(sinSwing * 10, 0, 1, 0);

                // show the sunny side!
                mat.rotate(-45, 0, 1, 0);

                // rotate around the centre point
                mat.translate(-0.5f, -0.5f, -0.5f);

                Game.graphics.instantTextureShader.use();

                itemRenderer.model(mat);
                itemRenderer.view(Game.camera.getHandViewMatrix(interp));
                itemRenderer.proj(Game.camera.getFixedProjectionMatrix());

                // actually apply uniform (it's not automatic here because we don't use instantdraw!)
                itemRenderer.applyMat();

                vao.render();
            }

            else {
                mat.push();


                // we need to fixup the rotation a bit because items don't rotate somehow??

                mat.translate(sinSwingSqrt * -0.35f, 0, sinSwing * -0.2f);
                //mat.translate(sinSwingSqrtish * 0.7f + sinSwing * -0.6f, 0, sinSwing * -0.5f);
                //mat.translate(sinSwingSqrt * -0.7f, ((float)-getLower(interp) + circleishThing) * 0.35f, sinSwing * 0.5f);

                mat.translate(0.5f, 0.5f, 0.5f);

                mat.rotate(sinSwingSqrt * 30, 0, 0, 1);
                mat.rotate(sinSwing * 20, 0, 1, 0);

                mat.rotate(sinSwingSqrt * 90, 1, 0, 0);
                mat.rotate(sinSwingSqrt * 20, 0, 0, 1);

                // only rotate a *bit*
                mat.rotate(sinSwing * 10, 0, 1, 0);


                // it's too much to the left..
                mat.translate(0.5f, 0.2f, 0f);

                mat.translate(0, recoilKick, 0); // recoil translation

                // rotate into direction
                // overrotate a bit so it's not as "harsh" into the distance
                mat.rotate(80, 0, 1, 0);

                // we rotate the item "into place"
                mat.rotate(20, 0, 0, 1);

                // do shit around the centre point
                mat.translate(-0.5f, -0.5f, -0.5f);

                // we can't just shrink here because it messes the swing animation up..
                //mat.scale(0.5f);


                if (item.isBlock() && Block.renderItemLike[item.getBlock()!.id]) {
                    itemRenderer.setTexture(Game.textures.blockTexture);
                }
                else {
                    itemRenderer.setTexture(Game.textures.itemTexture);
                }


                //Console.Out.WriteLine((mat.top).print());
                var m = mat.top * Game.camera.getHandViewMatrix(interp) * Game.camera.getFixedProjectionMatrix();
                var m2 = mat.top * Game.camera.getHandViewMatrix(interp);
                itemRenderer.setMVP(ref m);
                itemRenderer.setMV(ref m2);


                renderItemInHand(handItem, WorldRenderer.getLightColour((byte)(l & 15), (byte)(l >> 4)));

                //Game.GL.Disable(EnableCap.CullFace);
                //Game.GL.FrontFace(FrontFaceDirection.CW);
                itemRenderer.end();

                mat.pop();
            }

            mat.pop();
        }

        var liquid = player.getBlockAtEyes();

        //Console.Out.WriteLine(liquid);

        // Render water overlay if player is underwater
        if (liquid == Block.WATER) {
            renderWaterOverlay();
        }
        else if (liquid == Block.LAVA) {
            renderLavaOverlay();
        }

        if (player.fireTicks > 0) {
            renderFireOverlay();
        }
    }

    private void renderEmptyHand(double interp, byte lightLevel) {
        var swingProgress = (float)player.getSwingProgress(interp);
        var sinSwing = MathF.Sin(swingProgress * MathF.PI);
        var sinSwingSqrt = MathF.Sin(MathF.Sqrt(swingProgress) * MathF.PI);
        var circleishThing = MathF.Sin(MathF.Sqrt(swingProgress) * MathF.PI * 2f);
        var circleishishThing = MathF.Sin(float.Pow(swingProgress, 0.75f) * MathF.PI * 2f);

        var mat = Game.graphics.model;
        mat.push();
        mat.loadIdentity();

        var ide = EntityRenderers.ide;

        // Set matrix components for automatic computation
        ide.model(mat);
        ide.view(Game.camera.getHandViewMatrix(interp));
        ide.proj(Game.camera.getProjectionMatrix());


        var entity = player;

        // Get the actual rightArm from the player's model
        var rightArm = player.modelRenderer.model.rightArm;

        const float sc = 1 / 16f;

        // interpolate position and rotation
        var interpPos = Vector3D.Lerp(entity.prevPosition, entity.position, interp);
        var interpRot = Vector3.Lerp(entity.prevRotation, entity.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(entity.prevBodyRotation, entity.bodyRotation, (float)interp);

        var headRotX = interpRot.X - interpBodyRot.X; // pitch diff
        var headRotY = interpRot.Y - interpBodyRot.Y; // yaw diff

        // Translate to interpolated position
        //mat.translate((float)interpPos.X, (float)interpPos.Y, (float)interpPos.Z);
        // Rotate to face the correct direction
        //mat.rotate(interpRot.Y, 0, 1, 0);
        //mat.rotate(interpRot.Z, 1, 0, 0);
        //mat.rotate(interpBodyRot.X, 1, 0, 0);


        //mat.translate(sinSwingSqrt * -0.2f, ((float)-getLower(interp) + circleishThing) * 0.15f, sinSwing * 0.15f);
        //mat.translate(0.65f, -1.45f, 1f);

        //mat.scale(8f);
        //mat.translate(0.5f, 2.5f, -0.5f);

        // translate enough so we can only see the tip hehehe
        //mat.translate(0.4f, -0f, 3f);

        //mat.translate(sinSwingSqrt * -0.35f, 0, sinSwing * -0.2f);
        //mat.translate(sinSwingSqrtish * 0.7f + sinSwing * -0.6f, 0, sinSwing * -0.5f);
        //mat.translate(sinSwingSqrt * -0.7f, ((float)-getLower(interp) + circleishThing) * 0.35f, sinSwing * 0.5f);


        //mat.rotate(sinSwingSqrt * 30, 0, 0, 1);
        //mat.rotate(-sinSwing * 20, 0, 1, 0);

        //mat.rotate(sinSwingSqrt * 5, 1, 0, 0);
        //mat.rotate(sinSwingSqrt * 20, 0, 0, 1);

        // only rotate a *bit*
        //mat.rotate(-sinSwing * 10, 0, 1, 0);

        // rotate it a bit
        //mat.rotate(5, 1, 0, 0);
        //DEBUG TESTER
        //mat.translate(0f, 0f, 4f);

        mat.translate(0.4f, -1.0f, 0.3f);

        //mat.translate(sinSwingSqrt * -0.35f, 0, sinSwing * -0.2f);
        //mat.translate(sinSwingSqrtish * 0.7f + sinSwing * -0.6f, 0, sinSwing * -0.5f);
        mat.translate(sinSwingSqrt * -0.1f, ((float)-getLower(interp) + circleishThing) * 0.2f, sinSwing * 0.1f);

        //mat.rotate(-45 - 5, 1, 0, 0);
        // we cheat a bit, we need to change the base
        mat.translate(0, 0.2f, 0);
        mat.rotate(sinSwingSqrt * 50, 0, 0, 1);
        mat.translate(0, -0.2f, 0);
        //mat.rotate(-(-45 - 5), 1, 0, 0);
        //mat.rotate(sinSwing * 20, 0, 1, 0);

        //mat.rotate(sinSwingSqrt * 20, 1, 0, 0);
        //mat.rotate(sinSwingSqrt * 20, 0, 0, 1);

        // only rotate a *bit*
        //mat.rotate(-sinSwing * 10, 0, 1, 0);

        // we rotate the item "into place"
        mat.rotate(-45 - 5, 1, 0, 0);
        mat.rotate(60 + 15, 0, 0, 1);

        mat.rotate(-90 + 10, 1, 0, 0);

        // apply head pitch!!
        //mat.rotate(-headRotX, 1, 0, 0);

        // go to the arm BUT REVERSE hehehehe
        mat.translate(-rightArm.position.X * sc, -rightArm.position.Y * sc, -rightArm.position.Z * sc);

        // How about not having ANY of this??
        var originalRotation = rightArm.rotation;
        rightArm.rotation = new Vector3(0, 0, 0);

        // Set human texture
        Game.graphics.tex(0, Game.textures.human);

        // test point
        //Console.Out.WriteLine(Vector3.Transform(Vector3.Zero, mat.top));

        var c = WorldRenderer.getLightColour((byte)(lightLevel & 15), (byte)(lightLevel >> 4));

        EntityRenderers.ide.setColour(new Color(c.R, c.G, c.B, (byte)255));
        rightArm.render(mat, sc);

        // Restore original rotation
        rightArm.rotation = originalRotation;

        mat.pop();

        // Render water overlay if player is underwater
        if (player.isUnderWater()) {
            renderWaterOverlay();
        }
    }

    public void renderItemInHand(ItemStack stack, Color lightOverride) {
        var item = stack.getItem();
        var texUV = item.getTexture(stack);

        //Console.Out.WriteLine(lightOverride);

        Span<Color> shade = [
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

        BTextureAtlas tex;
        // we need to handle the block/item case separately!
        tex = stack.getItem().isBlock() ? Game.textures.blockTexture : Game.textures.itemTexture;

        Vector2 s = UVPair.texCoords(tex, texUV);
        Vector2 t = UVPair.texCoords(tex, texUV + 1);

        var u0 = s.X;
        var v0 = s.Y;
        var u1 = t.X;
        var v1 = t.Y;


        // Front face
        Color frontShade = lightOverride * shade[5];
        addQuad(0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, u0, v0, u0, v1, u1, v1, u1, v0, frontShade);

        //return;

        // Back face
        Color backShade = lightOverride * shade[4];
        addQuad(0, 0, thickness, 0, 1, thickness, 1, 1, thickness, 1, 0, thickness, u0, v1, u0, v0, u1, v0, u1, v1,
            backShade);

        // Left face - slices from x=0 to x=15/16
        Color leftShade = lightOverride * shade[0];
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
        Color rightShade = lightOverride * shade[1];
        for (int i = 0; i < strips; i++) {
            float r = (float)i / strips;
            float x = r + thickness;
            u = u0 + (u1 - u0) * r + epsilon;

            addQuad(x, 0, thickness, x, 1, thickness, x, 1, 0, x, 0, 0,
                u, v1, u, v0, u, v0, u, v1, rightShade);
        }

        // Top face - slices from y=1 to y=1/16
        Color topShade = lightOverride * shade[2];
        for (int i = 0; i < strips; i++) {
            float r = (float)i / strips;
            float y = 1 - r;
            v = v0 + (v1 - v0) * r + epsilon;

            addQuad(0, y, thickness, 0, y, 0, 1, y, 0, 1, y, thickness,
                u0, v, u0, v, u1, v, u1, v, topShade);
        }

        // Bottom face - slices from y=15/16 to y=0
        Color bottomShade = lightOverride * shade[3];
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
        float u1, float v1, float u2, float v2, float u3, float v3, float u4, float v4, Color shade) {
        // Add 4 vertices for quad

        //itemRenderer.begin(PrimitiveType.Quads);

        var itemRenderer = Game.graphics.idt;
        itemRenderer.addVertex(new BlockVertexTinted(x1, y1, z1, u1, v1, shade.R, shade.G, shade.B, shade.A));
        itemRenderer.addVertex(new BlockVertexTinted(x2, y2, z2, u2, v2, shade.R, shade.G, shade.B, shade.A));
        itemRenderer.addVertex(new BlockVertexTinted(x3, y3, z3, u3, v3, shade.R, shade.G, shade.B, shade.A));
        itemRenderer.addVertex(new BlockVertexTinted(x4, y4, z4, u4, v4, shade.R, shade.G, shade.B, shade.A));

        //itemRenderer.end();
    }

    // Render held item in third person (called from HumanModel)
    public void renderThirdPerson(MatrixStack mat, double interp) {
        var handItem = this.handItem ?? ItemStack.EMPTY;
        if (handItem == ItemStack.EMPTY) {
            return;
        }

        var itemRenderer = Game.graphics.idt;

        var a = handItem.getItem().isBlock() && !Block.renderItemLike[handItem.getItem().getBlock()!.id];

        var world = player.world;
        var pos = player.position.toBlockPos();
        Game.graphics.tex(0, Game.textures.blockTexture);
        Game.blockRenderer.setupStandalone();

        var l = world.inWorld(pos.X, pos.Y, pos.Z) ? world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;

        var item = handItem.getItem();

        if (a) {
            Game.blockRenderer.renderBlock(handItem.getItem().getBlock()!, (byte)handItem.metadata, Vector3I.Zero,
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
        var sinSwing = MathF.Sin(swingProgress * MathF.PI);
        var sinSwingSqrt = MathF.Sin(MathF.Sqrt(swingProgress) * MathF.PI);


        mat.push();
        mat.translate(-0.5f, -1.28f, -0.3f);

        //mat.translate(sinSwingSqrt2 * 0.3f, sinSwingSqrt * 0.15f, sinSwingSqrt * 0.1f);

        if (a) {
            mat.translate(0.5f, 0.5f, 0.5f);

            mat.scale(0.2f);

            mat.rotate(sinSwingSqrt * 60, 1, 0, 0);
            mat.rotate(sinSwingSqrt * 20, 0, 0, 1);

            // only rotate a *bit*
            mat.rotate(sinSwing * 10, 0, 1, 0);

            // show the sunny side!
            mat.rotate(-45, 0, 1, 0);

            // rotate around the centre point
            mat.translate(-0.5f, -0.5f, -0.5f);

            Game.graphics.instantTextureShader.use();
            itemRenderer.model(mat);
            itemRenderer.view(Game.camera.getViewMatrix(interp));
            itemRenderer.proj(Game.camera.getProjectionMatrix());

            // actually apply uniform (it's not automatic here because we don't use instantdraw!)
            itemRenderer.applyMat();

            vao.render();

            // debug test point
        }

        else {
            mat.push();

            // we need to fixup the rotation a bit because items don't rotate somehow??
            //mat.translate(0, 0, sinSwing * -0.3f);

            mat.translate(0.75f, 0.45f, 0.55f);

            // bit of fixup
            mat.translate(-0.1f * sinSwing, 0, 0);

            mat.scale(0.5f);

            //mat.rotate(sinSwingSqrt * 30, 0, 0, 1);
            //mat.rotate(sinSwing * 20, 0, 1, 0);

            // rotate into a proper orientation
            mat.rotate(70, 1, 0, 0);

            mat.rotate(sinSwingSqrt * 60, 1, 0, 0);
            mat.rotate(sinSwingSqrt * 20, 0, 0, 1);

            // only rotate a *bit*
            mat.rotate(sinSwing * 10, 0, 1, 0);


            // it's too much to the left..
            //mat.translate(0.5f, 0.2f, 0f);

            // rotate into direction
            // overrotate a bit so it's not as "harsh" into the distance
            mat.rotate(80, 0, 1, 0);

            // we rotate the item "into place"
            mat.rotate(20.0f, 0, 0, 1);

            // do shit around the centre point
            mat.translate(-0.5f, -0.5f, -0.5f);

            // we can't just shrink here because it messes the swing animation up..
            //mat.scale(0.5f);


            if (item.isBlock() && Block.renderItemLike[item.getBlock()!.id]) {
                itemRenderer.setTexture(Game.textures.blockTexture);
            }
            else {
                itemRenderer.setTexture(Game.textures.itemTexture);
            }


            //Console.Out.WriteLine((mat.top).print());
            itemRenderer.model(mat);
            itemRenderer.view(Game.camera.getViewMatrix(interp));
            itemRenderer.proj(Game.camera.getProjectionMatrix());


            renderItemInHand(handItem, WorldRenderer.getLightColour((byte)(l & 15), (byte)(l >> 4)));

            //Game.GL.Disable(EnableCap.CullFace);
            //Game.GL.FrontFace(FrontFaceDirection.CW);
            itemRenderer.end();

            mat.pop();
        }

        mat.pop();
    }

    private void renderWaterOverlay() {
        // Set the water overlay texture
        waterOverlayRenderer.setTexture(Game.textures.waterOverlay);

        // Set identity MVP matrix (screen space coordinates)
        var identityMVP = Matrix4x4.Identity;
        waterOverlayRenderer.setMVP(ref identityMVP);


        // Draw a full-screen quad with slightly blue tint
        const float alpha = 0.5f;
        // multiply by lighting

        var world = Game.world;
        var blockPos = player.position.toBlockPos();

        var skylight = world.getSkyLight(blockPos.X, blockPos.Y, blockPos.Z);
        var blocklight = world.getBlockLight(blockPos.X, blockPos.Y, blockPos.Z);
        var tint = WorldRenderer.getLightColour(skylight, blocklight);

        var r = tint.R;
        var g = tint.G;
        var b = tint.B;

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

    private void renderLavaOverlay() {
        // Set the lava overlay texture
        waterOverlayRenderer.setTexture(Game.textures.lavaOverlay);

        // Set identity MVP matrix (screen space coordinates)
        var identityMVP = Matrix4x4.Identity;
        waterOverlayRenderer.setMVP(ref identityMVP);


        // Draw a full-screen quad with slightly blue tint
        const float alpha = 0.4f;
        // multiply by lighting

        var world = Game.world;
        var blockPos = player.position.toBlockPos();

        var skylight = world.getSkyLight(blockPos.X, blockPos.Y, blockPos.Z);
        var blocklight = world.getBlockLight(blockPos.X, blockPos.Y, blockPos.Z);
        var tint = WorldRenderer.getLightColour(skylight, blocklight);

        var r = tint.R;
        var g = tint.G;
        var b = tint.B;

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

    private void renderFireOverlay() {
        // use block texture atlas for animated fire
        waterOverlayRenderer.setTexture(Game.textures.blockTexture);

        var identityMVP = Matrix4x4.Identity;
        waterOverlayRenderer.setMVP(ref identityMVP);

        const float alpha = 0.8f;

        // fire is fullbright
        var tint = WorldRenderer.getLightColour(15, 15);

        var r = tint.R;
        var g = tint.G;
        var b = tint.B;

        var fireUV = Block.uv("blocks.png", 3, 14);
        var uvMin = UVPair.texCoords(Game.textures.blockTexture, fireUV);
        var uvMax = UVPair.texCoords(Game.textures.blockTexture, fireUV + 1);

        waterOverlayRenderer.begin(PrimitiveType.Triangles);

        waterOverlayRenderer.addVertex(new BlockVertexTinted(-1, -1, 0, uvMin.X, uvMax.Y, r, g, b, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(1, -1, 0, uvMax.X, uvMax.Y, r, g, b, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(-1, 1, 0, uvMin.X, uvMin.Y, r, g, b, (byte)(alpha * 255)));

        waterOverlayRenderer.addVertex(new BlockVertexTinted(-1, 1, 0, uvMin.X, uvMin.Y, r, g, b, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(1, -1, 0, uvMax.X, uvMax.Y, r, g, b, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(1, 1, 0, uvMax.X, uvMin.Y, r, g, b, (byte)(alpha * 255)));

        waterOverlayRenderer.end();
    }

    public void update(double dt) {
        prevLower = lower;

        // update recoil animation
        prevRecoilProgress = recoilProgress;
        if (player.recoilTime > 0) {
            recoilProgress = (float)player.recoilTime / 12f;
        }
        else {
            recoilProgress = 0;
        }

        // if the player has the same item, raise, else lower
        double target;

        var d = dt * 5;
        if (handSlot == player.inventory.selected && handItem == player.inventory.getSelected()) {
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
            handSlot = player.inventory.selected;
            handItem = player.inventory.getSelected();
        }
    }
}