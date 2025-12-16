using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class Door : Block {

    public Item theDoor;

    public Door(string name) : base(name) { }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
        partialBlock();
        transparency();
        itemLike();
    }

    /** bits 0-1: facing, bit 2: open, bit 3: upper, bit 4: hinge (0=left, 1=right) */
    private static byte facing(byte m) => (byte)(m & 0b11);

    private static bool open(byte m) => (m & 0b100) != 0;
    private static bool upper(byte m) => (m & 0b1000) != 0;
    private static bool hinge(byte m) => (m & 0b10000) != 0;

    // check if door at position already has a partner on opposite side
    private bool hasPartner(World world, int x, int y, int z, byte doorFacing, bool doorHinge) {
        int px = x, pz = z;
        switch (doorFacing) {
            case 0: pz += doorHinge ? 1 : -1; break; // WEST: partner on opposite hinge side
            case 1: pz += doorHinge ? -1 : 1; break; // EAST
            case 2: px += doorHinge ? -1 : 1; break; // SOUTH
            case 3: px += doorHinge ? 1 : -1; break; // NORTH
        }
        var partner = world.getBlockRaw(px, y, pz);
        return partner.getID() == id && facing(partner.getMetadata()) == doorFacing;
    }

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        byte f = (byte)info.hfacing;
        if (f > 3) f = 0;

        bool hingeRight = true;
        int lx = x, lz = z, rx = x, rz = z;

        switch (f) {
            case 0: lz++; rz--; break; // WEST
            case 1: lz--; rz++; break; // EAST
            case 2: lx--; rx++; break; // SOUTH
            case 3: lx++; rx--; break; // NORTH
        }

        var lb = world.getBlockRaw(lx, y, lz);
        bool hasLeft = lb.getID() == id && facing(lb.getMetadata()) == f;
        // check if left door already has a partner - if so, don't form double door
        if (hasLeft && hasPartner(world, lx, y, lz, f, hinge(lb.getMetadata()))) {
            hasLeft = false;
        }
        if (hasLeft) hingeRight = true;

        var rb = world.getBlockRaw(rx, y, rz);
        bool hasRight = rb.getID() == id && facing(rb.getMetadata()) == f;
        // check if right door already has a partner - if so, don't form double door
        if (hasRight && hasPartner(world, rx, y, rz, f, hinge(rb.getMetadata()))) {
            hasRight = false;
        }
        if (hasRight) hingeRight = false;

        byte lower = (byte)(f | (hingeRight ? 0b10000 : 0));
        byte upper = (byte)(lower | 0b1000);

        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(lower));
        world.setBlockMetadata(x, y + 1, z, ((uint)id).setMetadata(upper));

        // update neighbour door hinge if we just placed next to it
        if (hasLeft) {
            var lm = lb.getMetadata();
            var newLower = (byte)((lm & ~0b10000) | 0); // hinge left
            var newUpper = (byte)(newLower | 0b1000);
            world.setBlockMetadata(lx, y, lz, ((uint)id).setMetadata(newLower));
            world.setBlockMetadata(lx, y + 1, lz, ((uint)id).setMetadata(newUpper));
        } else if (hasRight) {
            var rm = rb.getMetadata();
            var newLower = (byte)((rm & ~0b10000) | 0b10000); // hinge right
            var newUpper = (byte)(newLower | 0b1000);
            world.setBlockMetadata(rx, y, rz, ((uint)id).setMetadata(newLower));
            world.setBlockMetadata(rx, y + 1, rz, ((uint)id).setMetadata(newUpper));
        }

        world.blockUpdateNeighbours(x, y, z);
        world.blockUpdateNeighbours(x, y + 1, z);
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) =>
        world.getBlock(x, y, z) == 0 && world.getBlock(x, y + 1, z) == 0;

    public override bool onUse(World world, int x, int y, int z, Player player) {
        var m = world.getBlockRaw(x, y, z).getMetadata();
        int ly = upper(m) ? y - 1 : y;

        var nl = (byte)(world.getBlockRaw(x, ly, z).getMetadata() ^ 0b100);
        var nu = (byte)(world.getBlockRaw(x, ly + 1, z).getMetadata() ^ 0b100);

        world.setBlockMetadata(x, ly, z, ((uint)id).setMetadata(nl));
        world.setBlockMetadata(x, ly + 1, z, ((uint)id).setMetadata(nu));

        // toggle neighbour door if it's a double door
        byte f = facing(m);
        int nx = x, nz = z;

        switch (f) {
            case 0: nz = hinge(m) ? z + 1 : z - 1; break; // WEST
            case 1: nz = hinge(m) ? z - 1 : z + 1; break; // EAST
            case 2: nx = hinge(m) ? x - 1 : x + 1; break; // SOUTH
            case 3: nx = hinge(m) ? x + 1 : x - 1; break; // NORTH
        }

        var nb = world.getBlockRaw(nx, ly, nz);
        if (nb.getID() == id && facing(nb.getMetadata()) == f) {
            // check if neighbor has a partner that is NOT this door
            // if so, don't toggle (neighbor is part of another double door)
            bool nh = hinge(nb.getMetadata());
            if (hasPartner(world, nx, ly, nz, f, nh)) {
                // neighbor has a partner - verify it's THIS door
                int px = nx, pz = nz;
                switch (f) {
                    case 0: pz += nh ? 1 : -1; break;
                    case 1: pz += nh ? -1 : 1; break;
                    case 2: px += nh ? -1 : 1; break;
                    case 3: px += nh ? 1 : -1; break;
                }
                // only toggle if partner is this door
                if (px != x || pz != z) return true;
            }

            var nnl = (byte)(world.getBlockRaw(nx, ly, nz).getMetadata() ^ 0b100);
            var nnu = (byte)(world.getBlockRaw(nx, ly + 1, nz).getMetadata() ^ 0b100);
            world.setBlockMetadata(nx, ly, nz, ((uint)id).setMetadata(nnl));
            world.setBlockMetadata(nx, ly + 1, nz, ((uint)id).setMetadata(nnu));
        }

        return true;
    }

    public override void onBreak(World world, int x, int y, int z, byte metadata) {
        int oy = upper(metadata) ? y - 1 : y + 1;
        if (world.getBlock(x, oy, z) == id) world.setBlock(x, oy, z, 0);
    }

    public override void update(World world, int x, int y, int z) {
        var m = world.getBlockRaw(x, y, z).getMetadata();
        if (upper(m)) {
            if (world.getBlock(x, y - 1, z) != id) world.setBlock(x, y, z, 0);
        } else {
            if (!fullBlock[world.getBlock(x, y - 1, z)] || world.getBlock(x, y + 1, z) != id)
                world.setBlock(x, y, z, 0);
        }
    }

    public override UVPair getTexture(int faceIdx, int metadata) {
        return upper((byte)metadata) ? uvs[0] : uvs[1];
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);

        var m = br.getBlock().getMetadata();
        var f = facing(m);
        var o = open(m);
        var u = upper(m);
        var h = hinge(m);

        // check if we should mirror the texture
        // left door: mirror player-facing side only
        // right door: mirror opposite side only
        bool mirrorLeftDoor = false, mirrorRightDoor = false;
        if (br.world != null) {
            int ly = u ? y - 1 : y;
            bool isDoubleDoor = hasPartner(br.world, x, ly, z, f, h);
            if (isDoubleDoor) {
                if (!h) mirrorLeftDoor = true;   // left door: player-facing
                else mirrorRightDoor = true;     // right door: opposite
            }
        }

        x &= 15; y &= 15; z &= 15;

        UVPair tex = u ? uvs[0] : uvs[1];

        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            tex = new UVPair(br.forceTex.u, br.forceTex.v);
        }

        var uv0 = UVPair.texCoordsi(tex);
        var uv1 = UVPair.texCoordsi(tex + 1);
        var u0 = uv0.X;
        var v0 = uv0.Y;
        var u1 = uv1.X;
        var v1 = uv1.Y;

        const float t = 2f / 16f;
        float x0, z0, x1, z1;

        if (o) {
            (x0, z0, x1, z1) = (f, h) switch {
                (0, false) => (0f, 1f - t, 1f, 1f),
                (0, true) or (1, false) => (0f, 0f, 1f, t),
                (1, true) => (0f, 1f - t, 1f, 1f),
                (2, false) => (0f, 0f, t, 1f),
                (2, true) or (3, false) => (1f - t, 0f, 1f, 1f),
                _ => (0f, 0f, t, 1f)
            };
        } else {
            (x0, z0, x1, z1) = (f, h) switch {
                (0, _) => (1f - t, 0f, 1f, 1f),
                (1, _) => (0f, 0f, t, 1f),
                (2, _) => (0f, 1f - t, 1f, 1f),
                _ => (0f, 0f, 1f, t)
            };
        }

        // determine which faces to mirror based on door facing
        bool mirrorWest = false, mirrorEast = false, mirrorSouth = false, mirrorNorth = false;
        if (mirrorLeftDoor) {
            // left door: mirror player-facing face only
            switch (f) {
                case 0: mirrorEast = true; break;
                case 1: mirrorWest = true; break;
                case 2: mirrorNorth = true; break;
                case 3: mirrorSouth = true; break;
            }
        } else if (mirrorRightDoor) {
            // right door: mirror opposite face only
            switch (f) {
                case 0: mirrorWest = true; break;
                case 1: mirrorEast = true; break;
                case 2: mirrorSouth = true; break;
                case 3: mirrorNorth = true; break;
            }
        }

        var ue = u1 - u0;
        var ve = v1 - v0;

        // WEST face
        if (x0 > 0f || !br.shouldCullFace(RawDirection.WEST)) {
            float wUMin, wUMax;
            if (mirrorWest) {
                wUMin = u0 + ue * z1;
                wUMax = u0 + ue * z0;
            } else {
                wUMin = u0 + ue * z0;
                wUMax = u0 + ue * z1;
            }
            var wVMin = v0 + ve * (1f - 1f);
            var wVMax = v0 + ve * (1f - 0f);
            br.quadf(vertices, x, y, z,
                x0, 1f, z1, x0, 0f, z1, x0, 0f, z0, x0, 1f, z0,
                wUMin, wVMin, wUMax, wVMax, RawDirection.WEST);
        }

        // EAST face
        if (x1 < 1f || !br.shouldCullFace(RawDirection.EAST)) {
            float eUMin, eUMax;
            if (mirrorEast) {
                eUMin = u0 + ue * z1;
                eUMax = u0 + ue * z0;
            } else {
                eUMin = u0 + ue * z0;
                eUMax = u0 + ue * z1;
            }
            var eVMin = v0 + ve * (1f - 1f);
            var eVMax = v0 + ve * (1f - 0f);
            br.quadf(vertices, x, y, z,
                x1, 1f, z0, x1, 0f, z0, x1, 0f, z1, x1, 1f, z1,
                eUMin, eVMin, eUMax, eVMax, RawDirection.EAST);
        }

        // SOUTH face
        if (z0 > 0f || !br.shouldCullFace(RawDirection.SOUTH)) {
            float sUMin, sUMax;
            if (mirrorSouth) {
                sUMin = u0 + ue * x1;
                sUMax = u0 + ue * x0;
            } else {
                sUMin = u0 + ue * x0;
                sUMax = u0 + ue * x1;
            }
            var sVMin = v0 + ve * (1f - 1f);
            var sVMax = v0 + ve * (1f - 0f);
            br.quadf(vertices, x, y, z,
                x0, 1f, z0, x0, 0f, z0, x1, 0f, z0, x1, 1f, z0,
                sUMin, sVMin, sUMax, sVMax, RawDirection.SOUTH);
        }

        // NORTH face
        if (z1 < 1f || !br.shouldCullFace(RawDirection.NORTH)) {
            float nUMin, nUMax;
            if (mirrorNorth) {
                nUMin = u0 + ue * x1;
                nUMax = u0 + ue * x0;
            } else {
                nUMin = u0 + ue * x0;
                nUMax = u0 + ue * x1;
            }
            var nVMin = v0 + ve * (1f - 1f);
            var nVMax = v0 + ve * (1f - 0f);
            br.quadf(vertices, x, y, z,
                x1, 1f, z1, x1, 0f, z1, x0, 0f, z1, x0, 1f, z1,
                nUMin, nVMin, nUMax, nVMax, RawDirection.NORTH);
        }

        // DOWN face
        if (!br.shouldCullFace(RawDirection.DOWN)) {
            var dUMin = u0 + ue * x0;
            var dUMax = u0 + ue * x1;
            var dVMin = v0 + ve * (1f - z1);
            var dVMax = v0 + ve * (1f - z0);
            br.quadf(vertices, x, y, z,
                x1, 0f, z1, x1, 0f, z0, x0, 0f, z0, x0, 0f, z1,
                dUMin, dVMin, dUMax, dVMax, RawDirection.DOWN);
        }

        // UP face
        if (!br.shouldCullFace(RawDirection.UP)) {
            var upUMin = u0 + ue * x0;
            var upUMax = u0 + ue * x1;
            var upVMin = v0 + ve * (1f - z0);
            var upVMax = v0 + ve * (1f - z1);
            br.quadf(vertices, x, y, z,
                x0, 1f, z1, x0, 1f, z0, x1, 1f, z0, x1, 1f, z1,
                upUMin, upVMin, upUMax, upVMax, RawDirection.UP);
        }
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var f = facing(metadata);
        var o = open(metadata);
        var h = hinge(metadata);
        const float t = 2f / 16f;

        if (o) {
            aabbs.Add((f, h) switch {
                (0, false) => new AABB(x, y, z + 1f - t, x + 1f, y + 1f, z + 1f),
                (0, true) or (1, false) => new AABB(x, y, z, x + 1f, y + 1f, z + t),
                (1, true) => new AABB(x, y, z + 1f - t, x + 1f, y + 1f, z + 1f),
                (2, false) => new AABB(x, y, z, x + t, y + 1f, z + 1f),
                (2, true) or (3, false) => new AABB(x + 1f - t, y, z, x + 1f, y + 1f, z + 1f),
                _ => new AABB(x, y, z, x + t, y + 1f, z + 1f)
            });
        } else {
            aabbs.Add((f, h) switch {
                (0, _) => new AABB(x + 1f - t, y, z, x + 1f, y + 1f, z + 1f),
                (1, _) => new AABB(x, y, z, x + t, y + 1f, z + 1f),
                (2, _) => new AABB(x, y, z + 1f - t, x + 1f, y + 1f, z + 1f),
                _ => new AABB(x, y, z, x + 1f, y + 1f, z + t)
            });
        }
    }

    public override byte maxValidMetadata() => 31; // 5 bits

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        // onBreak removes the other half via setBlock which doesn't trigger getDrop!
        if (!upper(metadata) && canBreak) {
            drops.Add(getActualItem(metadata));
        }
    }

    public override ItemStack getActualItem(byte metadata) {
        return new ItemStack(theDoor, 1, 0);
    }
}