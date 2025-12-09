using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class Stairs : Block {
    public Stairs(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
        partialBlock();
    }

    /** Metadata encoding for stairs:
     * Bits 0-1: horizontal facing (0=WEST, 1=EAST, 2=SOUTH, 3=NORTH)
     * Bit 2: upside-down (0=bottom-half, 1=top-half) (unused)
     * Bits 3-7: Reserved
     */
    static byte getFacing(byte metadata) => (byte)(metadata & 0b11);

    static bool isUpsideDown(byte metadata) => (metadata & 0b100) != 0;
    static byte setFacing(byte metadata, byte facing) => (byte)((metadata & ~0b11) | (facing & 0b11));
    static byte setUpsideDown(byte metadata, bool upsideDown) => (byte)((metadata & ~0b100) | (upsideDown ? 0b100 : 0));

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        var opposite = Direction.getOpposite(info.hfacing);
        byte meta = setFacing(0, (byte)opposite);

        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(meta));
        world.blockUpdateNeighbours(x, y, z);
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);
        x &= 15;
        y &= 15;
        z &= 15;

        var facing = getFacing(br.getBlock().getMetadata());

        var min = uvs[0];
        var max = uvs[0] + 1;
        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = new UVPair(br.forceTex.u, br.forceTex.v);
            max = min + 1;
        }

        var uv0 = UVPair.texCoords(min);
        var uv1 = UVPair.texCoords(max);
        var u0 = uv0.X;
        var v0 = uv0.Y;
        var u1 = uv1.X;
        var v1 = uv1.Y;

        var (tx1, tz1, tx2, tz2) = facing switch {
            0 => (0.5f, 0f, 1f, 1f),
            1 => (0f, 0f, 0.5f, 1f),
            2 => (0f, 0.5f, 1f, 1f),
            _ => (0f, 0f, 1f, 0.5f)
        };

        br.renderCube(x, y, z, vertices, 0f, 0f, 0f, 1f, 0.5f, 1f, u0, v0, u1, v1);
        br.renderCube(x, y, z, vertices, tx1, 0.5f, tz1, tx2, 1f, tz2, u0, v0, u1, v1);
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var facing = getFacing(metadata);

        aabbs.Add(new AABB(x, y, z, x + 1f, y + 0.5f, z + 1f));
        aabbs.Add(facing switch {
            0 => new AABB(x + 0.5f, y + 0.5f, z, x + 1f, y + 1f, z + 1f),
            1 => new AABB(x, y + 0.5f, z, x + 0.5f, y + 1f, z + 1f),
            2 => new AABB(x, y + 0.5f, z + 0.5f, x + 1f, y + 1f, z + 1f),
            _ => new AABB(x, y + 0.5f, z, x + 1f, y + 1f, z + 0.5f)
        });
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        var existingId = world.getBlockRaw(x, y, z).getID();
        // prevent placing stairs into existing stairs
        return existingId != id && existingId != MAPLE_STAIRS.id;
    }

    public override bool same(ItemStack self, ItemStack other) {
        return self.id == other.id;
    }

    public override ItemStack getCanonical(byte metadata) {
        return new ItemStack(getItem(), 1, 0);
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        if (canBreak) {
            drops.Add(new ItemStack(getItem(), 1, 0));
        }
    }

    public override byte maxValidMetadata() => 3;
}