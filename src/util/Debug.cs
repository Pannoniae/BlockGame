using BlockGame.ui;
using Molten.DoublePrecision;
using TrippyGL;

namespace BlockGame.util;

public class Debug {
    private const int MAX_LINE_VERTICES = 512;
    private const int MAX_POINT_VERTICES = 512;

    private readonly VertexColor[] lineVertices = new VertexColor[MAX_LINE_VERTICES];
    private readonly VertexBuffer<VertexColor> lineVertexBuffer = new(Game.GD, MAX_LINE_VERTICES, BufferUsage.StreamDraw);
    private int currentLine = 0;
    private VertexColor[] pointVertices = new VertexColor[MAX_POINT_VERTICES];
    private VertexBuffer<VertexColor> pointVertexBuffer = new(Game.GD, MAX_LINE_VERTICES, BufferUsage.StreamDraw);
    private int currentPoint = 0;

    private SimpleShaderProgram debugShader = SimpleShaderProgram.Create<VertexColor>(Game.GD, excludeWorldMatrix: true);

    public void renderTick(double interp) {
        // nothing to do, so why set the shader?
        //if (currentLine == 0) {
        //    return;
        //}
        // don't forget to use the program before setting uniforms.
        //Game.GD.ResetShaderProgramStates();
        Game.GL.UseProgram(debugShader.Handle);
        debugShader.Projection = Screen.GAME_SCREEN.world.player.camera.getProjectionMatrix();
        debugShader.View = Screen.GAME_SCREEN.world.player.camera.getViewMatrix(interp);
    }

    public void drawLine(Vector3D from, Vector3D to, Color4b colour = default) {
        if (colour == default) {
            colour = Color4b.Red;
        }
        if (currentLine >= MAX_LINE_VERTICES - 2) {
            flushLines();
        }
        //Console.Out.WriteLine(currentLine);
        lineVertices[currentLine] = new VertexColor(from.toVec3(), colour);
        currentLine++;
        lineVertices[currentLine] = new VertexColor(to.toVec3(), colour);
        currentLine++;
    }

    public void drawAABB(AABB aabb, Color4b colour = default) {
        // corners
        var lsw = aabb.min;
        var lse = new Vector3D(aabb.maxX, aabb.minY, aabb.minZ);
        var lnw = new Vector3D(aabb.minX, aabb.minY, aabb.maxZ);
        var lne = new Vector3D(aabb.maxX, aabb.minY, aabb.maxZ);

        var usw = new Vector3D(aabb.minX, aabb.maxY, aabb.minZ);
        var use = new Vector3D(aabb.maxX, aabb.maxY, aabb.minZ);
        var unw = new Vector3D(aabb.minX, aabb.maxY, aabb.maxZ);
        var une = new Vector3D(aabb.maxX, aabb.maxY, aabb.maxZ);

        // join them with lines
        drawLine(lsw, lse);
        drawLine(lse, lne);
        drawLine(lne, lnw);
        drawLine(lnw, lsw);

        drawLine(usw, use);
        drawLine(use, une);
        drawLine(une, unw);
        drawLine(unw, usw);

        // draw columns
        drawLine(lsw, usw);
        drawLine(lse, use);
        drawLine(lne, une);
        drawLine(lnw, unw);
    }

    public void flushLines() {
        // nothing to do
        if (currentLine == 0) {
            return;
        }
        var GD = Game.GD;
        lineVertexBuffer.DataSubset.SetData(lineVertices.AsSpan(0, currentLine));
        GD.VertexArray = lineVertexBuffer;
        GD.ShaderProgram = debugShader;
        GD.DrawArrays(PrimitiveType.Lines, 0, (uint)currentLine);
        currentLine = 0;
    }
}