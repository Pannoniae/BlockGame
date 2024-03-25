using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class Debug {
    private const int MAX_LINE_VERTICES = 512;
    private const int MAX_POINT_VERTICES = 512;

    private VertexColor[] lineVertices = new VertexColor[MAX_LINE_VERTICES];
    private VertexBuffer<VertexColor> lineVertexBuffer = new(Game.instance.GD, MAX_LINE_VERTICES, BufferUsage.StreamDraw);
    private int currentLine = 0;
    private VertexColor[] pointVertices = new VertexColor[MAX_POINT_VERTICES];
    private VertexBuffer<VertexColor> pointVertexBuffer = new(Game.instance.GD, MAX_LINE_VERTICES, BufferUsage.StreamDraw);
    private int currentPoint = 0;

    private SimpleShaderProgram debugShader = SimpleShaderProgram.Create<VertexColor>(Game.instance.GD);

    public void update(double interp) {
        debugShader.Projection = GameScreen.world.player.camera.getProjectionMatrix();
        debugShader.View = GameScreen.world.player.camera.getViewMatrix(interp);
    }

    public void drawLine(Vector3D<double> from, Vector3D<double> to, Color4b colour) {
        lineVertices[currentLine] = new VertexColor(from.toVec3(), colour);
        currentLine++;
        lineVertices[currentLine] = new VertexColor(to.toVec3(), colour);
        currentLine++;
    }

    public void flushLines() {
        var GD = Game.instance.GD;
        lineVertexBuffer.DataSubset.SetData(lineVertices.AsSpan(0, currentLine));
        GD.VertexArray = lineVertexBuffer;
        GD.ShaderProgram = debugShader;
        GD.DrawArrays(PrimitiveType.Lines, 0, (uint)currentLine);
        currentLine = 0;
    }
}