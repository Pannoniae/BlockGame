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
    public static Block GOLD = register(6, new Block(Block.cubeUVs(6, 0)));
}

public class Block {
    public static readonly int atlasSize = 256;

    public UVPair[] uvs = new UVPair[6];
    public AABB aabb;

    public static Vector2 texCoords(int x, int y) {
        return new Vector2(x * 16f / atlasSize, y * 16f / atlasSize);
    }

    public static Vector2 texCoords(UVPair uv) {
        return new Vector2(uv.u * 16f / atlasSize, uv.v * 16f / atlasSize);
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
}

public readonly record struct UVPair(int u, int v);