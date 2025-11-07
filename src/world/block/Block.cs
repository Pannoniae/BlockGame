using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.item;
using Vector3D = Molten.DoublePrecision.Vector3D;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace BlockGame.world.block;

/**
 * For now, we'll only have 65536 blocks for typechecking (ushort -> uint), this can be extended later.
 */
[SuppressMessage("Compiler",
    "CS8618:Non-nullable field must contain a non-null value when exiting constructor. Consider adding the \'required\' modifier or declaring as nullable.")]
public class Block {
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

    /// <summary>
    /// Display name
    /// </summary>
    public string name;

    public BlockItem item;

    /// <summary>
    /// Block material - defines tool requirements, hardness, and sound
    /// </summary>
    public Material mat;

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

    public const int atlasSize = 512;
    public const int textureSize = 16;


    public const float atlasRatio = textureSize / (float)atlasSize;
    public const float atlasRatioInv = 1 / atlasRatio;


    public static Block AIR;
    public static Block GRASS;
    public static Block DIRT;
    public static Block SAND;
    public static Block BASALT;
    public static Block STONE;
    public static Block COBBLESTONE;
    public static Block GRAVEL;
    public static Block HELLSTONE;
    public static Block BLOODSTONE;
    public static Block HELLROCK;
    public static Block INFERNO_ROCK;
    public static Block GLASS;
    public static Block CALCITE;
    public static Block CLAY_BLOCK;
    public static Block BRICK_BLOCK;
    public static Block STONE_BRICK;
    public static Block SAND_BRICK;
    public static Block GOLD_CANDY;
    public static Block CINNABAR_CANDY;
    public static Block DIAMOND_CANDY;
    public static Block LAVA;

    public static Block LANTERN;

    public static Block TALL_GRASS;

    public static Block SHORT_GRASS;

    public static Block YELLOW_FLOWER;

    //public static Block RED_FLOWER;
    public static Block MARIGOLD;
    public static Block BLUE_TULIP;
    public static Block THISTLE;

    public static Block OAK_LOG;
    public static Block OAK_PLANKS;
    public static Block OAK_STAIRS;
    public static Block OAK_SLAB;
    public static Block LEAVES;
    public static Block OAK_SAPLING;

    public static Block MAPLE_LOG;
    public static Block MAPLE_PLANKS;
    public static Block MAPLE_STAIRS;
    public static Block MAPLE_SLAB;
    public static Block MAPLE_LEAVES;
    public static Block MAPLE_SAPLING;

    public static Block MAHOGANY_LOG;
    public static Block MAHOGANY_PLANKS;
    public static Block MAHOGANY_STAIRS;
    public static Block MAHOGANY_SLAB;
    public static Block MAHOGANY_LEAVES;
    public static Block MAHOGANY_SAPLING;

    public static Block STONE_SLAB;
    public static Block COBBLESTONE_SLAB;

    public static Block OAK_CHEST;
    public static Block DOOR;
    public static Block MAHOGANY_DOOR;

    public static Block CANDY;

    public static Block HEAD;

    public static Block WATER;

    public static Block CINNABAR_ORE;
    public static Block TITANIUM_ORE;
    public static Block AMBER_ORE;
    public static Block AMETHYST_ORE;
    public static Block EMERALD_ORE;
    public static Block DIAMOND_ORE;
    public static Block GOLD_ORE;
    public static Block IRON_ORE;
    public static Block COAL_ORE;
    public static Block COPPER_ORE;
    public static Block TIN_ORE;
    public static Block SILVER_ORE;

    public static Block TORCH;
    public static Block CRAFTING_TABLE;
    public static Block MAHOGANY_CHEST;
    public static Block BRICK_FURNACE;
    public static Block FURNACE;
    public static Block LADDER;
    public static Block FIRE;
    public static Block SIGN;

    // Compatibility wrappers for old static arrays
    public static XUList<Block> blocks => Registry.BLOCKS.values;
    public static XUList<bool> fullBlock => Registry.BLOCKS.fullBlock;
    public static XUList<bool> transparent => Registry.BLOCKS.transparent;
    public static XUList<bool> translucent => Registry.BLOCKS.translucent;
    public static XUList<bool> waterSolid => Registry.BLOCKS.waterSolid;
    public static XUList<bool> lavaSolid => Registry.BLOCKS.lavaSolid;
    public static XUList<bool> randomTick => Registry.BLOCKS.randomTick;
    public static XUList<bool> renderTick => Registry.BLOCKS.renderTick;
    public static XUList<bool> liquid => Registry.BLOCKS.liquid;
    public static XUList<bool> customCulling => Registry.BLOCKS.customCulling;
    public static XUList<bool> renderItemLike => Registry.BLOCKS.renderItemLike;
    public static XUList<bool> selection => Registry.BLOCKS.selection;
    public static XUList<bool> collision => Registry.BLOCKS.collision;
    public static XUList<byte> lightLevel => Registry.BLOCKS.lightLevel;
    public static XUList<byte> lightAbsorption => Registry.BLOCKS.lightAbsorption;
    public static XUList<double> hardness => Registry.BLOCKS.hardness;
    public static XUList<double> flammable => Registry.BLOCKS.flammable;
    public static XUList<bool> log => Registry.BLOCKS.log;
    public static XUList<bool> leaves => Registry.BLOCKS.leaves;
    public static XUList<byte> updateDelay => Registry.BLOCKS.updateDelay;
    public static XUList<AABB?> AABB => Registry.BLOCKS.AABB;
    public static XUList<bool> customAABB => Registry.BLOCKS.customAABB;
    public static XUList<RenderType> renderType => Registry.BLOCKS.renderType;
    public static XUList<ToolType> tool => Registry.BLOCKS.tool;
    public static XUList<MaterialTier> tier => Registry.BLOCKS.tier;

    public static XUList<bool> noItem => Registry.BLOCKS.noItem;

    public static XUList<bool> isBlockEntity => Registry.BLOCKS.isBlockEntity;

    public static int currentID => Registry.BLOCKS.count();

    public static Block register(string stringID, Block block) {
        int id = Registry.BLOCKS.register(stringID, block);
        block.id = (ushort)id; // assign runtime ID to block
        block.onRegister(id); // call hook after ID assignment

        // auto-register corresponding BlockItem with same string ID

        if (noItem[block.id]) {
            return block;
        }

        var blockItem = block.createItem();
        Item.register(stringID, blockItem);
        Item.material[blockItem.id] = true; // all blocks are materials lol

        block.item = blockItem;

        return block;
    }

    /**
     * Called after the block has been registered and assigned an ID.
     * Override to set block properties that require the ID.
     */
    protected virtual void onRegister(int id) {
    }

    /** Override to create a custom BlockItem type for this block */
    protected virtual BlockItem createItem() {
        return new BlockItem(this);
    }

    /** Get the BlockItem for this block */
    public BlockItem getItem() {
        return item;
    }

    /**
     * Get the actual item associated with this block.
     * For example, it gets the door item for door blocks and NOT the technical door block item.
     *
     * This is used for block picking and hopefully more things soon?
     */
    public virtual ItemStack getActualItem(byte metadata) {
        return new ItemStack(item, 1, metadata);
    }

    public static Block? get(int id) {
        return Registry.BLOCKS.getOrDefault(id, null!);
    }

    public static Block? get(string id) {
        return Registry.BLOCKS.getOrDefault(id, null!);
    }

    public static void preLoad() {
        AIR = register("air", new Block("Air"));
        AIR.setModel(BlockModel.emptyBlock());
        AIR.air();
        GRASS = register("grass", new GrassBlock("Grass"));
        GRASS.tick();
        GRASS.setTex(grassUVs(0, 0, 1, 0, 2, 0));
        GRASS.setModel(BlockModel.makeCube(GRASS));
        GRASS.material(Material.EARTH);

        DIRT = register("dirt", new Block("Dirt"));
        DIRT.setTex(cubeUVs(2, 0));
        renderType[DIRT.id] = RenderType.CUBE;
        DIRT.material(Material.EARTH);

        SAND = register("sand", new FallingBlock("Sand"));
        SAND.setTex(cubeUVs(3, 0));
        renderType[SAND.id] = RenderType.CUBE;
        SAND.material(Material.EARTH);
        // less hard than dirt!
        SAND.setHardness(0.5);

        BASALT = register("basalt", new Block("Basalt"));
        BASALT.setTex(cubeUVs(4, 0));
        renderType[BASALT.id] = RenderType.CUBE;
        BASALT.material(Material.STONE);

        STONE = register("stone", new Block("Stone"));
        STONE.setTex(cubeUVs(5, 0));
        renderType[STONE.id] = RenderType.CUBE;
        STONE.material(Material.STONE);

        COBBLESTONE = register("cobblestone", new Block("Cobblestone"));
        COBBLESTONE.setTex(cubeUVs(6, 1));
        //renderType[COBBLESTONE.id] = RenderType.CUBE;
        COBBLESTONE.setModel(BlockModel.makeCube(COBBLESTONE));
        COBBLESTONE.material(Material.STONE);

        GRAVEL = register("gravel", new Block("Gravel"));
        GRAVEL.setTex(cubeUVs(7, 0));
        renderType[GRAVEL.id] = RenderType.CUBE;
        GRAVEL.material(Material.EARTH);

        HELLSTONE = register("hellstone", new Block("Hellstone"));
        HELLSTONE.setTex(cubeUVs(8, 0));
        renderType[HELLSTONE.id] = RenderType.CUBE;
        HELLSTONE.light(15);
        HELLSTONE.material(Material.HELL);

        BLOODSTONE = register("bloodstone", new Block("Bloodstone"));
        BLOODSTONE.setTex(cubeUVs(8, 1));
        renderType[BLOODSTONE.id] = RenderType.CUBE;
        BLOODSTONE.material(Material.HELL);

        HELLROCK = register("hellrock", new Block("Hellrock"));
        HELLROCK.setTex(cubeUVs(9, 0));
        renderType[HELLROCK.id] = RenderType.CUBE;
        HELLROCK.material(Material.HELL);

        INFERNO_ROCK = register("infernoRock", new Block("Inferno Rock"));
        INFERNO_ROCK.setTex(cubeUVs(10, 0));
        renderType[INFERNO_ROCK.id] = RenderType.CUBE;
        INFERNO_ROCK.material(Material.HELL);

        GLASS = register("glass", new Block("Glass"));
        GLASS.setTex(cubeUVs(6, 0));
        renderType[GLASS.id] = RenderType.CUBE;
        GLASS.transparency();
        GLASS.material(Material.GLASS);

        CALCITE = register("calcite", new Block("Calcite"));
        CALCITE.setTex(cubeUVs(11, 0));
        renderType[CALCITE.id] = RenderType.CUBE;
        CALCITE.material(Material.STONE);

        CLAY_BLOCK = register("clayBlock", new Block("Clay Block"));
        CLAY_BLOCK.setTex(cubeUVs(12, 0));
        renderType[CLAY_BLOCK.id] = RenderType.CUBE;
        CLAY_BLOCK.material(Material.EARTH);

        BRICK_BLOCK = register("brickBlock", new Block("Brick Block"));
        BRICK_BLOCK.setTex(cubeUVs(0, 2));
        renderType[BRICK_BLOCK.id] = RenderType.CUBE;
        BRICK_BLOCK.material(Material.STONE);

        STONE_BRICK = register("stoneBrick", new Block("Stone Brick"));
        STONE_BRICK.setTex(cubeUVs(1, 2));
        renderType[STONE_BRICK.id] = RenderType.CUBE;
        STONE_BRICK.material(Material.STONE);

        SAND_BRICK = register("sandBrick", new Block("Sand Brick"));
        SAND_BRICK.setTex(cubeUVs(2, 2));
        renderType[SAND_BRICK.id] = RenderType.CUBE;
        SAND_BRICK.material(Material.STONE);

        GOLD_CANDY = register("goldCandy", new Block("Gold Candy"));
        GOLD_CANDY.setTex(cubeUVs(0, 8));
        renderType[GOLD_CANDY.id] = RenderType.CUBE;
        GOLD_CANDY.material(Material.METAL);

        CINNABAR_CANDY = register("cinnabarCandy", new Block("Cinnabar Candy"));
        CINNABAR_CANDY.setTex(cubeUVs(1, 8));
        renderType[CINNABAR_CANDY.id] = RenderType.CUBE;
        CINNABAR_CANDY.material(Material.METAL);

        DIAMOND_CANDY = register("diamondCandy", new Block("Diamond Candy"));
        DIAMOND_CANDY.setTex(cubeUVs(2, 8));
        renderType[DIAMOND_CANDY.id] = RenderType.CUBE;
        DIAMOND_CANDY.material(Material.METAL);

        LANTERN = register("lantern", new Block("Lantern"));
        LANTERN.setTex(new UVPair(6, 3), new UVPair(7, 3), new UVPair(8, 3));
        LANTERN.setModel(BlockModel.makeLantern(LANTERN));
        LANTERN.light(15);
        LANTERN.partialBlock();
        LANTERN.material(Material.METAL);

        TALL_GRASS = register("tallGrass", new Grass("Tall Grass"));
        TALL_GRASS.setTex(crossUVs(11, 5));
        TALL_GRASS.setModel(BlockModel.makeGrass(TALL_GRASS));
        TALL_GRASS.transparency();
        TALL_GRASS.noCollision();
        TALL_GRASS.waterTransparent();
        TALL_GRASS.material(Material.ORGANIC);
        TALL_GRASS.setHardness(0);
        TALL_GRASS.setFlammable(80);

        SHORT_GRASS = register("shortGrass", new Grass("Short Grass"));
        SHORT_GRASS.setTex(crossUVs(10, 5));
        SHORT_GRASS.setModel(BlockModel.makeGrass(SHORT_GRASS));
        SHORT_GRASS.transparency();
        SHORT_GRASS.shortGrassAABB();
        SHORT_GRASS.noCollision();
        SHORT_GRASS.waterTransparent();
        SHORT_GRASS.material(Material.ORGANIC);
        SHORT_GRASS.setHardness(0);
        SHORT_GRASS.setFlammable(80);

        YELLOW_FLOWER = register("yellowFlower", new Flower("Yellow Flower"));
        YELLOW_FLOWER.setTex(crossUVs(15, 5));
        YELLOW_FLOWER.setModel(BlockModel.makeGrass(YELLOW_FLOWER));
        YELLOW_FLOWER.transparency();
        YELLOW_FLOWER.flowerAABB();
        YELLOW_FLOWER.noCollision();
        YELLOW_FLOWER.waterTransparent();
        YELLOW_FLOWER.itemLike();
        YELLOW_FLOWER.setFlammable(60);

        //RED_FLOWER = register("redFlower", new Flower("Red Flower"));
        //RED_FLOWER.setTex(crossUVs(11, 1));
        //RED_FLOWER.setModel(BlockModel.makeGrass(RED_FLOWER));
        //RED_FLOWER.transparency();
        //RED_FLOWER.flowerAABB();
        //RED_FLOWER.noCollision();
        //RED_FLOWER.waterTransparent();
        //RED_FLOWER.itemLike();
        //RED_FLOWER.material(Material.ORGANIC);

        MARIGOLD = register("marigold", new Flower("Marigold"));
        MARIGOLD.setTex(crossUVs(12, 5));
        MARIGOLD.setModel(BlockModel.makeGrass(MARIGOLD));
        MARIGOLD.transparency();
        MARIGOLD.flowerAABB();
        MARIGOLD.noCollision();
        MARIGOLD.waterTransparent();
        MARIGOLD.itemLike();
        MARIGOLD.setFlammable(60);

        // hehe
        BLUE_TULIP = register("blueTulip", new Flower("Blue Tulip"));
        BLUE_TULIP.setTex(crossUVs(13, 5));
        BLUE_TULIP.setModel(BlockModel.makeGrass(BLUE_TULIP));
        BLUE_TULIP.transparency();
        BLUE_TULIP.flowerAABB();
        BLUE_TULIP.noCollision();
        BLUE_TULIP.waterTransparent();
        BLUE_TULIP.itemLike();
        BLUE_TULIP.setFlammable(60);

        THISTLE = register("thistle", new Flower("Thistle"));
        THISTLE.setTex(crossUVs(14, 5));
        THISTLE.setModel(BlockModel.makeGrass(THISTLE));
        THISTLE.transparency();
        // slightly larger aabb..
        AABB[THISTLE.id] = new AABB(new Vector3D(0 + 4 / 16f, 0, 0 + 4 / 16f), new Vector3D(1 - 4 / 16f, 1f, 1 - 4 / 16f));
        THISTLE.noCollision();
        THISTLE.waterTransparent();
        THISTLE.itemLike();
        THISTLE.setFlammable(60);

        OAK_LOG = register("oakLog", new Block("Oak Log"));
        OAK_LOG.setTex(grassUVs(2, 5, 1, 5, 3, 5));
        OAK_LOG.setModel(BlockModel.makeCube(OAK_LOG));
        OAK_LOG.material(Material.WOOD);
        log[OAK_LOG.id] = true;
        OAK_LOG.setFlammable(5);

        OAK_PLANKS = register("oakPlanks", new Block("Oak Planks"));
        OAK_PLANKS.setTex(cubeUVs(0, 5));
        renderType[OAK_PLANKS.id] = RenderType.CUBE;
        OAK_PLANKS.material(Material.WOOD);
        OAK_PLANKS.setFlammable(30);

        OAK_STAIRS = register("oakStairs", new Stairs("Oak Stairs"));
        OAK_STAIRS.setTex(cubeUVs(0, 5));
        OAK_STAIRS.partialBlock();
        OAK_STAIRS.material(Material.WOOD);
        OAK_STAIRS.setFlammable(30);

        OAK_SLAB = register("oakSlab", new Slabs("Planks Slab"));
        OAK_SLAB.setTex(cubeUVs(0, 5));
        OAK_SLAB.material(Material.WOOD);
        OAK_SLAB.setFlammable(30);

        LEAVES = register("oakLeaves", new Leaves("Oak Leaves"));
        LEAVES.setTex(cubeUVs(4, 5));
        renderType[LEAVES.id] = RenderType.CUBE;
        LEAVES.transparency();
        LEAVES.setLightAbsorption(1);
        leaves[LEAVES.id] = true;
        LEAVES.setFlammable(60);

        OAK_SAPLING = register("oakSapling", new Sapling("Oak Sapling", SaplingType.OAK));
        OAK_SAPLING.setTex(crossUVs(16, 5));
        OAK_SAPLING.setModel(BlockModel.makeGrass(OAK_SAPLING));
        OAK_SAPLING.transparency();
        OAK_SAPLING.noCollision();
        OAK_SAPLING.waterTransparent();
        OAK_SAPLING.itemLike();
        OAK_SAPLING.material(Material.ORGANIC);
        OAK_SAPLING.setFlammable(60);

        MAPLE_LOG = register("mapleLog", new Block("Maple Log"));
        MAPLE_LOG.setTex(grassUVs(7, 5, 6, 5, 8, 5));
        MAPLE_LOG.setModel(BlockModel.makeCube(MAPLE_LOG));
        MAPLE_LOG.material(Material.WOOD);
        log[MAPLE_LOG.id] = true;
        MAPLE_LOG.setFlammable(5);

        MAPLE_PLANKS = register("maplePlanks", new Block("Maple Planks"));
        MAPLE_PLANKS.setTex(cubeUVs(5, 5));
        renderType[MAPLE_PLANKS.id] = RenderType.CUBE;
        MAPLE_PLANKS.material(Material.WOOD);
        MAPLE_PLANKS.setFlammable(30);

        MAPLE_STAIRS = register("mapleStairs", new Stairs("Maple Stairs"));
        MAPLE_STAIRS.setTex(cubeUVs(5, 5));
        MAPLE_STAIRS.partialBlock();
        MAPLE_STAIRS.material(Material.WOOD);
        MAPLE_STAIRS.setFlammable(30);

        MAPLE_SLAB = register("mapleSlab", new Slabs("Maple Planks Slab"));
        MAPLE_SLAB.setTex(cubeUVs(5, 5));
        MAPLE_SLAB.material(Material.WOOD);
        MAPLE_SLAB.setFlammable(30);

        MAPLE_LEAVES = register("mapleLeaves", new Leaves("Maple Leaves"));
        MAPLE_LEAVES.setTex(cubeUVs(9, 5));
        renderType[MAPLE_LEAVES.id] = RenderType.CUBE;
        MAPLE_LEAVES.transparency();
        leaves[MAPLE_LEAVES.id] = true;
        MAPLE_LEAVES.setFlammable(60);

        MAPLE_SAPLING = register("mapleSapling", new Sapling("Maple Sapling", SaplingType.MAPLE));
        MAPLE_SAPLING.setTex(crossUVs(17, 5));
        MAPLE_SAPLING.setModel(BlockModel.makeGrass(MAPLE_SAPLING));
        MAPLE_SAPLING.transparency();
        MAPLE_SAPLING.noCollision();
        MAPLE_SAPLING.waterTransparent();
        MAPLE_SAPLING.itemLike();
        MAPLE_SAPLING.material(Material.ORGANIC);
        MAPLE_SAPLING.setFlammable(60);

        MAHOGANY_LOG = register("mahoganyLog", new Block("Mahogany Log"));
        MAHOGANY_LOG.setTex(grassUVs(5, 2, 4, 2, 6, 2));
        MAHOGANY_LOG.setModel(BlockModel.makeCube(MAHOGANY_LOG));
        MAHOGANY_LOG.material(Material.WOOD);
        log[MAHOGANY_LOG.id] = true;
        MAHOGANY_LOG.setFlammable(5);

        MAHOGANY_PLANKS = register("mahoganyPlanks", new Block("Mahogany Planks"));
        MAHOGANY_PLANKS.setTex(cubeUVs(3, 2));
        renderType[MAHOGANY_PLANKS.id] = RenderType.CUBE;
        MAHOGANY_PLANKS.material(Material.WOOD);
        MAHOGANY_PLANKS.setFlammable(30);

        MAHOGANY_STAIRS = register("mahoganyStairs", new Stairs("Mahogany Stairs"));
        MAHOGANY_STAIRS.setTex(cubeUVs(3, 2));
        MAHOGANY_STAIRS.partialBlock();
        MAHOGANY_STAIRS.material(Material.WOOD);
        MAHOGANY_STAIRS.setFlammable(30);

        MAHOGANY_SLAB = register("mahoganySlab", new Slabs("Mahogany Slab"));
        MAHOGANY_SLAB.setTex(cubeUVs(3, 2));
        MAHOGANY_SLAB.material(Material.WOOD);
        MAHOGANY_SLAB.setFlammable(30);

        MAHOGANY_LEAVES = register("mahoganyLeaves", new Leaves("Mahogany Leaves"));
        MAHOGANY_LEAVES.setTex(cubeUVs(7, 2));
        renderType[MAHOGANY_LEAVES.id] = RenderType.CUBE;
        MAHOGANY_LEAVES.transparency();
        leaves[MAHOGANY_LEAVES.id] = true;
        MAHOGANY_LEAVES.setFlammable(60);

        MAHOGANY_SAPLING = register("mahoganySapling", new Sapling("Mahogany Sapling", SaplingType.MAHOGANY));
        MAHOGANY_SAPLING.setTex(crossUVs(18, 5));
        MAHOGANY_SAPLING.setModel(BlockModel.makeGrass(MAHOGANY_SAPLING));
        MAHOGANY_SAPLING.transparency();
        MAHOGANY_SAPLING.noCollision();
        MAHOGANY_SAPLING.waterTransparent();
        MAHOGANY_SAPLING.itemLike();
        MAHOGANY_SAPLING.material(Material.ORGANIC);
        MAHOGANY_SAPLING.setFlammable(60);


        STONE_SLAB = register("stoneSlab", new Slabs("Stone Slab"));
        STONE_SLAB.setTex(cubeUVs(5, 0));
        STONE_SLAB.material(Material.STONE);

        COBBLESTONE_SLAB = register("cobblestoneSlab", new Slabs("Cobblestone Slab"));
        COBBLESTONE_SLAB.setTex(cubeUVs(6, 1));
        COBBLESTONE_SLAB.material(Material.STONE);

        //ide mas kovekbol is SLAB!
        //utana stone es mas kovekbol keszult STAIR!

        CANDY = register("candy", new CandyBlock("Candy"));
        CANDY.material(Material.FOOD);

        HEAD = register("head", new Block("Head"));
        HEAD.setTex(HeadUVs(0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3));
        HEAD.setModel(BlockModel.makeHalfCube(HEAD));
        HEAD.partialBlock();

        WATER = register("water", new Liquid("Water", 15, 8, false));
        WATER.setTex(new UVPair(0, 13), new UVPair(1, 14));
        WATER.makeLiquid();

        LAVA = register("lava", new Liquid("Lava", 30, 4, true));
        LAVA.setTex(new UVPair(0, 16), new UVPair(1, 17));
        LAVA.makeLiquid();

        // idk the tiers, these are just placeholders!! stop looking at my ore class lmao

        CINNABAR_ORE = register("cinnabarOre", new Block("Cinnabar"));
        CINNABAR_ORE.setTex(cubeUVs(10, 1));
        renderType[CINNABAR_ORE.id] = RenderType.CUBE;
        CINNABAR_ORE.material(Material.FANCY_STONE);
        CINNABAR_ORE.setHardness(6.0);
        CINNABAR_ORE.setTier(MaterialTier.GOLD);

        TITANIUM_ORE = register("titaniumOre", new Block("Titanium Ore"));
        TITANIUM_ORE.setTex(cubeUVs(11, 1));
        renderType[TITANIUM_ORE.id] = RenderType.CUBE;
        TITANIUM_ORE.material(Material.FANCY_STONE);
        TITANIUM_ORE.setHardness(7.5);
        TITANIUM_ORE.setTier(MaterialTier.GOLD);

        AMBER_ORE = register("amberOre", new Block("Amber Ore"));
        AMBER_ORE.setTex(cubeUVs(12, 1));
        renderType[AMBER_ORE.id] = RenderType.CUBE;
        AMBER_ORE.material(Material.FANCY_STONE);
        AMBER_ORE.setHardness(3.0);
        AMBER_ORE.setTier(MaterialTier.STONE);

        AMETHYST_ORE = register("amethystOre", new Block("Amethyst Ore"));
        AMETHYST_ORE.setTex(cubeUVs(13, 1));
        renderType[AMETHYST_ORE.id] = RenderType.CUBE;
        AMETHYST_ORE.material(Material.FANCY_STONE);
        AMETHYST_ORE.setHardness(4.0);
        AMETHYST_ORE.setTier(MaterialTier.IRON);

        EMERALD_ORE = register("emeraldOre", new Block("Emerald Ore"));
        EMERALD_ORE.setTex(cubeUVs(14, 1));
        renderType[EMERALD_ORE.id] = RenderType.CUBE;
        EMERALD_ORE.material(Material.FANCY_STONE);
        EMERALD_ORE.setHardness(5.0);
        EMERALD_ORE.setTier(MaterialTier.GOLD);

        DIAMOND_ORE = register("diamondOre", new Block("Diamond Ore"));
        DIAMOND_ORE.setTex(cubeUVs(15, 1));
        renderType[DIAMOND_ORE.id] = RenderType.CUBE;
        DIAMOND_ORE.material(Material.FANCY_STONE);
        DIAMOND_ORE.setHardness(4.0);
        DIAMOND_ORE.setTier(MaterialTier.GOLD);

        GOLD_ORE = register("goldOre", new Block("Gold Ore"));
        GOLD_ORE.setTex(cubeUVs(0, 1));
        renderType[GOLD_ORE.id] = RenderType.CUBE;
        GOLD_ORE.material(Material.FANCY_STONE);
        GOLD_ORE.setHardness(3.0);
        GOLD_ORE.setTier(MaterialTier.IRON);

        IRON_ORE = register("ironOre", new Block("Iron Ore"));
        IRON_ORE.setTex(cubeUVs(1, 1));
        renderType[IRON_ORE.id] = RenderType.CUBE;
        IRON_ORE.material(Material.FANCY_STONE);
        IRON_ORE.setHardness(3.0);
        IRON_ORE.setTier(MaterialTier.STONE);

        COPPER_ORE = register("copperOre", new Block("Copper Ore"));
        COPPER_ORE.setTex(cubeUVs(5, 1));
        renderType[COPPER_ORE.id] = RenderType.CUBE;
        COPPER_ORE.material(Material.FANCY_STONE);
        COPPER_ORE.setHardness(2.5);
        COPPER_ORE.setTier(MaterialTier.STONE);

        COAL_ORE = register("coalOre", new Block("Coal Ore"));
        COAL_ORE.setTex(cubeUVs(4, 1));
        renderType[COAL_ORE.id] = RenderType.CUBE;
        COAL_ORE.material(Material.FANCY_STONE);
        COAL_ORE.setHardness(2.0);
        COAL_ORE.setTier(MaterialTier.WOOD);

        TIN_ORE = register("tinOre", new Block("Tin Ore"));
        TIN_ORE.setTex(cubeUVs(2, 1));
        renderType[TIN_ORE.id] = RenderType.CUBE;
        TIN_ORE.material(Material.FANCY_STONE);
        TIN_ORE.setHardness(2.5);
        TIN_ORE.setTier(MaterialTier.STONE);

        SILVER_ORE = register("silverOre", new Block("Silver Ore"));
        SILVER_ORE.setTex(cubeUVs(3, 1));
        renderType[SILVER_ORE.id] = RenderType.CUBE;
        SILVER_ORE.material(Material.FANCY_STONE);
        SILVER_ORE.setHardness(3.5);
        SILVER_ORE.setTier(MaterialTier.IRON);

        TORCH = register("torch", new Torch("Torch"));
        TORCH.setTex(cubeUVs(9, 3));
        TORCH.itemLike();
        TORCH.material(Material.ORGANIC);

        CRAFTING_TABLE = register("craftingTable", new CraftingTable("Crafting Table"));
        CRAFTING_TABLE.setTex(CTUVs(4, 3, 3, 3, 2, 3, 5, 3));
        CRAFTING_TABLE.setModel(BlockModel.makeCube(CRAFTING_TABLE));
        CRAFTING_TABLE.material(Material.WOOD);
        CRAFTING_TABLE.setFlammable(30);

        MAHOGANY_CHEST = register("mahoganyChest", new Chest("Chest"));
        MAHOGANY_CHEST.setTex(chestUVs(2, 4, 0, 4, 1, 4, 3, 4));
        MAHOGANY_CHEST.material(Material.WOOD);
        MAHOGANY_CHEST.setFlammable(30);

        OAK_CHEST = register("oakChest", new Chest("Oak Chest"));
        OAK_CHEST.setTex(chestUVs(2, 9, 0, 9, 1, 9, 3, 9));
        OAK_CHEST.material(Material.WOOD);
        OAK_CHEST.setFlammable(30);

        DOOR = register("door", new Door("Door"));
        DOOR.setTex(cubeUVs(0, 10));
        DOOR.material(Material.WOOD);
        DOOR.setFlammable(30);

        MAHOGANY_DOOR = register("mahoganyDoor", new Door("Mahogany Door"));
        MAHOGANY_DOOR.setTex(cubeUVs(1, 10));
        MAHOGANY_DOOR.material(Material.WOOD);

        BRICK_FURNACE = register("brickFurnace", new Furnace("Brick Furnace"));
        BRICK_FURNACE.setTex(furnaceUVs(4, 4, 5, 4, 6, 4, 7, 4));
        BRICK_FURNACE.material(Material.STONE);
        BRICK_FURNACE.light(15);

        FURNACE = register("furnace", new Furnace("Furnace"));
        FURNACE.setTex(furnaceUVs(8, 4, 9, 4, 10, 4, 11, 4));
        FURNACE.material(Material.STONE);
        FURNACE.light(15);

        LADDER = register("ladder", new Ladder("Ladder"));
        LADDER.setTex(new UVPair(10, 3));
        LADDER.transparency();
        LADDER.noCollision();
        // render as item!
        LADDER.itemLike();
        LADDER.material(Material.WOOD);
        LADDER.setHardness(0.5);
        LADDER.setFlammable(30);

        FIRE = register("fire", new FireBlock("Fire"));
        FIRE.setTex(new UVPair(3, 14));
        renderType[FIRE.id] = RenderType.FIRE;
        FIRE.itemLike();
        FIRE.transparency();
        FIRE.noCollision();
        FIRE.noSelection();
        FIRE.light(15);
        FIRE.tick();
        FIRE.material(Material.HELL);

        SIGN = register("sign", new SignBlock("Sign"));
        SIGN.setTex(new UVPair(2, 10), new UVPair(1, 5));


        // set default hardness for blocks that haven't set it
        for (int i = 0; i < currentID; i++) {
            if (hardness[i] == -0.1) {
                hardness[i] = 1;
            }
        }

        // unbreakable blocks (negative hardness)
        HELLROCK.setHardness(-1);
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

        //inventoryBlacklist[ Block.WATER.id = true;
        //inventoryBlacklist[7] = true;
    }


    public static bool isFullBlock(int id) {
        return fullBlock[id];
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

    public static UVPair[] furnaceUVs(int frontX, int frontY, int litX, int litY, int sideX, int sideY, int top_bottomX, int top_bottomY) {
        return [
            new(frontX, frontY), new UVPair(litX, litY), new(sideX, sideY), new UVPair(top_bottomX, top_bottomY)
        ];
    }

    public static UVPair[] CTUVs(int topX, int topY, int xx, int xy, int zx, int zy, int bottomX, int bottomY) {
        return [
            new(xx, xy), new(xx, xy), new(zx, zy), new(zx, zy), new(bottomX, bottomY),
            new(topX, topY)
        ];
    }

    public static UVPair[] chestUVs(int topX, int topY, int xx, int xy, int zx, int zy, int bottomX, int bottomY) {
        return [
            new(xx, xy), new(xx, xy), new(zx, zy), new(xx, xy), new(bottomX, bottomY),
            new(topX, topY)
        ];
    }

    public static UVPair[] crossUVs(int x, int y) {
        return [new(x, y), new(x, y)];
    }

    public static UVPair[] HeadUVs(int leftX, int leftY, int rightX, int rightY, int frontX, int frontY, int backX,
        int backY, int bottomX, int bottomY, int topX, int topY) {
        return [
            new(leftX, leftY), new(rightX, rightY), new(frontX, frontY), new(backX, backY), new(bottomX, bottomY),
            new(topX, topY)
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

    public static Color packColour(RawDirection direction, byte ao, byte light) {
        return packColour((byte)direction, ao, light);
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

    public Block(string name) {
        this.id = id;
        this.name = name;
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

    public Block setHardness(double hards) {
        hardness[id] = hards;
        return this;
    }

    public Block setFlammable(double value) {
        flammable[id] = value;
        return this;
    }

    public Block material(Material mat) {
        this.mat = mat;
        tool[id] = mat.toolType;
        tier[id] = mat.tier;
        hardness[id] = mat.hardness;
        return this;
    }

    public Block setTier(MaterialTier t) {
        tier[id] = t;
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
        //br.setupWorld();
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

    /**
     * Returns the item drops when this block is broken.
     * By default, blocks drop themselves as an item.
     */
    public virtual (Item item, byte metadata, int count) getDrop(World world, int x, int y, int z, byte metadata) {
        return (getItem(), metadata, 1);
    }

    /**
     * Called when right-clicking on the block.
     * Returns true if the interaction was handled (prevents block placement).
     */
    public virtual bool onUse(World world, int x, int y, int z, Player player) {
        return false;
    }

    /**
     * Called when an entity walks on the block (only if the block has collision).
     */
    public virtual void onStepped(World world, int x, int y, int z, Entity entity) {
    }

    // todo add biome tinting, later?

    public virtual void shatter(World world, int x, int y, int z) {
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
                    var ttl = (int)(5f / (Game.clientRandom.NextSingle() + 0.05f));

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

                    float u = uv.u + Game.clientRandom.NextSingle() * 0.75f;
                    float v = uv.v + Game.clientRandom.NextSingle() * 0.75f;
                    Vector2 us = UVPair.texCoords(u, v);

                    // break particles: explode outward from center, biased upward
                    var dx = (particleX - x - 0.5f);
                    var dy = (particleY - y - 0.5f);
                    var dz = (particleZ - z - 0.5f);

                    var motion = Particle.abbMotion(new Vector3(dx * 2, dy * 2 + 0.6f, dz * 2));

                    var particle = new Particle(
                        world,
                        particlePosition);
                    particle.texture = "textures/blocks.png";
                    particle.u = us.X;
                    particle.v = us.Y;
                    particle.size = new Vector2(size);
                    particle.uvsize = new Vector2(1 / 16f * size);
                    particle.maxAge = ttl;
                    world.particles.add(particle);

                    particle.velocity = motion.toVec3D();
                }
            }
        }
    }

    /** mining particles for when block is being broken */
    public virtual void shatter(World world, int x, int y, int z, RawDirection hitFace, AABB? hitAABB = null) {
        UVPair uv;

        if (model == null || model.faces.Length == 0) {
            var custom = renderType[id] == RenderType.CUSTOM || renderType[id] == RenderType.CUBE_DYNTEXTURE;
            if (!custom && (uvs == null || uvs.Length == 0)) {
                return;
            }
        }

        // spawn fewer particles for mining (2-4 particles)
        var count = Game.clientRandom.Next(2, 5);

        // use hit AABB if provided, otherwise default to full block
        var bbn = hitAABB?.min ?? new Vector3D(x, y, z);
        var bbx = hitAABB?.max ?? new Vector3D(x + 1, y + 1, z + 1);

        for (var i = 0; i < count; i++) {
            // spawn particles just outside the hit face to avoid collision with block
            float px = 0;
            float py = 0;
            float pz = 0;

            const float offset = 0.08f;

            // constrain particles to the actual AABB bounds
            switch (hitFace) {
                case RawDirection.UP:
                    px = (float)(bbn.X + Game.clientRandom.NextSingle() * (bbx.X - bbn.X));
                    py = (float)bbx.Y + offset;
                    pz = (float)(bbn.Z + Game.clientRandom.NextSingle() * (bbx.Z - bbn.Z));
                    break;
                case RawDirection.DOWN:
                    px = (float)(bbn.X + Game.clientRandom.NextSingle() * (bbx.X - bbn.X));
                    py = (float)bbn.Y - offset;
                    pz = (float)(bbn.Z + Game.clientRandom.NextSingle() * (bbx.Z - bbn.Z));
                    break;
                case RawDirection.NORTH:
                    px = (float)(bbn.X + Game.clientRandom.NextSingle() * (bbx.X - bbn.X));
                    py = (float)(bbn.Y + Game.clientRandom.NextSingle() * (bbx.Y - bbn.Y));
                    pz = (float)bbx.Z + offset;
                    break;
                case RawDirection.SOUTH:
                    px = (float)(bbn.X + Game.clientRandom.NextSingle() * (bbx.X - bbn.X));
                    py = (float)(bbn.Y + Game.clientRandom.NextSingle() * (bbx.Y - bbn.Y));
                    pz = (float)bbn.Z - offset;
                    break;
                case RawDirection.EAST:
                    px = (float)bbx.X + offset;
                    py = (float)(bbn.Y + Game.clientRandom.NextSingle() * (bbx.Y - bbn.Y));
                    pz = (float)(bbn.Z + Game.clientRandom.NextSingle() * (bbx.Z - bbn.Z));
                    break;
                case RawDirection.WEST:
                    px = (float)bbn.X - offset;
                    py = (float)(bbn.Y + Game.clientRandom.NextSingle() * (bbx.Y - bbn.Y));
                    pz = (float)(bbn.Z + Game.clientRandom.NextSingle() * (bbx.Z - bbn.Z));
                    break;
            }

            var particlePosition = new Vector3D(px, py, pz);
            var size = Game.clientRandom.NextSingle() * 0.08f + 0.04f;
            var ttl = (int)(14f / (Game.clientRandom.NextSingle() + 0.05f));

            switch (renderType[id]) {
                case RenderType.CUBE_DYNTEXTURE:
                case RenderType.CUSTOM:
                    var meta = world.getBlockMetadata(x, y, z);
                    uv = getTexture(0, meta);
                    break;
                case RenderType.MODEL:
                    uv = uvs[Game.clientRandom.Next(0, uvs.Length)];
                    break;
                default:
                    uv = uvs[0];
                    break;
            }

            float u = uv.u + Game.clientRandom.NextSingle() * 0.75f;
            float v = uv.v + Game.clientRandom.NextSingle() * 0.75f;
            Vector2 us = UVPair.texCoords(u, v);

            // mining particles: fall down with slight horizontal drift
            var rx = (Game.clientRandom.NextSingle() - 0.5f) * 0.3f;
            var rz = (Game.clientRandom.NextSingle() - 0.5f) * 0.3f;
            var motion = Particle.abbMotion(new Vector3(rx, 0.5f, rz));

            var particle = new Particle(world, particlePosition);
            particle.texture = "textures/blocks.png";
            particle.u = us.X;
            particle.v = us.Y;
            particle.size = new Vector2(size);
            particle.uvsize = new Vector2(1 / 16f * size);
            particle.maxAge = ttl;
            world.particles.add(particle);
            particle.velocity = motion.toVec3D();
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
        // standard placement rules
        // liquids can always be placed into
        if (liquid[id]) {
            return true;
        }

        // can be placed into

        // otherwise, can't place if there's anything there lol
        return world.getBlock(x, y, z) == 0;
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

    public override string ToString() {
        return $"Block{{id={id}, name={name}}}";
    }
}

public static class BlockExtensions {
    extension(uint block) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort getID() {
            return (ushort)(block & 0xFFFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte getMetadata() {
            return (byte)(block >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint setMetadata(byte metadata) {
            return (block & 0xFFFFFF) | ((uint)metadata << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint setID(ushort id) {
            return (block & 0xFF000000) | id;
        }
    }
}

public class Flower(string name) : Block(name) {
    protected override void onRegister(int id) {
        material(Material.ORGANIC);
        hardness[id] = 0;
    }

    public override void update(World world, int x, int y, int z) {
        if (world.inWorld(x, y - 1, z) && world.getBlock(x, y - 1, z) == 0) {
            world.setBlock(x, y, z, AIR.id);
        }
    }
}

public class Grass(string name) : Block(name) {
    public override void update(World world, int x, int y, int z) {
        if (world.inWorld(x, y - 1, z) && world.getBlock(x, y - 1, z) == 0) {
            world.setBlock(x, y, z, AIR.id);
        }
    }

    public override (Item item, byte metadata, int count) getDrop(World world, int x, int y, int z, byte metadata) {
        return (null!, 0, 0);
    }
}

public class GrassBlock(string name) : Block(name) {
    public override (Item item, byte metadata, int count) getDrop(World world, int x, int y, int z, byte metadata) {
        // grass drops dirt
        return (DIRT.getItem(), 0, 1);
    }

    public override void randomUpdate(World world, int x, int y, int z) {
        // turn to dirt if full block above
        if (y < World.WORLDHEIGHT - 1 && isFullBlock(world.getBlock(x, y + 1, z))) {
            world.setBlock(x, y, z, DIRT.id);
            return;
        }

        // spread grass to nearby dirt
        // try 3 times!
        for (int i = 0; i < 3; i++) {
            var r = world.random.Next(27); // 3x3x3
            int dx = (r % 3) - 1;
            int dy = ((r / 3) % 3) - 1;
            int dz = (r / 9) - 1;

            int nx = x + dx;
            int ny = y + dy;
            int nz = z + dz;

            // if target is dirt with air above, spread
            if (world.getBlock(nx, ny, nz) == DIRT.id) {
                if (ny < World.WORLDHEIGHT - 1 && world.getBlock(nx, ny + 1, nz) == AIR.id) {
                    world.setBlock(nx, ny, nz, GRASS.id);
                }
            }
        }
    }
}

/// <summary>
/// Represents a block face. If noAO, don't let AO cast on this face.
/// If it's not a full face, it's always drawn to ensure it's drawn even when there's a solid block next to it.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Face(
    float x1,
    float y1,
    float z1,
    float x2,
    float y2,
    float z2,
    float x3,
    float y3,
    float z3,
    float x4,
    float y4,
    float z4,
    UVPair min,
    UVPair max,
    RawDirection direction,
    bool noAO = false,
    bool nonFullFace = false) {
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
    FIRE,
    CUSTOM,
    CUBE_DYNTEXTURE
}

public enum ToolType : byte {
    NONE,
    PICKAXE,
    AXE,
    SHOVEL,
    HOE,
    SCYTHE,
}

public record class MaterialTier(MaterialTiers tier, double level, double speed, int durability) {
    public static readonly MaterialTier NONE = new(MaterialTiers.NONE, 0, 1, 0);
    public static readonly MaterialTier WOOD = new(MaterialTiers.WOOD, 1, 1.25, 32);
    public static readonly MaterialTier STONE = new(MaterialTiers.STONE, 2, 1.3, 128);
    public static readonly MaterialTier COPPER = new(MaterialTiers.COPPER, 2.5, 1.4, 256);
    public static readonly MaterialTier IRON = new(MaterialTiers.IRON, 3, 1.5, 384);
    public static readonly MaterialTier GOLD = new(MaterialTiers.GOLD, 3.5, 2, 1024);

    /** The index of the tier (NO GAMEPLAY EFFECT, DON'T USE IT FOR THAT), only use for sorting or indexing */
    public readonly MaterialTiers tier = tier;

    /** The "tier value", should roughly be increasing but can be the same or less than the previous. Used for determining stats */
    public readonly double level = level;

    public readonly double speed = speed;

    /** max durability for tools/weapons of this tier */
    public readonly int durability = durability;

    // todo add more stats here like durability, damage, speed, etc. as needed
}

public enum MaterialTiers : byte {
    NONE,
    WOOD,
    STONE,
    COPPER,
    IRON,
    GOLD,
}

public enum SoundMaterial : byte {
    WOOD,
    STONE,
    METAL,
    DIRT,
    GRASS,
    SAND,
    GLASS,
    ORGANIC
}

public static class SoundMaterialExtensions {
    extension(SoundMaterial mat) {
        public string stepCategory() => mat switch {
            SoundMaterial.GRASS => "step/grass",
            SoundMaterial.DIRT => "step/grass",
            SoundMaterial.SAND => "step",
            SoundMaterial.WOOD => "step/wood",
            SoundMaterial.STONE => "step",
            SoundMaterial.METAL => "step",
            SoundMaterial.GLASS => "step",
            SoundMaterial.ORGANIC => "step/grass",
            _ => "step"
        };

        public string breakCategory() => mat switch {
            SoundMaterial.WOOD => "break/wood",
            SoundMaterial.STONE => "break/stone",
            SoundMaterial.SAND => "break/sand",
            SoundMaterial.METAL => "break/stone",
            SoundMaterial.DIRT => "break/grass",
            SoundMaterial.GRASS => "break/grass",
            SoundMaterial.GLASS => "break/stone",
            SoundMaterial.ORGANIC => "break/grass",
            _ => "step"
        };

        public string knockCategory() => mat switch {
            SoundMaterial.WOOD => "knock/wood",
            SoundMaterial.STONE => "break/stone",
            SoundMaterial.SAND => "break/sand",
            SoundMaterial.METAL => "break/stone",
            SoundMaterial.DIRT => "knock/grass",
            SoundMaterial.GRASS => "knock/grass",
            SoundMaterial.GLASS => "break/stone",
            SoundMaterial.ORGANIC => "knock/grass",
            _ => "step"
        };
    }
}

/**
 * Block hardness: 0.5 to 30+ (wide range)
 * Tool speed: something like 1.0 to 4.0 (narrow range)
 * Tier scaling: Handles the actual progression via the fancy-ass logarithmic formula
 */
public class Material {
    public static readonly Material WOOD = new Material(SoundMaterial.WOOD, ToolType.AXE, MaterialTier.NONE, 2);
    public static readonly Material STONE = new Material(SoundMaterial.STONE, ToolType.PICKAXE, MaterialTier.WOOD, 1.5);
    public static readonly Material METAL = new Material(SoundMaterial.METAL, ToolType.PICKAXE, MaterialTier.STONE, 4);
    public static readonly Material EARTH = new Material(SoundMaterial.DIRT, ToolType.SHOVEL, MaterialTier.NONE, 0.6);

    public static readonly Material ORGANIC =
        new Material(SoundMaterial.ORGANIC, ToolType.NONE, MaterialTier.NONE, 0.25);

    /** Yummy! */
    public static readonly Material FOOD = new Material(SoundMaterial.ORGANIC, ToolType.NONE, MaterialTier.NONE, 0.8);

    public static readonly Material GLASS = new Material(SoundMaterial.GLASS, ToolType.NONE, MaterialTier.NONE, 0.2);

    /** Mostly ores */
    public static readonly Material FANCY_STONE =
        new Material(SoundMaterial.STONE, ToolType.PICKAXE, MaterialTier.STONE, 3);

    /** TODO */
    public static readonly Material HELL = new Material(SoundMaterial.STONE, ToolType.PICKAXE, MaterialTier.NONE, 2);

    public SoundMaterial smat;
    public ToolType toolType;
    public MaterialTier tier;
    public double hardness;

    public Material(SoundMaterial smat, ToolType toolType, MaterialTier tier, double hardness) {
        this.smat = smat;
        this.toolType = toolType;
        this.tier = tier;
        this.hardness = hardness;
    }
}