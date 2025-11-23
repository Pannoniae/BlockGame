using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.entity;
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

    public static List<ItemStack> drops = [];

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

    public static BlockTextureAtlas atlas;

    protected static readonly List<AABB> AABBList = [];

    // atlas dimensions - updated when texture pack is loaded
    public static int atlasSize = 512;
    public const int textureSize = 16;

    public static float atlasRatio = textureSize / (float)atlasSize;
    public static float atlasRatioInv = 1 / atlasRatio;

    /// <summary>
    /// Update atlas size after loading texture pack. Recalculates atlasRatio.
    /// </summary>
    public static void updateAtlasSize(int newAtlasSize) {
        atlasSize = newAtlasSize;
        atlasRatio = textureSize / (float)atlasSize;
        atlasRatioInv = 1 / atlasRatio;
    }

    public static UVPair uv(string source, int x, int y) {
        if (Net.mode.isDed()) {
            return new UVPair(0, 0);
        }
        return atlas.uv(source, x, y);
    }

    public static UVPair[] uvRange(string source, int x, int y, int i) {
        if (Net.mode.isDed()) {
            return new UVPair[i];
        }

        var uvs = new UVPair[i];
        for (int j = 0; j < i; j++) {
            uvs[j] = atlas.uv(source, x + j, y);
        }
        return uvs;
    }

    public static Block AIR;
    public static Block GRASS;
    public static Block DIRT;
    public static Block SAND;
    public static Block BASALT;
    public static Block STONE;
    public static Block COBBLESTONE;
    public static Block GRAVEL;
    public static Block HELLSTONE;

    public static Block SNOW_GRASS;
    public static Block SNOW;
    //public static Block BLOODSTONE;
    public static Block HELLROCK;
    //public static Block INFERNO_ROCK;
    public static Block GLASS;
    public static Block GLASS_FRAMED_X;
    public static Block GLASS_FRAMED_R;
    public static Block GLASS_FRAMED_T;
    public static Block GLASS_FRAMED_C;

    //public static Block CALCITE;
    public static Block STONE_SLAB;
    public static Block COBBLESTONE_SLAB;
    public static Block BASALT_SLAB;
    public static Block BRICKBLOCK_SLAB;

    public static Block STONE_STAIRS;
    public static Block COBBLESTONE_STAIRS;
    public static Block BASALT_STAIRS;
    public static Block BRICKBLOCK_STAIRS;

    public static Block CLAY_BLOCK;
    public static Block BRICK_BLOCK;
    public static Block STONE_BRICK;
    public static Block STONE_BRICK_SLAB;
    public static Block STONE_BRICK_STAIRS;
    public static Block SAND_BRICK;
    public static Block SAND_BRICK_SLAB;
    public static Block SAND_BRICK_STAIRS;
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

    public static Block PINE_LOG;
    public static Block PINE_PLANKS;
    public static Block PINE_STAIRS;
    public static Block PINE_SLAB;
    public static Block PINE_LEAVES;
    public static Block PINE_SAPLING;

    public static Block ICE;

    public static Block CACTUS;

    public static Block OAK_CHEST;
    public static Block OAK_DOOR;
    public static Block MAHOGANY_DOOR;

    public static Block CANDY;
    public static Block CANDY_SLAB;
    public static Block CANDY_STAIRS;

    //public static Block HEAD;

    public static Block WATER;

    public static Block CINNABAR_ORE;
    //public static Block TITANIUM_ORE;
    //public static Block AMBER_ORE;
    //public static Block AMETHYST_ORE;
    //public static Block EMERALD_ORE;
    public static Block DIAMOND_ORE;
    public static Block GOLD_ORE;
    public static Block IRON_ORE;
    public static Block COAL_ORE;
    public static Block COPPER_ORE;
    public static Block TIN_ORE;
    //public static Block SILVER_ORE;

    public static Block GOLD_BLOCK;
    public static Block IRON_BLOCK;
    public static Block COPPER_BLOCK;
    //public static Block TITANIUM_BLOCK;
    //public static Block SILVER_BLOCK;
    //public static Block TIN_BLOCK;
    public static Block DIAMOND_BLOCK;
    public static Block COAL_BLOCK;
    public static Block CINNABAR_BLOCK;

    public static Block TORCH;
    public static Block CRAFTING_TABLE;
    public static Block MAHOGANY_CHEST;
    public static Block BRICK_FURNACE;
    public static Block BRICK_FURNACE_LIT;
    public static Block FURNACE;
    public static Block FURNACE_LIT;
    public static Block LADDER;
    public static Block FIRE;
    public static Block SIGN;

    public static Block OAK_FENCE;
    public static Block MAHOGANY_FENCE;
    public static Block PINE_FENCE;
    public static Block MAPLE_FENCE;

    public static Block OAK_GATE;
    public static Block MAHOGANY_GATE;
    public static Block PINE_GATE;
    public static Block MAPLE_GATE;

    public static Block FARMLAND;
    public static Block CROP_WHEAT;

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

    public static XUList<float> friction => Registry.BLOCKS.friction;

    public static XUList<bool> natural => Registry.BLOCKS.natural;



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
        if (!Net.mode.isDed()) {
            atlas = (BlockTextureAtlas)Game.textures.blockTexture;
        }

        AIR = register("air", new Block("Air"));
        AIR.setModel(BlockModel.emptyBlock());
        AIR.air();

        GRASS = register("grass", new GrassBlock("Grass"));
        GRASS.tick();
        GRASS.setTex(uv("blocks.png", 0, 0), uv("blocks.png", 1, 0), uv("blocks.png", 2, 0));
        renderType[GRASS.id] = RenderType.CUBE_DYNTEXTURE;
        GRASS.material(Material.EARTH);
        natural[GRASS.id] = true;

        DIRT = register("dirt", new Block("Dirt"));
        DIRT.setTex(uv("blocks.png", 2, 0));
        renderType[DIRT.id] = RenderType.CUBE;
        DIRT.material(Material.EARTH);
        natural[DIRT.id] = true;

        SAND = register("sand", new FallingBlock("Sand"));
        SAND.setTex(uv("blocks.png", 3, 0));
        renderType[SAND.id] = RenderType.CUBE;
        SAND.material(Material.EARTH);
        // less hard than dirt!
        SAND.setHardness(0.3);
        natural[SAND.id] = true;

        SNOW_GRASS = register("snowGrass", new GrassBlock("Snowy Grass"));
        SNOW_GRASS.tick();
        SNOW_GRASS.setTex(uv("blocks.png", 13, 0), uv("blocks.png", 14, 0), uv("blocks.png", 2, 0));
        renderType[SNOW_GRASS.id] = RenderType.CUBE_DYNTEXTURE;
        SNOW_GRASS.material(Material.EARTH);
        natural[SNOW_GRASS.id] = true;

        SNOW = register("snow", new Block("Snow"));
        SNOW.setTex(uv("blocks.png", 15, 0));
        renderType[SNOW.id] = RenderType.CUBE;
        SNOW.material(Material.EARTH);
        SNOW.setHardness(0.3);
        natural[SNOW.id] = true;

        BASALT = register("basalt", new Block("Basalt"));
        BASALT.setTex(uv("blocks.png", 4, 0));
        renderType[BASALT.id] = RenderType.CUBE;
        BASALT.material(Material.STONE);

        BASALT_SLAB = register("basaltSlab", new Slabs("Basalt Slab"));
        BASALT_SLAB.setTex(cubeUVs(4, 0));
        BASALT_SLAB.material(Material.STONE);

        BASALT_STAIRS = register("basaltStairs", new Stairs("Basalt Stairs"));
        BASALT_STAIRS.setTex(cubeUVs(4, 0));
        BASALT_STAIRS.partialBlock();
        BASALT_STAIRS.material(Material.STONE);

        STONE = register("stone", new StoneBlock("Stone"));
        STONE.setTex(uv("blocks.png", 5, 0));
        renderType[STONE.id] = RenderType.CUBE;
        STONE.material(Material.STONE);
        natural[STONE.id] = true;

        STONE_SLAB = register("stoneSlab", new Slabs("Stone Slab"));
        STONE_SLAB.setTex(cubeUVs(5, 0));
        STONE_SLAB.material(Material.STONE);

        STONE_STAIRS = register("stoneStairs", new Stairs("Stone Stairs"));
        STONE_STAIRS.setTex(cubeUVs(5, 0));
        STONE_STAIRS.partialBlock();
        STONE_STAIRS.material(Material.STONE);

        COBBLESTONE = register("cobblestone", new Block("Cobblestone"));
        COBBLESTONE.setTex(uv("blocks.png", 6, 1));
        renderType[COBBLESTONE.id] = RenderType.CUBE;
        COBBLESTONE.material(Material.STONE);

        COBBLESTONE_SLAB = register("cobblestoneSlab", new Slabs("Cobblestone Slab"));
        COBBLESTONE_SLAB.setTex(cubeUVs(6, 1));
        COBBLESTONE_SLAB.material(Material.STONE);

        COBBLESTONE_STAIRS = register("cobblestoneStairs", new Stairs("Cobblestone Stairs"));
        COBBLESTONE_STAIRS.setTex(cubeUVs(6, 1));
        COBBLESTONE_STAIRS.partialBlock();
        COBBLESTONE_STAIRS.material(Material.STONE);

        GRAVEL = register("gravel", new GravelBlock("Gravel"));
        GRAVEL.setTex(uv("blocks.png", 7, 0));
        renderType[GRAVEL.id] = RenderType.CUBE;
        GRAVEL.material(Material.EARTH);
        natural[GRAVEL.id] = true;

        HELLSTONE = register("hellstone", new Block("Hellstone"));
        HELLSTONE.setTex(uv("blocks.png", 8, 0));
        renderType[HELLSTONE.id] = RenderType.CUBE;
        HELLSTONE.light(15);
        HELLSTONE.material(Material.HELL);
        // todo assign naturalness to hell blocks when we add them

        //BLOODSTONE = register("bloodstone", new Block("Bloodstone"));
        //BLOODSTONE.setTex(cubeUVs(8, 1));
        //renderType[BLOODSTONE.id] = RenderType.CUBE;
        //BLOODSTONE.material(Material.HELL);

        HELLROCK = register("hellrock", new Block("Hellrock"));
        HELLROCK.setTex(uv("blocks.png", 9, 0));
        renderType[HELLROCK.id] = RenderType.CUBE;
        HELLROCK.material(Material.HELL);

        //INFERNO_ROCK = register("infernoRock", new Block("Inferno Rock"));
        //INFERNO_ROCK.setTex(cubeUVs(10, 0));
        //renderType[INFERNO_ROCK.id] = RenderType.CUBE;
        //INFERNO_ROCK.material(Material.HELL);

        //CALCITE = register("calcite", new Block("Calcite"));
        //CALCITE.setTex(cubeUVs(11, 0));
        //renderType[CALCITE.id] = RenderType.CUBE;
        //CALCITE.material(Material.STONE);

        CLAY_BLOCK = register("clayBlock", new Block("Clay"));
        CLAY_BLOCK.setTex(uv("blocks.png", 12, 0));
        renderType[CLAY_BLOCK.id] = RenderType.CUBE;
        CLAY_BLOCK.material(Material.EARTH);
        natural[CLAY_BLOCK.id] = true;

        GLASS = register("glass", new Block("Glass"));
        GLASS.setTex(uv("blocks.png", 6, 0));
        renderType[GLASS.id] = RenderType.CUBE;
        GLASS.transparency();
        GLASS.material(Material.GLASS);

        GLASS_FRAMED_X = register("framedXglass", new Block("X-Framed Glass"));
        GLASS_FRAMED_X.setTex(uv("blocks.png", 17, 0));
        renderType[GLASS.id] = RenderType.CUBE;
        GLASS_FRAMED_X.transparency();
        GLASS_FRAMED_X.material(Material.GLASS);

        GLASS_FRAMED_R = register("framedRglass", new Block("Rhombus Framed Glass"));
        GLASS_FRAMED_R.setTex(uv("blocks.png", 18, 0));
        renderType[GLASS.id] = RenderType.CUBE;
        GLASS_FRAMED_R.transparency();
        GLASS_FRAMED_R.material(Material.GLASS);

        GLASS_FRAMED_T = register("framedTglass", new Block("Tulip Framed Glass"));
        GLASS_FRAMED_T.setTex(uv("blocks.png", 19, 0));
        renderType[GLASS.id] = RenderType.CUBE;
        GLASS_FRAMED_T.transparency();
        GLASS_FRAMED_T.material(Material.GLASS);

        GLASS_FRAMED_C = register("framedCglass", new Block("Cross Framed Glass"));
        GLASS_FRAMED_C.setTex(uv("blocks.png", 20, 0));
        renderType[GLASS.id] = RenderType.CUBE;
        GLASS_FRAMED_C.transparency();
        GLASS_FRAMED_C.material(Material.GLASS);

        BRICK_BLOCK = register("brickBlock", new Block("Brick Block"));
        BRICK_BLOCK.setTex(cubeUVs(0, 2));
        renderType[BRICK_BLOCK.id] = RenderType.CUBE;
        BRICK_BLOCK.material(Material.STONE);

        BRICKBLOCK_SLAB = register("brickBlockSlab", new Slabs("Brick Block Slab"));
        BRICKBLOCK_SLAB.setTex(cubeUVs(0, 2));
        BRICKBLOCK_SLAB.material(Material.STONE);

        BRICKBLOCK_STAIRS = register("brickBlockStairs", new Stairs("Brick Block Stairs"));
        BRICKBLOCK_STAIRS.setTex(cubeUVs(0, 2));
        BRICKBLOCK_STAIRS.partialBlock();
        BRICKBLOCK_STAIRS.material(Material.STONE);

        STONE_BRICK = register("stoneBrick", new Block("Stone Brick"));
        STONE_BRICK.setTex(uv("blocks.png", 1, 2));
        renderType[STONE_BRICK.id] = RenderType.CUBE;
        STONE_BRICK.material(Material.STONE);

        STONE_BRICK_SLAB = register("stoneBrickSlab", new Slabs("Stone Brick Slab"));
        STONE_BRICK_SLAB.setTex(cubeUVs(1, 2));
        STONE_BRICK_SLAB.material(Material.STONE);

        STONE_BRICK_STAIRS = register("stoneBrickStairs", new Stairs("Stone Brick Stairs"));
        STONE_BRICK_STAIRS.setTex(cubeUVs(1, 2));
        STONE_BRICK_STAIRS.partialBlock();
        STONE_BRICK_STAIRS.material(Material.STONE);

        SAND_BRICK = register("sandBrick", new Block("Sand Brick"));
        SAND_BRICK.setTex(uv("blocks.png", 2, 2));
        renderType[SAND_BRICK.id] = RenderType.CUBE;
        SAND_BRICK.material(Material.STONE);

        SAND_BRICK_SLAB = register("sandBrickSlab", new Slabs("Sand Brick Slab"));
        SAND_BRICK_SLAB.setTex(cubeUVs(2, 2));
        SAND_BRICK_SLAB.material(Material.STONE);
        SAND_BRICK_STAIRS = register("sandBrickStairs", new Stairs("Sand Brick Stairs"));
        SAND_BRICK_STAIRS.setTex(cubeUVs(2, 2));
        SAND_BRICK_STAIRS.partialBlock();
        SAND_BRICK_STAIRS.material(Material.STONE);


        COAL_ORE = register("coalOre", new CoalOreBlock("Coal Ore"));
        COAL_ORE.setTex(uv("blocks.png", 4, 1));
        renderType[COAL_ORE.id] = RenderType.CUBE;
        COAL_ORE.material(Material.FANCY_STONE);
        COAL_ORE.setHardness(1.5);
        COAL_ORE.setTier(MaterialTier.WOOD);
        natural[COAL_ORE.id] = true;

        COPPER_ORE = register("copperOre", new Block("Copper Ore"));
        COPPER_ORE.setTex(uv("blocks.png", 5, 1));
        renderType[COPPER_ORE.id] = RenderType.CUBE;
        COPPER_ORE.material(Material.FANCY_STONE);
        COPPER_ORE.setHardness(1.75);
        COPPER_ORE.setTier(MaterialTier.STONE);
        natural[COPPER_ORE.id] = true;

        IRON_ORE = register("ironOre", new Block("Iron Ore"));
        IRON_ORE.setTex(uv("blocks.png", 1, 1));
        renderType[IRON_ORE.id] = RenderType.CUBE;
        IRON_ORE.material(Material.FANCY_STONE);
        IRON_ORE.setHardness(2.0);
        IRON_ORE.setTier(MaterialTier.STONE);
        natural[IRON_ORE.id] = true;

        GOLD_ORE = register("goldOre", new Block("Gold Ore"));
        GOLD_ORE.setTex(uv("blocks.png", 0, 1));
        renderType[GOLD_ORE.id] = RenderType.CUBE;
        GOLD_ORE.material(Material.FANCY_STONE);
        GOLD_ORE.setHardness(2.5);
        GOLD_ORE.setTier(MaterialTier.IRON);
        natural[GOLD_ORE.id] = true;

        TIN_ORE = register("tinOre", new Block("Tin Ore"));
        TIN_ORE.setTex(cubeUVs(2, 1));
        renderType[TIN_ORE.id] = RenderType.CUBE;
        TIN_ORE.material(Material.FANCY_STONE);
        TIN_ORE.setHardness(2.5);
        TIN_ORE.setTier(MaterialTier.STONE);
        natural[TIN_ORE.id] = true;

        //SILVER_ORE = register("silverOre", new Block("Silver Ore"));
        //SILVER_ORE.setTex(cubeUVs(3, 1));
        //renderType[SILVER_ORE.id] = RenderType.CUBE;
        //SILVER_ORE.material(Material.FANCY_STONE);
        //SILVER_ORE.setHardness(3.5);
        //SILVER_ORE.setTier(MaterialTier.IRON);
        //natural[SILVER_ORE.id] = true;

        DIAMOND_ORE = register("diamondOre", new Block("Diamond Ore"));
        DIAMOND_ORE.setTex(uv("blocks.png", 15, 1));
        renderType[DIAMOND_ORE.id] = RenderType.CUBE;
        DIAMOND_ORE.material(Material.FANCY_STONE);
        DIAMOND_ORE.setHardness(3.0);
        DIAMOND_ORE.setTier(MaterialTier.GOLD);
        natural[DIAMOND_ORE.id] = true;

        CINNABAR_ORE = register("cinnabarOre", new GlowingOre("Cinnabar"));
        CINNABAR_ORE.setTex(uv("blocks.png", 9, 1));
        renderType[CINNABAR_ORE.id] = RenderType.CUBE_DYNTEXTURE;
        CINNABAR_ORE.material(Material.FANCY_STONE);
        CINNABAR_ORE.setHardness(4.0);
        CINNABAR_ORE.setTier(MaterialTier.GOLD);
        natural[CINNABAR_ORE.id] = true;

        //TITANIUM_ORE = register("titaniumOre", new Block("Titanium Ore"));
        //TITANIUM_ORE.setTex(cubeUVs(11, 1));
        //renderType[TITANIUM_ORE.id] = RenderType.CUBE;
        //TITANIUM_ORE.material(Material.FANCY_STONE);
        //TITANIUM_ORE.setHardness(7.5);
        //TITANIUM_ORE.setTier(MaterialTier.GOLD);
        //natural[TITANIUM_ORE.id] = true;

        //AMBER_ORE = register("amberOre", new Block("Amber Ore"));
        //AMBER_ORE.setTex(cubeUVs(12, 1));
        //renderType[AMBER_ORE.id] = RenderType.CUBE;
        //AMBER_ORE.material(Material.FANCY_STONE);
        //AMBER_ORE.setHardness(3.0);
        //AMBER_ORE.setTier(MaterialTier.STONE);
        //natural[AMBER_ORE.id] = true;

        //AMETHYST_ORE = register("amethystOre", new Block("Amethyst Ore"));
        //AMETHYST_ORE.setTex(cubeUVs(13, 1));
        //renderType[AMETHYST_ORE.id] = RenderType.CUBE;
        //AMETHYST_ORE.material(Material.FANCY_STONE);
        //AMETHYST_ORE.setHardness(4.0);
        //AMETHYST_ORE.setTier(MaterialTier.IRON);
        //natural[AMETHYST_ORE.id] = true;

        //EMERALD_ORE = register("emeraldOre", new Block("Emerald Ore"));
        //EMERALD_ORE.setTex(cubeUVs(14, 1));
        //renderType[EMERALD_ORE.id] = RenderType.CUBE;
        //EMERALD_ORE.material(Material.FANCY_STONE);
        //EMERALD_ORE.setHardness(5.0);
        //EMERALD_ORE.setTier(MaterialTier.GOLD);
        //natural[EMERALD_ORE.id] = true;

        LANTERN = register("lantern", new Block("Lantern"));
        LANTERN.setTex(uv("blocks.png", 6, 3), uv("blocks.png", 7, 3), uv("blocks.png", 8, 3));
        LANTERN.setModel(BlockModel.makeLantern(LANTERN));
        LANTERN.light(15);
        LANTERN.partialBlock();
        LANTERN.material(Material.METAL);

        // the blocks
        COAL_BLOCK = register("coalBlock", new Block("Block of Coal"));
        COAL_BLOCK.setTex(cubeUVs(6, 8));
        renderType[COAL_BLOCK.id] = RenderType.CUBE;
        COAL_BLOCK.material(Material.METAL);
        COAL_BLOCK.setHardness(3.0);
        COAL_BLOCK.setTier(MaterialTier.STONE);

        COPPER_BLOCK = register("copperBlock", new Block("Block of Copper"));
        COPPER_BLOCK.setTex(cubeUVs(7, 8));
        renderType[COPPER_BLOCK.id] = RenderType.CUBE;
        COPPER_BLOCK.material(Material.METAL);
        COPPER_BLOCK.setHardness(3.5);
        COPPER_BLOCK.setTier(MaterialTier.IRON);

        IRON_BLOCK = register("ironBlock", new Block("Block of Iron"));
        IRON_BLOCK.setTex(cubeUVs(5, 8));
        renderType[IRON_BLOCK.id] = RenderType.CUBE;
        IRON_BLOCK.material(Material.METAL);
        IRON_BLOCK.setHardness(3.5);
        IRON_BLOCK.setTier(MaterialTier.IRON);

        GOLD_BLOCK = register("goldBlock", new Block("Block of Gold"));
        GOLD_BLOCK.setTex(cubeUVs(4, 8));
        renderType[GOLD_BLOCK.id] = RenderType.CUBE;
        GOLD_BLOCK.material(Material.METAL);
        GOLD_BLOCK.setHardness(4.0);
        GOLD_BLOCK.setTier(MaterialTier.GOLD);

        DIAMOND_BLOCK = register("diamondBlock", new Block("Block of Diamond"));
        DIAMOND_BLOCK.setTex(cubeUVs(8, 8));
        renderType[DIAMOND_BLOCK.id] = RenderType.CUBE;
        DIAMOND_BLOCK.material(Material.METAL);
        DIAMOND_BLOCK.setHardness(4.0);
        DIAMOND_BLOCK.setTier(MaterialTier.GOLD);

        CINNABAR_BLOCK = register("cinnabarBlock", new Block("Block of Cinnabar"));
        CINNABAR_BLOCK.setTex(cubeUVs(9, 8));
        renderType[CINNABAR_BLOCK.id] = RenderType.CUBE;
        CINNABAR_BLOCK.material(Material.METAL);
        CINNABAR_BLOCK.setHardness(4.0);
        CINNABAR_BLOCK.setTier(MaterialTier.GOLD);


        TALL_GRASS = register("tallGrass", new Grass("Tall Grass"));
        TALL_GRASS.setTex(crossUVs(11, 5));
        TALL_GRASS.setModel(BlockModel.makeGrass(TALL_GRASS));
        TALL_GRASS.transparency();
        TALL_GRASS.noCollision();
        TALL_GRASS.waterTransparent();
        TALL_GRASS.material(Material.ORGANIC);
        TALL_GRASS.setHardness(0);
        TALL_GRASS.setFlammable(80);
        tool[TALL_GRASS.id] = ToolType.SCYTHE;

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
        tool[SHORT_GRASS.id] = ToolType.SCYTHE;

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
        OAK_PLANKS.setTex(uv("blocks.png", 0, 5));
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
        LEAVES.setTex(uv("blocks.png", 4, 5));
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
        MAPLE_PLANKS.setTex(uv("blocks.png", 5, 5));
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
        MAPLE_LEAVES.setTex(uv("blocks.png", 9, 5));
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
        MAHOGANY_PLANKS.setTex(uv("blocks.png", 3, 2));
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

        CACTUS = register("cactus", new Cactus("Cactus"));
        CACTUS.setTex(grassUVs(1, 3, 0, 3, 1, 3));
        CACTUS.setModel(BlockModel.makeCube(CACTUS));
        CACTUS.material(Material.ORGANIC);
        CACTUS.setHardness(0.4);
        CACTUS.setFlammable(10);
        CACTUS.tick();

        PINE_LOG = register("pineLog", new Block("Pine Log"));
        PINE_LOG.setTex(grassUVs(10, 2, 9, 2, 11, 2));
        PINE_LOG.setModel(BlockModel.makeCube(PINE_LOG));
        PINE_LOG.material(Material.WOOD);
        log[PINE_LOG.id] = true;
        PINE_LOG.setFlammable(5);

        PINE_PLANKS = register("pinePlanks", new Block("Pine Planks"));
        PINE_PLANKS.setTex(uv("blocks.png", 8, 2));
        renderType[PINE_PLANKS.id] = RenderType.CUBE;
        PINE_PLANKS.material(Material.WOOD);
        PINE_PLANKS.setFlammable(30);

        PINE_STAIRS = register("pineStairs", new Stairs("Pine Stairs"));
        PINE_STAIRS.setTex(cubeUVs(8, 2));
        PINE_STAIRS.partialBlock();
        PINE_STAIRS.material(Material.WOOD);
        PINE_STAIRS.setFlammable(30);

        PINE_SLAB = register("pineSlab", new Slabs("Pine Slab"));
        PINE_SLAB.setTex(cubeUVs(8, 2));
        PINE_SLAB.material(Material.WOOD);
        PINE_SLAB.setFlammable(30);

        PINE_LEAVES = register("pineLeaves", new Leaves("Pine Leaves"));
        PINE_LEAVES.setTex(uv("blocks.png", 12, 2));
        renderType[PINE_LEAVES.id] = RenderType.CUBE;
        PINE_LEAVES.transparency();
        leaves[PINE_LEAVES.id] = true;
        PINE_LEAVES.setFlammable(60);

        PINE_SAPLING = register("pineSapling", new Sapling("Pine Sapling", SaplingType.PINE));
        PINE_SAPLING.setTex(crossUVs(19, 5));
        PINE_SAPLING.setModel(BlockModel.makeGrass(PINE_SAPLING));
        PINE_SAPLING.transparency();
        PINE_SAPLING.noCollision();
        PINE_SAPLING.waterTransparent();
        PINE_SAPLING.itemLike();
        PINE_SAPLING.material(Material.ORGANIC);
        PINE_SAPLING.setFlammable(60);

        ICE = register("ice", new Block("Ice"));
        ICE.setTex(uv("blocks.png", 16, 0));
        renderType[ICE.id] = RenderType.CUBE;
        ICE.translucency();
        ICE.material(Material.GLASS);
        ICE.setHardness(0.5);
        ICE.setFriction(0.96f);

        GOLD_CANDY = register("goldCandy", new Block("Gold Candy"));
        GOLD_CANDY.setTex(uv("blocks.png", 0, 8));
        renderType[GOLD_CANDY.id] = RenderType.CUBE;
        GOLD_CANDY.material(Material.FOOD);

        CINNABAR_CANDY = register("cinnabarCandy", new Block("Cinnabar Candy"));
        CINNABAR_CANDY.setTex(uv("blocks.png", 1, 8));
        renderType[CINNABAR_CANDY.id] = RenderType.CUBE;
        CINNABAR_CANDY.material(Material.FOOD);

        DIAMOND_CANDY = register("diamondCandy", new Block("Diamond Candy"));
        DIAMOND_CANDY.setTex(uv("blocks.png", 2, 8));
        renderType[DIAMOND_CANDY.id] = RenderType.CUBE;
        DIAMOND_CANDY.material(Material.FOOD);

        CANDY = register("candy", new CandyBlock("Candy"));
        CANDY.material(Material.FOOD);

        CANDY_SLAB = register("candySlab", new CandySlab("Candy Slab"));
        CANDY_SLAB.material(Material.FOOD);

        CANDY_STAIRS = register("candyStairs", new CandyStairs("Candy Stairs"));
        CANDY_STAIRS.material(Material.FOOD);
        CANDY_STAIRS.partialBlock();

        //HEAD = register("head", new Block("Head"));
        //HEAD.setTex(HeadUVs(0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3));
        //HEAD.setModel(BlockModel.makeHalfCube(HEAD));
        //HEAD.partialBlock();

        WATER = register("water", new Liquid("Water", 15, 8, false));
        WATER.setTex(uv("blocks.png", 0, 13), uv("blocks.png", 1, 14));
        WATER.makeLiquid();

        LAVA = register("lava", new Liquid("Lava", 30, 4, true));
        LAVA.setTex(uv("blocks.png", 0, 16), uv("blocks.png", 1, 17));
        LAVA.makeLiquid();

        // idk the tiers, these are just placeholders!! stop looking at my ore class lmao

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

        OAK_DOOR = register("door", new Door("Oak Door"));
        OAK_DOOR.setTex(uv("blocks.png", 0, 10), uv("blocks.png", 0, 11));
        OAK_DOOR.material(Material.WOOD);
        OAK_DOOR.setFlammable(30);

        MAHOGANY_DOOR = register("mahoganyDoor", new Door("Mahogany Door"));
        MAHOGANY_DOOR.setTex(uv("blocks.png", 1, 10), uv("blocks.png", 1, 11));
        MAHOGANY_DOOR.material(Material.WOOD);

        BRICK_FURNACE = register("brickFurnace", new Furnace("Brick Furnace", false));
        BRICK_FURNACE.setTex(furnaceUVs(4, 4, 5, 4, 6, 4, 7, 4));
        BRICK_FURNACE.material(Material.STONE);

        BRICK_FURNACE_LIT = register("brickFurnaceLit", new Furnace("Brick Furnace", true));
        BRICK_FURNACE_LIT.setTex(furnaceUVs(4, 4, 5, 4, 6, 4, 7, 4));
        BRICK_FURNACE_LIT.material(Material.STONE);
        BRICK_FURNACE_LIT.light(8);

        FURNACE = register("furnace", new Furnace("Furnace", false));
        FURNACE.setTex(furnaceUVs(8, 4, 9, 4, 10, 4, 11, 4));
        FURNACE.material(Material.STONE);

        FURNACE_LIT = register("furnaceLit", new Furnace("Furnace", true));
        FURNACE_LIT.setTex(furnaceUVs(8, 4, 9, 4, 10, 4, 11, 4));
        FURNACE_LIT.material(Material.STONE);
        FURNACE_LIT.light(8);

        LADDER = register("ladder", new Ladder("Ladder"));
        LADDER.setTex(uv("blocks.png", 10, 3));
        LADDER.transparency();
        LADDER.noCollision();
        // render as item!
        LADDER.itemLike();
        LADDER.material(Material.WOOD);
        LADDER.setHardness(0.5);
        LADDER.setFlammable(30);

        FIRE = register("fire", new FireBlock("Fire"));
        FIRE.setTex(uv("blocks.png", 3, 14));
        renderType[FIRE.id] = RenderType.FIRE;
        FIRE.itemLike();
        FIRE.transparency();
        FIRE.noCollision();
        FIRE.noSelection();
        FIRE.light(15);
        FIRE.tick();
        FIRE.material(Material.HELL);

        SIGN = register("sign", new SignBlock("Sign"));
        SIGN.setTex(uv("blocks.png", 2, 10), uv("blocks.png", 1, 5));

        OAK_FENCE = register("oakFence", new Fence("Oak Fence"));
        OAK_FENCE.setTex(uv("blocks.png", 11, 3));
        OAK_FENCE.material(Material.WOOD);
        OAK_FENCE.setFlammable(30);

        MAHOGANY_FENCE = register("mahoganyFence", new Fence("Mahogany Fence"));
        MAHOGANY_FENCE.setTex(uv("blocks.png", 12, 3));
        MAHOGANY_FENCE.material(Material.WOOD);
        MAHOGANY_FENCE.setFlammable(30);

        MAPLE_FENCE = register("mapleFence", new Fence("Maple Fence"));
        MAPLE_FENCE.setTex(uv("blocks.png", 13, 3));
        MAPLE_FENCE.material(Material.WOOD);
        MAPLE_FENCE.setFlammable(30);

        PINE_FENCE = register("pineFence", new Fence("Pine Fence"));
        PINE_FENCE.setTex(uv("blocks.png", 14, 3));
        PINE_FENCE.material(Material.WOOD);
        PINE_FENCE.setFlammable(30);

        OAK_GATE = register("oakGate", new Gate("Oak Gate"));
        OAK_GATE.setTex(uv("blocks.png", 15, 3));
        OAK_GATE.material(Material.WOOD);
        OAK_GATE.setFlammable(30);

        MAHOGANY_GATE = register("mahoganyGate", new Gate("Mahogany Gate"));
        MAHOGANY_GATE.setTex(uv("blocks.png", 16, 3));
        MAHOGANY_GATE.material(Material.WOOD);
        MAHOGANY_GATE.setFlammable(30);

        MAPLE_GATE = register("mapleGate", new Gate("Maple Gate"));
        MAPLE_GATE.setTex(uv("blocks.png", 17, 3));
        MAPLE_GATE.material(Material.WOOD);
        MAPLE_GATE.setFlammable(30);

        PINE_GATE = register("pineGate", new Gate("Pine Gate"));
        PINE_GATE.setTex(uv("blocks.png", 18, 3));
        PINE_GATE.material(Material.WOOD);
        PINE_GATE.setFlammable(30);

        FARMLAND = register("farmland", new Farmland("Farmland"));
        var farmlandUVs = grassUVs(26, 5, 2, 0, 2, 0);
        var wetUVs = new UVPair(27, 5);
        FARMLAND.setTex(farmlandUVs[0], farmlandUVs[1], farmlandUVs[2], farmlandUVs[3], farmlandUVs[4], farmlandUVs[5], wetUVs);
        renderType[FARMLAND.id] = RenderType.CUBE_DYNTEXTURE;
        FARMLAND.setModel(BlockModel.makeFarmland(FARMLAND));
        FARMLAND.partialBlock();
        FARMLAND.material(Material.EARTH);
        FARMLAND.setHardness(0.6);

        CROP_WHEAT = register("wheatCrop", new Crop("Wheat", 6));
        CROP_WHEAT.setTex(uvRange("blocks.png", 20, 5, 6));
        renderType[CROP_WHEAT.id] = RenderType.CROP;
        CROP_WHEAT.transparency();
        CROP_WHEAT.noCollision();
        CROP_WHEAT.itemLike();
        CROP_WHEAT.waterTransparent();
        CROP_WHEAT.material(Material.ORGANIC);



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
        value = (value & 0xFF000000) | id;
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
        var c = uv("blocks.png", x, y);
        return [c, c, c, c, c, c];
    }

    public static UVPair[] grassUVs(int topX, int topY, int sideX, int sideY, int bottomX, int bottomY) {
        var side = uv("blocks.png", sideX, sideY);
        var bottom = uv("blocks.png", bottomX, bottomY);
        var top = uv("blocks.png", topX, topY);
        return [side, side, side, side, bottom, top];
    }

    public static UVPair[] furnaceUVs(int frontX, int frontY, int litX, int litY, int sideX, int sideY, int top_bottomX, int top_bottomY) {
        return [
            uv("blocks.png", frontX, frontY),
            uv("blocks.png", litX, litY),
            uv("blocks.png", sideX, sideY),
            uv("blocks.png", top_bottomX, top_bottomY)
        ];
    }

    public static UVPair[] CTUVs(int topX, int topY, int xx, int xy, int zx, int zy, int bottomX, int bottomY) {
        var x = uv("blocks.png", xx, xy);
        var z = uv("blocks.png", zx, zy);
        var bottom = uv("blocks.png", bottomX, bottomY);
        var top = uv("blocks.png", topX, topY);
        return [x, x, z, z, bottom, top];
    }

    public static UVPair[] chestUVs(int topX, int topY, int xx, int xy, int zx, int zy, int bottomX, int bottomY) {
        var x = uv("blocks.png", xx, xy);
        var z = uv("blocks.png", zx, zy);
        var bottom = uv("blocks.png", bottomX, bottomY);
        var top = uv("blocks.png", topX, topY);
        return [x, x, z, x, bottom, top];
    }

    public static UVPair[] crossUVs(int x, int y) {
        var c = uv("blocks.png", x, y);
        return [c, c];
    }

    public static UVPair[] HeadUVs(int leftX, int leftY, int rightX, int rightY, int frontX, int frontY, int backX,
        int backY, int bottomX, int bottomY, int topX, int topY) {
        return [
            uv("blocks.png", leftX, leftY),
            uv("blocks.png", rightX, rightY),
            uv("blocks.png", frontX, frontY),
            uv("blocks.png", backX, backY),
            uv("blocks.png", bottomX, bottomY),
            uv("blocks.png", topX, topY)
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
        id = id;
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

    private void setFriction(float f) {
        friction[id] = f;
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
    public virtual void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        //return canBreak ? (getItem(), metadata, 1) : (null, 0, 0);
        if (canBreak) {
            drops.Add(new ItemStack(getItem(), 1, metadata));
        }
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

    public virtual void place(World world, int x, int y, int z, byte metadata, Placement info) {
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
    public virtual bool canPlace(World world, int x, int y, int z, Placement info) {
        // standard placement rules
        // liquids can always be placed into
        if (liquid[id]) {
            return true;
        }

        var targetBlock = world.getBlock(x, y, z);

        // can place into air or water
        return targetBlock == 0 || targetBlock == WATER.id;
    }

    /**
     * Returns the maximum valid metadata value for this block type.
     * Default implementation returns 0 (no metadata variants).
     */
    public virtual byte maxValidMetadata() {
        return 0;
    }

    /**
     * Are these two item stacks the same item?
     * Return true if they shouldn't be treated as the same item (for pick block, etc.)
     * For example, different coloured candy items would return false, but different stack sizes or stair orientations would return true.
     */
    public virtual bool same(ItemStack self, ItemStack other) {
        return self.id == other.id && self.metadata == other.metadata;
    }


    public virtual ItemStack getCanonical(byte metadata) {
        return new ItemStack(getItem(), 1, metadata);
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

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        // drop seeds if broken with scythe
        if (canBreak && world.random.NextDouble() < 0.125) {
            drops.Add(new ItemStack(Item.SEEDS, 1, 0));
        }
    }
}

public class GrassBlock(string name) : Block(name) {
    public override void getDrop(List<ItemStack> drops, World world, int y, int z, int i, byte metadata, bool canBreak) {
        // grass drops dirt
        drops.Add(new ItemStack(DIRT.getItem(), 1, 0));
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

    public override UVPair getTexture(int faceIdx, int metadata) {
        return faceIdx switch {
            // top: uv[0], bottom: uv[1], sides: uv[2]
            5 => uvs[0],
            4 => uvs[2],
            _ => uvs[1]
        };
    }
}

public class GravelBlock(string name) : FallingBlock(name) {
    public override void getDrop(List<ItemStack> drops, World world, int y, int z, int i, byte metadata, bool canBreak) {
        drops.Add(world.random.Next(12) == 0 ? new ItemStack(Item.FLINT, 1, 0) : new ItemStack(getItem(), 1, 0));
    }
}

public class StoneBlock(string name) : Block(name) {
    public override void getDrop(List<ItemStack> drops, World world, int y, int z, int i, byte metadata, bool canBreak) {
        // stone drops cobblestone
        if (canBreak) {
            drops.Add(new ItemStack(COBBLESTONE.getItem(), 1, 0));
        }
        else {
            // if can't break, drop nothing
        }
    }
}

public class CoalOreBlock(string name) : Block(name) {
    public override void getDrop(List<ItemStack> drops, World world, int y, int z, int i, byte metadata, bool canBreak) {
        if (canBreak) {
            drops.Add(new ItemStack(Item.COAL, 1, 0));
        }
    }
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
    CUBE_DYNTEXTURE,
    GRASS,
    CROP
}