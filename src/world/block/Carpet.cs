using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class Carpet : Block {
    // orientation constants
    public const byte FLOOR = 0;    // on top face (y+)
    public const byte CEILING = 1;  // on bottom face (y-)
    public const byte NORTH = 2;    // on north face (z+)
    public const byte SOUTH = 3;    // on south face (z-)
    public const byte EAST = 4;     // on east face (x+)
    public const byte WEST = 5;     // on west face (x-)

    public Carpet(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
        partialBlock();
        transparency();
        tick();
        material(Material.ORGANIC);
        setHardness(0.1);

        // setup UV array for 24 colors (same as CandyBlock)
        uvs = new UVPair[24];
        for (int i = 0; i < 24; i++) {
            int row = i / 16;
            int col = i % 16;
            uvs[i] = uv("blocks.png", col, 6 + row);
        }
    }

    protected override BlockItem createItem() {
        return new CarpetItem(this);
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        // check all 6 directions for solid blocks
        bool solidAbove = world.inWorld(x, y + 1, z) && fullBlock[world.getBlock(x, y + 1, z)];
        bool solidBelow = world.inWorld(x, y - 1, z) && fullBlock[world.getBlock(x, y - 1, z)];
        bool solidNorth = world.inWorld(x, y, z + 1) && fullBlock[world.getBlock(x, y, z + 1)];
        bool solidSouth = world.inWorld(x, y, z - 1) && fullBlock[world.getBlock(x, y, z - 1)];
        bool solidEast = world.inWorld(x + 1, y, z) && fullBlock[world.getBlock(x + 1, y, z)];
        bool solidWest = world.inWorld(x - 1, y, z) && fullBlock[world.getBlock(x - 1, y, z)];

        return solidAbove || solidBelow || solidNorth || solidSouth || solidEast || solidWest;
    }

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        byte orientation = info.face switch {
            RawDirection.UP => FLOOR,
            RawDirection.DOWN => CEILING,
            RawDirection.NORTH => SOUTH,
            RawDirection.SOUTH => NORTH,
            RawDirection.EAST => WEST,
            RawDirection.WEST => EAST,
            _ => FLOOR
        };

        // preserve color from metadata, set orientation
        byte color = getColor(metadata);
        metadata = 0;
        metadata = setColor(metadata, color);
        metadata = setOrientation(metadata, orientation);

        uint blockValue = id;
        blockValue = blockValue.setMetadata(metadata);

        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
    }

    /**
     * Metadata encoding for carpet:
     * Bits 0-4 (5 bits): Color index (0-23 for 24 colors)
     * Bits 5-7 (3 bits): Orientation (0-5 for 6 faces)
     */
    public static byte getColor(byte metadata) => (byte)(metadata & 0x1F);
    public static byte setColor(byte metadata, byte color) => (byte)((metadata & 0xE0) | (color & 0x1F));
    public static byte getOrientation(byte metadata) => (byte)((metadata >> 5) & 0x07);
    public static byte setOrientation(byte metadata, byte orientation) => (byte)((metadata & 0x1F) | ((orientation & 0x07) << 5));

    public override byte maxValidMetadata() => 191; // 24 colors * 8 orientations - 1 (but only 6 used)

    public string getName(byte metadata) => $"{CandyBlock.colourNames[getColor(metadata)]} Carpet";

    public override UVPair getTexture(int faceIdx, int metadata) {
        var color = getColor((byte)metadata);
        return uvs[color];
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        if (!canBreak) return;

        // extract color and create carpet with FLOOR orientation
        byte color = getColor(metadata);
        byte dropMeta = 0;
        dropMeta = setColor(dropMeta, color);
        dropMeta = setOrientation(dropMeta, FLOOR);

        drops.Add(new ItemStack(item, 1, dropMeta));
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        byte orientation = getOrientation(metadata);
        const float t = 1f / 16f; // thickness

        aabbs.Add(orientation switch {
            FLOOR => new AABB(x, y, z, x + 1f, y + t, z + 1f),
            CEILING => new AABB(x, y + 1f - t, z, x + 1f, y + 1f, z + 1f),
            NORTH => new AABB(x, y, z + 1f - t, x + 1f, y + 1f, z + 1f),
            SOUTH => new AABB(x, y, z, x + 1f, y + 1f, z + t),
            EAST => new AABB(x + 1f - t, y, z, x + 1f, y + 1f, z + 1f),
            WEST => new AABB(x, y, z, x + t, y + 1f, z + 1f),
            _ => new AABB(x, y, z, x + 1f, y + t, z + 1f)
        });
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);

        x &= 15;
        y &= 15;
        z &= 15;

        var block = br.getBlock();
        var metadata = block.getMetadata();
        byte orientation = getOrientation(metadata);
        var color = getColor(metadata);

        var min = uvs[color];
        var max = uvs[color] + 1;

        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = br.forceTex;
            max = br.forceTex + 1;
        }

        var uv0 = UVPair.texCoords(min);
        var uv1 = UVPair.texCoords(max);
        float u0 = uv0.X;
        float v0 = uv0.Y;
        float u1 = uv1.X;
        float v1 = uv1.Y;

        const float t = 1f / 16f; // thickness

        // render based on orientation
        switch (orientation) {
            case FLOOR:
                br.renderCube(x, y, z, vertices, 0f, 0f, 0f, 1f, t, 1f, u0, v0, u1, v1);
                break;
            case CEILING:
                br.renderCube(x, y, z, vertices, 0f, 1f - t, 0f, 1f, 1f, 1f, u0, v0, u1, v1);
                break;
            case NORTH:
                br.renderCube(x, y, z, vertices, 0f, 0f, 1f - t, 1f, 1f, 1f, u0, v0, u1, v1);
                break;
            case SOUTH:
                br.renderCube(x, y, z, vertices, 0f, 0f, 0f, 1f, 1f, t, u0, v0, u1, v1);
                break;
            case EAST:
                br.renderCube(x, y, z, vertices, 1f - t, 0f, 0f, 1f, 1f, 1f, u0, v0, u1, v1);
                break;
            case WEST:
                br.renderCube(x, y, z, vertices, 0f, 0f, 0f, t, 1f, 1f, u0, v0, u1, v1);
                break;
        }
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        // right-click on carpet removes it
        var block = world.getBlockRaw(x, y, z);
        var metadata = block.getMetadata();

        // drop the carpet as an item
        var drops = new List<ItemStack>();
        getDrop(drops, world, x, y, z, metadata, true);
        foreach (var drop in drops) {
            player.inventory.addItem(drop);
        }

        // remove the carpet
        world.setBlock(x, y, z, AIR.id);

        return true;
    }

    public override void update(World world, int x, int y, int z) {
        var block = world.getBlockRaw(x, y, z);
        var metadata = block.getMetadata();
        byte orientation = getOrientation(metadata);

        // check if supporting block still exists based on orientation
        ushort supportingBlockId = orientation switch {
            FLOOR => world.getBlock(x, y - 1, z),
            CEILING => world.getBlock(x, y + 1, z),
            NORTH => world.getBlock(x, y, z + 1),
            SOUTH => world.getBlock(x, y, z - 1),
            EAST => world.getBlock(x + 1, y, z),
            WEST => world.getBlock(x - 1, y, z),
            _ => (ushort)0
        };

        // if supporting block is air or non-solid, break the carpet
        if (supportingBlockId == 0 || !fullBlock[supportingBlockId]) {
            world.setBlock(x, y, z, AIR.id);
        }
    }
}
