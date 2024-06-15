using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace BlockGame.util;

public class Blocks {

    private const int MAXBLOCKS = 128;
    public static Block[] blocks = new Block[MAXBLOCKS];

    /// <summary>
    /// Stores whether the block is a full block or not.
    /// </summary>
    public static bool[] fullBlockCache = new bool[MAXBLOCKS];

    public static readonly int maxBlock = 31;

    public static Block register(Block block) {
        return blocks[block.id] = block;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Block get(int id) {
        return blocks[id];
    }

    public static bool tryGet(int id, out Block block) {
        var cond = id is >= 0 and < MAXBLOCKS;
        block = cond ? blocks[id] : blocks[1];
        return cond;
    }

    public static void postLoad() {
        for (int i = 0; i < maxBlock; i++) {
            fullBlockCache[blocks[i].id] = blocks[i].isFullBlock;
        }
    }


    public static bool isFullBlock(int id) {
        return fullBlockCache[id];
    }

    public static Block AIR = register(new Block(0, "Air", BlockModel.emptyBlock()).air());
    public static Block GRASS = register(new Block(1, "Grass", BlockModel.makeCube(Block.grassUVs(0, 0, 1, 0, 2, 0))));
    public static Block DIRT = register(new Block(2, "Dirt", BlockModel.makeCube(Block.cubeUVs(2, 0))));
    public static Block SAND = register(new FallingBlock(3, "Sand", BlockModel.makeCube(Block.cubeUVs(3, 0))));
    public static Block BASALT = register(new Block(4, "Basalt", BlockModel.makeCube(Block.cubeUVs(4, 0))));
    public static Block STONE = register(new Block(5, "Stone", BlockModel.makeCube(Block.cubeUVs(5, 0))));

    public static Block GLASS = register(new Block(6, "Glass", BlockModel.makeCube(Block.cubeUVs(6, 0)))
        .transparency()
    );

    public static Block WATER = register(new Water(7, "Water", BlockModel.makeLiquid(Block.cubeUVs(7, 0)))
        .makeLiquid());

    public static Block WOODEN_PLANKS = register(new Block(8, "Wooden Planks", BlockModel.makeCube(Block.cubeUVs(8, 0))));

    public static Block WOODEN_STAIRS = register(new Block(9, "Wooden Stairs", BlockModel.makeStairs(Block.cubeUVs(8, 0))).partialBlock());

    public static Block LOG = register(new Block(10, "Wooden Log", BlockModel.makeCube(Block.grassUVs(10, 0, 9, 0, 11, 0))));
    public static Block LEAVES = register(new Block(11, "Leaves", BlockModel.makeCube(Block.cubeUVs(12, 0))).transparency());

    public static Block YELLOW_FLOWER = register(new Flower(12, "Yellow Flower", BlockModel.makeGrass(Block.crossUVs(13, 0)))).transparency().flowerAABB().noCollision();
    public static Block RED_FLOWER = register(new Flower(13, "Red Flower", BlockModel.makeGrass(Block.crossUVs(14, 0)))).transparency().flowerAABB().noCollision();

    public static Block LANTERN = register(new Block(14, "Lantern", BlockModel.makeCube(Block.grassUVs(15, 1, 13, 1, 14, 1))).light(15));
    public static Block METAL_CUBE_BLUE = register(new Block(15, "Blue Metalish Block", BlockModel.makeCube(Block.cubeUVs(0, 1))));
    public static Block CANDY_LIGHT_BLUE = register(new Block(16, "Light Blue Candy", BlockModel.makeCube(Block.cubeUVs(0, 2))));
    public static Block CANDY_CYAN = register(new Block(17, "Cyan Candy", BlockModel.makeCube(Block.cubeUVs(1, 2))));
    public static Block CANDY_TURQUOISE = register(new Block(18, "Turquoise Candy", BlockModel.makeCube(Block.cubeUVs(2, 2))));
    public static Block CANDY_DARK_GREEN = register(new Block(19, "Dark Green Candy", BlockModel.makeCube(Block.cubeUVs(3, 2))));
    public static Block CANDY_LIGHT_GREEN = register(new Block(20, "Light Green Candy", BlockModel.makeCube(Block.cubeUVs(4, 2))));
    public static Block CANDY_ORANGE = register(new Block(21, "Orange Candy", BlockModel.makeCube(Block.cubeUVs(5, 2))));
    public static Block CANDY_YELLOW = register(new Block(22, "YELLOW Candy", BlockModel.makeCube(Block.cubeUVs(6, 2))));
    public static Block CANDY_LIGHT_RED = register(new Block(23, "Ligth Red Candy", BlockModel.makeCube(Block.cubeUVs(7, 2))));
    public static Block CANDY_PINK = register(new Block(24, "Pink Candy", BlockModel.makeCube(Block.cubeUVs(8, 2))));
    public static Block CANDY_PURPLE = register(new Block(25, "Purple Candy", BlockModel.makeCube(Block.cubeUVs(9, 2))));
    public static Block VIOLET = register(new Block(26, "Violet Candy", BlockModel.makeCube(Block.cubeUVs(10, 2))));
    public static Block CANDY_RED = register(new Block(27, "Red Candy", BlockModel.makeCube(Block.cubeUVs(11, 2))));
    public static Block CANDY_DARK_BLUE = register(new Block(28, "Dark Blue Candy", BlockModel.makeCube(Block.cubeUVs(12, 2))));
    public static Block CANDY_WHITE = register(new Block(29, "White Candy", BlockModel.makeCube(Block.cubeUVs(13, 2))));
    public static Block CANDY_GREY = register(new Block(30, "Grey Candy", BlockModel.makeCube(Block.cubeUVs(14, 2))));
    public static Block CANDY_BLACK = register(new Block(31, "Black Candy", BlockModel.makeCube(Block.cubeUVs(15, 2))));


    public static bool isSolid(int block) {
        return block != 0 && get(block).type == BlockType.SOLID;
    }

    public static bool notSolid(int block) {
        return block == 0 || get(block).type != BlockType.SOLID;
    }

    public static bool isTransparent(int block) {
        return block != 0 && get(block).type == BlockType.TRANSPARENT;
    }

    public static bool isTranslucent(int block) {
        return block != 0 && get(block).type == BlockType.TRANSLUCENT;
    }

    public static bool hasCollision(int block) {
        return block != 0 && get(block).collision;
    }

    public static bool isSolid(Block block) {
        return block.id != 0 && block.type == BlockType.SOLID;
    }

    public static bool notSolid(Block block) {
        return block.id == 0 || block.type != BlockType.SOLID;
    }

    public static bool isTransparent(Block block) {
        return block.id != 0 && block.type == BlockType.TRANSPARENT;
    }

    public static bool isTranslucent(Block block) {
        return block.id != 0 && block.type == BlockType.TRANSLUCENT;
    }

    public static bool hasCollision(Block block) {
        return block.id != 0 && block.collision;
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

    [Obsolete("Use Blocks.isFullBlock() instead.")]
    public bool isFullBlock = true;

    /// <summary>
    /// Is fully transparent? (glass, leaves, etc.)
    /// Is translucent? (partially transparent blocks like water)
    /// </summary>
    public BlockType type = BlockType.SOLID;

    public bool collision = true;
    public AABB? aabb;

    public bool selection = true;
    public AABB? selectionAABB;

    /// <summary>
    /// Is this block a liquid?
    /// </summary>
    public bool liquid = false;

    /// <summary>
    /// How much light does this block emit? (0 for none.)
    /// </summary>
    public byte lightLevel = 0;

    /// <summary>
    /// If true, this block has a custom render method. (Used for dynamic blocks....)
    /// </summary>
    public bool customRender = false;

    public static readonly int atlasSize = 256;
    public BlockModel model;

    /// <summary>
    /// 0 = 0, 65535 = 1
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<Half> texCoordsH(int x, int y) {
        return new Vector2D<Half>((Half)(x * 16f / atlasSize), (Half)(y * 16f / atlasSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<Half> texCoordsH(UVPair uv) {
        return new Vector2D<Half>((Half)(uv.u * 16f / atlasSize), (Half)(uv.v * 16f / atlasSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<float> texCoords(float x, float y) {
        return new Vector2D<float>(x * 16f / atlasSize, y * 16f / atlasSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<float> texCoords(UVPair uv) {
        return new Vector2D<float>(uv.u * 16f / atlasSize, uv.v * 16f / atlasSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float texU(float u) {
        return u * 16f / atlasSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float texV(float v) {
        return v * 16f / atlasSize;
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

    public static UVPair[] crossUVs(int x, int y) {
        return [new(x, y), new(x, y)];
    }

    // this will pack the data into the uint
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort packData(byte direction, byte ao, byte light) {
        // if none, treat it as an up
        direction = (byte)(direction == 12 ? 5 : direction);
        return (ushort)(light << 8 | ao << 3 | direction);
    }

    public static AABB fullBlock() {
        return new AABB(new Vector3D<double>(0, 0, 0), new Vector3D<double>(1, 1, 1));
    }

    public Block flowerAABB() {
        var offset = 3 / 8f;
        selectionAABB = new AABB(new Vector3D<double>(0 + offset, 0, 0 + offset), new Vector3D<double>(1 - offset, 0.5, 1 - offset));
        return this;
    }

    public Block(ushort id, string name, BlockModel model) {
        this.id = id;
        this.name = name;
        this.model = model;

        aabb = fullBlock();
        selectionAABB = fullBlock();
    }

    public Block transparency() {
        type = BlockType.TRANSPARENT;
        isFullBlock = false;
        return this;
    }

    public Block translucency() {
        type = BlockType.TRANSLUCENT;
        isFullBlock = false;
        return this;
    }

    public Block noCollision() {
        collision = false;
        aabb = null;
        return this;
    }

    public Block noSelection() {
        selection = false;
        selectionAABB = null;
        return this;
    }

    public Block partialBlock() {
        isFullBlock = false;
        return this;
    }

    public Block makeLiquid() {
        translucency();
        noCollision();
        noSelection();
        liquid = true;
        isFullBlock = false;
        return this;
    }

    public Block setCustomRender() {
        customRender = true;
        return this;
    }

    public Block light(byte amount) {
        lightLevel = amount;
        return this;
    }


    public virtual void update(World world, Vector3D<int> pos) {

    }

    public virtual void render(World world, Vector3D<int> pos, List<BlockVertex> vertexBuffer, List<ushort> indexBuffer, ushort currentIndex) {

    }

    public Block air() {
        noCollision();
        noSelection();
        isFullBlock = false;
        return this;
    }
}

public class Flower(ushort id, string name, BlockModel uvs) : Block(id, name, uvs) {

    public override void update(World world, Vector3D<int> pos) {
        if (world.inWorld(pos.X, pos.Y - 1, pos.Z) && world.getBlock(pos.X, pos.Y - 1, pos.Z) == 0) {
            world.setBlockRemesh(pos.X, pos.Y, pos.Z, Blocks.AIR.id);
        }
    }
}

public class Water(ushort id, string name, BlockModel uvs) : Block(id, name, uvs) {

    public override void update(World world, Vector3D<int> pos) {
        foreach (var dir in Direction.directionsWaterSpread) {
            // queue block updates
            var neighbourBlock = pos + dir;
            if (world.getBlock(neighbourBlock) == Blocks.AIR.id) {
                world.runLater(neighbourBlock, () => {
                    if (world.getBlock(neighbourBlock) == Blocks.AIR.id) {
                        world.setBlockRemesh(neighbourBlock.X, neighbourBlock.Y, neighbourBlock.Z, Blocks.WATER.id);
                    }
                }, 10);
                world.blockUpdate(neighbourBlock, 10);
            }
        }
    }
}

public class FallingBlock(ushort id, string name, BlockModel uvs) : Block(id, name, uvs) {
    public override void update(World world, Vector3D<int> pos) {
        var y = pos.Y - 1;
        bool isSupported = true;
        // if not supported, set flag
        while (world.getBlock(new Vector3D<int>(pos.X, y, pos.Z)) == 0) {
            // decrement Y
            isSupported = false;
            y--;
        }
        if (!isSupported) {
            world.setBlockRemesh(pos.X, pos.Y, pos.Z, 0);
            world.setBlockRemesh(pos.X, y + 1, pos.Z, id);
        }

        // if sand above, update
        if (world.getBlock(new Vector3D<int>(pos.X, pos.Y + 1, pos.Z)) == id) {
            world.blockUpdate(new Vector3D<int>(pos.X, pos.Y + 1, pos.Z));
        }
    }
}

/// <summary>
/// Stores UV in block coordinates (1 = 16px)
/// </summary>
public readonly record struct UVPair(float u, float v) {

    public readonly float u = u;
    public readonly float v = v;

    public static UVPair operator +(UVPair uv, float q) {
        return new UVPair(uv.u + q, uv.v + q);
    }

    public static UVPair operator +(UVPair uv, UVPair other) {
        return new UVPair(uv.u + other.u, uv.v + other.v);
    }
}

/// <summary>
/// Represents a block face. If noAO, don't let AO cast on this face.
/// If it's not a full face, it's always drawn to ensure it's drawn even when there's a solid block next to it.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Face(
    float x1, float y1, float z1,
    float x2, float y2, float z2,
    float x3, float y3, float z3,
    float x4, float y4, float z4,
    UVPair min, UVPair max, RawDirection direction, bool noAO = false, bool nonFullFace = false) {

    public const int MAX_FACES = 10;

    public readonly float x1 = x1;
    public readonly float y1 = y1;
    public readonly float z1 = z1;
    public readonly float x2 = x2;
    public readonly float y2 = y2;
    public readonly float z2 = z2;
    public readonly float x3 = x3;
    public readonly float y3 = y3;
    public readonly float z3 = z3;
    public readonly float x4 = x4;
    public readonly float y4 = y4;
    public readonly float z4 = z4;
    public readonly UVPair min = min;
    public readonly UVPair max = max;
    public readonly RawDirection direction = direction;
    public readonly byte flags = (byte)(ChunkSectionRenderer.toByte(nonFullFace) | ChunkSectionRenderer.toByte(noAO) << 1);

    public bool nonFullFace => (flags & (byte)FaceFlags.NON_FULL_FACE) != 0;
    public bool noAO => (flags & (byte)FaceFlags.NO_AO) != 0;
}

[Flags]
public enum FaceFlags : byte {
    NON_FULL_FACE = 1,
    NO_AO = 2
}

/// <summary>
/// Defines the render type / layer of a block.
/// </summary>
public enum BlockType : byte {
    SOLID,
    TRANSPARENT,
    TRANSLUCENT
}