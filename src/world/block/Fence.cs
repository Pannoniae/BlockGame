using BlockGame.util;
using BlockGame.world.block.entity;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class Fence : EntityBlock {
    public int fenceType;
    public Item fenceItem; // set during item registration

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

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        // 2 pixel thick panel at far edge of block
        var facing = metadata & 0b11;
        switch (facing) {
            case 1: // west edge (x = 0 to 2/16)
                aabbs.Add(new AABB(x, y, z, x + 2f / 16f, y + 1, z + 1));
                break;
            case 0: // east edge (x = 14/16 to 1)
                aabbs.Add(new AABB(x + 14f / 16f, y, z, x + 1, y + 1, z + 1));
                break;
            case 2: // south edge (z = 0 to 2/16)
                aabbs.Add(new AABB(x, y, z, x + 1, y + 1, z + 2f / 16f));
                break;
            default: // north edge (z = 14/16 to 1)
                aabbs.Add(new AABB(x, y, z + 14f / 16f, x + 1, y + 1, z + 1));
                break;
        }
    }

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        // pick the edge closest to the hit point within the block
        var fx = (float)(info.hitPoint.X - Math.Floor(info.hitPoint.X));
        var fz = (float)(info.hitPoint.Z - Math.Floor(info.hitPoint.Z));
        // distances to each edge
        float dWest = fx;        // 0: west edge
        float dEast = 1f - fx;   // 1: east edge
        float dSouth = fz;       // 2: south edge (-Z)
        float dNorth = 1f - fz;  // 3: north edge (+Z)
        float min = Math.Min(Math.Min(dWest, dEast), Math.Min(dSouth, dNorth));
        byte facing;
        if (min == dWest) facing = 1;
        else if (min == dEast) facing = 0;
        else if (min == dSouth) facing = 2;
        else facing = 3;
        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(facing));
        world.blockUpdateNeighbours(x, y, z);
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        if (!base.canPlace(world, x, y, z, info)) return false;
        var below = world.getBlock(x, y - 1, z);
        return fullBlock[below] || below == id;
    }

    public override byte maxValidMetadata() => 3;

    public override ItemStack getActualItem(byte metadata) {
        return new ItemStack(fenceItem, 1);
    }
}
