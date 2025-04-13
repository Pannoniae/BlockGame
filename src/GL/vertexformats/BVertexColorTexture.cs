using System.Numerics;
using System.Runtime.InteropServices;

namespace BlockGame.GL.vertexformats;

/// <summary>
/// A vertex with position, color, and texture coordinates
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct BVertexColorTexture {
    /// <summary>The position of the vertex</summary>
    public Vector3 Position;

    /// <summary>The color of the vertex</summary>
    public Color4b Color;

    /// <summary>The texture coordinates of the vertex</summary>
    public Vector2 TexCoords;

    /// <summary>
    /// Creates a new BVertexColorTexture
    /// </summary>
    /// <param name="position">The position of the vertex</param>
    /// <param name="color">The color of the vertex</param>
    /// <param name="texCoords">The texture coordinates of the vertex</param>
    public BVertexColorTexture(Vector3 position, Color4b color, Vector2 texCoords) {
        Position = position;
        Color = color;
        TexCoords = texCoords;
    }
}