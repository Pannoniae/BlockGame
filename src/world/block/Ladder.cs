using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.entity;

namespace BlockGame.world.block;

public class Ladder : Block {
    public Ladder(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
    }

    /** metadata bits 0-1: horizontal facing (0=WEST, 1=EAST, 2=SOUTH, 3=NORTH)
     * ladder faces the opposite direction of the block it's attached to
     */
    private static byte getFacing(byte metadata) => (byte)(metadata & 0b11);

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        var facing = info.face;

        // can't place on top or bottom faces, but just in case, don't place
        if (facing is RawDirection.UP or RawDirection.DOWN) {
            return;
        }


        uint blockValue = id;
        blockValue = blockValue.setMetadata((byte)facing);
        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
    }

    public override byte maxValidMetadata() => 3;

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        if (!base.canPlace(world, x, y, z, info)) return false;

        // can't place on top or bottom faces
        if (info.face is RawDirection.UP or RawDirection.DOWN) return false;

        // check if there's a solid block behind where the ladder would be placed
        var o = Direction.getDirection(info.face.opposite());
        var sx = x + o.X;
        var sy = y + o.Y;
        var sz = z + o.Z;

        var sb = world.getBlock(sx, sy, sz);
        return fullBlock[sb];
    }

    public override void update(World world, int x, int y, int z) {
        // check if still attached to a valid block
        var block = world.getBlockRaw(x, y, z);
        var metadata = block.getMetadata();
        var facing = getFacing(metadata);

        // determine which direction the ladder is facing (where support should be)
        RawDirection dir = facing switch {
            0 => RawDirection.WEST,
            1 => RawDirection.EAST,
            2 => RawDirection.SOUTH,
            3 => RawDirection.NORTH,
            _ => RawDirection.SOUTH
        };

        var o = Direction.getDirection(dir.opposite());
        var sx = x + o.X;
        var sy = y + o.Y;
        var sz = z + o.Z;

        var sb = world.getBlock(sx, sy, sz);
        if (!fullBlock[sb]) {
            drops.Clear();
            getDrop(drops, world, x, y, z, metadata, true);

            foreach (var drop in drops) {
                var dropItem = drop.getItem();
                var dropCount = drop.quantity;
                var dropMeta = drop.metadata;
                world.spawnBlockDrop(x, y, z, dropItem, dropCount, dropMeta);
            }

            world.setBlock(x, y, z, 0);
        }
    }

    /** allow entities to climb when inside ladder */
    public override void interact(World world, int x, int y, int z, Entity e) {
        e.onLadder = true;
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);
        x &= 15;
        y &= 15;
        z &= 15;

        var block = br.getBlock();
        var metadata = block.getMetadata();
        var facing = getFacing(metadata);

        var min = uvs[0];
        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = br.forceTex;
        }

        var uv0 = UVPair.texCoords(min);
        var uv1 = UVPair.texCoords(min + 1);
        var u0 = uv0.X;
        var v0 = uv0.Y;
        var u1 = uv1.X;
        var v1 = uv1.Y;

        const float t = 1f / 16f;

        br.applySimpleLighting(RawDirection.NONE);
        br.begin();

        switch (facing) {
            case 0: // WEST
                br.vertex(x + 1 - t, y + 1, z + 1, u0, v0);
                br.vertex(x + 1 - t, y, z + 1, u0, v1);
                br.vertex(x + 1 - t, y, z, u1, v1);
                br.vertex(x + 1 - t, y + 1, z, u1, v0);
                break;
            case 1: // EAST
                br.vertex(x + t, y + 1, z, u0, v0);
                br.vertex(x + t, y, z, u0, v1);
                br.vertex(x + t, y, z + 1, u1, v1);
                br.vertex(x + t, y + 1, z + 1, u1, v0);
                break;
            case 2: // SOUTH
                br.vertex(x, y + 1, z + 1 - t, u0, v0);
                br.vertex(x, y, z + 1 - t, u0, v1);
                br.vertex(x + 1, y, z + 1 - t, u1, v1);
                br.vertex(x + 1, y + 1, z + 1 - t, u1, v0);
                break;
            default: // NORTH
                br.vertex(x + 1, y + 1, z + t, u0, v0);
                br.vertex(x + 1, y, z + t, u0, v1);
                br.vertex(x, y, z + t, u1, v1);
                br.vertex(x, y + 1, z + t, u1, v0);
                break;
        }

        br.end(vertices);
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var facing = getFacing(metadata);
        const float t = 1f / 16f;

        aabbs.Add(facing switch {
            0 => new AABB(x + 1f - t, y, z, x + 1f, y + 1f, z + 1f), // WEST
            1 => new AABB(x, y, z, x + t, y + 1f, z + 1f), // EAST
            2 => new AABB(x, y, z + 1f - t, x + 1f, y + 1f, z + 1f), // SOUTH
            _ => new AABB(x, y, z, x + 1f, y + 1f, z + t) // NORTH
        });
    }

    public override UVPair getTexture(int faceIdx, int metadata) => uvs[0];
}