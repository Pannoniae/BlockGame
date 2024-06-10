using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.util;
using Silk.NET.OpenGL;

namespace BlockGame;

public class PlayerRenderer {

    public Player player;
    public StreamingVAO vao;
    private List<BlockVertex> vertices = new();
    private List<ushort> indices = new();

    private int uMVP;
    private int blockTexture;

    public Shader heldBlockShader;

    public PlayerRenderer(Player player) {
        this.player = player;
        vao = new StreamingVAO();
        vao.setSize(Face.MAX_FACES * 4);
        heldBlockShader = new Shader(Game.GL, "shaders/simpleBlock.vert", "shaders/simpleBlock.frag");
        uMVP = heldBlockShader.getUniformLocation("uMVP");
        blockTexture = heldBlockShader.getUniformLocation("blockTexture");
    }

    public void render(double dt, double interp) {
        WorldRenderer.meshBlock(Blocks.get(player.hotbar.getSelected().block), ref vertices, ref indices);
        vao.upload(CollectionsMarshal.AsSpan(vertices), CollectionsMarshal.AsSpan(indices));
        Game.GL.ActiveTexture(TextureUnit.Texture0);
        Game.GL.BindTexture(TextureTarget.Texture2D, Game.instance.blockTexture.Handle);
        var location = Matrix4x4.CreateTranslation(player.camera.position + player.camera.forward);
        heldBlockShader.use();
        heldBlockShader.setUniform(uMVP, location * player.camera.getViewMatrix(interp) * player.camera.getProjectionMatrix());
        heldBlockShader.setUniform(blockTexture, 0);
        vao.render();
    }
}