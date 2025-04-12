using System.Numerics;
using System.Runtime.InteropServices;

namespace BlockGame.GL.vertexformats;

/// <summary>
/// A vertex with position, color, and texture coordinates
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct BVertexColorTexture
{
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
    public BVertexColorTexture(Vector3 position, Color4b color, Vector2 texCoords)
    {
        Position = position;
        Color = color;
        TexCoords = texCoords;
    }
}

/// <summary>
/// A color with 4 byte components (RGBA)
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct Color4b : IEquatable<Color4b>
{
    /// <summary>The red component</summary>
    public byte R;
        
    /// <summary>The green component</summary>
    public byte G;
        
    /// <summary>The blue component</summary>
    public byte B;
        
    /// <summary>The alpha component</summary>
    public byte A;

    /// <summary>
    /// Creates a new Color4b from individual components
    /// </summary>
    public Color4b(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>
    /// Creates a new Color4b from individual components with default alpha (255)
    /// </summary>
    public Color4b(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
        A = 255;
    }
        
    /// <summary>Predefined white color</summary>
    public static readonly Color4b White = new Color4b(255, 255, 255, 255);
        
    /// <summary>Predefined black color</summary>
    public static readonly Color4b Black = new Color4b(0, 0, 0, 255);
        
    /// <summary>Predefined red color</summary>
    public static readonly Color4b Red = new Color4b(255, 0, 0, 255);
        
    /// <summary>Predefined green color</summary>
    public static readonly Color4b Green = new Color4b(0, 255, 0, 255);
        
    /// <summary>Predefined blue color</summary>
    public static readonly Color4b Blue = new Color4b(0, 0, 255, 255);
        
    /// <summary>Predefined transparent color</summary>
    public static readonly Color4b Transparent = new Color4b(0, 0, 0, 0);

    /// <summary>
    /// Compares this Color4b to another for equality
    /// </summary>
    public bool Equals(Color4b other)
    {
        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    /// <summary>
    /// Compares this Color4b to another object for equality
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is Color4b other && Equals(other);
    }

    /// <summary>
    /// Gets a hash code for this Color4b
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    /// <summary>
    /// Converts this Color4b to a string representation
    /// </summary>
    public override string ToString()
    {
        return $"RGBA({R}, {G}, {B}, {A})";
    }
}