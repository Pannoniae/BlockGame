using System.Numerics;
using BlockGame.GL.vertexformats;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame.GL;

public class Graphics {
    public readonly SpriteBatch mainBatch;
    public readonly SpriteBatch immediateBatch;
    
    public readonly InstantShader instantShader = new InstantShader(Game.GL, "shaders/batch.vert", "shaders/batch.frag");

    public readonly Silk.NET.OpenGL.GL GL;

    private int[] viewportParams = new int[4]; // x, y, width, height

    public Graphics() {
        GL = Game.GL;
        mainBatch = new SpriteBatch(GL, instantShader);
        immediateBatch = new SpriteBatch(GL, instantShader);
    }

    public void clearColor(Color4b color) {
        GL.ClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    public void saveViewport() {
        GL.GetInteger(GLEnum.Viewport, viewportParams);
    }
    
    public void restoreViewport() {
        GL.Viewport(viewportParams[0], viewportParams[1], (uint)viewportParams[2], (uint)viewportParams[3]);
    }
    
    public void resize(Vector2D<int> size) {
        GL.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        var ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);
        
        instantShader.World = Matrix4x4.Identity;
        instantShader.View = Matrix4x4.Identity;
        instantShader.Projection = ortho;
        
    }
}