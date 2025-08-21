using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL.vertexformats;
using Molten;
using Silk.NET.Maths;
using Vector3D = Molten.DoublePrecision.Vector3D;

namespace BlockGame.util;


/**
 * For now, we'll only have 65536 blocks for typechecking (ushort -> uint), this can be extended later.
 */
public class Block {
    
    /**
     * The maximum block ID we have. This ID is one past the end!
     * If you want to loop, do for (int i = 0; i &lt;= currentID; i++) { ... }
     * so you won't overread.
     */
    public static int currentID;

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
    
    public ushort metadata {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ushort)(value >> 24);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set => this.value = (this.value & 0xFFFFFF) | ((uint)(value << 24));
    }

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
    
    public static bool[] translucent = new bool[MAXBLOCKS];
    public static bool[] inventoryBlacklist = new bool[MAXBLOCKS];
    public static bool[] randomTick = new bool[MAXBLOCKS];
    public static bool[] liquid = new bool[MAXBLOCKS];
    public static bool[] customCulling = new bool[MAXBLOCKS];
    
    public static bool[] selection = new bool[MAXBLOCKS];
    public static bool[] collision = new bool[MAXBLOCKS];
    public static byte[] lightLevel = new byte[MAXBLOCKS];
    public static byte[] lightAbsorption = new byte[MAXBLOCKS];
    
    public static AABB?[] AABB = new AABB?[MAXBLOCKS];
    public static AABB?[] selectionAABB = new AABB?[MAXBLOCKS];
    public static RenderType[] renderType = new RenderType[MAXBLOCKS];
    
    
    public static Block AIR;
    public static Block GRASS;
    public static Block DIRT;
    public static Block SAND;
    public static Block BASALT;
    public static Block STONE;
    public static Block GRAVEL;
    public static Block HELLSTONE;
    public static Block WORLD_BOTTOM;
    public static Block GLASS;

    public static Block LANTERN;

    public static Block TALL_GRASS;
    public static Block SHORT_GRASS;
    public static Block YELLOW_FLOWER;
    public static Block RED_FLOWER;


    public static Block PLANKS;
    public static Block STAIRS;
    public static Block LOG;
    public static Block LEAVES;
    public static Block MAPLE_PLANKS;
    public static Block MAPLE_STAIRS;
    public static Block MAPLE_LOG;
    public static Block MAPLE_LEAVES;
    //public static Block MAHOGANY_LOG = register(new Block(19, "Mahogany Log", BlockModel.makeCube(Block.grassUVs(7, 5, 6, 5, 8, 5))));
    //public static Block MAHOGANY_LEAVES = register(new Block(20, "Maple Leaves", BlockModel.makeCube(Block.cubeUVs(9, 5))).transparency());

    public static Block METAL_CUBE_BLUE;
    public static Block CANDY_LIGHT_BLUE;
    public static Block CANDY_CYAN;
    public static Block CANDY_TURQUOISE;
    public static Block CANDY_DARK_GREEN;
    public static Block CANDY_LIGHT_GREEN;
    public static Block CANDY_ORANGE;
    public static Block CANDY_YELLOW;
    public static Block CANDY_LIGHT_RED;
    public static Block CANDY_PINK;
    public static Block CANDY_PURPLE;
    public static Block VIOLET;
    public static Block CANDY_RED;
    public static Block CANDY_DARK_BLUE;
    public static Block CANDY_WHITE;
    public static Block CANDY_GREY;
    public static Block CANDY_BLACK;

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
    public static Block HELLSTONE_1;
    public static Block HELLSTONE_2;

    public static Block register(Block block) {
        // update maxid
        currentID = Math.Max(currentID, block.id);
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
        for (int i = 0; i < blocks.Length; i++) {
            if (blocks[i] != null && renderType[i] == RenderType.CUBE) {
                // todo fix
                renderType[i] = RenderType.MODEL;
            }
        }
        AIR = register(new Block(Blocks.AIR, "Air").setModel(BlockModel.emptyBlock())).air();
        GRASS = register(new Block(Blocks.GRASS, "Grass").setModel(BlockModel.makeCube(grassUVs(0, 0, 1, 0, 2, 0))).tick());
        DIRT = register(new Block(Blocks.DIRT, "Dirt").setModel(BlockModel.makeCube(cubeUVs(2, 0))));
        SAND = register(new FallingBlock(Blocks.SAND, "Sand").setModel(BlockModel.makeCube(cubeUVs(3, 0))));
        BASALT = register(new Block(Blocks.BASALT, "Basalt").setModel(BlockModel.makeCube(cubeUVs(4, 0))));
        STONE = register(new Block(Blocks.STONE, "Stone").setModel(BlockModel.makeCube(cubeUVs(5, 0))));
        GRAVEL = register(new Block(Blocks.GRAVEL, "Gravel").setModel(BlockModel.makeCube(cubeUVs(7, 0))));
        HELLSTONE = register(new Block(Blocks.HELLSTONE, "Hellstone").setModel(BlockModel.makeCube(cubeUVs(8, 0))).light(15));
        WORLD_BOTTOM = register(new Block(Blocks.WORLD_BOTTOM, "World_Bottom").setModel(BlockModel.makeCube(cubeUVs(9, 0))));
        GLASS = register(new Block(Blocks.GLASS, "Glass").setModel(BlockModel.makeCube(cubeUVs(6, 0))).transparency());
        LANTERN = register(new Block(Blocks.LANTERN, "Lantern").setModel(BlockModel.makePartialCube(grassUVs(15, 1, 13, 1, 14, 1))).light(15).partialBlock());
        TALL_GRASS = register(new Flower(Blocks.TALL_GRASS, "Tall Grass").setModel(BlockModel.makeGrass(crossUVs(9,1)))).transparency().noCollision();
        SHORT_GRASS = register(new Flower(Blocks.SHORT_GRASS, "Short Grass").setModel(BlockModel.makeGrass(crossUVs(8,1)))).transparency().shortGrassAABB().noCollision();
        YELLOW_FLOWER = register(new Flower(Blocks.YELLOW_FLOWER, "Yellow Flower").setModel(BlockModel.makeGrass(crossUVs(10,1))).transparency().flowerAABB().noCollision());
        RED_FLOWER = register(new Flower(Blocks.RED_FLOWER, "Red Flower").setModel(BlockModel.makeGrass(crossUVs(11,1))).transparency().flowerAABB().noCollision());
        PLANKS = register(new Block(Blocks.PLANKS, "Planks").setModel(BlockModel.makeCube(cubeUVs(0, 5))));
        STAIRS = register(new Stairs(Blocks.STAIRS, "Stairs").setModel(BlockModel.makeCube(cubeUVs(0, 5))).partialBlock());
        LOG = register(new Block(Blocks.LOG, "Log").setModel(BlockModel.makeCube(grassUVs(2, 5, 1, 5, 3, 5))));
        LEAVES = register(new Block(Blocks.LEAVES, "Leaves").setModel(BlockModel.makeCube(cubeUVs(4, 5))).transparency().setLightAbsorption(1));
        MAPLE_PLANKS = register(new Block(Blocks.MAPLE_PLANKS, "Maple Planks").setModel(BlockModel.makeCube(cubeUVs(5, 5))));
        MAPLE_STAIRS = register(new Stairs(Blocks.MAPLE_STAIRS, "Maple Stairs").setModel(BlockModel.makeCube(cubeUVs(5, 5))).partialBlock());
        MAPLE_LOG = register(new Block(Blocks.MAPLE_LOG, "Maple Log").setModel(BlockModel.makeCube(grassUVs(7, 5, 6, 5, 8, 5))));
        MAPLE_LEAVES = register(new Block(Blocks.MAPLE_LEAVES, "Maple Leaves").setModel(BlockModel.makeCube(cubeUVs(9, 5))).transparency());
        METAL_CUBE_BLUE = register(new Block(Blocks.METAL_CUBE_BLUE, "Blue Metal Block").setModel(BlockModel.makeCube(cubeUVs(12, 1))));
        CANDY_LIGHT_BLUE = register(new Block(Blocks.CANDY_LIGHT_BLUE, "Light Blue Candy").setModel(BlockModel.makeCube(cubeUVs(0, 2))));
        CANDY_CYAN = register(new Block(Blocks.CANDY_CYAN, "Cyan Candy").setModel(BlockModel.makeCube(cubeUVs(1, 2))));
        CANDY_TURQUOISE = register(new Block(Blocks.CANDY_TURQUOISE, "Turquoise Candy").setModel(BlockModel.makeCube(cubeUVs(2, 2))));
        CANDY_DARK_GREEN = register(new Block(Blocks.CANDY_DARK_GREEN, "Dark Green Candy").setModel(BlockModel.makeCube(cubeUVs(3, 2))));
        CANDY_LIGHT_GREEN = register(new Block(Blocks.CANDY_LIGHT_GREEN, "Light Green Candy").setModel(BlockModel.makeCube(cubeUVs(4, 2))));
        CANDY_ORANGE = register(new Block(Blocks.CANDY_ORANGE, "Orange Candy").setModel(BlockModel.makeCube(cubeUVs(5, 2))));
        CANDY_YELLOW = register(new Block(Blocks.CANDY_YELLOW, "Yellow Candy").setModel(BlockModel.makeCube(cubeUVs(6, 2))));
        CANDY_LIGHT_RED = register(new Block(Blocks.CANDY_LIGHT_RED, "Light Red Candy").setModel(BlockModel.makeCube(cubeUVs(7, 2))));
        CANDY_PINK = register(new Block(Blocks.CANDY_PINK, "Pink Candy").setModel(BlockModel.makeCube(cubeUVs(8, 2))));
        CANDY_PURPLE = register(new Block(Blocks.CANDY_PURPLE, "Purple Candy").setModel(BlockModel.makeCube(cubeUVs(9, 2))));
        VIOLET = register(new Block(Blocks.VIOLET, "Violet Candy").setModel(BlockModel.makeCube(cubeUVs(10, 2))));
        CANDY_RED = register(new Block(Blocks.CANDY_RED, "Red Candy").setModel(BlockModel.makeCube(cubeUVs(11, 2))));
        CANDY_DARK_BLUE = register(new Block(Blocks.CANDY_DARK_BLUE, "Dark Blue Candy").setModel(BlockModel.makeCube(cubeUVs(12, 2))));
        CANDY_WHITE = register(new Block(Blocks.CANDY_WHITE, "White Candy").setModel(BlockModel.makeCube(cubeUVs(13, 2))));
        CANDY_GREY = register(new Block(Blocks.CANDY_GREY, "Grey Candy").setModel(BlockModel.makeCube(cubeUVs(14, 2))));
        CANDY_BLACK = register(new Block(Blocks.CANDY_BLACK, "Black Candy").setModel(BlockModel.makeCube(cubeUVs(15, 2))));
        HEAD = register(new Block(Blocks.HEAD, "Head").setModel(BlockModel.makeHalfCube(HeadUVs(0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3))).partialBlock());
        WATER = register(new Water(Blocks.WATER, "Water").setModel(BlockModel.makeLiquid(cubeUVs(0, 4))).makeLiquid());
        RED_ORE = register(new Block(Blocks.RED_ORE, "Red Ore").setModel(BlockModel.makeCube(cubeUVs(10, 0))));
        TITANIUM_ORE = register(new Block(Blocks.TITANIUM_ORE, "Titanium Ore").setModel(BlockModel.makeCube(cubeUVs(11, 0))));
        AMBER_ORE = register(new Block(Blocks.AMBER_ORE, "Amber Ore").setModel(BlockModel.makeCube(cubeUVs(12, 0))));
        AMETHYST_ORE = register(new Block(Blocks.AMETHYST_ORE, "Amethyst Ore").setModel(BlockModel.makeCube(cubeUVs(13, 0))));
        EMERALD_ORE = register(new Block(Blocks.EMERALD_ORE, "Emerald Ore").setModel(BlockModel.makeCube(cubeUVs(14, 0))));
        DIAMOND_ORE = register(new Block(Blocks.DIAMOND_ORE, "Diamond Ore").setModel(BlockModel.makeCube(cubeUVs(15, 0))));
        GOLD_ORE = register(new Block(Blocks.GOLD_ORE, "Gold Ore").setModel(BlockModel.makeCube(cubeUVs(0, 1))));
        IRON_ORE = register(new Block(Blocks.IRON_ORE, "Iron Ore").setModel(BlockModel.makeCube(cubeUVs(1, 1))));
        COAL_ORE = register(new Block(Blocks.COAL_ORE, "Coal Ore").setModel(BlockModel.makeCube(cubeUVs(4, 1))));
        HELLSTONE_1 = register(new Block(Blocks.HELLSTONE_1, "Hellstone1").setModel(BlockModel.makeCube(grassUVs(8, 0, 9,0, 9,0))));
        HELLSTONE_2 = register(new Block(Blocks.HELLSTONE_2, "Hellstone2").setModel(BlockModel.makeCube(grassUVs(8, 0, 7,1, 7,1))));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort getMetadata() {
        return (ushort)(value >> 24);
    }
    
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
            translucent[blocks[i].id] = blocks[i].layer == RenderLayer.TRANSLUCENT;
        }
        inventoryBlacklist[Blocks.WATER] = true;
        //inventoryBlacklist[7] = true;
    }


    public static bool isFullBlock(int id) {
        return fullBlock[id];
    }

    public static bool isBlacklisted(int block) {
        return inventoryBlacklist[block];
    }
    

    //public static Block TORCH = register(new Block(Blocks.TORCH, "Torch", BlockModel.makeTorch(grassUVs(4, 1,0, 1, 4,1))).partialBlock().torchAABB().light(8).transparency());

    public static bool isSolid(int block) {
        return block != 0 && get(block).layer == RenderLayer.SOLID;
    }

    public static bool notSolid(int block) {
        return block == 0 || get(block).layer != RenderLayer.SOLID;
    }

    public static bool isTransparent(int block) {
        return block != 0 && !fullBlock[block];
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
        return block.id != 0 && !fullBlock[block.id];
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
        return new Vector2D<Half>((Half)(x * atlasRatio), (Half)(y * atlasRatio));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D<Half> texCoordsH(UVPair uv) {
        return new Vector2D<Half>((Half)(uv.u * atlasRatio), (Half)(uv.v * atlasRatio));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 texCoords(float x, float y) {
        return new Vector2(x * atlasRatio, y * atlasRatio);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 texCoords(UVPair uv) {
        return new Vector2(uv.u * atlasRatio, uv.v * atlasRatio);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float texU(float u) {
        return u * atlasRatio;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float texV(float v) {
        return v * atlasRatio;
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

    public Block(ushort id, string name) {
        this.id = id;
        this.name = name;
        
        fullBlock[id] = true;
        selection[id] = true;
        collision[id] = true;
        liquid[id] = false;
        customCulling[id] = false;
        randomTick[id] = false;

        AABB[id] = fullBlockAABB();
        selectionAABB[id] = fullBlockAABB();
    }

    public Block setModel(BlockModel model) {
        this.model = model;
        return this;
    }
    
    public Block setTex(UVPair[] uvs) {
        this.uvs = uvs;
        return this;
    }

    public Block transparency() {
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
    
    public Block setLightAbsorption(byte amount) {
        lightAbsorption[id] = amount;
        return this;
    }


    public virtual void update(World world, Vector3I pos) {

    }
    
    [ClientOnly]
    public virtual void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        // setup
        br.setupWorld();
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
        
        if (model == null || model.faces.Length == 0) {
            // no model, no particles
            return;
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

                    var s = Game.clientRandom.NextSingle();
                    s *= s;
                    var speed = (s + 1) * 0.8f;

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
        var direction = Direction.getDirection(dir);
        var neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();
        
        // if it's not a full block, we render the face
        return !fullBlock[neighbourBlock];
    }

    public virtual void place(World world, int x, int y, int z, RawDirection dir) {
        world.setBlockMetadataRemesh(x, y, z, id);
        world.blockUpdateWithNeighbours(new Vector3I(x, y, z));
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

    public override void update(World world, Vector3I pos) {
        if (world.inWorld(pos.X, pos.Y - 1, pos.Z) && world.getBlock(pos.X, pos.Y - 1, pos.Z) == 0) {
            world.setBlockRemesh(pos.X, pos.Y, pos.Z, Blocks.AIR);
        }
    }
}

public class Water : Block {
    public Water(ushort id, string name) : base(id, name) {
        lightAbsorption[id] = 1;
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
    }

    public override void update(World world, Vector3I pos) {
        foreach (var dir in Direction.directionsWaterSpread) {
            // queue block updates
            var neighbourBlock = pos + dir;
            if (world.getBlock(neighbourBlock) == Blocks.AIR) {
                world.runLater(neighbourBlock, () => {
                    if (world.getBlock(neighbourBlock) == Blocks.AIR) {
                        world.setBlockRemesh(neighbourBlock.X, neighbourBlock.Y, neighbourBlock.Z, Blocks.WATER);
                    }
                }, 10);
                world.blockUpdate(neighbourBlock, 10);
            }
        }
    }
    
    /** Water doesn't get rendered next to water, but always gets rendered on the top face */
    public override bool cullFace(BlockRenderer br, int x, int y, int z, RawDirection dir) {
        var direction = Direction.getDirection(dir);
        var same = br.getBlockCached(direction.X, direction.Y, direction.Z).getID() == br.getBlock().getID();
        if (same) {
            return false;
        }
        return dir == RawDirection.UP || base.cullFace(br, x, y, z, dir);
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);
        
        const float height = 15f / 16f; // Water level is slightly below full height
        
        // Get texture coordinates for water
        var waterTexture = model.faces[0];
        var min = texCoords(waterTexture.min.u, waterTexture.min.v);
        var max = texCoords(waterTexture.max.u, waterTexture.max.v);
        
        var uMin = min.X;
        var vMin = min.Y;
        var uMax = max.X;
        var vMax = max.Y;
        
        Span<BlockVertexPacked> cache = stackalloc BlockVertexPacked[4];
        Span<Vector4> colourCache = stackalloc Vector4[4];
        Span<byte> lightColourCache = stackalloc byte[4];

        for (RawDirection d = 0; d < RawDirection.MAX; d++) {
            
            if (cullFace(br, x, y, z, d)) {
                br.applyFaceLighting(d, colourCache, lightColourCache);
                br.begin(cache);
                switch (d) {
                    case RawDirection.WEST:
                        br.vertex(x + 0, y + 1, z + 1, uMin, vMin);
                        br.vertex(x + 0, y + 0, z + 1, uMin, vMax);
                        br.vertex(x + 0, y + 0, z + 0, uMax, vMax);
                        br.vertex(x + 0, y + 1, z + 0, uMax, vMin);
                        break;
                    case RawDirection.EAST:
                        br.vertex(x + 1, y + 1, z + 0, uMin, vMin);
                        br.vertex(x + 1, y + 0, z + 0, uMin, vMax);
                        br.vertex(x + 1, y + 0, z + 1, uMax, vMax);
                        br.vertex(x + 1, y + 1, z + 1, uMax, vMin);
                        break;
                    case RawDirection.SOUTH:
                        br.vertex(x + 0, y + 1, z + 0, uMin, vMin);
                        br.vertex(x + 0, y + 0, z + 0, uMin, vMax);
                        br.vertex(x + 1, y + 0, z + 0, uMax, vMax);
                        br.vertex(x + 1, y + 1, z + 0, uMax, vMin);
                        break;
                    case RawDirection.NORTH:
                        br.vertex(x + 1, y + 1, z + 1, uMin, vMin);
                        br.vertex(x + 1, y + 0, z + 1, uMin, vMax);
                        br.vertex(x + 0, y + 0, z + 1, uMax, vMax);
                        br.vertex(x + 0, y + 1, z + 1, uMax, vMin);
                        break;
                    case RawDirection.DOWN:
                        br.vertex(x + 1, y + 0, z + 1, uMin, vMin);
                        br.vertex(x + 1, y + 0, z + 0, uMin, vMax);
                        br.vertex(x + 0, y + 0, z + 0, uMax, vMax);
                        br.vertex(x + 0, y + 0, z + 1, uMax, vMin);
                        break;
                    case RawDirection.UP:
                        br.vertex(x + 0, y + height, z + 1, uMin, vMin);
                        br.vertex(x + 0, y + height, z + 0, uMin, vMax);
                        br.vertex(x + 1, y + height, z + 0, uMax, vMax);
                        br.vertex(x + 1, y + height, z + 1, uMax, vMin);
                        break;
                }
                br.end(vertices);
            }
        }
    }
}

public class Stairs : Block {
    public Stairs(ushort id, string name) : base(id, name) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
    }

    /**
     * Metadata encoding for stairs:
     * Bits 0-1: Horizontal facing direction (0=WEST, 1=EAST, 2=SOUTH, 3=NORTH)
     * Bit 2: Upside-down (0=normal/bottom-half, 1=upside-down/top-half) (NOT USED YET)
     * Bits 3-7: Reserved
     */
    public static byte getFacing(byte metadata) => (byte)(metadata & 0b11);
    public static bool isUpsideDown(byte metadata) => (metadata & 0b100) != 0;
    public static byte setFacing(byte metadata, byte facing) => (byte)((metadata & ~0b11) | (facing & 0b11));
    public static byte setUpsideDown(byte metadata, bool upsideDown) => (byte)((metadata & ~0b100) | (upsideDown ? 0b100 : 0));
    
    private uint calculateMetadata(ushort blockId, RawDirection dir) {
        
        // we need to place in the opposite direction the player is facing
        var opposite = Direction.getOpposite(dir);
        
        // Create metadata
        byte metadata = 0;
        metadata = setFacing(metadata, (byte)opposite);
        metadata = setUpsideDown(metadata, false);
        
        // Create full block value with metadata
        uint blockValue = blockId;
        blockValue = blockValue.setMetadata(metadata);
        
        return blockValue;
    }


    public override void place(World world, int x, int y, int z, RawDirection dir) {
        
        var stair = calculateMetadata(id, dir);
        world.setBlockMetadataRemesh(x, y, z, stair);
        world.blockUpdateWithNeighbours(new Vector3I(x, y, z));
        
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);
        
        var block = br.getBlock();
        var metadata = block.getMetadata();
        var facing = getFacing(metadata);

        var texture = model.faces[0];
        var baseUMin = texU(texture.min.u);
        var baseVMin = texV(texture.min.v);
        var baseUMax = texU(texture.max.u);
        var baseVMax = texV(texture.max.v);
        
        Span<BlockVertexPacked> cache = stackalloc BlockVertexPacked[4];
        Span<Vector4> colourCache = stackalloc Vector4[4];
        Span<byte> lightColourCache = stackalloc byte[4];

        // bottom step: full width/depth, half height
        const float bx1 = 0f;
        const float by1 = 0f;
        const float bz1 = 0f;
        const float bx2 = 1f;
        const float by2 = 0.5f;
        const float bz2 = 1f;

        // top step: half width/depth in facing direction, half height
        float tx1, tz1, tx2, tz2;
        const float ty1 = 0.5f;
        const float ty2 = 1f;
        
        switch (facing) {
            case 0: tx1 = 0.5f; tx2 = 1f; tz1 = 0f; tz2 = 1f; break; // WEST
            case 1: tx1 = 0f; tx2 = 0.5f; tz1 = 0f; tz2 = 1f; break; // EAST
            case 2: tx1 = 0f; tx2 = 1f; tz1 = 0.5f; tz2 = 1f; break; // SOUTH
            default: tx1 = 0f; tx2 = 1f; tz1 = 0f; tz2 = 0.5f; break; // NORTH
        }

        // render both steps
        var uRange = baseUMax - baseUMin;
        var vRange = baseVMax - baseVMin;

        // WEST face
        var westVMin = baseVMin + vRange * 0.5f;
        var direction = Direction.getDirection(RawDirection.WEST);
        var neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();
        
        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.WEST, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + bx1, y + by2, z + bz2, baseUMin, westVMin);
            br.vertex(x + bx1, y + by1, z + bz2, baseUMin, baseVMax);
            br.vertex(x + bx1, y + by1, z + bz1, baseUMax, baseVMax);
            br.vertex(x + bx1, y + by2, z + bz1, baseUMax, westVMin);
            br.end(vertices);
        }

        // EAST face
        var eastVMin = baseVMin + vRange * 0.5f;

        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.EAST, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + bx2, y + by2, z + bz1, baseUMin, eastVMin);
            br.vertex(x + bx2, y + by1, z + bz1, baseUMin, baseVMax);
            br.vertex(x + bx2, y + by1, z + bz2, baseUMax, baseVMax);
            br.vertex(x + bx2, y + by2, z + bz2, baseUMax, eastVMin);
            br.end(vertices);
        }

        // SOUTH face
        var southVMin = baseVMin + vRange * 0.5f;
        direction = Direction.getDirection(RawDirection.SOUTH);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();

        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.SOUTH, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + bx1, y + by2, z + bz1, baseUMin, southVMin);
            br.vertex(x + bx1, y + by1, z + bz1, baseUMin, baseVMax);
            br.vertex(x + bx2, y + by1, z + bz1, baseUMax, baseVMax);
            br.vertex(x + bx2, y + by2, z + bz1, baseUMax, southVMin);
            br.end(vertices);
        }

        // NORTH face
        var northVMin = baseVMin + vRange * 0.5f;
         direction = Direction.getDirection(RawDirection.NORTH);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();
        
        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.NORTH, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + bx2, y + by2, z + bz2, baseUMin, northVMin);
            br.vertex(x + bx2, y + by1, z + bz2, baseUMin, baseVMax);
            br.vertex(x + bx1, y + by1, z + bz2, baseUMax, baseVMax);
            br.vertex(x + bx1, y + by2, z + bz2, baseUMax, northVMin);
            br.end(vertices);
        }

        // DOWN face
        direction = Direction.getDirection(RawDirection.DOWN);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();

        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.DOWN, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + bx2, y + by1, z + bz2, baseUMin, baseVMin);
            br.vertex(x + bx2, y + by1, z + bz1, baseUMin, baseVMax);
            br.vertex(x + bx1, y + by1, z + bz1, baseUMax, baseVMax);
            br.vertex(x + bx1, y + by1, z + bz2, baseUMax, baseVMin);
            br.end(vertices);
        }

        // UP face  
        direction = Direction.getDirection(RawDirection.UP);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();
            
        bool shouldRender5 = !fullBlock[neighbourBlock] || true;

        if (shouldRender5) {
            br.applyFaceLighting(RawDirection.UP, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + bx1, y + by2, z + bz2, baseUMin, baseVMin);
            br.vertex(x + bx1, y + by2, z + bz1, baseUMin, baseVMax);
            br.vertex(x + bx2, y + by2, z + bz1, baseUMax, baseVMax);
            br.vertex(x + bx2, y + by2, z + bz2, baseUMax, baseVMin);
            br.end(vertices);
        }

        // WEST face
        var westUMin1 = tx1 > 0f ? baseUMin + uRange * tz1 : baseUMin;
        var westUMax1 = tx1 > 0f ? baseUMin + uRange * tz2 : baseUMax;
        var westVMin1 = baseVMin + vRange * 0.5f;
        direction = Direction.getDirection(RawDirection.WEST);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();
        
        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.WEST, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + tx1, y + ty2, z + tz2, westUMin1, westVMin1);
            br.vertex(x + tx1, y + ty1, z + tz2, westUMin1, baseVMax);
            br.vertex(x + tx1, y + ty1, z + tz1, westUMax1, baseVMax);
            br.vertex(x + tx1, y + ty2, z + tz1, westUMax1, westVMin1);
            br.end(vertices);
        }

        // EAST face
        var eastUMin1 = tx2 < 1f ? baseUMin + uRange * tz1 : baseUMin;
        var eastUMax1 = tx2 < 1f ? baseUMin + uRange * tz2 : baseUMax;
        var eastVMin1 = baseVMin + vRange * 0.5f;
        direction = Direction.getDirection(RawDirection.EAST);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();

        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.EAST, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + tx2, y + ty2, z + tz1, eastUMin1, eastVMin1);
            br.vertex(x + tx2, y + ty1, z + tz1, eastUMin1, baseVMax);
            br.vertex(x + tx2, y + ty1, z + tz2, eastUMax1, baseVMax);
            br.vertex(x + tx2, y + ty2, z + tz2, eastUMax1, eastVMin1);
            br.end(vertices);
        }

        // SOUTH face
        var southUMin1 = tz1 > 0f ? baseUMin + uRange * tx1 : baseUMin;
        var southUMax1 = tz1 > 0f ? baseUMin + uRange * tx2 : baseUMax;
        var southVMin1 = baseVMin + vRange * 0.5f;
        direction = Direction.getDirection(RawDirection.SOUTH);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();

        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.SOUTH, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + tx1, y + ty2, z + tz1, southUMin1, southVMin1);
            br.vertex(x + tx1, y + ty1, z + tz1, southUMin1, baseVMax);
            br.vertex(x + tx2, y + ty1, z + tz1, southUMax1, baseVMax);
            br.vertex(x + tx2, y + ty2, z + tz1, southUMax1, southVMin1);
            br.end(vertices);
        }

        // NORTH face
        var northUMin1 = tz2 < 1f ? baseUMin + uRange * tx1 : baseUMin;
        var northUMax1 = tz2 < 1f ? baseUMin + uRange * tx2 : baseUMax;
        var northVMin1 = baseVMin + vRange * 0.5f;
        direction = Direction.getDirection(RawDirection.NORTH);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();

        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.NORTH, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + tx2, y + ty2, z + tz2, northUMin1, northVMin1);
            br.vertex(x + tx2, y + ty1, z + tz2, northUMin1, baseVMax);
            br.vertex(x + tx1, y + ty1, z + tz2, northUMax1, baseVMax);
            br.vertex(x + tx1, y + ty2, z + tz2, northUMax1, northVMin1);
            br.end(vertices);
        }

        // DOWN face
        var downUMin1 = (tx2 - tx1 < 1f || tz2 - tz1 < 1f) ? baseUMin + uRange * tx1 : baseUMin;
        var downUMax1 = (tx2 - tx1 < 1f || tz2 - tz1 < 1f) ? baseUMin + uRange * tx2 : baseUMax;
        var downVMin1 = (tx2 - tx1 < 1f || tz2 - tz1 < 1f) ? baseVMin + vRange * tz1 : baseVMin;
        var downVMax1 = (tx2 - tx1 < 1f || tz2 - tz1 < 1f) ? baseVMin + vRange * tz2 : baseVMax;
        direction = Direction.getDirection(RawDirection.DOWN);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();

        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.DOWN, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + tx2, y + ty1, z + tz2, downUMin1, downVMin1);
            br.vertex(x + tx2, y + ty1, z + tz1, downUMin1, downVMax1);
            br.vertex(x + tx1, y + ty1, z + tz1, downUMax1, downVMax1);
            br.vertex(x + tx1, y + ty1, z + tz2, downUMax1, downVMin1);
            br.end(vertices);
        }

        // UP face  
        var upUMin1 = (tx2 - tx1 < 1f || tz2 - tz1 < 1f) ? baseUMin + uRange * tx1 : baseUMin;
        var upUMax1 = (tx2 - tx1 < 1f || tz2 - tz1 < 1f) ? baseUMin + uRange * tx2 : baseUMax;
        var upVMin1 = (tx2 - tx1 < 1f || tz2 - tz1 < 1f) ? baseVMin + vRange * tz1 : baseVMin;
        var upVMax1 = (tx2 - tx1 < 1f || tz2 - tz1 < 1f) ? baseVMin + vRange * tz2 : baseVMax;
        direction = Direction.getDirection(RawDirection.UP);
        neighbourBlock = br.getBlockCached(direction.X, direction.Y, direction.Z).getID();

        if (!fullBlock[neighbourBlock]) {
            br.applyFaceLighting(RawDirection.UP, colourCache, lightColourCache);
            br.begin(cache);
            br.vertex(x + tx1, y + ty2, z + tz2, upUMin1, upVMin1);
            br.vertex(x + tx1, y + ty2, z + tz1, upUMin1, upVMax1);
            br.vertex(x + tx2, y + ty2, z + tz1, upUMax1, upVMax1);
            br.vertex(x + tx2, y + ty2, z + tz2, upUMax1, upVMin1);
            br.end(vertices);
        }
    }
}

public class FallingBlock(ushort id, string name) : Block(id, name) {
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
            world.setBlockRemesh(pos.X, y + 1, pos.Z, getID());
        }

        // if sand above, update
        if (world.getBlock(new Vector3I(pos.X, pos.Y + 1, pos.Z)) == getID()) {
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
    CROSS,
    MODEL,
    CUSTOM
}