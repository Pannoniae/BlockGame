using System.Numerics;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using TrippyGL;
using TrippyGL.ImageSharp;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace BlockGame;

public class GUI {

    public Matrix4x4 ortho;

    public GL GL;

    public FloatVAO crosshair;

    public Shader guiShader;
    private SimpleShaderProgram shader;
    public int projection;
    public int uColor;

    public bool debugScreen;

    public TextureBatcher tb;
    public Texture2D guiTexture;

    public const int crosshairSize = 10;
    public const int crosshairThickness = 2;

    public GUI() {
        GL = Game.instance.GL;
        crosshair = new FloatVAO();
        guiShader = new Shader(Game.instance.GL,"gui.vert", "gui.frag");
        projection = guiShader.getUniformLocation("projection");
        uColor = guiShader.getUniformLocation("uColor");
        guiTexture = Texture2DExtensions.FromFile(Game.instance.GD, "gui.png");

        tb = new TextureBatcher(Game.instance.GD);
        shader = SimpleShaderProgram.Create<VertexColorTexture>(Game.instance.GD);
        tb.SetShaderProgram(shader);
        resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
    }

    public void draw() {
        crosshair.bind();
        guiShader.use();
        guiShader.setUniform(projection, ortho);
        guiShader.setUniform(uColor, new Vector4(0.1f, 0.1f, 0.1f, 0.1f));
        crosshair.render();
        Game.instance.GD.ResetVertexArrayStates();
        Game.instance.GD.ResetShaderProgramStates();
        Game.instance.GD.ResetTextureStates();
        Game.instance.GD.ResetBufferStates();
        Game.instance.GD.ResetBlendStates();
        Game.instance.GD.ShaderProgram = shader;
        tb.Begin(BatcherBeginMode.Immediate);
        tb.Draw(guiTexture, new Vector2(Game.instance.centreX, Game.instance.centreY), Color4b.Red);
        tb.Draw(guiTexture, new Vector2(0, 0), Color4b.Red);
        tb.End();
    }

    public void drawCrosshair() {
        var centreX = Game.instance.centreX;
        var centreY = Game.instance.centreY;

        float[] verts = [
            // vertical
            centreX - crosshairThickness, centreY - crosshairSize, 0f,
            centreX - crosshairThickness, centreY + crosshairSize, 0f,
            centreX + crosshairThickness, centreY + crosshairSize, 0f,
            centreX + crosshairThickness, centreY + crosshairSize, 0f,
            centreX + crosshairThickness, centreY - crosshairSize, 0f,
            centreX - crosshairThickness, centreY - crosshairSize, 0f,

            // horizontal
            centreX - crosshairSize, centreY - crosshairThickness, 0f,
            centreX - crosshairSize, centreY + crosshairThickness, 0f,
            centreX + crosshairSize, centreY + crosshairThickness, 0f,
            centreX + crosshairSize, centreY + crosshairThickness, 0f,
            centreX + crosshairSize, centreY - crosshairThickness, 0f,
            centreX - crosshairSize, centreY - crosshairThickness, 0f,
        ];
        crosshair.bind();
        crosshair.upload(verts);
        crosshair.format();
    }

    public void resize(Vector2D<int> size) {
        Game.instance.GD.SetViewport(0, 0, (uint)size.X, (uint)size.Y);
        ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);
        shader.Projection = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, 0f, 1f);
        //drawCrosshair();
    }

    public void imGuiDraw() {
        var i = Game.instance;
        ImGui.Text($"{i.camera.position.X}, {i.camera.position.Y}, {i.camera.position.Z}");
        ImGui.Text(i.targetedPos.HasValue
            ? $"{i.targetedPos.Value.X}, {i.targetedPos.Value.Y}, {i.targetedPos.Value.Z} {i.previousPos.Value.X}, {i.previousPos.Value.Y}, {i.previousPos.Value.Z}"
            : "No target");
        ImGui.Text("FPS: " + i.fps);
        ImGui.Text($"W:{i.width} H:{i.height}");
        ImGui.Text($"CX:{i.centreX} CY:{i.centreY}");
    }
}