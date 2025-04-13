using BlockGame.ui;
using Molten.DoublePrecision;
using Silk.NET.OpenGL;
using System.Numerics;
using BlockGame.GL;
using Color4b = BlockGame.GL.vertexformats.Color4b;
using Shader = BlockGame.GL.Shader;


namespace BlockGame.util;

public class Debug {
    private const int MAX_LINE_VERTICES = 512;
    private const int MAX_POINT_VERTICES = 512;

    private readonly VertexTinted[] lineVertices = new VertexTinted[MAX_LINE_VERTICES];
    // Replace TrippyGL VertexBuffer with standard GL buffers
    private uint lineVao;
    private uint lineVbo;
    private int currentLine = 0;
    private VertexTinted[] pointVertices = new VertexTinted[MAX_POINT_VERTICES];
    private uint pointVao;
    private uint pointVbo;
    private int currentPoint = 0;

    // Replace SimpleShaderProgram with standard shader program
    private InstantShader debugShader;
    
    public Debug() {
        unsafe {
            // Create and setup VAO/VBO for lines
            lineVao = Game.GL.CreateVertexArray();
            lineVbo = Game.GL.CreateBuffer();
            Game.GL.BindVertexArray(lineVao);
            Game.GL.BindBuffer(BufferTargetARB.ArrayBuffer, lineVbo);
            Game.GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(MAX_LINE_VERTICES * sizeof(VertexTinted)),
                (void*)0, BufferUsageARB.StreamDraw);

            // Set up vertex attributes (position and color)
            Game.GL.EnableVertexAttribArray(0);
            Game.GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(VertexTinted),
                (void*)0);
            Game.GL.EnableVertexAttribArray(1);
            Game.GL.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, (uint)sizeof(VertexTinted),
                (void*)12); // Offset to color (3 floats = 12 bytes)

            // Same for points
            pointVao = Game.GL.CreateVertexArray();
            pointVbo = Game.GL.CreateBuffer();
            Game.GL.BindVertexArray(pointVao);
            Game.GL.BindBuffer(BufferTargetARB.ArrayBuffer, pointVbo);
            Game.GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(MAX_POINT_VERTICES * sizeof(VertexTinted)),
                IntPtr.Zero, BufferUsageARB.StreamDraw);
            Game.GL.EnableVertexAttribArray(0);
            Game.GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(VertexTinted),
                (void*)0);
            Game.GL.EnableVertexAttribArray(1);
            Game.GL.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, (uint)sizeof(VertexTinted),
                (void*)12);

            // Create debug shader program (assumes the shaders are the same as the SimpleShaderProgram)
            debugShader = new InstantShader(Game.GL, "shaders/debug.vert", "shaders/debug.frag");
        }
    }
    
    public void renderTick(double interp) {
        debugShader.use();
        
        // Set projection and view uniforms
        Matrix4x4 projMatrix = Screen.GAME_SCREEN.world.player.camera.getProjectionMatrix();
        Matrix4x4 viewMatrix = Screen.GAME_SCREEN.world.player.camera.getViewMatrix(interp);
        
        debugShader.setProjection(projMatrix);
        debugShader.setView(viewMatrix);
    }

    public void drawLine(Vector3D from, Vector3D to, Color4b colour = default) {
        if (colour == default) {
            colour = Color4b.Red;
        }
        if (currentLine >= MAX_LINE_VERTICES - 2) {
            flushLines();
        }
        lineVertices[currentLine] = new VertexTinted(from.toVec3(), colour);
        currentLine++;
        lineVertices[currentLine] = new VertexTinted(to.toVec3(), colour);
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

        Game.GL.BindVertexArray(lineVao);
        Game.GL.BindBuffer(BufferTargetARB.ArrayBuffer, lineVbo);
        unsafe {
            Game.GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0,
                (nuint)(currentLine * sizeof(VertexTinted)), lineVertices);
        }

        debugShader.use();
        Game.GL.DrawArrays(PrimitiveType.Lines, 0, (uint)currentLine);
        
        currentLine = 0;
    }
}