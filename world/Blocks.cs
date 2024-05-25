using System.Runtime.CompilerServices;
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
        return ChunkSectionRenderer.access(blocks, id);
    }

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
        .makeLiquid());

    public static Block ICE = register(new Block(8, "Ice", BlockModel.makeCube(Block.cubeUVs(8, 0)))
        .translucency()
        .noCollision()
        .noSelection());

    public static Block CURSED_GRASS = register(new Flower(9, "Cursed Grass", BlockModel.makeGrass(Block.crossUVs(13, 0)))
        .transparency()
        .noCollision()
        .flowerAABB());

    public static Block LOG = register(new Block(10, "Wooden Log", BlockModel.makeCube(Block.grassUVs(10, 0, 9, 0, 11, 0))));
    public static Block LEAVES = register(new Block(11, "Leaves", BlockModel.makeCube(Block.cubeUVs(12, 0))).transparency());

    public static bool isSolid(int block) {
        if (block == 0) {
            return false;
        }
        var bl = get(block);
        return !bl.transparent && !bl.translucent;
    }

    public static bool notSolid(int block) {
        if (block == 0) {
            return true;
        }
        var bl = get(block);
        return bl.transparent || bl.translucent;
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
    public AABB? aabb;

    public bool selection = true;
    public AABB? selectionAABB;

    /// <summary>
    /// Is this block a liquid?
    /// </summary>
    public bool liquid = false;

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
        selectionAABB = null;
        return this;
    }
    
    public Block makeLiquid() {
        translucency();
        noCollision();
        noSelection();
        liquid = true;
        return this;
    }


    public virtual void update(World world, Vector3D<int> pos) {

    }
}

public class Flower(ushort id, string name, BlockModel uvs) : Block(id, name, uvs) {

    public override void update(World world, Vector3D<int> pos) {
        if (world.inWorld(pos.X, pos.Y - 1, pos.Z) && world.getBlock(pos.X, pos.Y - 1, pos.Z) == 0) {
            world.setBlock(pos.X, pos.Y, pos.Z, Blocks.AIR.id);
        }
    }
}

public class Water(ushort id, string name, BlockModel uvs) : Block(id, name, uvs) {

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
public readonly record struct Face(
    float x1, float y1, float z1,
    float x2, float y2, float z2,
    float x3, float y3, float z3,
    float x4, float y4, float z4,
    UVPair min, UVPair max, RawDirection direction, bool noAO = false, bool nonFullFace = false) {

    public const int MAX_FACES = 6;

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
    public readonly bool noAO = noAO;
    public readonly bool nonFullFace = nonFullFace;
}

public class BlockModel {
    public Face[] faces;

    public static BlockModel makeCube(UVPair[] uvs) {
        var model = new BlockModel();
        model.faces = new Face[6];
        // west
        model.faces[0] = new(0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, uvs[0], uvs[0] + 1, RawDirection.WEST);
        // east
        model.faces[1] = new(1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 1, 1, uvs[1], uvs[1] + 1, RawDirection.EAST);
        // south
        model.faces[2] = new(0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, uvs[2], uvs[2] + 1, RawDirection.SOUTH);
        // north
        model.faces[3] = new(1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, uvs[3], uvs[3] + 1, RawDirection.NORTH);
        // down
        model.faces[4] = new(1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, uvs[4], uvs[4] + 1, RawDirection.DOWN);
        // up
        model.faces[5] = new(0, 1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 1, uvs[5], uvs[5] + 1, RawDirection.UP);
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
        model.faces[0] = new(0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, uvs[0], uvs[0] + 1, RawDirection.WEST, true);
        // east
        model.faces[1] = new(1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 1, 1, uvs[1], uvs[1] + 1, RawDirection.EAST, true);
        // south
        model.faces[2] = new(0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 0, uvs[2], uvs[2] + 1, RawDirection.SOUTH, true);
        // north
        model.faces[3] = new(1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, uvs[3], uvs[3] + 1, RawDirection.NORTH, true);
        // down
        model.faces[4] = new(1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, uvs[4], uvs[4] + 1, RawDirection.DOWN, true);
        // up
        model.faces[5] = new(0, height, 1, 0, height, 0, 1, height, 0, 1, height, 1, uvs[5], uvs[5] + 1, RawDirection.UP, true, true);
        return model;
    }

    // make a nice X
    public static BlockModel makeGrass(UVPair[] uvs) {
        var model = new BlockModel();
        model.faces = new Face[4];

        // offset from edge
        var offset = 1 / (8 * MathF.Sqrt(2));

        // x1
        model.faces[0] = new(0 + offset, 1, 1 - offset, 0 + offset, 0, 1 - offset, 1 - offset, 0, 0 + offset, 1 - offset, 1, 0 + offset,
            uvs[0], uvs[0] + 1, RawDirection.WEST, true, true);
        // x2
        model.faces[1] = new(0 + offset, 1, 0 + offset, 0 + offset, 0, 0 + offset, 1 - offset, 0, 1 - offset, 1 - offset, 1, 1 - offset,
            uvs[1], uvs[1] + 1, RawDirection.SOUTH, true, true);
        // x1 rear
        model.faces[2] = new(1 - offset, 1, 0 + offset, 1 - offset, 0, 0 + offset, 0 + offset, 0, 1 - offset, 0 + offset, 1, 1 - offset,
            uvs[0], uvs[0] + 1, RawDirection.EAST, true, true);
        // x2 rear
        model.faces[3] = new(1 - offset, 1, 1 - offset, 1 - offset, 0, 1 - offset, 0 + offset, 0, 0 + offset, 0 + offset, 1, 0 + offset,
            uvs[1], uvs[1] + 1, RawDirection.NORTH, true, true);
        return model;
    }
}