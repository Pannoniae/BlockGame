using BlockGame.main;
using BlockGame.util;
using BlockGame.world.block.entity;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class Fence : EntityBlock {
    public int fenceType;
    public Item fenceItem; // set during item registration

    // edge bitmask: each bit = one panel edge
    public const byte EAST = 1;  // bit 0, panel at +X
    public const byte WEST = 2;  // bit 1, panel at -X
    public const byte SOUTH = 4; // bit 2, panel at -Z
    public const byte NORTH = 8; // bit 3, panel at +Z

    // rotation per edge index (bit position)
    public static readonly float[] edgeRot = [90, -90, 180, 0];

    public Fence(string name, int fenceType = 0) : base(name) {
        this.fenceType = fenceType;
    }

    protected override void onRegister(int id) {
        base.onRegister(id);
        renderType[id] = RenderType.NONE;
        customAABB[id] = true;
        transparency();
        partialBlock();
        waterTransparent();
        itemLike();
    }

    public override BlockEntity get() {
        return new FenceBlockEntity();
    }

    private static byte edgeBitFromHit(double hitX, double hitZ) {
        var fx = (float)(hitX - Math.Floor(hitX));
        var fz = (float)(hitZ - Math.Floor(hitZ));
        float dWest = fx;
        float dEast = 1f - fx;
        float dSouth = fz;
        float dNorth = 1f - fz;
        if (dWest <= dEast && dWest <= dSouth && dWest <= dNorth) return WEST;
        if (dEast <= dSouth && dEast <= dNorth) return EAST;
        if (dSouth <= dNorth) return SOUTH;
        return NORTH;
    }

    /** Determine which active panel the player is looking at. */
    private static byte hitEdge(byte meta) {
        var hp = Game.raycast.point;
        var bx = Game.raycast.block.X;
        var bz = Game.raycast.block.Z;
        var fx = (float)(hp.X - bx);
        var fz = (float)(hp.Z - bz);

        // check which panel zone the hit point falls in
        bool inEast = fx >= 14f / 16f && (meta & EAST) != 0;
        bool inWest = fx <= 2f / 16f && (meta & WEST) != 0;
        bool inSouth = fz <= 2f / 16f && (meta & SOUTH) != 0;
        bool inNorth = fz >= 14f / 16f && (meta & NORTH) != 0;

        // single match
        int count = (inEast ? 1 : 0) + (inWest ? 1 : 0) + (inSouth ? 1 : 0) + (inNorth ? 1 : 0);
        if (count == 1) {
            if (inEast) return EAST;
            if (inWest) return WEST;
            if (inSouth) return SOUTH;
            return NORTH;
        }

        // corner overlap: use raycast face to disambiguate
        if (count >= 2) {
            var face = Game.raycast.face;
            if (face == RawDirection.EAST && inEast) return EAST;
            if (face == RawDirection.WEST && inWest) return WEST;
            if (face == RawDirection.SOUTH && inSouth) return SOUTH;
            if (face == RawDirection.NORTH && inNorth) return NORTH;
        }

        // fallback: nearest active edge
        byte nearest = edgeBitFromHit(hp.X, hp.Z);
        if ((meta & nearest) != 0) return nearest;
        // just return the first active edge
        for (int i = 0; i < 4; i++)
            if ((meta & (1 << i)) != 0) return (byte)(1 << i);
        return 0;
    }

    public override bool tryPartialBreak(World world, int x, int y, int z) {
        var meta = world.getBlockRaw(x, y, z).getMetadata();
        // only one panel? let normal break destroy the block
        if ((meta & (meta - 1)) == 0) return false;
        // remove the panel the player is looking at
        byte edge = hitEdge(meta);
        if (edge == 0) return false;
        byte newMeta = (byte)(meta & ~edge);
        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(newMeta));
        world.blockUpdateNeighbours(x, y, z);
        return true;
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        if ((metadata & EAST) != 0)
            aabbs.Add(new AABB(x + 14f / 16f, y, z, x + 1, y + 1, z + 1));
        if ((metadata & WEST) != 0)
            aabbs.Add(new AABB(x, y, z, x + 2f / 16f, y + 1, z + 1));
        if ((metadata & SOUTH) != 0)
            aabbs.Add(new AABB(x, y, z, x + 1, y + 1, z + 2f / 16f));
        if ((metadata & NORTH) != 0)
            aabbs.Add(new AABB(x, y, z + 14f / 16f, x + 1, y + 1, z + 1));
    }

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        byte edge = edgeBitFromHit(info.hitPoint.X, info.hitPoint.Z);
        // merge with existing edges if a fence is already here
        var existing = world.getBlock(x, y, z);
        byte current = 0;
        if (blocks[existing] is Fence)
            current = world.getBlockRaw(x, y, z).getMetadata();
        byte combined = (byte)(current | edge);
        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(combined));
        world.blockUpdateNeighbours(x, y, z);
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        var target = world.getBlock(x, y, z);
        // allow adding edges to an existing fence of the same type
        if (target == id) {
            byte edge = edgeBitFromHit(info.hitPoint.X, info.hitPoint.Z);
            byte current = world.getBlockRaw(x, y, z).getMetadata();
            return (current & edge) == 0; // only if this edge isn't set yet
        }
        if (!base.canPlace(world, x, y, z, info)) return false;
        var below = world.getBlock(x, y - 1, z);
        return fullBlock[below] || below == id;
    }

    public override byte maxValidMetadata() => 15;

    public override ItemStack getActualItem(byte metadata) {
        return new ItemStack(fenceItem, 1);
    }
}
