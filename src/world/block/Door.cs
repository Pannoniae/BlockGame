using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;

namespace BlockGame.world.block;

public class Door : Block {
    public Door(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
        partialBlock();
        transparency();
    }

    /**
     * Metadata encoding for doors:
     * Bits 0-1: Horizontal facing direction (0=WEST, 1=EAST, 2=SOUTH, 3=NORTH)
     * Bit 2: Open/Closed state (0=closed, 1=open)
     * Bits 3-7: Reserved
     */
    public static byte getFacing(byte metadata) => (byte)(metadata & 0b11);
    public static bool isOpen(byte metadata) => (metadata & 0b100) != 0;

    public static byte setFacing(byte metadata, byte facing) => (byte)((metadata & ~0b11) | (facing & 0b11));
    public static byte setOpen(byte metadata, bool open) => (byte)((metadata & ~0b100) | (open ? 0b100 : 0));

    public override void place(World world, int x, int y, int z, byte metadata, RawDirection dir) {
        // get player's horizontal facing and reverse it (door faces away from player)
        byte facing = (byte)dir;
        if (facing > 3) facing = 0; // clamp to horizontal only

        // reverse the direction so door faces away from player
        facing = facing switch {
            0 => 1, // WEST -> EAST
            1 => 0, // EAST -> WEST
            2 => 3, // SOUTH -> NORTH
            3 => 2, // NORTH -> SOUTH
            _ => 0
        };

        // create metadata
        byte meta = 0;
        meta = setFacing(meta, facing);
        meta = setOpen(meta, false);

        // place single block
        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(meta));
        world.blockUpdateNeighbours(x, y, z);
    }

    public override bool canPlace(World world, int x, int y, int z, RawDirection dir) {
        // need space for door (current block + block above for rendering clearance)
        return world.getBlock(x, y, z) == 0 && world.getBlock(x, y + 1, z) == 0;
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        var block = world.getBlockRaw(x, y, z);
        var metadata = block.getMetadata();

        bool currentlyOpen = isOpen(metadata);

        // toggle open state
        metadata = setOpen(metadata, !currentlyOpen);

        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(metadata));

        // play sound
        // TODO: add door sound

        return true; // interaction handled
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);

        x &= 15;
        y &= 15;
        z &= 15;

        var block = br.getBlock();
        var metadata = block.getMetadata();
        var facing = getFacing(metadata);
        var open = isOpen(metadata);

        var min = uvs[0];
        var max = uvs[0] + 1;

        var u0 = UVPair.texU(min.u);
        var v0 = UVPair.texV(min.v);
        var u1 = UVPair.texU(max.u);
        var v1 = UVPair.texV(max.v);

        // door dimensions: 16 pixels X (1 block), 32 pixels Y (2 blocks), 2 pixels Z
        const float thickness = 2f / 16f; // 2 pixels = 2/16 blocks

        // calculate door bounds based on facing and open state
        float x0, z0, x1, z1, y0, y1;

        y0 = 0f;
        y1 = 2f; // extends 2 blocks tall (32 pixels)

        if (open) {
            // rotated 90 degrees around hinge corner
            switch (facing) {
                case 0: // WEST facing, hinge at northeast (x=1, z=1)
                    x0 = 1f - thickness; x1 = 1f; z0 = 0f; z1 = 1f;
                    break;
                case 1: // EAST facing, hinge at southwest (x=0, z=0)
                    x0 = 0f; x1 = thickness; z0 = 0f; z1 = 1f;
                    break;
                case 2: // SOUTH facing, hinge at southeast (x=1, z=0)
                    x0 = 0f; x1 = 1f; z0 = 0f; z1 = thickness;
                    break;
                default: // NORTH facing, hinge at northwest (x=0, z=1)
                    x0 = 0f; x1 = 1f; z0 = 1f - thickness; z1 = 1f;
                    break;
            }
        } else {
            // closed, positioned at hinge edge
            switch (facing) {
                case 0: // WEST facing, against north wall
                    x0 = 0f; x1 = 1f; z0 = 1f - thickness; z1 = 1f;
                    break;
                case 1: // EAST facing, against south wall
                    x0 = 0f; x1 = 1f; z0 = 0f; z1 = thickness;
                    break;
                case 2: // SOUTH facing, against east wall
                    x0 = 1f - thickness; x1 = 1f; z0 = 0f; z1 = 1f;
                    break;
                default: // NORTH facing, against west wall
                    x0 = 0f; x1 = thickness; z0 = 0f; z1 = 1f;
                    break;
            }
        }

        br.renderCube(x, y, z, vertices, x0, y0, z0, x1, y1, z1, u0, v0, u1, v1);
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();

        var facing = getFacing(metadata);
        var open = isOpen(metadata);

        const float thickness = 2f / 16f;

        if (open) {
            // rotated collision (2 blocks tall) - rotated around hinge
            switch (facing) {
                case 0: // WEST - against east wall
                    aabbs.Add(new AABB(x + 1f - thickness, y, z, x + 1f, y + 2f, z + 1f));
                    break;
                case 1: // EAST - against west wall
                    aabbs.Add(new AABB(x, y, z, x + thickness, y + 2f, z + 1f));
                    break;
                case 2: // SOUTH - against south wall
                    aabbs.Add(new AABB(x, y, z, x + 1f, y + 2f, z + thickness));
                    break;
                default: // NORTH - against north wall
                    aabbs.Add(new AABB(x, y, z + 1f - thickness, x + 1f, y + 2f, z + 1f));
                    break;
            }
        } else {
            // closed collision (2 blocks tall) - positioned at hinge edge
            switch (facing) {
                case 0: // WEST facing, against north wall
                    aabbs.Add(new AABB(x, y, z + 1f - thickness, x + 1f, y + 2f, z + 1f));
                    break;
                case 1: // EAST facing, against south wall
                    aabbs.Add(new AABB(x, y, z, x + 1f, y + 2f, z + thickness));
                    break;
                case 2: // SOUTH facing, against east wall
                    aabbs.Add(new AABB(x + 1f - thickness, y, z, x + 1f, y + 2f, z + 1f));
                    break;
                default: // NORTH facing, against west wall
                    aabbs.Add(new AABB(x, y, z, x + thickness, y + 2f, z + 1f));
                    break;
            }
        }
    }

    public override byte maxValidMetadata() {
        return 7; // 3 bits used (0-7)
    }
}