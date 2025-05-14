using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.util;
using Silk.NET.OpenGL;
using Shader = BlockGame.GL.Shader;

namespace BlockGame;

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
        handItem = player.hotbar.getSelected();
        vao = new StreamingVAO<BlockVertexTinted>();
        vao.bind();
        vao.setSize(Face.MAX_FACES * 4);
        uMVP = Game.graphics.instantTextureShader.getUniformLocation("uMVP");
        tex = Game.graphics.instantTextureShader.getUniformLocation("tex");
        
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
        Game.GL.ActiveTexture(TextureUnit.Texture0);
        Game.GL.BindTexture(TextureTarget.Texture2D, Game.textureManager.blockTextureGUI.handle);
        var light = world.getLight(pos.X, pos.Y, pos.Z);
        WorldRenderer.meshBlockTinted(Block.get(handItem.block), ref vertices, ref indices, light);
        vao.bind();
        vao.upload(CollectionsMarshal.AsSpan(vertices), CollectionsMarshal.AsSpan(indices));

        var swingProgress = player.getSwingProgress(interp);
        // thx classicube? the description is bs with the matrices but it gives some ideas for the maths
        var sinSwing = Math.Sin(swingProgress * Math.PI);
        var sinSwingSqrt = Math.Sin(Math.Sqrt(swingProgress) * Math.PI);
        // we need something like a circle?
        var circleishThing = Math.Sin(Math.Sqrt(swingProgress) * Math.PI * 2);

        // rotate 45 degrees
        var mat = Matrix4x4.CreateRotationY(Meth.deg2rad(45), new Vector3(0.5f, 0.5f, 0.5f)) *
                  // swing block rotation
                  Matrix4x4.CreateRotationY((float)(-sinSwing * Meth.deg2rad(20)), new Vector3(0.5f, 0.5f, 0.5f)) *
                  Matrix4x4.CreateRotationZ((float)(sinSwingSqrt * Meth.deg2rad(20)), new Vector3(0.5f, 0.5f, 0.5f)) *
                  Matrix4x4.CreateRotationX((float)(sinSwingSqrt * Meth.deg2rad(20)), new Vector3(0.5f, 0.5f, 0.5f)) *
                  // scale down
                  Matrix4x4.CreateScale(0.6f, new Vector3(0.5f, 0.5f, 0.5f)) *
                  // translate into place
                  Matrix4x4.CreateTranslation(new Vector3(0.75f, (float)(-1.45f - (getLower(interp) * 0.35f)), 1f)) *
                  // swing translation
                  Matrix4x4.CreateTranslation((float)(sinSwingSqrt * -0.7f), (float)(circleishThing * 0.35f), (float)(sinSwing * 0.6f));
        Game.graphics.instantTextureShader.use();
        Game.graphics.instantTextureShader.setUniform(uMVP, mat * player.camera.getHandViewMatrix(interp) * player.camera.getFixedProjectionMatrix());
        Game.graphics.instantTextureShader.setUniform(tex, 0);
        vao.render();
        
        // Render water overlay if player is underwater
        if (player.isUnderWater()) {
            renderWaterOverlay();
        }
    }

    private void renderWaterOverlay() {
        // Set the water overlay texture
        waterOverlayRenderer.setTexture(Game.textureManager.waterOverlay.handle);
        
        // Set identity MVP matrix (screen space coordinates)
        var identityMVP = Matrix4x4.Identity;
        waterOverlayRenderer.setMVP(identityMVP);
        
        
        // Draw a full-screen quad with slightly blue tint
        float alpha = 0.5f;
        
        waterOverlayRenderer.begin(PrimitiveType.Triangles);
        
        waterOverlayRenderer.addVertex(new BlockVertexTinted(-1, -1, 0, 0f, 0f, 255, 255, 255, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(1, -1, 0, 1f, 0f, 255, 255, 255, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(-1, 1, 0, 0f, 1f, 255, 255, 255, (byte)(alpha * 255)));
        
        waterOverlayRenderer.addVertex(new BlockVertexTinted(-1, 1, 0, 0f, 1f, 255, 255, 255, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(1, -1, 0, 1f, 0f, 255, 255, 255, (byte)(alpha * 255)));
        waterOverlayRenderer.addVertex(new BlockVertexTinted(1, 1, 0, 1f, 1f, 255, 255, 255, (byte)(alpha * 255)));
        
        // Render the overlay
        waterOverlayRenderer.end();
    }

    public void update(double dt) {
        prevLower = lower;
        // if the player has the same item, raise, else lower
        double target;

        var d = dt * 10;
        if (handSlot == player.hotbar.selected && handItem == player.hotbar.getSelected()) {
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
            handSlot = player.hotbar.selected;
            handItem = player.hotbar.getSelected();
        }
    }
}

public class MatrixStack2 {
    private Stack<Matrix4x4> _stack = new();

    public MatrixStack2() {
        _stack.Push(Matrix4x4.Identity);
    }

    public Matrix4x4 Current => _stack.Peek();

    public void Push() => _stack.Push(Current);

    public void Pop() => _stack.Pop();

    public void Scale(float scale, Vector3 center) =>
        _stack.Push(Current * Matrix4x4.CreateScale(scale, center));

    public void RotateX(float angle, Vector3 center) =>
        _stack.Push(Current * Matrix4x4.CreateRotationX(angle, center));

    public void RotateX(float angle) =>
        _stack.Push(Current * Matrix4x4.CreateRotationX(angle));

    public void RotateY(float angle, Vector3 center) =>
        _stack.Push(Current * Matrix4x4.CreateRotationY(angle, center));

    public void RotateY(float angle) =>
        _stack.Push(Current * Matrix4x4.CreateRotationY(angle));

    public void RotateZ(float angle, Vector3 center) =>
        _stack.Push(Current * Matrix4x4.CreateRotationZ(angle, center));

    public void RotateZ(float angle) =>
        _stack.Push(Current * Matrix4x4.CreateRotationZ(angle));

    public void Translate(Vector3 translation) =>
        _stack.Push(Current * Matrix4x4.CreateTranslation(translation));
}