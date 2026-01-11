using BlockGame.GL;
using BlockGame.main;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.block;

public partial class Block {
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

    public static Block PALM_LOG;
    public static Block PALM_PLANKS;
    public static Block PALM_STAIRS;
    public static Block PALM_SLAB;
    public static Block PALM_LEAVES;
    public static Block PALM_SAPLING;
    public static Block BANANAFRUIT;

    public static Block FERN_LOG;

    public static Block REDWOOD_LOG;
    public static Block REDWOOD_PLANKS;
    public static Block REDWOOD_STAIRS;
    public static Block REDWOOD_SLAB;
    public static Block REDWOOD_LEAVES;
    public static Block REDWOOD_SAPLING;

    public static Block ICE;

    public static Block CACTUS;

    public static Block OAK_CHEST;
    public static Block MAHOGANY_CHEST;
    public static Block REDWOOD_CHEST;
    public static Block PINE_CHEST;
    public static Block MAPLE_CHEST;

    public static Door OAK_DOOR;
    public static Door MAHOGANY_DOOR;
    public static Door MAPLE_DOOR;
    public static Door PINE_DOOR;
    public static Door GLASS_DOOR;

    public static Block CANDY;
    public static Block CANDY_SLAB;
    public static Block CANDY_STAIRS;
    public static Carpet CARPET;

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
    public static Block BRICK_FURNACE;
    public static Block BRICK_FURNACE_LIT;
    public static Block FURNACE;
    public static Block FURNACE_LIT;
    public static Block LADDER;
    public static Block FIRE;
    public static Block NG;
    public static Block SIGN;
    public static Fence OAK_FENCE;
    public static Fence MAHOGANY_FENCE;
    public static Fence PINE_FENCE;
    public static Fence MAPLE_FENCE;

    /*public static Block OAK_GATE;
    public static Block MAHOGANY_GATE;
    public static Block PINE_GATE;
    public static Block MAPLE_GATE;*/

    public static Block FARMLAND;
    public static Block MUSHROOM_BROWN;
    public static Block MUSHROOM_RED;
    public static Block MUSHROOM_GREEN;
    public static Block BLACKBERRY_BUSH;
    public static Block BLACKBERRY_BUSH_SAPLING;

    public static Crop CROP_WHEAT;
    public static Block FERN_GREEN;
    public static Block FERN_RED;
    public static Crop CROP_CARROT;
    public static Crop CROP_TEA;
    public static Crop CROP_STRAWBERRY;

    public static Wire WIRE;
    public static Block IRON_CHAIN;
    public static Block BUTTON;
    public static Block IC_DETECTOR;
    public static Block OBSERVER;

    public static void preLoad() {
        if (!Net.mode.isDed()) {
            atlas = Game.textures.blockTexture;
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
        SAND.material(Material.SAND);
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
        GRAVEL.material(Material.SAND);
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

        CLAY_BLOCK = register("clayBlock", new ClayBlock("Clay"));
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

        GLASS_FRAMED_R = register("framedRglass", new Block("Rhombus-Framed Glass"));
        GLASS_FRAMED_R.setTex(uv("blocks.png", 18, 0));
        renderType[GLASS.id] = RenderType.CUBE;
        GLASS_FRAMED_R.transparency();
        GLASS_FRAMED_R.material(Material.GLASS);

        GLASS_FRAMED_T = register("framedTglass", new Block("Tulip-Framed Glass"));
        GLASS_FRAMED_T.setTex(uv("blocks.png", 19, 0));
        renderType[GLASS.id] = RenderType.CUBE;
        GLASS_FRAMED_T.transparency();
        GLASS_FRAMED_T.material(Material.GLASS);

        GLASS_FRAMED_C = register("framedCglass", new Block("Cross-Framed Glass"));
        GLASS_FRAMED_C.setTex(uv("blocks.png", 20, 0));
        renderType[GLASS.id] = RenderType.CUBE;
        GLASS_FRAMED_C.transparency();
        GLASS_FRAMED_C.material(Material.GLASS);

        BRICK_BLOCK = register("brickBlock", new Block("Brick"));
        BRICK_BLOCK.setTex(cubeUVs(0, 2));
        renderType[BRICK_BLOCK.id] = RenderType.CUBE;
        BRICK_BLOCK.material(Material.STONE);

        BRICKBLOCK_SLAB = register("brickBlockSlab", new Slabs("Brick Slab"));
        BRICKBLOCK_SLAB.setTex(cubeUVs(0, 2));
        BRICKBLOCK_SLAB.material(Material.STONE);

        BRICKBLOCK_STAIRS = register("brickBlockStairs", new Stairs("Brick Stairs"));
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

        IRON_CHAIN = register("ironChain", new Chain("Iron Chain"));
        IRON_CHAIN.setTex(uv("blocks.png", 16, 3));
        IRON_CHAIN.transparency();
        IRON_CHAIN.itemLike();
        IRON_CHAIN.material(Material.METAL);
        IRON_CHAIN.setHardness(0.75);

        BUTTON = register("button", new Button("Button"));
        BUTTON.setTex(cubeUVs(15, 3));
        renderType[BUTTON.id] = RenderType.CUSTOM;
        BUTTON.partialBlock();
        BUTTON.material(Material.METAL);
        BUTTON.setHardness(0.5);
        BUTTON.noCollision();

        /*IC_DETECTOR = register("ICDetector", new Block("IC Detector"));
        IC_DETECTOR.setTex(grassUVs(17, 3, 18, 3, 5, 0));
        renderType[IC_DETECTOR.id] = RenderType.CUBE_DYNTEXTURE;
        IC_DETECTOR.material(Material.STONE);
        */

        OBSERVER = register("observer", new Block("Observer"));
        OBSERVER.setTex(ldetectorUVs(5, 0,19, 3, 20, 3));
        renderType[OBSERVER.id] = RenderType.CUBE_DYNTEXTURE;
        OBSERVER.material(Material.STONE);
        OBSERVER.light(10);


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
        registerLeafTexture("blocks.png", 4, 5);
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
        registerLeafTexture("blocks.png", 9, 5);
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
        registerLeafTexture("blocks.png", 7, 2);
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
        registerLeafTexture("blocks.png", 12, 2);
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

        PALM_LOG = register("palmLog", new Block("Palm Log"));
        PALM_LOG.setTex(grassUVs(15, 2, 14, 2, 16, 2));
        PALM_LOG.setModel(BlockModel.makeCube(PALM_LOG));
        PALM_LOG.material(Material.WOOD);
        log[PALM_LOG.id] = true;
        PALM_LOG.setFlammable(5);

        PALM_PLANKS = register("palmPlanks", new Block("Palm Planks"));
        PALM_PLANKS.setTex(uv("blocks.png", 13, 2));
        renderType[PALM_PLANKS.id] = RenderType.CUBE;
        PALM_PLANKS.material(Material.WOOD);
        PALM_PLANKS.setFlammable(30);

        PALM_STAIRS = register("palmStairs", new Stairs("Palm Stairs"));
        PALM_STAIRS.setTex(cubeUVs(13, 2));
        PALM_STAIRS.partialBlock();
        PALM_STAIRS.material(Material.WOOD);
        PALM_STAIRS.setFlammable(30);

        PALM_SLAB = register("palmSlab", new Slabs("Palm Slab"));
        PALM_SLAB.setTex(cubeUVs(13, 2));
        PALM_SLAB.material(Material.WOOD);
        PALM_SLAB.setFlammable(30);

        PALM_LEAVES = register("palmLeaves", new Leaves("Palm Leaves"));
        PALM_LEAVES.setTex(uv("blocks.png", 17, 2));
        registerLeafTexture("blocks.png", 17, 2);
        renderType[PALM_LEAVES.id] = RenderType.CUBE;
        PALM_LEAVES.transparency();
        leaves[PALM_LEAVES.id] = true;
        PALM_LEAVES.setFlammable(60);

        PALM_SAPLING = register("palmSapling", new Sapling("Palm Sapling", SaplingType.PALM));
        PALM_SAPLING.setTex(crossUVs(17, 6));
        PALM_SAPLING.setModel(BlockModel.makeGrass(PALM_SAPLING));
        PALM_SAPLING.transparency();
        PALM_SAPLING.noCollision();
        PALM_SAPLING.waterTransparent();
        PALM_SAPLING.itemLike();
        PALM_SAPLING.material(Material.ORGANIC);
        PALM_SAPLING.setFlammable(60);

        BANANAFRUIT = register("bananafruit", new Leaves("Banana"));
        BANANAFRUIT.setTex(crossUVs(17, 7));
        BANANAFRUIT.setModel(BlockModel.makeGrass(BANANAFRUIT));
        BANANAFRUIT.transparency();

        REDWOOD_LOG = register("redwoodLog", new Block("Redwood Log"));
        REDWOOD_LOG.setTex(grassUVs(20, 2, 19, 2, 21, 2));
        REDWOOD_LOG.setModel(BlockModel.makeCube(REDWOOD_LOG));
        REDWOOD_LOG.material(Material.WOOD);
        log[REDWOOD_LOG.id] = true;
        REDWOOD_LOG.setFlammable(5);

        REDWOOD_PLANKS = register("redwoodPlanks", new Block("Redwood Planks"));
        REDWOOD_PLANKS.setTex(uv("blocks.png", 18, 2));
        renderType[REDWOOD_PLANKS.id] = RenderType.CUBE;
        REDWOOD_PLANKS.material(Material.WOOD);
        REDWOOD_PLANKS.setFlammable(30);

        REDWOOD_STAIRS = register("redwoodStairs", new Stairs("Redwood Stairs"));
        REDWOOD_STAIRS.setTex(cubeUVs(18, 2));
        REDWOOD_STAIRS.partialBlock();
        REDWOOD_STAIRS.material(Material.WOOD);
        REDWOOD_STAIRS.setFlammable(30);

        REDWOOD_SLAB = register("redwoodSlab", new Slabs("Redwood Slab"));
        REDWOOD_SLAB.setTex(cubeUVs(18, 2));
        REDWOOD_SLAB.material(Material.WOOD);
        REDWOOD_SLAB.setFlammable(30);

        REDWOOD_LEAVES = register("redwoodLeaves", new Leaves("Redwood Leaves"));
        REDWOOD_LEAVES.setTex(uv("blocks.png", 22, 2));
        registerLeafTexture("blocks.png", 22, 2);
        renderType[REDWOOD_LEAVES.id] = RenderType.CUBE;
        REDWOOD_LEAVES.transparency();
        leaves[REDWOOD_LEAVES.id] = true;
        REDWOOD_LEAVES.setFlammable(60);

        REDWOOD_SAPLING = register("redwoodSapling", new Sapling("Redwood Sapling", SaplingType.REDWOOD));
        REDWOOD_SAPLING.setTex(crossUVs(16, 6));
        REDWOOD_SAPLING.setModel(BlockModel.makeGrass(REDWOOD_SAPLING));
        REDWOOD_SAPLING.transparency();
        REDWOOD_SAPLING.noCollision();
        REDWOOD_SAPLING.waterTransparent();
        REDWOOD_SAPLING.itemLike();
        REDWOOD_SAPLING.material(Material.ORGANIC);
        REDWOOD_SAPLING.setFlammable(60);

        FERN_LOG = register("fernLog", new Block("Fern"));
        FERN_LOG.setTex(grassUVs(24, 2, 23, 2, 25, 2));
        FERN_LOG.setModel(BlockModel.makeCube(FERN_LOG));
        FERN_LOG.material(Material.WOOD);
        log[FERN_LOG.id] = true;
        FERN_LOG.setFlammable(5);

        FERN_GREEN = register("greenFern", new Block("Green Fern"));
        FERN_GREEN.setTex(crossUVs(19, 6));
        FERN_GREEN.setModel(BlockModel.makeGrass(FERN_GREEN));
        FERN_GREEN.transparency();
        FERN_GREEN.noCollision();
        FERN_GREEN.waterTransparent();
        FERN_GREEN.itemLike();
        FERN_GREEN.setFlammable(60);

        FERN_RED = register("redFern", new Block("Red Fern"));
        FERN_RED.setTex(crossUVs(18, 6));
        FERN_RED.setModel(BlockModel.makeGrass(FERN_RED));
        FERN_RED.transparency();
        FERN_RED.noCollision();
        FERN_RED.waterTransparent();
        FERN_RED.itemLike();
        FERN_RED.setFlammable(60);

        MUSHROOM_BROWN = register("brownMushroom", new Leaves("Brown Mushroom"));
        MUSHROOM_BROWN.setTex(crossUVs(28, 2));
        MUSHROOM_BROWN.setModel(BlockModel.makeGrass(MUSHROOM_BROWN));
        MUSHROOM_BROWN.transparency();
        MUSHROOM_BROWN.noCollision();
        MUSHROOM_BROWN.waterTransparent();
        MUSHROOM_BROWN.itemLike();
        MUSHROOM_BROWN.material(Material.FOOD);

        MUSHROOM_RED = register("redMushroom", new Leaves("Red Mushroom"));
        MUSHROOM_RED.setTex(crossUVs(29, 2));
        MUSHROOM_RED.setModel(BlockModel.makeGrass(MUSHROOM_RED));
        MUSHROOM_RED.transparency();
        MUSHROOM_RED.noCollision();
        MUSHROOM_RED.waterTransparent();
        MUSHROOM_RED.itemLike();
        MUSHROOM_RED.material(Material.FOOD);

        MUSHROOM_GREEN = register("greenMushroom", new Block("Green Mushroom"));
        MUSHROOM_GREEN.setTex(crossUVs(19, 7));
        MUSHROOM_GREEN.setModel(BlockModel.makeGrass(MUSHROOM_GREEN));
        MUSHROOM_GREEN.transparency();
        MUSHROOM_GREEN.noCollision();
        MUSHROOM_GREEN.waterTransparent();
        MUSHROOM_GREEN.itemLike();
        MUSHROOM_GREEN.light(10);

        BLACKBERRY_BUSH = register("blackberryBush", new Bush("Blackberry Bush"));
        BLACKBERRY_BUSH.setTex(crossUVs(19, 8));
        BLACKBERRY_BUSH.setModel(BlockModel.makeGrass(BLACKBERRY_BUSH));
        BLACKBERRY_BUSH.transparency();
        BLACKBERRY_BUSH.noCollision();
        BLACKBERRY_BUSH.waterTransparent();
        BLACKBERRY_BUSH.itemLike();
        BLACKBERRY_BUSH.setFlammable(60);

        BLACKBERRY_BUSH_SAPLING = register("blackberryBushSapling", new BushSapling("Blackberry Bush Sapling", BLACKBERRY_BUSH));
        BLACKBERRY_BUSH_SAPLING.setTex(crossUVs(18, 8));
        BLACKBERRY_BUSH_SAPLING.setModel(BlockModel.makeGrass(BLACKBERRY_BUSH_SAPLING));
        BLACKBERRY_BUSH_SAPLING.transparency();
        BLACKBERRY_BUSH_SAPLING.noCollision();
        BLACKBERRY_BUSH_SAPLING.waterTransparent();
        BLACKBERRY_BUSH_SAPLING.itemLike();


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

        CARPET = register("carpet", new Carpet("Carpet"));
        CARPET.material(Material.ORGANIC);

        /*HEAD = register("head", new Block("Head"));
        HEAD.setTex(HeadUVs(0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3));
        HEAD.setModel(BlockModel.makeHalfCube(HEAD));
        HEAD.partialBlock();*/

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
        TORCH.material(Material.WOOD);
        hardness[TORCH.id] = 0;

        CRAFTING_TABLE = register("craftingTable", new CraftingTable("Crafting Table"));
        CRAFTING_TABLE.setTex(CTUVs(4, 3, 3, 3, 2, 3, 5, 3));
        CRAFTING_TABLE.setModel(BlockModel.makeCube(CRAFTING_TABLE));
        CRAFTING_TABLE.material(Material.WOOD);
        CRAFTING_TABLE.setFlammable(30);

        OAK_CHEST = register("oakChest", new Chest("Oak Chest"));
        OAK_CHEST.setTex(HeadUVs(0, 9, 0, 9, 1, 9, 4, 9, 3, 9, 2 , 9));
        OAK_CHEST.material(Material.WOOD);
        OAK_CHEST.setFlammable(30);

        REDWOOD_CHEST = register("redwoodChest", new Chest("Redwood Chest"));
        REDWOOD_CHEST.setTex(HeadUVs(0, 4,0, 4, 1, 4, 4, 4, 3, 4, 2, 4));
        REDWOOD_CHEST.material(Material.WOOD);
        REDWOOD_CHEST.setFlammable(30);

        MAPLE_CHEST = register("mapleChest", new Chest("Maple Chest"));
        MAPLE_CHEST.setTex(HeadUVs(5, 9,5, 9, 6, 9, 9, 9, 8, 9, 7, 9));
        MAPLE_CHEST.material(Material.WOOD);
        MAPLE_CHEST.setFlammable(30);

        PINE_CHEST = register("pineChest", new Chest("Pine Chest"));
        PINE_CHEST.setTex(HeadUVs(10, 9,10, 9, 11, 9, 14, 9, 13, 9, 12, 9));
        PINE_CHEST.material(Material.WOOD);
        PINE_CHEST.setFlammable(30);

        MAHOGANY_CHEST = register("mahoganyChest", new Chest("Mahogany Chest"));
        MAHOGANY_CHEST.setTex(HeadUVs(15, 9,15, 9, 16, 9, 19, 9, 18, 9, 17, 9));
        MAHOGANY_CHEST.material(Material.WOOD);
        MAHOGANY_CHEST.setFlammable(30);

        OAK_DOOR = register("door", new Door("Oak Door"));
        OAK_DOOR.setTex(uv("blocks.png", 0, 10), uv("blocks.png", 0, 11));
        OAK_DOOR.material(Material.WOOD);
        OAK_DOOR.setFlammable(30);

        MAHOGANY_DOOR = register("mahoganyDoor", new Door("Mahogany Door"));
        MAHOGANY_DOOR.setTex(uv("blocks.png", 1, 10), uv("blocks.png", 1, 11));
        MAHOGANY_DOOR.material(Material.WOOD);
        MAHOGANY_DOOR.setFlammable(30);

        MAPLE_DOOR = register("mapleDoor", new Door("Maple Door"));
        MAPLE_DOOR.setTex(uv("blocks.png", 6, 10), uv("blocks.png", 6, 11));
        MAPLE_DOOR.material(Material.WOOD);
        MAPLE_DOOR.setFlammable(30);

        PINE_DOOR = register("pineDoor", new Door("Pine Door"));
        PINE_DOOR.setTex(uv("blocks.png", 5, 10), uv("blocks.png", 5, 11));
        PINE_DOOR.material(Material.WOOD);
        PINE_DOOR.setFlammable(30);

        GLASS_DOOR = register("glassDoor", new Door("Glass Door"));
        GLASS_DOOR.setTex(uv("blocks.png", 4, 10), uv("blocks.png", 4, 11));
        GLASS_DOOR.material(Material.GLASS);

        BRICK_FURNACE = register("brickFurnace", new Furnace("Brick Furnace", false));
        BRICK_FURNACE.setTex(furnaceUVs(5, 4, 6, 4, 7, 4, 8, 4));
        BRICK_FURNACE.material(Material.STONE);

        BRICK_FURNACE_LIT = register("brickFurnaceLit", new Furnace("Brick Furnace", true));
        BRICK_FURNACE_LIT.setTex(furnaceUVs(5, 4, 5, 6, 7, 4, 8, 4));
        BRICK_FURNACE_LIT.material(Material.STONE);
        BRICK_FURNACE_LIT.light(8);

        FURNACE = register("furnace", new Furnace("Furnace", false));
        FURNACE.setTex(furnaceUVs(9, 4, 10, 4, 11, 4, 12, 4));
        FURNACE.material(Material.STONE);

        FURNACE_LIT = register("furnaceLit", new Furnace("Furnace", true));
        FURNACE_LIT.setTex(furnaceUVs(9, 4, 10, 4, 11, 4, 12, 4));
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

        /*NG = register("ng", new Block("Nitroglycerine"));
        NG.setTex(chestUVs(21, 0, 22, 0, 22, 0, 23, 0));
        NG.transparency();*/

        SIGN = register("sign", new SignBlock("Sign"));
        SIGN.setTex(uv("blocks.png", 2, 10), uv("blocks.png", 1, 5));
        SIGN.material(Material.WOOD);
        SIGN.setFlammable(30);

        OAK_FENCE = register("oakFence", new Fence("Oak Fence", 0));
        OAK_FENCE.setTex(uv("blocks.png", 11, 3));
        OAK_FENCE.material(Material.WOOD);
        OAK_FENCE.setFlammable(30);

        MAHOGANY_FENCE = register("mahoganyFence", new Fence("Mahogany Fence", 1));
        MAHOGANY_FENCE.setTex(uv("blocks.png", 12, 3));
        MAHOGANY_FENCE.material(Material.WOOD);
        MAHOGANY_FENCE.setFlammable(30);

        MAPLE_FENCE = register("mapleFence", new Fence("Maple Fence", 2));
        MAPLE_FENCE.setTex(uv("blocks.png", 13, 3));
        MAPLE_FENCE.material(Material.WOOD);
        MAPLE_FENCE.setFlammable(30);

        PINE_FENCE = register("pineFence", new Fence("Pine Fence", 3));
        PINE_FENCE.setTex(uv("blocks.png", 14, 3));
        PINE_FENCE.material(Material.WOOD);
        PINE_FENCE.setFlammable(30);

        /*OAK_GATE = register("oakGate", new Gate("Oak Gate"));
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
        */

        FARMLAND = register("farmland", new Farmland("Farmland"));
        var farmlandUVs = grassUVs(26, 5, 2, 0, 2, 0);
        var wetUVs = uv("blocks.png", 27, 5);
        FARMLAND.setTex(farmlandUVs[0], farmlandUVs[1], farmlandUVs[2], farmlandUVs[3], farmlandUVs[4], farmlandUVs[5], wetUVs);
        renderType[FARMLAND.id] = RenderType.CUSTOM;
        FARMLAND.partialBlock();
        FARMLAND.material(Material.EARTH);
        FARMLAND.setHardness(0.6);

        CROP_WHEAT = (Crop)register("wheatCrop", new Crop("Wheat", 6));
        CROP_WHEAT.setTex(uvRange("blocks.png", 20, 5, 6));
        renderType[CROP_WHEAT.id] = RenderType.CROP;
        CROP_WHEAT.transparency();
        CROP_WHEAT.noCollision();
        CROP_WHEAT.itemLike();
        CROP_WHEAT.waterTransparent();
        CROP_WHEAT.material(Material.ORGANIC);

        CROP_CARROT = (Crop)register("carrotCrop", new Crop("Carrot", 6));
        CROP_CARROT.setTex(uvRange("blocks.png", 20, 6, 6));
        renderType[CROP_CARROT.id] = RenderType.CROP;
        CROP_CARROT.transparency();
        CROP_CARROT.noCollision();
        CROP_CARROT.itemLike();
        CROP_CARROT.waterTransparent();
        CROP_CARROT.material(Material.ORGANIC);

        CROP_TEA = (Crop)register("teaCrop", new Crop("Tea Leaves", 6));
        CROP_TEA.setTex(uvRange("blocks.png", 20, 7, 6));
        renderType[CROP_TEA.id] = RenderType.CROP;
        CROP_TEA.transparency();
        CROP_TEA.noCollision();
        CROP_TEA.itemLike();
        CROP_TEA.waterTransparent();
        CROP_TEA.material(Material.ORGANIC);

        CROP_STRAWBERRY = (Crop)register("strawberryCrop", new Crop("Strawberry", 6));
        CROP_STRAWBERRY.setTex(uvRange("blocks.png", 20, 8, 6));
        renderType[CROP_STRAWBERRY.id] = RenderType.CROP;
        CROP_STRAWBERRY.transparency();
        CROP_STRAWBERRY.noCollision();
        CROP_STRAWBERRY.itemLike();
        CROP_STRAWBERRY.waterTransparent();
        CROP_STRAWBERRY.material(Material.ORGANIC);


        WIRE = register("wire", new Wire("Wire"));
        WIRE.setTex(uv("blocks.png", 0, 19), uv("blocks.png", 1, 19), uv("blocks.png", 2, 19),
            uv("blocks.png", 0, 20), uv("blocks.png", 1, 20), uv("blocks.png", 2, 20));
        renderType[WIRE.id] = RenderType.CUSTOM;
        WIRE.transparency();
        WIRE.noCollision();
        WIRE.itemLike();
        WIRE.material(Material.METAL);
        WIRE.setHardness(0.1);


        // set default hardness for blocks that haven't set it
        for (int i = 0; i < currentID; i++) {
            if (hardness[i] == -0.1) {
                hardness[i] = 1;
            }
        }

        // unbreakable blocks (negative hardness)
        HELLROCK.setHardness(-1);
    }
}