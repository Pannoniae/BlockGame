using System.Numerics;

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
    public static Block GRAVEL = register(2, new Block(Block.cubeUVs(3, 0)));
    public static Block BASALT = register(3, new Block(Block.cubeUVs(4, 0)));
}

public class Block {
    public static readonly int atlasSize = 256;

    public UVPair[] uvs = new UVPair[6];

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

    public Block(UVPair[] uvs) {
        for (int i = 0; i < 6; i++) {
            this.uvs[i] = uvs[i];
        }
    }
}

public readonly record struct UVPair(int u, int v);