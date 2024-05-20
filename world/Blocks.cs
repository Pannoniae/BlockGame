using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.CompilerServices;
using Silk.NET.Maths;

namespace BlockGame;

public class Blocks {

    private const int MAXBLOCKS = 128;
    public static Block[] blocks = new Block[MAXBLOCKS];

    public static Block register(Block block) {
        return blocks[block.id] = block;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Block get(int id) {
        return blocks[id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool tryGet(int id, out Block block) {
        var cond = id is >= 0 and < MAXBLOCKS;
        block = cond ? blocks[id] : blocks[1];
        return cond;
    }

    public static Block AIR = register(new Block(0, "Air", BlockModel.makeCube(Block.cubeUVs(0, 0))));
    public static Block GRASS = register(new Block(1, "Grass", BlockModel.makeCube(Block.grassUVs(0, 0, 1, 0, 2, 0))));
    public static Block DIRT = register(new Block(2, "Dirt", BlockModel.makeCube(Block.cubeUVs(2, 0))));
    public static Block GRAVEL = register(new Block(3, "Gravel", BlockModel.makeCube(Block.cubeUVs(3, 0))));
    public static Block BASALT = register(new Block(4, "Basalt", BlockModel.makeCube(Block.cubeUVs(4, 0))));
    public static Block STONE = register(new Block(5, "Stone", BlockModel.makeCube(Block.cubeUVs(5, 0))));

    public static Block GLASS = register(new Block(6, "Glass", BlockModel.makeCube(Block.cubeUVs(6, 0)))
        .transparency()
    );

    public static Block WATER = register(new Water(7, "Water", BlockModel.makeLiquid(Block.cubeUVs(7, 0)))
        .translucency()
        .noCollision()
        .noSelection());

    public static Block ICE = register(new Block(8, "Ice", BlockModel.makeCube(Block.cubeUVs(8, 0)))
        .translucency()
        .noCollision()
        .noSelection());


    public static Block LOG = register(new Block(9, "Wooden Log", BlockModel.makeCube(Block.grassUVs(10, 0, 9, 0, 11, 0))));
    public static Block LEAVES = register(new Block(10, "Leaves", BlockModel.makeCube(Block.cubeUVs(12, 0))).transparency());

    public static bool isSolid(int block) {
        if (block == 0) {
            return false;
        }
        var bl = get(block);
        return !bl.transparent && !bl.translucent;
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
    public BlockModel model;
    public AABB? aabb;

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
        return new Vector2D<float>((x * 16f / atlasSize), (y * 16f / atlasSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<float> texCoords(UVPair uv) {
        return new Vector2D<float>((uv.u * 16f / atlasSize), (uv.v * 16f / atlasSize));
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort packData(byte direction, byte ao) {
        return (ushort)(ao << 3 | direction);
    }

    public static AABB fullBlock() {
        return new AABB(new Vector3D<double>(0, 0, 0), new Vector3D<double>(1, 1, 1));
    }

    public Block(ushort id, string name, BlockModel model, AABB? aabb = null) {
        this.id = id;
        this.name = name;
        this.model = model;

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


    public virtual void update(World world, Vector3D<int> pos) {

    }
}

public class Water(ushort id, string name, BlockModel uvs, AABB? aabb = null) : Block(id, name, uvs, aabb) {

    public override void update(World world, Vector3D<int> pos) {
        foreach (var dir in Direction.directionsWaterSpread) {
            // queue block updates
            var neighbourBlock = pos + dir;
            if (world.getBlock(neighbourBlock) == Blocks.AIR.id) {
                world.runLater(() => world.setBlock(neighbourBlock.X, neighbourBlock.Y, neighbourBlock.Z, Blocks.WATER.id), 10);
                world.blockUpdate(neighbourBlock, 10);
            }
        }
    }
}

/// <summary>
/// Stores UV in block coordinates (1 = 16px)
/// </summary>
public readonly record struct UVPair(float u, float v) {

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
public readonly record struct Face(
    float x1, float y1, float z1,
    float x2, float y2, float z2,
    float x3, float y3, float z3,
    float x4, float y4, float z4,
    UVPair min, UVPair max, RawDirection direction, bool noAO = false, bool nonFullFace = false);

public class BlockModel {
    public Face[] faces;

    public static BlockModel makeCube(UVPair[] uvs) {
        var model = new BlockModel();
        model.faces = new Face[6];
        // west
        model.faces[0] = new Face(0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, uvs[0], uvs[0] + 1, RawDirection.WEST);
        // east
        model.faces[1] = new Face(1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 1, 1, uvs[1], uvs[1] + 1, RawDirection.EAST);
        // south
        model.faces[2] = new Face(0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, uvs[2], uvs[2] + 1, RawDirection.SOUTH);
        // north
        model.faces[3] = new Face(1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, uvs[3], uvs[3] + 1, RawDirection.NORTH);
        // down
        model.faces[4] = new Face(1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, uvs[4], uvs[4] + 1, RawDirection.DOWN);
        // up
        model.faces[5] = new Face(0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1, uvs[5], uvs[5] + 1, RawDirection.UP);
        return model;
    }

    /// <summary>
    /// Liquids are cubes but only the bottom 7/8th of the block is drawn
    /// </summary>
    public static BlockModel makeLiquid(UVPair[] uvs) {
        var model = new BlockModel();
        model.faces = new Face[6];

        var topUV = new UVPair(0, 1 / 16f);
        var height = 15 / 16f;

        // west
        model.faces[0] = new Face(0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, uvs[0], uvs[0] + 1, RawDirection.WEST, true);
        // east
        model.faces[1] = new Face(1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 1, 1, uvs[1], uvs[1] + 1, RawDirection.EAST, true);
        // south
        model.faces[2] = new Face(0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, uvs[2], uvs[2] + 1, RawDirection.SOUTH, true);
        // north
        model.faces[3] = new Face(1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, uvs[3], uvs[3] + 1, RawDirection.NORTH, true);
        // down
        model.faces[4] = new Face(1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, uvs[4], uvs[4] + 1, RawDirection.DOWN, true);
        // up
        model.faces[5] = new Face(0, height, 1, 0, height, 0, 1, height, 0, 1, height, 1, uvs[5], uvs[5] + 1, RawDirection.UP, true, true);
        return model;
    }
}