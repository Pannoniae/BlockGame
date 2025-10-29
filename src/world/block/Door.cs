using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class DoorBlockItem : BlockItem {
    public DoorBlockItem(Block block) : base(block) {
    }
}

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

        /** bits 0-1: facing, bit 2: open, bit 3: upper, bit 4: hinge (0=left, 1=right) */
        private static byte facing(byte m) => (byte)(m & 0b11);

        private static bool open(byte m) => (m & 0b100) != 0;
        private static bool upper(byte m) => (m & 0b1000) != 0;
        private static bool hinge(byte m) => (m & 0b10000) != 0;

        public override void place(World world, int x, int y, int z, byte metadata, RawDirection dir) {
            byte f = (byte)dir;
            if (f > 3) f = 0;

            bool hingeRight = false;
            int lx = x, lz = z, rx = x, rz = z;

            switch (f) {
                case 0:
                    lz++;
                    rz--;
                    break; // WEST
                case 1:
                    lz--;
                    rz++;
                    break; // EAST
                case 2:
                    lx--;
                    rx++;
                    break; // SOUTH
                case 3:
                    lx++;
                    rx--;
                    break; // NORTH
            }

            var lb = world.getBlockRaw(lx, y, lz);
            if (lb.getID() == id && facing(lb.getMetadata()) == f) hingeRight = true;

            var rb = world.getBlockRaw(rx, y, rz);
            if (rb.getID() == id && facing(rb.getMetadata()) == f) hingeRight = false;

            byte lower = (byte)(f | (hingeRight ? 0b10000 : 0));
            byte upper = (byte)(lower | 0b1000);

            world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(lower));
            world.setBlockMetadata(x, y + 1, z, ((uint)id).setMetadata(upper));
            world.blockUpdateNeighbours(x, y, z);
            world.blockUpdateNeighbours(x, y + 1, z);
        }

        public override bool canPlace(World world, int x, int y, int z, RawDirection dir) =>
            world.getBlock(x, y, z) == 0 && world.getBlock(x, y + 1, z) == 0;

        public override bool onUse(World world, int x, int y, int z, Player player) {
            var m = world.getBlockRaw(x, y, z).getMetadata();
            int ly = upper(m) ? y - 1 : y;

            var nl = (byte)(world.getBlockRaw(x, ly, z).getMetadata() ^ 0b100);
            var nu = (byte)(world.getBlockRaw(x, ly + 1, z).getMetadata() ^ 0b100);

            world.setBlockMetadata(x, ly, z, ((uint)id).setMetadata(nl));
            world.setBlockMetadata(x, ly + 1, z, ((uint)id).setMetadata(nu));
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
            }
            else {
                if (!fullBlock[world.getBlock(x, y - 1, z)] || world.getBlock(x, y + 1, z) != id)
                    world.setBlock(x, y, z, 0);
            }
        }

        public override (Item item, byte metadata, int count) getDrop(World world, int x, int y, int z, byte metadata) {
            // only drop from lower half to avoid double drops
            if (upper(metadata)) return (null!, 0, 0);
            return (Item.DOOR, 0, 1);
        }

        protected override BlockItem createItem() {
            return new DoorBlockItem(this);
        }

        public override UVPair getTexture(int faceIdx, int metadata) => uvs[0];


        public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
            base.render(br, x, y, z, vertices);
            x &= 15;
            y &= 15;
            z &= 15;

            var m = br.getBlock().getMetadata();
            var f = facing(m);
            var o = open(m);
            var u = upper(m);
            var h = hinge(m);

            var tex = uvs[0];
            if (!u) tex = new UVPair(tex.u, tex.v + 1);

            var u0 = UVPair.texU(tex.u);
            var v0 = UVPair.texV(tex.v);
            var u1 = UVPair.texU(tex.u + 1);
            var v1 = UVPair.texV(tex.v + 1);

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
            }
            else {
                (x0, z0, x1, z1) = (f, h) switch {
                    (0, _) => (1f - t, 0f, 1f, 1f),
                    (1, _) => (0f, 0f, t, 1f),
                    (2, _) => (0f, 1f - t, 1f, 1f),
                    _ => (0f, 0f, 1f, t)
                };
            }

            br.renderCube(x, y, z, vertices, x0, 0f, z0, x1, 1f, z1, u0, v0, u1, v1);
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
            }
            else {
                aabbs.Add((f, h) switch {
                    (0, _) => new AABB(x + 1f - t, y, z, x + 1f, y + 1f, z + 1f),
                    (1, _) => new AABB(x, y, z, x + t, y + 1f, z + 1f),
                    (2, _) => new AABB(x, y, z + 1f - t, x + 1f, y + 1f, z + 1f),
                    _ => new AABB(x, y, z, x + 1f, y + 1f, z + t)
                });
            }
        }

        public override byte maxValidMetadata() => 31; // 5 bits
}