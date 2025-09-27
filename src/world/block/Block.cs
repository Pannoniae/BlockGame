using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.item;
using Molten;
using Silk.NET.Maths;
using Vector3D = Molten.DoublePrecision.Vector3D;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace BlockGame.world.block;


/**
 * For now, we'll only have 65536 blocks for typechecking (ushort -> uint), this can be extended later.
 */
[SuppressMessage("Compiler", "CS8618:Non-nullable field must contain a non-null value when exiting constructor. Consider adding the \'required\' modifier or declaring as nullable.")]
public class Block {
    
    /**
     * The maximum block ID we have. This ID is one past the end!
     * If you want to loop, do for (int i = 0; i &lt; currentID; i++) { ... }
     * so you won't overread.
     */
    public static int currentID = 0;

    private const int particleCount = 4;

    /// <summary>
    /// Block ID
    /// </summary>
    private uint value;
    
    public ushort id {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ushort)(value & 0xFFFFFF);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set => this.value = (this.value & 0xFF000000) | value;
    }
    
    /*public ushort metadata {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ushort)(value >> 24);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set => this.value = (this.value & 0xFFFFFF) | ((uint)(value << 24));
    }*/

    /// <summary>
    /// Display name
    /// </summary>
    public string name;

    /// <summary>
    /// Is fully transparent? (glass, leaves, etc.)
    /// Is translucent? (partially transparent blocks like water)
    /// </summary>
    public RenderLayer layer = RenderLayer.SOLID;
    
    /**
     * The block's model, if it has one.
     * Only has one if RenderType is MODEL.
     */
    public BlockModel? model;
    
    /**
     * The texture of the block.
     */
    public UVPair[]? uvs;

    protected static readonly List<AABB> AABBList = [];

    public const int atlasSize = 256;
    public const int textureSize = 16;


    public const float atlasRatio = textureSize / (float)atlasSize;
    public const float atlasRatioInv = 1 / atlasRatio;
    
    
    private const int MAXBLOCKS = 128;
    public static Block?[] blocks = new Block[MAXBLOCKS];

    /**
     * Stores whether the block is a full, opaque block or not.
     */
    public static bool[] fullBlock = new bool[MAXBLOCKS];
    
    /**
     * Is this block transparent? (glass, leaves, etc.)
     */
    public static bool[] transparent = new bool[MAXBLOCKS];
    
    public static bool[] translucent = new bool[MAXBLOCKS];
    
    /**
     * If false, water can break this block (like tall grass, flowers, etc.)
     * If true, water cannot break this block (like stone, dirt, stairs, etc.)
     */
    public static bool[] waterSolid = new bool[MAXBLOCKS];
    public static bool[] inventoryBlacklist = new bool[MAXBLOCKS];
    public static bool[] randomTick = new bool[MAXBLOCKS];
    public static bool[] renderTick = new bool[MAXBLOCKS];
    public static bool[] liquid = new bool[MAXBLOCKS];
    public static bool[] customCulling = new bool[MAXBLOCKS];
    public static bool[] renderItemLike = new bool[MAXBLOCKS];

    public static bool[] selection = new bool[MAXBLOCKS];
    public static bool[] collision = new bool[MAXBLOCKS];
    public static byte[] lightLevel = new byte[MAXBLOCKS];
    public static byte[] lightAbsorption = new byte[MAXBLOCKS];
    public static double[] hardness = new double[MAXBLOCKS];

    /**
     Block update delay in ticks. 0 = normal immediate block updates
    */
    public static byte[] updateDelay = new byte[MAXBLOCKS];
    
    public static AABB?[] AABB = new AABB?[MAXBLOCKS];
    public static bool[] customAABB = new bool[MAXBLOCKS];
    
    public static RenderType[] renderType = new RenderType[MAXBLOCKS];
    
    
    public static Block AIR;
    public static Block GRASS;
    public static Block DIRT;
    public static Block SAND;
    public static Block BASALT;
    public static Block STONE;
    public static Block GRAVEL;
    public static Block HELLSTONE;
    public static Block HELLROCK;
    public static Block GLASS;
    public static Block CALCITE;
    public static Block BRICK_BLOCK;

    public static Block LANTERN;

    public static Block TALL_GRASS;
    public static Block SHORT_GRASS;
    public static Block YELLOW_FLOWER;
    public static Block RED_FLOWER;
    public static Block THISTLE;


    public static Block PLANKS;
    public static Block STAIRS;
    public static Block STONE_SLAB;
    public static Block OAK_SLAB;
    public static Block MAPLE_PLANKS_SLAB;
    public static Block LOG;
    public static Block LEAVES;
    public static Block MAPLE_PLANKS;
    public static Block MAPLE_STAIRS;
    public static Block MAPLE_LOG;
    public static Block MAPLE_LEAVES;
    //public static Block MAHOGANY_LOG = register(new Block(19, "Mahogany Log", BlockModel.makeCube(Block.grassUVs(7, 5, 6, 5, 8, 5))));
    //public static Block MAHOGANY_LEAVES = register(new Block(20, "Maple Leaves", BlockModel.makeCube(Block.cubeUVs(9, 5))).transparency());
    
    public static Block CANDY;

    public static Block HEAD;

    public static Block WATER;

    public static Block RED_ORE;
    public static Block TITANIUM_ORE;
    public static Block AMBER_ORE;
    public static Block AMETHYST_ORE;
    public static Block EMERALD_ORE;
    public static Block DIAMOND_ORE;
    public static Block GOLD_ORE;
    public static Block IRON_ORE;
    public static Block COAL_ORE;
    public static Block TORCH;
    public static Block CRAFTING_TABLE;

    public static Block register(Block block) {
        // update maxid
        if (block.id >= currentID) {
            currentID = block.id + 1;
        }
        return blocks[block.id] = block;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Block? get(int id) {
        return blocks[id];
    }

    public static bool tryGet(int id, out Block block) {
        var cond = id >= 0 && id < currentID;
        block = (cond ? blocks[id] : blocks[1])!;
        return cond;
    }

    public static void preLoad() {
        AIR = register(new Block(Blocks.AIR, "Air").setModel(BlockModel.emptyBlock())).air();
        GRASS = register(new Block(Blocks.GRASS, "Grass")).tick();
        GRASS.setTex(grassUVs(0, 0, 1, 0, 2, 0));
        GRASS.setModel(BlockModel.makeCube(GRASS));
        
        DIRT = register(new Block(Blocks.DIRT, "Dirt"));
        DIRT.setTex(cubeUVs(2, 0));
        DIRT.setModel(BlockModel.makeCube(DIRT));
        
        SAND = register(new FallingBlock(Blocks.SAND, "Sand"));
        SAND.setTex(cubeUVs(3, 0));
        SAND.setModel(BlockModel.makeCube(SAND));
        
        BASALT = register(new Block(Blocks.BASALT, "Basalt"));
        BASALT.setTex(cubeUVs(4, 0));
        BASALT.setModel(BlockModel.makeCube(BASALT));
        
        STONE = register(new Block(Blocks.STONE, "Stone"));
        STONE.setTex(cubeUVs(5, 0));
        STONE.setModel(BlockModel.makeCube(STONE));

        GRAVEL = register(new Block(Blocks.GRAVEL, "Gravel"));
        GRAVEL.setTex(cubeUVs(7, 0));
        GRAVEL.setModel(BlockModel.makeCube(GRAVEL));
        
        HELLSTONE = register(new Block(Blocks.HELLSTONE, "Hellstone"));
        HELLSTONE.setTex(cubeUVs(8, 0));
        HELLSTONE.setModel(BlockModel.makeCube(HELLSTONE));
        HELLSTONE.light(10);
        
        HELLROCK = register(new Block(Blocks.HELLROCK, "Hellrock"));
        HELLROCK.setTex(cubeUVs(9, 0));
        HELLROCK.setModel(BlockModel.makeCube(HELLROCK));
        
        GLASS = register(new Block(Blocks.GLASS, "Glass"));
        GLASS.setTex(cubeUVs(6, 0));
        GLASS.setModel(BlockModel.makeCube(GLASS));
        GLASS.transparency();

        CALCITE = register(new Block(Blocks.CALCITE, "Calcite"));
        CALCITE.setTex(cubeUVs(6, 1));
        CALCITE.setModel(BlockModel.makeCube(CALCITE));

        BRICK_BLOCK = register(new Block(Blocks.BRICK_BLOCK, "Brick Block"));
        BRICK_BLOCK.setTex(cubeUVs(5, 1));
        BRICK_BLOCK.setModel(BlockModel.makeCube(BRICK_BLOCK));
        
        LANTERN = register(new Block(Blocks.LANTERN, "Lantern"));
        LANTERN.setTex(new(13, 1), new(14, 1), new(15, 1));
        LANTERN.setModel(BlockModel.makeLantern(LANTERN));
        LANTERN.light(15);
        LANTERN.partialBlock();
        
        TALL_GRASS = register(new Flower(Blocks.TALL_GRASS, "Tall Grass"));
        TALL_GRASS.setTex(crossUVs(9, 1));
        TALL_GRASS.setModel(BlockModel.makeGrass(TALL_GRASS));
        TALL_GRASS.transparency();
        TALL_GRASS.noCollision();
        TALL_GRASS.waterTransparent();
        
        SHORT_GRASS = register(new Flower(Blocks.SHORT_GRASS, "Short Grass"));
        SHORT_GRASS.setTex(crossUVs(8, 1));
        SHORT_GRASS.setModel(BlockModel.makeGrass(SHORT_GRASS));
        SHORT_GRASS.transparency();
        SHORT_GRASS.shortGrassAABB();
        SHORT_GRASS.noCollision();
        SHORT_GRASS.waterTransparent();
        
        YELLOW_FLOWER = register(new Flower(Blocks.YELLOW_FLOWER, "Yellow Flower"));
        YELLOW_FLOWER.setTex(crossUVs(10, 1));
        YELLOW_FLOWER.setModel(BlockModel.makeGrass(YELLOW_FLOWER));
        YELLOW_FLOWER.transparency();
        YELLOW_FLOWER.flowerAABB();
        YELLOW_FLOWER.noCollision();
        YELLOW_FLOWER.waterTransparent();
        YELLOW_FLOWER.itemLike();

        RED_FLOWER = register(new Flower(Blocks.RED_FLOWER, "Red Flower"));
        RED_FLOWER.setTex(crossUVs(11, 1));
        RED_FLOWER.setModel(BlockModel.makeGrass(RED_FLOWER));
        RED_FLOWER.transparency();
        RED_FLOWER.flowerAABB();
        RED_FLOWER.noCollision();
        RED_FLOWER.waterTransparent();
        RED_FLOWER.itemLike();

        THISTLE = register(new Flower(Blocks.THISTLE, "Thistle"));
        THISTLE.setTex(crossUVs(12, 1));
        THISTLE.setModel(BlockModel.makeGrass(THISTLE));
        THISTLE.transparency();
        THISTLE.flowerAABB();
        THISTLE.waterTransparent();
        THISTLE.itemLike();
        
        PLANKS = register(new Block(Blocks.PLANKS, "Planks"));
        PLANKS.setTex(cubeUVs(0, 5));
        PLANKS.setModel(BlockModel.makeCube(PLANKS));

        STAIRS = register(new Stairs(Blocks.STAIRS, "Stairs"));
        STAIRS.setTex(cubeUVs(0, 5));
        STAIRS.partialBlock();

        STONE_SLAB = register(new Slabs(Blocks.STONE_SLAB, "Stone Slab"));
        STONE_SLAB.setTex(cubeUVs(5, 0));

        OAK_SLAB = register(new Slabs(Blocks.OAK_SLAB, "Planks Slab"));
        OAK_SLAB.setTex(cubeUVs(0, 5));

        MAPLE_PLANKS_SLAB = register(new Slabs(Blocks.MAPLE_PLANKS_SLAB, "Maple Planks Slab"));
        MAPLE_PLANKS_SLAB.setTex(cubeUVs(5, 5));

        LOG = register(new Block(Blocks.LOG, "Log"));
        LOG.setTex(grassUVs(2, 5, 1, 5, 3, 5));
        LOG.setModel(BlockModel.makeCube(LOG));
        
        LEAVES = register(new Block(Blocks.LEAVES, "Leaves"));
        LEAVES.setTex(cubeUVs(4, 5));
        LEAVES.setModel(BlockModel.makeCube(LEAVES));
        LEAVES.transparency();
        LEAVES.setLightAbsorption(1);
        
        MAPLE_PLANKS = register(new Block(Blocks.MAPLE_PLANKS, "Maple Planks"));
        MAPLE_PLANKS.setTex(cubeUVs(5, 5));
        MAPLE_PLANKS.setModel(BlockModel.makeCube(MAPLE_PLANKS));
        
        MAPLE_STAIRS = register(new Stairs(Blocks.MAPLE_STAIRS, "Maple Stairs"));
        MAPLE_STAIRS.setTex(cubeUVs(5, 5));
        MAPLE_STAIRS.partialBlock();
        
        MAPLE_LOG = register(new Block(Blocks.MAPLE_LOG, "Maple Log"));
        MAPLE_LOG.setTex(grassUVs(7, 5, 6, 5, 8, 5));
        MAPLE_LOG.setModel(BlockModel.makeCube(MAPLE_LOG));
        
        MAPLE_LEAVES = register(new Block(Blocks.MAPLE_LEAVES, "Maple Leaves"));
        MAPLE_LEAVES.setTex(cubeUVs(9, 5));
        MAPLE_LEAVES.setModel(BlockModel.makeCube(MAPLE_LEAVES));
        MAPLE_LEAVES.transparency();
        
        CANDY = register(new CandyBlock(Blocks.CANDY, "Candy"));
        
        HEAD = register(new Block(Blocks.HEAD, "Head"));
        HEAD.setTex(HeadUVs(0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3));
        HEAD.setModel(BlockModel.makeHalfCube(HEAD));
        HEAD.partialBlock();

        WATER = register(new Water(Blocks.WATER, "Water", 15, 8));
        WATER.setTex(new UVPair(0, 4), new UVPair(0, 8));
        WATER.makeLiquid();
        
        
        RED_ORE = register(new Block(Blocks.RED_ORE, "Red Ore"));
        RED_ORE.setTex(cubeUVs(10, 0));
        RED_ORE.setModel(BlockModel.makeCube(RED_ORE));
        
        TITANIUM_ORE = register(new Block(Blocks.TITANIUM_ORE, "Titanium Ore"));
        TITANIUM_ORE.setTex(cubeUVs(11, 0));
        TITANIUM_ORE.setModel(BlockModel.makeCube(TITANIUM_ORE));
        
        AMBER_ORE = register(new Block(Blocks.AMBER_ORE, "Amber Ore"));
        AMBER_ORE.setTex(cubeUVs(12, 0));
        AMBER_ORE.setModel(BlockModel.makeCube(AMBER_ORE));
        
        AMETHYST_ORE = register(new Block(Blocks.AMETHYST_ORE, "Amethyst Ore"));
        AMETHYST_ORE.setTex(cubeUVs(13, 0));
        AMETHYST_ORE.setModel(BlockModel.makeCube(AMETHYST_ORE));
        
        EMERALD_ORE = register(new Block(Blocks.EMERALD_ORE, "Emerald Ore"));
        EMERALD_ORE.setTex(cubeUVs(14, 0));
        EMERALD_ORE.setModel(BlockModel.makeCube(EMERALD_ORE));
        
        DIAMOND_ORE = register(new Block(Blocks.DIAMOND_ORE, "Diamond Ore"));
        DIAMOND_ORE.setTex(cubeUVs(15, 0));
        DIAMOND_ORE.setModel(BlockModel.makeCube(DIAMOND_ORE));
        
        GOLD_ORE = register(new Block(Blocks.GOLD_ORE, "Gold Ore"));
        GOLD_ORE.setTex(cubeUVs(0, 1));
        GOLD_ORE.setModel(BlockModel.makeCube(GOLD_ORE));
        
        IRON_ORE = register(new Block(Blocks.IRON_ORE, "Iron Ore"));
        IRON_ORE.setTex(cubeUVs(1, 1));
        IRON_ORE.setModel(BlockModel.makeCube(IRON_ORE));
        
        COAL_ORE = register(new Block(Blocks.COAL_ORE, "Coal Ore"));
        COAL_ORE.setTex(cubeUVs(4, 1));
        COAL_ORE.setModel(BlockModel.makeCube(COAL_ORE));
        
        TORCH = register(new Torch(Blocks.TORCH, "Torch"));
        TORCH.setTex(cubeUVs(0, 6));
        TORCH.itemLike();
        
        CRAFTING_TABLE = register(new Block(Blocks.CRAFTING_TABLE, "Crafting Table"));
        CRAFTING_TABLE.setTex(CTUVs(4,3, 3,3, 2, 3, 5,3));
        CRAFTING_TABLE.setModel(BlockModel.makeCube(CRAFTING_TABLE));
        
        // I'm lazy so we cheat! We register all the "special" items here (only the ones which require custom item classes because they have a dynamic name or other special behaviour)
        Item.register(new CandyBlockItem(Blocks.CANDY, "Candy Block"));
        
        // register items for all blocks
        for (int i = 0; i < currentID; i++) {
            if (blocks[i] != null && Item.get(-i) == null) {
                Item.register(new BlockItem(i, blocks[i].name));
            }
        }

        // set hardness
        for (int i = 0; i < currentID; i++) {
            if (hardness[i] == 0) {
                hardness[i] = 1;
            }
        }
    }
    
    // I've removed this because realistically it will always be null / 0 and it would mislead the API caller
    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort getMetadata() {
        return (ushort)(value >> 24);
    }*/
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort getID() {
        return (ushort)(value & 0xFFFFFF);
    }
    
    public ushort setMetadata(ushort metadata) {
        value = value & 0xFFFFFF | (uint)(metadata << 24);
        return getID();
    }
    
    public ushort setID(ushort id) {
        this.value = (this.value & 0xFF000000) | id;
        return getID();
    }

    public static void postLoad() {
        for (int i = 0; i < currentID; i++) {
            if (blocks[i] != null) {
                translucent[blocks[i].id] = blocks[i].layer == RenderLayer.TRANSLUCENT;
            }
        }

        //inventoryBlacklist[Blocks.WATER] = true;
        //inventoryBlacklist[7] = true;
    }


    public static bool isFullBlock(int id) {
        return fullBlock[id];
    }

    public static bool isBlacklisted(int block) {
        return inventoryBlacklist[block];
    }

    //public static Block TORCH = register(new Block(Blocks.TORCH, "Torch", BlockModel.makeTorch(grassUVs(4, 1,0, 1, 4,1))).partialBlock().torchAABB().light(8).transparency());

    public static bool notSolid(int block) {
        return block == 0 || get(block).layer != RenderLayer.SOLID;
    }

    public static bool isTranslucent(int block) {
        return translucent[block];
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
    
    public static UVPair[] CTUVs(int topX, int topY, int xx, int xy, int zx, int zy, int bottomX, int bottomY) {
        return [
            new(xx, xy),new(xx, xy),  new(zx, zy), new(zx, zy), new(bottomX, bottomY),
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
        var a = 2;
        return (ushort)(light << 8 | ao << 3 | direction & 0b111);
    }
    
    
    // ivec2 lightCoords = ivec2((lightValue >> 4) & 0xFu, lightValue & 0xFu);
    // compute tint (light * ao * direction)
    // per-face lighting
    // float lColor = a[direction]
    // tint = texelFetch(lightTexture, lightCoords, 0) * a[direction] * aoArray[aoValue];
    public static Color packColour(byte direction, byte ao, byte light) {
        
        Span<float> aoArray = [1.0f, 0.75f, 0.5f, 0.25f];
        Span<float> a = [0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1];
        
        direction = (byte)(direction & 0b111);
        var blocklight = (byte)(light >> 4);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);
        float tint = a[direction] * aoArray[ao];
        var ab = new Color(lightVal.R / 255f * tint, lightVal.G / 255f * tint, lightVal.B / 255f * tint, 1);
        return ab;
    }
    
    public static Color4b packColour(RawDirection direction, byte ao, byte light) {
        return packColour((byte)direction, ao, light).to4b();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color packColour(byte direction, byte ao) {
        Span<float> aoArray = [1.0f, 0.75f, 0.5f, 0.25f];
        Span<float> a = [0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1];
        
        direction &= 0b111;
        byte tint = (byte)(a[direction] * aoArray[ao] * 255);
        return new Color(tint, tint, tint, (byte)255);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint packColourB(byte direction, byte ao) {
        // can we just inline the array?
        
        Span<float> aoArray = [1.0f, 0.75f, 0.5f, 0.25f];
        Span<float> a = [0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1];
        
        
        direction &= 0b111;
        byte tint = (byte)(a[direction] * aoArray[ao] * 255);
        return (uint)(tint | (tint << 8) | (tint << 16) | (255 << 24));
    }

    public static AABB fullBlockAABB() {
        return new AABB(new Vector3D(0, 0, 0), new Vector3D(1, 1, 1));
    }

    public Block flowerAABB() {
        var offset = 6 / 16f;
        AABB[id] = new AABB(new Vector3D(0 + offset, 0, 0 + offset), new Vector3D(1 - offset, 0.5, 1 - offset));
        return this;
    }

    public Block shortGrassAABB() {
        var offset = 4 / 16f;
        AABB[id] = new AABB(new Vector3D(0, 0, 0), new Vector3D(1, offset, 1));
        return this;
    }

    public Block torchAABB() {
        var offset = 6 / 16f;
        AABB[id] = new AABB(new Vector3D(0 + offset, 0, 0 + offset), new Vector3D(1 - offset, 1, 1 - offset));
        noCollision();
        return this;
    }

    public Block(ushort id, string name) {
        this.id = id;
        this.name = name;
        
        fullBlock[id] = true;
        waterSolid[id] = true;
        selection[id] = true;
        collision[id] = true;
        liquid[id] = false;
        customCulling[id] = false;
        randomTick[id] = false;

        AABB[id] = fullBlockAABB();
    }

    public Block setModel(BlockModel model) {
        this.model = model;
        renderType[id] = RenderType.MODEL;
        return this;
    }
    
    public Block setTex(UVPair[] uvs) {
        this.uvs = uvs;
        return this;
    }
    
    public Block setTex(params ReadOnlySpan<UVPair> uvs) {
        this.uvs = uvs.ToArray();
        return this;
    }

    public Block transparency() {
        transparent[id] = true;
        fullBlock[id] = false;
        return this;
    }

    public Block translucency() {
        layer = RenderLayer.TRANSLUCENT;
        translucent[id] = true;
        fullBlock[id] = false;
        return this;
    }
    
    public Block waterTransparent() {
        waterSolid[id] = false;
        return this;
    }

    public Block noCollision() {
        collision[id] = false;
        //AABB[id] = null;
        return this;
    }

    public Block noSelection() {
        selection[id] = false;
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
        waterTransparent();
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
    
    public Block setLightAbsorption(byte amount) {
        lightAbsorption[id] = amount;
        return this;
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

    public Block itemLike() {
        renderItemLike[id] = true;
        return this;
    }

    public Block setHardness(double hardness) {
        Block.hardness[id] = hardness;
        return this;
    }
    
    // CUSTOM BEHAVIOURS
    
    /**
     * This is a fucking mess but the alternative is making an even worse mess. There are 4 distinct update types -
     * for (neighbour) updates, for scheduled updates (delayed), for random updates (if randomTick is true and player is close enough), and for render updates (if renderTick is true and the player is nearby).
     *
     * If you don't want to copypaste code, I'd recommend making a custom method and calling that from the relevant update methods, maybe with some bool parameters.
     * The alternative would have been to have a single update method with a flag enum, but that would have been a nightmare to use and EVEN MORE SPAGHETTI
     */
    
    /**
     * Coords are for the updated block.
     */
    public virtual void update(World world, int x, int y, int z) {

    }
    
    /**
     * Only called when this is a delayed update!
     */
    public virtual void scheduledUpdate(World world, int x, int y, int z) {

    }
    
    /**
     * Only called when you don't want it! (i.e. randomly)
     */
    public virtual void randomUpdate(World world, int x, int y, int z) {

    }
    
    /**
     * This should have been called renderTick but that name already existed, oh well
     * Called around the player frequently for blocks that need it (if you want to do particle effects or some fancy shit)
     */
    [ClientOnly]
    public virtual void renderUpdate(World world, int x, int y, int z) {

    }
    
    public virtual void interact(World world, int x, int y, int z, Entity e) {
        
    }
    
    public virtual Vector3D push(World world, int x, int y, int z, Entity e) {
        return Vector3D.Zero;
    }
    
    [ClientOnly]
    public virtual void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        // setup
        br.setupWorld();
    }
    
    /**
     * Called after the block has been set in the world.
     */
    public virtual void onPlace(World world, int x, int y, int z, byte metadata) {
        
    }
    
    /**
    * Called after the block is removed from the world.
     */
    public virtual void onBreak(World world, int x, int y, int z, byte metadata) {
        
    }

    public virtual void crack(World world, int x, int y, int z) {

        UVPair uv;
        
        if (model == null || model.faces.Length == 0) {
            // no model, no particles

            // unless there's textures!
            
            // UNLESS it's custom texture
            var custom = renderType[id] == RenderType.CUSTOM || renderType[id] == RenderType.CUBE_DYNTEXTURE;
            if (!custom && (uvs == null || uvs.Length == 0)) {
                return;
            }
        }
        
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

                    switch (renderType[id]) {
                        // if custom texture, get that
                        case RenderType.CUBE_DYNTEXTURE:
                        case RenderType.CUSTOM:
                            var meta = world.getBlockMetadata(x, y, z);
                            uv = getTexture(0, meta);
                            break;
                        case RenderType.MODEL:
                            uv = uvs[Game.clientRandom.Next(0, uvs.Length)];
                            break;
                        default:
                            // no model, just textures
                            uv = uvs[0];
                            break;
                    }

                    float u = UVPair.texU(uv.u + Game.clientRandom.NextSingle() * 0.75f); 
                    float v = UVPair.texV(uv.v + Game.clientRandom.NextSingle() * 0.75f);

                    // the closer to the centre, the less the motion
                    // dx gives a number between -0.5 and 0.5 -> remap to between 0.5 and 3
                    var dx = (particleX - x - 0.5f);
                    var dy = (particleY - y - 0.5f);
                    var dz = (particleZ - z - 0.5f);


                    // between -0.7 and 0.7
                    var motion = new Vector3(dx * 3 + (Game.clientRandom.NextSingle() - 0.5f) * 0.2f,
                        dy * 3 + (Game.clientRandom.NextSingle() - 0.5f) * 0.2f,
                        dz * 3 + (Game.clientRandom.NextSingle() - 0.5f) * 0.2f);

                    var s = Game.clientRandom.NextSingle();
                    s *= s;
                    var speed = (s + 1) * 0.8f;

                    motion *= speed;
                    motion.Y += 0.15f;

                    var particle = new Particle(
                        world,
                        particlePosition);
                    particle.texture = "textures/blocks.png";
                    particle.u = u;
                    particle.v = v;
                    particle.size = new Vector2(size);
                    particle.uvsize = new Vector2(1 / 16f * size);
                    particle.maxAge = ttl;
                    world.particles.add(particle);

                    particle.velocity = motion.toVec3D();
                }
            }
        }
    }
    
    /**
     * Returns whether a face should be rendered.
     */
    public virtual bool cullFace(BlockRenderer br, int x, int y, int z, RawDirection dir) {
        
        // if none, always render
        if (dir == RawDirection.NONE) {
            return true;
        }
        
        var direction = Direction.getDirection(dir);
        var neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();
        
        // if it's not a full block, we render the face
        return !fullBlock[neighbourBlock];
    }

    public virtual void place(World world, int x, int y, int z, byte metadata, RawDirection dir) {
        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(metadata));
        world.blockUpdateNeighbours(x, y, z);
    }
    
    public virtual void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        if (!AABB[id].HasValue || !collision[id]) {
            return;
        }
        var aabb = AABB[id]!.Value;
        aabbs.Add(new AABB((float)(x + aabb.min.X), (float)(y + aabb.min.Y), (float)(z + aabb.min.Z),
                           (float)(x + aabb.max.X), (float)(y + aabb.max.Y), (float)(z + AABB[id]!.Value.max.Z)));
    }
    
    /**
     * Check if this block can be placed at the given position.
     * Entity collision checking is handled by the placement method.
     * Override for block-specific placement rules.
     */
    public virtual bool canPlace(World world, int x, int y, int z, RawDirection dir) {
        // by default, non-collidable blocks can be replaced
        return !collision[world.getBlock(x, y, z)];
    }
    
    /**
     * Returns the maximum valid metadata value for this block type.
     * Default implementation returns 0 (no metadata variants).
     */
    public virtual byte maxValidMetadata() {
        return 0;
    }
    
    /**
     * Returns the texture for a specific face index and metadata.
     * Default implementation returns the static texture.
     * Override for dynamic texture selection based on metadata.
     */
    public virtual UVPair getTexture(int faceIdx, int metadata) {
        return uvs?[Math.Min(faceIdx, uvs.Length - 1)] ?? new UVPair(0, 0);
    }
}

public static class BlockExtensions {
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort getID(this uint block) {
        return (ushort)(block & 0xFFFFFF);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte getMetadata(this uint block) {
        return (byte)(block >> 24);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint setMetadata(this uint block, byte metadata) {
        return (block & 0xFFFFFF) | ((uint)metadata << 24);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint setID(this uint block, ushort id) {
        return (block & 0xFF000000) | id;
    }
}

public class Flower(ushort id, string name) : Block(id, name) {

    public override void update(World world, int x, int y, int z) {
        if (world.inWorld(x, y - 1, z) && world.getBlock(x, y - 1, z) == 0) {
            world.setBlock(x, y, z, Blocks.AIR);
        }
    }
}

public class FallingBlock(ushort id, string name) : Block(id, name) {
    public override void update(World world, int x, int y, int z) {
        var ym = y - 1;
        bool isSupported = true;
        // if not supported, set flag
        while (world.getBlock(new Vector3I(x, ym, z)) == 0) {
            // decrement Y
            isSupported = false;
            ym--;
        }
        if (!isSupported) {
            world.setBlock(x, y, z, 0);
            world.setBlock(x, ym + 1,z, getID());
        }

        // if sand above, update
        if (world.getBlock(new Vector3I(x, y + 1, z)) == getID()) {
            // if you do an update immediately, it will cause a stack overflow lol
            world.scheduleBlockUpdate(new Vector3I(x, y + 1, z), 1);
        }
    }

    public override void scheduledUpdate(World world, int x, int y, int z) {
        // run a normal update
        update(world, x, y, z);
    }
}

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
    
    public static Vector2 texCoords(BTexture2D tex, UVPair uv) {
        return new Vector2(uv.u / tex.width, uv.v / tex.height);
    }
    
    public static float texU(BTexture2D tex, float u) {
        return u / tex.width;
    }
    
    public static float texV(BTexture2D tex, float v) {
        return v / tex.height;
    }

    public static int texUI(float u) {
        return (int)(u * ATLASSIZE);
    }

    public static int texUI(BTexture2D tex, float u) {
        return (int)(u * tex.width);
    }

    public static int texVI(float v) {
        return (int)(v * ATLASSIZE);
    }

    public static int texVI(BTexture2D tex, float v) {
        return (int)(v * tex.height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float texU(float u) {
        return u * Block.atlasRatio;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float texV(float v) {
        return v * Block.atlasRatio;
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
    public readonly byte flags = (byte)(nonFullFace.toByte() | noAO.toByte() << 1);

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
    TRANSLUCENT
}

public enum RenderType : byte {
    CUBE,
    MODEL,
    CROSS,
    CUSTOM,
    CUBE_DYNTEXTURE
}