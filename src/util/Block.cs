using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL;
using Molten;
using Silk.NET.Maths;
using Vector3D = Molten.DoublePrecision.Vector3D;

namespace BlockGame.util;

public class Block {

    private const int particleCount = 4;

    /// <summary>
    /// Block ID
    /// </summary>
    public ushort id;

    /// <summary>
    /// Display name
    /// </summary>
    public string name;

    /// <summary>
    /// Is fully transparent? (glass, leaves, etc.)
    /// Is translucent? (partially transparent blocks like water)
    /// </summary>
    public RenderLayer layer = RenderLayer.SOLID;
    public RenderType type = RenderType.MODEL;

    public BlockModel? model;

    public const int atlasSize = 256;
    public const int textureSize = 16;


    public const float atlasRatio = textureSize / (float)atlasSize;
    
    
    private const int MAXBLOCKS = 128;
    public static Block?[] blocks = new Block[MAXBLOCKS];

    /// <summary>
    /// Stores whether the block is a full block or not.
    /// </summary>
    public static bool[] fullBlock = new bool[MAXBLOCKS];
    
    public static bool[] translucent = new bool[MAXBLOCKS];
    public static bool[] inventoryBlacklist = new bool[MAXBLOCKS];
    public static bool[] randomTick = new bool[MAXBLOCKS];
    public static bool[] liquid = new bool[MAXBLOCKS];
    public static bool[] selection = new bool[MAXBLOCKS];
    public static bool[] collision = new bool[MAXBLOCKS];
    public static byte[] lightLevel = new byte[MAXBLOCKS];
    public static AABB?[] AABB = new AABB?[MAXBLOCKS];
    public static AABB?[] selectionAABB = new AABB?[MAXBLOCKS];
    public static RenderType[] renderType = new RenderType[MAXBLOCKS];
    
    public static UVPair?[] uvs = new UVPair?[6];

    public static readonly int maxBlock = 45;

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

    public static void preLoad() {
        
    }

    public static void postLoad() {
        for (int i = 0; i <= maxBlock; i++) {
            translucent[blocks[i].id] = blocks[i].layer == RenderLayer.TRANSLUCENT;
        }
        inventoryBlacklist[38] = true;
        //inventoryBlacklist[7] = true;
    }


    public static bool isFullBlock(int id) {
        return fullBlock[id];
    }

    public static bool isBlacklisted(int block) {
        return inventoryBlacklist[block];
    }

    public static Block AIR = register(new Block(0, "Air", BlockModel.emptyBlock()).air());
    public static Block GRASS = register(new Block(1, "Grass", BlockModel.makeCube(grassUVs(0, 0, 1, 0, 2, 0))).tick());
    public static Block DIRT = register(new Block(2, "Dirt", BlockModel.makeCube(cubeUVs(2, 0))));
    public static Block SAND = register(new FallingBlock(3, "Sand", BlockModel.makeCube(cubeUVs(3, 0))));
    public static Block BASALT = register(new Block(4, "Basalt", BlockModel.makeCube(cubeUVs(4, 0))));
    public static Block STONE = register(new Block(5, "Stone", BlockModel.makeCube(cubeUVs(5, 0))));
    public static Block GRAVEL = register(new Block(6, "Gravel", BlockModel.makeCube(cubeUVs(7, 0))));
    public static Block HELLSTONE = register(new Block(7, "Hellstone", BlockModel.makeCube(grassUVs(8, 0, 9, 0, 9, 0))).light(15));
    public static Block GLASS = register(new Block(8, "Glass", BlockModel.makeCube(cubeUVs(6, 0))).transparency());

    public static Block LANTERN = register(new Block(9, "Lantern", BlockModel.makePartialCube(grassUVs(15, 1, 13, 1, 14, 1))).light(14).partialBlock());

    public static Block TALL_GRASS = register(new Flower(43, "Tall Grass", BlockModel.makeGrass(crossUVs(9,1)))).transparency().noCollision();
    public static Block SHORT_GRASS = register(new Flower(44, "Short Grass", BlockModel.makeGrass(crossUVs(8,1)))).transparency().shortGrassAABB().noCollision();
    public static Block YELLOW_FLOWER = register(new Flower(10, "Yellow Flower", BlockModel.makeGrass(crossUVs(10,1))).transparency().flowerAABB().noCollision());
    public static Block RED_FLOWER = register(new Flower(11, "Red Flower", BlockModel.makeGrass(crossUVs(11,1))).transparency().flowerAABB().noCollision());


    public static Block PLANKS = register(new Block(12, "Planks", BlockModel.makeCube(cubeUVs(0, 5))));
    public static Block STAIRS = register(new Block(13, "Stairs", BlockModel.makeStairs(cubeUVs(0, 5))).partialBlock());
    public static Block LOG = register(new Block(14, "Log", BlockModel.makeCube(grassUVs(2, 5, 1, 5, 3, 5))));
    public static Block LEAVES = register(new Block(15, "Leaves", BlockModel.makeCube(cubeUVs(4, 5))).transparency());
    public static Block MAPLE_PLANKS = register(new Block(16, "Maple Planks", BlockModel.makeCube(cubeUVs(5, 5))));
    public static Block MAPLE_STAIRS = register(new Block(17, "Maple Stairs", BlockModel.makeStairs(cubeUVs(5, 5))).partialBlock());
    public static Block MAPLE_LOG = register(new Block(18, "Maple Log", BlockModel.makeCube(grassUVs(7, 5, 6, 5, 8, 5))));
    public static Block MAPLE_LEAVES = register(new Block(19, "Maple Leaves", BlockModel.makeCube(cubeUVs(9, 5))).transparency());
    //public static Block MAHOGANY_LOG = register(new Block(19, "Mahogany Log", BlockModel.makeCube(Block.grassUVs(7, 5, 6, 5, 8, 5))));
    //public static Block MAHOGANY_LEAVES = register(new Block(20, "Maple Leaves", BlockModel.makeCube(Block.cubeUVs(9, 5))).transparency());

    public static Block METAL_CUBE_BLUE = register(new Block(20, "Blue Metal Block", BlockModel.makeCube(cubeUVs(12, 1))));
    public static Block CANDY_LIGHT_BLUE = register(new Block(21, "Light Blue Candy", BlockModel.makeCube(cubeUVs(0, 2))));
    public static Block CANDY_CYAN = register(new Block(22, "Cyan Candy", BlockModel.makeCube(cubeUVs(1, 2))));
    public static Block CANDY_TURQUOISE = register(new Block(23, "Turquoise Candy", BlockModel.makeCube(cubeUVs(2, 2))));
    public static Block CANDY_DARK_GREEN = register(new Block(24, "Dark Green Candy", BlockModel.makeCube(cubeUVs(3, 2))));
    public static Block CANDY_LIGHT_GREEN = register(new Block(25, "Light Green Candy", BlockModel.makeCube(cubeUVs(4, 2))));
    public static Block CANDY_ORANGE = register(new Block(26, "Orange Candy", BlockModel.makeCube(cubeUVs(5, 2))));
    public static Block CANDY_YELLOW = register(new Block(27, "Yellow Candy", BlockModel.makeCube(cubeUVs(6, 2))));
    public static Block CANDY_LIGHT_RED = register(new Block(28, "Light Red Candy", BlockModel.makeCube(cubeUVs(7, 2))));
    public static Block CANDY_PINK = register(new Block(29, "Pink Candy", BlockModel.makeCube(cubeUVs(8, 2))));
    public static Block CANDY_PURPLE = register(new Block(30, "Purple Candy", BlockModel.makeCube(cubeUVs(9, 2))));
    public static Block VIOLET = register(new Block(31, "Violet Candy", BlockModel.makeCube(cubeUVs(10, 2))));
    public static Block CANDY_RED = register(new Block(32, "Red Candy", BlockModel.makeCube(cubeUVs(11, 2))));
    public static Block CANDY_DARK_BLUE = register(new Block(33, "Dark Blue Candy", BlockModel.makeCube(cubeUVs(12, 2))));
    public static Block CANDY_WHITE = register(new Block(34, "White Candy", BlockModel.makeCube(cubeUVs(13, 2))));
    public static Block CANDY_GREY = register(new Block(35, "Grey Candy", BlockModel.makeCube(cubeUVs(14, 2))));
    public static Block CANDY_BLACK = register(new Block(36, "Black Candy", BlockModel.makeCube(cubeUVs(15, 2))));

    public static Block HEAD = register(new Block(37, "Head", BlockModel.makeHalfCube(HeadUVs(0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3))).partialBlock());

    public static Block WATER = register(new Water(38, "Water", BlockModel.makeLiquid(cubeUVs(0, 4))).makeLiquid());

    public static Block RED_ORE = register(new Block(39, "Red Ore", BlockModel.makeCube(cubeUVs(10, 0))));
    public static Block PURPLE_ORE = register(new Block(40, "Purple Ore", BlockModel.makeCube(cubeUVs(11, 0))));
    public static Block AMBER_ORE = register(new Block(41, "Amber Ore", BlockModel.makeCube(cubeUVs(12, 0))));
    public static Block VIOLET_ORE = register(new Block(42, "Violet Ore", BlockModel.makeCube(cubeUVs(13, 0))));

    public static Block TORCH = register(new Block(45, "Torch", BlockModel.makeTorch(grassUVs(4, 1,0, 1, 4,1))).partialBlock().torchAABB().light(8).transparency());

    public static bool isSolid(int block) {
        return block != 0 && get(block).layer == RenderLayer.SOLID;
    }

    public static bool notSolid(int block) {
        return block == 0 || get(block).layer != RenderLayer.SOLID;
    }

    public static bool isTransparent(int block) {
        return block != 0 && get(block).layer == RenderLayer.TRANSPARENT;
    }

    public static bool isTranslucent(int block) {
        return translucent[block];
    }

    public static bool notTranslucent(int block) {
        return !translucent[block];
    }

    public static bool hasCollision(int block) {
        return block != 0 && collision[block];
    }

    public static bool isSolid(Block block) {
        return block.id != 0 && block.layer == RenderLayer.SOLID;
    }

    public static bool notSolid(Block block) {
        return block.id == 0 || block.layer != RenderLayer.SOLID;
    }

    public static bool isTransparent(Block block) {
        return block.id != 0 && block.layer == RenderLayer.TRANSPARENT;
    }

    public static bool isTranslucent(Block block) {
        return block.id != 0 && block.layer == RenderLayer.TRANSLUCENT;
    }

    public static bool hasCollision(Block block) {
        return block.id != 0 && collision[block.id];
    }

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
    public static Vector2F texCoords(float x, float y) {
        return new Vector2F(x * 16f / atlasSize, y * 16f / atlasSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2F texCoords(UVPair uv) {
        return new Vector2F(uv.u * 16f / atlasSize, uv.v * 16f / atlasSize);
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

    public static UVPair[] HeadUVs(int leftX, int leftY, int rightX, int rightY, int frontX, int frontY, int backX, int backY, int bottomX, int bottomY, int topX, int topY) {
        return [
            new(leftX, leftY), new(rightX, rightY), new(frontX, frontY), new(backX, backY), new(bottomX, bottomY), new(topX, topY)
        ];
    }

    // this will pack the data into the uint
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort packData(byte direction, byte ao, byte light) {
        // idx[0] = texU == 1, idx[1] = texV == 1

        // if none, treat it as an up (strip 4th byte)
        return (ushort)(light << 8 | ao << 3 | direction & 0b111);
    }
    
    
    // ivec2 lightCoords = ivec2((lightValue >> 4) & 0xFu, lightValue & 0xFu);
    // compute tint (light * ao * direction)
    // per-face lighting
    // float lColor = a[direction]
    // tint = texelFetch(lightTexture, lightCoords, 0) * a[direction] * aoArray[aoValue];
    public static Color packColour(byte direction, byte ao, byte light) {
        direction = (byte)(direction & 0b111);
        var blocklight = (byte)(light >> 4);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textureManager.lightTexture.getPixel(blocklight, skylight);
        float tint = WorldRenderer.a[direction] * WorldRenderer.aoArray[ao];
        var ab = new Color(lightVal.R / 255f * tint, lightVal.G / 255f * tint, lightVal.B / 255f * tint, 1);
        return ab;
    }

    public static AABB fullBlockAABB() {
        return new AABB(new Vector3D(0, 0, 0), new Vector3D(1, 1, 1));
    }

    public Block flowerAABB() {
        var offset = 6 / 16f;
        selectionAABB[id] = new AABB(new Vector3D(0 + offset, 0, 0 + offset), new Vector3D(1 - offset, 0.5, 1 - offset));
        return this;
    }

    public Block shortGrassAABB() {
        var offset = 4 / 16f;
        selectionAABB[id] = new AABB(new Vector3D(0, 0, 0), new Vector3D(1, offset, 1));
        return this;
    }

    public Block torchAABB() {
        var offset = 6 / 16f;
        selectionAABB[id] = new AABB(new Vector3D(0 + offset, 0, 0 + offset), new Vector3D(1 - offset, 1, 1 - offset));
        noCollision();
        return this;
    }

    public Block(ushort id, string name, BlockModel? model) {
        this.id = id;
        this.name = name;
        this.model = model;
        
        fullBlock[id] = true;
        selection[id] = true;
        collision[id] = true;
        liquid[id] = false;
        randomTick[id] = false;

        AABB[id] = fullBlockAABB();
        selectionAABB[id] = fullBlockAABB();
    }

    public Block transparency() {
        layer = RenderLayer.TRANSPARENT;
        fullBlock[id] = false;
        return this;
    }

    public Block translucency() {
        layer = RenderLayer.TRANSLUCENT;
        fullBlock[id] = false;
        return this;
    }

    public Block noCollision() {
        collision[id] = false;
        AABB[id] = null;
        return this;
    }

    public Block noSelection() {
        selection[id] = false;
        selectionAABB[id] = null;
        return this;
    }

    public Block partialBlock() {
        fullBlock[id] = false;
        return this;
    }

    public Block makeLiquid() {
        translucency();
        noCollision();
        noSelection();
        liquid[id] = true;
        fullBlock[id] = false;
        return this;
    }

    public Block setCustomRender() {
        renderType[id] = RenderType.CUSTOM;
        return this;
    }

    public Block light(byte amount) {
        lightLevel[id] = amount;
        return this;
    }


    public virtual void update(World world, Vector3I pos) {

    }

    public virtual ushort render(World world, Vector3I pos, List<BlockVertexPacked> vertexBuffer) {
        return 0;
    }

    public Block air() {
        noCollision();
        noSelection();
        fullBlock[id] = false;
        return this;
    }

    public Block tick() {
        randomTick[id] = true;
        return this;
    }

    public virtual void crack(World world, int x, int y, int z) {
        var factor = 1f / particleCount;
        for (var x1 = 0; x1 < particleCount; x1++) {
            for (var y1 = 0; y1 < particleCount; y1++) {
                for (var z1 = 0; z1 < particleCount; z1++) {
                    var particleX = x + (x1 + 0.5f) * factor + (Game.clientRandom.NextSingle() - 0.5f) * 0.15f;
                    var particleY = y + (y1 + 0.5f) * factor + (Game.clientRandom.NextSingle() - 0.5f) * 0.15f;
                    var particleZ = z + (z1 + 0.5f) * factor + (Game.clientRandom.NextSingle() - 0.5f) * 0.15f;
                    var particlePosition = new Vector3D(particleX, particleY, particleZ);

                    var size = Game.clientRandom.NextSingle() * 0.1f + 0.05f;
                    var ttl = (int)(3f / (Game.clientRandom.NextSingle() + 0.05f));

                    var randFace = model.faces[Game.clientRandom.Next(0, model.faces.Length)];

                    var randU = texU(randFace.min.u + Game.clientRandom.NextSingle() * 0.75f);
                    var randV = texV(randFace.min.v + Game.clientRandom.NextSingle() * 0.75f);

                    // the closer to the centre, the less the motion
                    // dx gives a number between -0.5 and 0.5 -> remap to between 0.5 and 3
                    var dx = (particleX - x - 0.5f);
                    var dy = (particleY - y - 0.5f);
                    var dz = (particleZ - z - 0.5f);


                    // between -0.7 and 0.7
                    var motion = new Vector3(dx * 3 + (Game.clientRandom.NextSingle() - 0.5f) * 0.2f,
                        dy * 3 + (Game.clientRandom.NextSingle() - 0.5f) * 0.2f,
                        dz * 3 + (Game.clientRandom.NextSingle() - 0.5f) * 0.2f);

                    var speed = (MathF.Pow(Game.clientRandom.NextSingle(), 2) + 1) * 0.8f;

                    motion *= speed;
                    motion.Y += 0.15f;

                    var particle = new Particle(
                        world,
                        particlePosition,
                        "textures/blocks.png",
                        randU,
                        randV,
                        size,
                        1 / 16f * size,
                        ttl);
                    world.particleManager.add(particle);

                    particle.velocity = motion.toVec3D();
                }
            }
        }
    }

}

public class Flower(ushort id, string name, BlockModel uvs) : Block(id, name, uvs) {

    public override void update(World world, Vector3I pos) {
        if (world.inWorld(pos.X, pos.Y - 1, pos.Z) && world.getBlock(pos.X, pos.Y - 1, pos.Z) == 0) {
            world.setBlockRemesh(pos.X, pos.Y, pos.Z, Block.AIR.id);
        }
    }
}

public class Water(ushort id, string name, BlockModel uvs) : Block(id, name, uvs) {

    public override void update(World world, Vector3I pos) {
        foreach (var dir in Direction.directionsWaterSpread) {
            // queue block updates
            var neighbourBlock = pos + dir;
            if (world.getBlock(neighbourBlock) == Block.AIR.id) {
                world.runLater(neighbourBlock, () => {
                    if (world.getBlock(neighbourBlock) == Block.AIR.id) {
                        world.setBlockRemesh(neighbourBlock.X, neighbourBlock.Y, neighbourBlock.Z, Block.WATER.id);
                    }
                }, 10);
                world.blockUpdate(neighbourBlock, 10);
            }
        }
    }
}

public class FallingBlock(ushort id, string name, BlockModel uvs) : Block(id, name, uvs) {
    public override void update(World world, Vector3I pos) {
        var y = pos.Y - 1;
        bool isSupported = true;
        // if not supported, set flag
        while (world.getBlock(new Vector3I(pos.X, y, pos.Z)) == 0) {
            // decrement Y
            isSupported = false;
            y--;
        }
        if (!isSupported) {
            world.setBlockRemesh(pos.X, pos.Y, pos.Z, 0);
            world.setBlockRemesh(pos.X, y + 1, pos.Z, id);
        }

        // if sand above, update
        if (world.getBlock(new Vector3I(pos.X, pos.Y + 1, pos.Z)) == id) {
            world.blockUpdate(new Vector3I(pos.X, pos.Y + 1, pos.Z));
        }
    }
}

/// <summary>
/// Stores UV in block coordinates (1 = 16px)
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct UVPair(float u, float v) {

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

    public const int MAX_FACES = 12;

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
    public readonly byte flags = (byte)(WorldRenderer.toByte(nonFullFace) | WorldRenderer.toByte(noAO) << 1);

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
public enum RenderLayer : byte {
    SOLID,
    TRANSPARENT,
    TRANSLUCENT
}

public enum RenderType : byte {
    CUBE,
    CROSS,
    MODEL,
    CUSTOM
}