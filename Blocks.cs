using Silk.NET.Maths;

namespace BlockGame;

public class Blocks {
    public static Dictionary<int, Block> blocks = new();

    public static Block register(Block block) {
        return blocks[block.id] = block;
    }

    public static Block get(int id) {
        return blocks[id];
    }

    public static bool tryGet(int id, out Block block) {
        var cond = blocks.TryGetValue(id, out var i);
        block = cond ? i : blocks[1];
        return cond;
    }

    public static Block GRASS = register(new Block(1, "Grass", Block.grassUVs(0, 0, 1, 0, 2, 0)));
    public static Block DIRT = register(new Block(2, "Dirt", Block.cubeUVs(2, 0)));
    public static Block GRAVEL = register(new Block(3, "Gravel", Block.cubeUVs(3, 0)));
    public static Block BASALT = register(new Block(4, "Basalt", Block.cubeUVs(4, 0)));
    public static Block STONE = register(new Block(5, "Stone", Block.cubeUVs(5, 0)));

    public static Block GLASS = register(new Block(6, "Glass", Block.cubeUVs(6, 0))
        .transparency()
    );

    public static Block WATER = register(new Block(7, "Water", Block.cubeUVs(7, 0))
        .translucency()
        .noCollision()
        .noSelection());

    public static Block ICE = register(new Block(8, "Ice", Block.cubeUVs(8, 0))
        .translucency()
        .noCollision()
        .noSelection());


    public static Block LOG = register(new Block(9, "Wooden Log", Block.grassUVs(10, 0, 9, 0, 11, 0)));
    public static Block LEAVES = register(new Block(10, "Leaves", Block.cubeUVs(12, 0)).transparency());

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
    /// <summary>
    /// Block ID
    /// </summary>
    public ushort id;

    /// <summary>
    /// Display name
    /// </summary>
    public string name = "";

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

    public static readonly int atlasSize = 256;

    public UVPair[] uvs = new UVPair[6];
    public AABB? aabb;

    /// <summary>
    /// 0 = 0, 65535 = 1
    /// </summary>
    public static Vector2D<float> texCoords(int x, int y) {
        return new Vector2D<float>(x * 16f / atlasSize, y * 16f / atlasSize);
    }

    public static Vector2D<float> texCoords(UVPair uv) {
        return new Vector2D<float>(uv.u * 16f / atlasSize, uv.v * 16f / atlasSize);
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

    // this will pack the data into the uint
    public static uint packData(byte direction) {
        return direction;
    }

    public static AABB fullBlock() {
        return new AABB(new Vector3D<double>(0, 0, 0), new Vector3D<double>(1, 1, 1));
    }

    public Block(ushort id, string name, UVPair[] uvs, AABB? aabb = null) {
        this.id = id;
        this.name = name;
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