using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.main;
using Silk.NET.Maths;

namespace BlockGame.world.block;

#pragma warning disable CS8618
/// <summary>
/// Stores UV in block coordinates (1 = 16px)
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct UVPair(float u, float v) {
    public const int ATLASSIZE = 16;

    public readonly float u = u;
    public readonly float v = v;

    public static UVPair operator +(UVPair uv, float q) {
        return new UVPair(uv.u + q, uv.v + q);
    }

    public static UVPair operator -(UVPair uv, float q) {
        return new UVPair(uv.u - q, uv.v - q);
    }

    public static UVPair operator +(UVPair uv, UVPair other) {
        return new UVPair(uv.u + other.u, uv.v + other.v);
    }

    /// <summary>
    /// 0 = 0, 65535 = 1
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<Half> texCoordsH(int x, int y) {
        return new Vector2D<Half>((Half)(x * Block.atlasRatio), (Half)(y * Block.atlasRatio));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<Half> texCoordsH(UVPair uv) {
        return new Vector2D<Half>((Half)(uv.u * Block.atlasRatio), (Half)(uv.v * Block.atlasRatio));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 texCoords(float x, float y) {
        return new Vector2(x * Block.atlasRatio, y * Block.atlasRatio);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 texCoords(UVPair uv) {
        return new Vector2(uv.u * Block.atlasRatio, uv.v * Block.atlasRatio);
    }

    public static Vector2 texCoords(BTexture2D tex, float x, float y) {
        return new Vector2(x / tex.width, y / tex.height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 texCoordsI(UVPair uv) {
        return new Vector2(uv.u * ATLASSIZE, uv.v * ATLASSIZE);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 texCoordsi(UVPair uv) {
        var tex = Game.textures.itemTexture;
        return new Vector2(uv.u * tex.atlasRatio, uv.v * tex.atlasRatio);
    }

    public static Vector2 texCoordsiI(UVPair uv) {
        var tex = Game.textures.itemTexture;
        return new Vector2(uv.u * tex.atlasSize, uv.v * tex.atlasSize);
    }

    public static Vector2 texCoords(BTexture2D tex, UVPair uv) {
        return new Vector2(uv.u / tex.width, uv.v / tex.height);
    }

    public static Vector2 texCoords(BTextureAtlas tex, UVPair uv) {
        return new Vector2(uv.u * tex.atlasRatio, uv.v * tex.atlasRatio);
    }
}