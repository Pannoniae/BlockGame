using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.main;
using Silk.NET.Maths;

namespace BlockGame.world.block;

#pragma warning disable CS8618
/**
 * Stores UV in normalised coordinates (0.0 to 1.0)
 */
[StructLayout(LayoutKind.Auto)]
public readonly record struct UVPair(float u, float v) {
    public const int ATLASSIZE = 16;

    public readonly float u = u;
    public readonly float v = v;

    public UVPair(float u) : this(u, u) {
    }

    public static UVPair operator +(UVPair uv, float q) {
        return new UVPair(uv.u + q, uv.v + q);
    }

    public static UVPair operator -(UVPair uv, float q) {
        return new UVPair(uv.u - q, uv.v - q);
    }

    public static UVPair operator +(UVPair uv, UVPair other) {
        return new UVPair(uv.u + other.u, uv.v + other.v);
    }

    /**
     * 0 = 0, 65535 = 1
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<Half> texCoordsH(int x, int y) {
        return new Vector2D<Half>((Half)(x * Block.atlasRatio.X), (Half)(y * Block.atlasRatio.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<Half> texCoordsH(UVPair uv) {
        return new Vector2D<Half>((Half)(uv.u * Block.atlasRatio.X), (Half)(uv.v * Block.atlasRatio.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 texCoords(float x, float y) {
        return new Vector2(x * Block.atlasRatio.X, y * Block.atlasRatio.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 texCoords(UVPair uv) {
        return new Vector2(uv.u * Block.atlasRatio.X, uv.v * Block.atlasRatio.Y);
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
        return new Vector2(uv.u * tex.atlasRatio.X, uv.v * tex.atlasRatio.Y);
    }

    public static Vector2 texCoordsiI(UVPair uv) {
        var tex = Game.textures.itemTexture;
        return new Vector2(uv.u * tex.atlasSize, uv.v * tex.atlasSize);
    }

    public static Vector2 texCoords(BTexture2D tex, UVPair uv) {
        return new Vector2(uv.u / tex.width, uv.v / tex.height);
    }

    public static Vector2 texCoords(BTextureAtlas tex, UVPair uv) {
        return new Vector2(uv.u * tex.atlasRatio.X, uv.v * tex.atlasRatio.Y);
    }
}