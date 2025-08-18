using Silk.NET.OpenGL;

using BlockGame;

public class TestAnalyzer {
    public void TestMethod() {
        GL gl = Game.GL;
        unsafe {
            // this SHOULD error (if it doesn't, analyzer is broken)
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, (void*)0);
        }
    }
}