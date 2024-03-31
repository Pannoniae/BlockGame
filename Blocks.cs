using System.Numerics;
using Silk.NET.Maths;

namespace BlockGame;

public class Blocks {
    public static Dictionary<int, Block> blocks = new();

    public static Block register(int id, Block block) {
        return blocks[id] = block;
    }

    public static Block get(int id) {
        return blocks[id];
    }

    public static Block GRASS = register(1, new Block(Block.grassUVs(0, 0, 1, 0, 2, 0)));
    public static Block DIRT = register(2, new Block(Block.cubeUVs(2, 0)));
    public static Block GRAVEL = register(3, new Block(Block.cubeUVs(3, 0)));
    public static Block BASALT = register(4, new Block(Block.cubeUVs(4, 0)));
    public static Block STONE = register(5, new Block(Block.cubeUVs(5, 0)));

    public static Block GLASS = register(6, new Block(Block.cubeUVs(7, 0))
        .transparency()
        );

    public static Block WATER = register(7, new Block(Block.cubeUVs(6, 0))
        .translucency()
        .noCollision()
        .noSelection());

    public static bool isSolid(int block) {
        return block != 0 && !get(block).transparent && !get(block).translucent;
    }

    public static bool isTransparent(int block) {
        return block != 0 && get(block).transparent;
    }

    public static bool isTranslucent(int block) {
        return block != 0 && get(block).translucent;
    }

    public static bool hasCollision(int block) {
        return block != 0 && get(block).collision;
    }
}

public class Block {
    public static readonly int atlasSize = 256;

    public UVPair[] uvs = new UVPair[6];
    public AABB? aabb;
    /// <summary>
    /// Is fully transparent? (glass, leaves, etc.)
    /// </summary>
    public bool transparent = false;
    /// <summary>
    /// Is translucent? (partially transparent blocks like water)
    /// </summary>
    public bool translucent = false;
    public bool collision = true;
    public bool selection = true;

    /// <summary>
    /// 0 = 0, 65535 = 1
    /// </summary>
    public static Vector2D<ushort> texCoords(int x, int y) {
        return new Vector2D<ushort>((ushort)(x * 16f / atlasSize * ushort.MaxValue), (ushort)(y * 16f / atlasSize * ushort.MaxValue));
    }

    public static Vector2D<ushort> texCoords(UVPair uv) {
        return new Vector2D<ushort>((ushort)(uv.u * 16f / atlasSize * ushort.MaxValue), (ushort)(uv.v * 16f / atlasSize * ushort.MaxValue));
    }

    public static UVPair[] cubeUVs(int x, int y) {
        return [new(x, y), new(x, y), new(x, y), new(x, y), new(x, y), new(x, y)];
    }

    public static UVPair[] grassUVs(int topX, int topY, int sideX, int sideY, int bottomX, int bottomY) {
        return [
            new(sideX, sideY), new(sideX, sideY), new(sideX, sideY), new(sideX, sideY), new(bottomX, bottomY),
            new(topX, topY)
        ];
    }

    public static AABB fullBlock() {
        return new AABB(new Vector3D<double>(0, 0, 0), new Vector3D<double>(1, 1, 1));
    }

    public Block(UVPair[] uvs, AABB? aabb = null) {
        for (int i = 0; i < 6; i++) {
            this.uvs[i] = uvs[i];
        }

        this.aabb = aabb ?? fullBlock();
    }

    public Block transparency() {
        transparent = true;
        return this;
    }

    public Block translucency() {
        translucent = true;
        return this;
    }

    public Block noCollision() {
        collision = false;
        aabb = null;
        return this;
    }

    public Block noSelection() {
        selection = false;
        return this;
    }
}

public readonly record struct UVPair(int u, int v);