using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class GUI {

    public Matrix4x4 ortho;

    public GL GL;

    public FloatVAO crosshair;

    public Shader guiShader;
    public int projection;
    public int uColor;

    public const int crosshairSize = 10;
    public const int crosshairThickness = 2;

    public GUI() {
        GL = Game.instance.GL;
        crosshair = new FloatVAO();
        guiShader = new Shader(Game.instance.GL,"gui.vert", "gui.frag");
        projection = guiShader.getUniformLocation("projection");
        uColor = guiShader.getUniformLocation("uColor");

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

        crosshair.upload(verts);
        crosshair.format();
        resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
    }

    public void draw() {
        crosshair.bind();
        guiShader.use();
        guiShader.setUniform(projection, ortho);
        guiShader.setUniform(uColor, new Vector4(0.1f, 0.1f, 0.1f, 0.1f));
        crosshair.render();
    }

    public void resize(Vector2D<int> size) {
        ortho = Matrix4x4.CreateOrthographic(size.X, size.Y, -1f, 1f);
    }
}