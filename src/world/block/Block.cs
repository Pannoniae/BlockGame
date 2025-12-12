using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.entity;
using BlockGame.world.item;
using Molten;
using Vector3D = Molten.DoublePrecision.Vector3D;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace BlockGame.world.block;

/**
 * For now, we'll only have 65536 blocks for typechecking (ushort -> uint), this can be extended later.
 */
[SuppressMessage("Compiler",
    "CS8618:Non-nullable field must contain a non-null value when exiting constructor. Consider adding the \'required\' modifier or declaring as nullable.")]
public partial class Block {
    private const int particleCount = 4;

    public static readonly List<ItemStack> drops = [];

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
    public readonly string name;

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
    public static Vector2I atlasSize = new Vector2I(512, 512);
    public const int textureSize = 16;

    public static Vector2 atlasRatio = new Vector2(textureSize) / new Vector2(atlasSize.X, atlasSize.Y);
    public static Vector2 atlasRatioInv = new Vector2(atlasSize.X, atlasSize.Y) / new Vector2(textureSize);

    /// <summary>
    /// Update atlas size after loading texture pack. Recalculates atlasRatio.
    /// </summary>
    public static void updateAtlasSize(int width, int height) {
        atlasSize = new Vector2I(width, height);
        atlasRatio = new Vector2(textureSize) / new Vector2(atlasSize.X, atlasSize.Y);
        atlasRatioInv = new Vector2(atlasSize.X, atlasSize.Y) / new Vector2(textureSize);
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

    // track leaf texture tiles for atlas post-processing
    // TODO maybe dont have this array, just iterate over all block tiles and if leaves[id] is true then go to the uvs[0], make opaque? or is that worse? IDK
    public static readonly HashSet<(string source, int tx, int ty)> leafTextureTiles = [];
    public static XUList<AABB?> AABB => Registry.BLOCKS.AABB;
    public static XUList<bool> customAABB => Registry.BLOCKS.customAABB;
    public static XUList<RenderType> renderType => Registry.BLOCKS.renderType;
    public static XUList<ToolType> tool => Registry.BLOCKS.tool;
    public static XUList<bool> optionalTool => Registry.BLOCKS.optionalTool;
    public static XUList<MaterialTier> tier => Registry.BLOCKS.tier;
    public static XUList<float> friction => Registry.BLOCKS.friction;
    public static XUList<bool> natural => Registry.BLOCKS.natural;


    public static XUList<bool> noItem => Registry.BLOCKS.noItem;

    public static XUList<bool> isBlockEntity => Registry.BLOCKS.isBlockEntity;

    public static XUList<bool> circuit => Registry.BLOCKS.circuit;

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

    public static T register<T>(string stringID, T block) where T : Block {
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

        updateLeafRenderMode();
    }

    public static void updateLeafRenderMode() {
        bool fast = Settings.instance.fastLeaves;
        for (int i = 0; i < currentID; i++) {
            if (leaves[i]) {
                fullBlock[i] = fast;
            }
        }
    }

    // todo this is a giant hack, do something better?
    public static void registerLeafTexture(string source, int x, int y) {
        if (!source.StartsWith("textures/")) {
            source = "textures/" + source;
        }

        leafTextureTiles.Add((source, x, y));
    }


    public static bool isFullBlock(int id) {
        return fullBlock[id];
    }

    //public static Block TORCH = register(new Block(Blocks.TORCH, "Torch", BlockModel.makeTorch(grassUVs(4, 1,0, 1, 4,1))).partialBlock().torchAABB().light(8).transparency());


    // this will pack the data into the uint


    // ivec2 lightCoords = ivec2((lightValue >> 4) & 0xFu, lightValue & 0xFu);
    // compute tint (light * ao * direction)
    // per-face lighting
    // float lColor = a[direction]
    // tint = texelFetch(lightTexture, lightCoords, 0) * a[direction] * aoArray[aoValue];

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
    CROP,
    WIRE
}