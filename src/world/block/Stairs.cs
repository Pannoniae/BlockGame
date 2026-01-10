using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class Stairs : Block {
    public Stairs(string name) : base(name) { }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
        partialBlock();
    }

    // Metadata: b0-1 = facing (0=W, 1=E, 2=S, 3=N), b2 = upside down
    public static byte getFacing(byte meta) => (byte)(meta & 0b11);
    public static bool isFlipped(byte meta) => (meta & 0b100) != 0;

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        byte facing = (byte)Direction.getOpposite(info.hfacing);
        bool flipped = (info.hitPoint.Y  - y) > 0.5f;
        byte meta = (byte)(facing | (flipped ? 0b100 : 0));
        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(meta));
        world.blockUpdateNeighbours(x, y, z);
    }

    // Corner detection: 0=none, 1=inner (quarter), 2=outer (3/4)
    // Inner: my step side adjacent to neighbor's side → step shrinks to quarter
    // Outer: my floor side adjacent to neighbor's side → step expands to 3/4
    static (int type, int dir) detectCorner(World world, int x, int y, int z, byte facing) {
        // facing→dir: 0=+X→1, 1=-X→0, 2=+Z→2, 3=-Z→3
        int stepDir = facing <= 1 ? (1 - facing) : facing;
        int floorDir = stepDir ^ 1; // opposite: 0↔1, 2↔3

        // Check step direction → inner corner
        var r = checkNeighbor(world, x, y, z, facing, stepDir, 1);
        if (r.type != 0) return r;

        // Check floor direction → outer corner
        return checkNeighbor(world, x, y, z, facing, floorDir, 2);
    }

    static (int type, int dir) checkNeighbor(World world, int x, int y, int z, byte myFacing, int dir, int type) {
        int nx = x, nz = z;
        switch (dir) {
            case 0: nx--; break;
            case 1: nx++; break;
            case 2: nz++; break;
            case 3: nz--; break;
        }

        var nid = world.getBlock(nx, y, nz);
        if (!isStairs(nid)) return (0, 0);

        var nFacing = getFacing(world.getBlockRaw(nx, y, nz).getMetadata());
        if (!isPerpendicular(myFacing, nFacing)) return (0, 0);

        // cornerDir = neighbor's step direction (determines which quadrant)
        int cornerDir = nFacing <= 1 ? (1 - nFacing) : nFacing;
        return (type, cornerDir);
    }

    public static bool isStairs(ushort id) => get(id) is Stairs;
    public static bool isPerpendicular(byte f1, byte f2) => (f1 <= 1) != (f2 <= 1);

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);

        var meta = br.getBlock().getMetadata();
        var facing = getFacing(meta);
        var flipped = isFlipped(meta);

        // corner detection needs world coords (x,y,z are world coords when meshing, 0,0,0 for GUI)
        var (cornerType, cornerDir) = br.world != null
            ? detectCorner(br.world, x, y, z, facing)
            : (0, 0);

        // renderCube needs local coords (0-15)
        int lx = x & 15, ly = y & 15, lz = z & 15;

        var min = uvs[0];
        var max = uvs[0] + 1;
        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = new UVPair(br.forceTex.u, br.forceTex.v);
            max = min + 1;
        }
        var uv0 = UVPair.texCoords(min);
        var uv1 = UVPair.texCoords(max);
        float u0 = uv0.X, v0 = uv0.Y, u1 = uv1.X, v1 = uv1.Y;

        bool rotateTop = facing < 2; // E/W facing needs rotated top UVs

        if (flipped) {
            // upside down: top half full, bottom half partial
            renderStepCube(br, lx, ly, lz, vertices, 0, 0.5f, 0, 1, 1, 1, u0, v0, u1, v1, rotateTop);
            renderStairTop(br, lx, ly, lz, vertices, facing, cornerType, cornerDir, u0, v0, u1, v1, true);
        } else {
            // normal: bottom half full, top half partial
            renderStepCube(br, lx, ly, lz, vertices, 0, 0, 0, 1, 0.5f, 1, u0, v0, u1, v1, rotateTop);
            renderStairTop(br, lx, ly, lz, vertices, facing, cornerType, cornerDir, u0, v0, u1, v1, false);
        }
    }

    static void renderStairTop(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices,
        byte facing, int cornerType, int cornerDir, float u0, float v0, float u1, float v1, bool flipped) {

        float yLo = flipped ? 0f : 0.5f;
        float yHi = flipped ? 0.5f : 1f;
        bool rotateTop = facing < 2; // E/W facing needs rotated top UVs
        // corners: match neighbor's orientation (cornerDir 0/1 = E/W neighbor = rotate)
        bool rotateCorner = cornerDir < 2;

        switch (cornerType) {
            case 0: {
                // normal stair - half slab on high side
                var (x1, z1, x2, z2) = getHalfBounds(facing);
                renderStepCube(br, x, y, z, vertices, x1, yLo, z1, x2, yHi, z2, u0, v0, u1, v1, rotateTop);
                break;
            }
            case 1: {
                // inner corner - quarter block, match neighbor's texture
                var (x1, z1, x2, z2) = getQuarterBounds(facing, cornerDir);
                renderStepCube(br, x, y, z, vertices, x1, yLo, z1, x2, yHi, z2, u0, v0, u1, v1, rotateCorner);
                break;
            }
            default:
                // outer corner - three quarters (render as two pieces)
                var (bounds1, bounds2) = getThreeQuarterBounds(facing, cornerDir);
                // main half follows own facing, extra quarter follows neighbor
                renderStepCube(br, x, y, z, vertices, bounds1.x1, yLo, bounds1.z1, bounds1.x2, yHi, bounds1.z2, u0, v0, u1, v1, rotateTop);
                renderStepCube(br, x, y, z, vertices, bounds2.x1, yLo, bounds2.z1, bounds2.x2, yHi, bounds2.z2, u0, v0, u1, v1, rotateCorner);
                break;
        }
    }

    // renders a partial cube, optionally rotating UP/DOWN face UVs for N/S stairs
    static void renderStepCube(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices,
        float x0, float y0, float z0, float x1, float y1, float z1,
        float u0, float v0, float u1, float v1, bool rotateTop) {

        if (!rotateTop) {
            br.renderCube(x, y, z, vertices, x0, y0, z0, x1, y1, z1, u0, v0, u1, v1);
            return;
        }

        // N/S stairs: render with rotated top/bottom UVs
        var ue = u1 - u0;
        var ve = v1 - v0;

        // WEST face - U along Z, V along Y
        if (x0 == 0f || !Block.fullBlock[br.getBlockCached(-1, 0, 0).getID()]) {
            br.applyFaceLighting(RawDirection.WEST);
            br.begin();
            br.vertex(x + x0, y + y1, z + z1, u0 + ue * z0, v0 + ve * (1f - y1));
            br.vertex(x + x0, y + y0, z + z1, u0 + ue * z0, v0 + ve * (1f - y0));
            br.vertex(x + x0, y + y0, z + z0, u0 + ue * z1, v0 + ve * (1f - y0));
            br.vertex(x + x0, y + y1, z + z0, u0 + ue * z1, v0 + ve * (1f - y1));
            br.end(vertices);
        }

        // EAST face - U along Z, V along Y
        if (x1 == 1f || !Block.fullBlock[br.getBlockCached(1, 0, 0).getID()]) {
            br.applyFaceLighting(RawDirection.EAST);
            br.begin();
            br.vertex(x + x1, y + y1, z + z0, u0 + ue * z0, v0 + ve * (1f - y1));
            br.vertex(x + x1, y + y0, z + z0, u0 + ue * z0, v0 + ve * (1f - y0));
            br.vertex(x + x1, y + y0, z + z1, u0 + ue * z1, v0 + ve * (1f - y0));
            br.vertex(x + x1, y + y1, z + z1, u0 + ue * z1, v0 + ve * (1f - y1));
            br.end(vertices);
        }

        // SOUTH face - U along X, V along Y
        if (z0 == 0f || !Block.fullBlock[br.getBlockCached(0, 0, -1).getID()]) {
            br.applyFaceLighting(RawDirection.SOUTH);
            br.begin();
            br.vertex(x + x0, y + y1, z + z0, u0 + ue * x0, v0 + ve * (1f - y1));
            br.vertex(x + x0, y + y0, z + z0, u0 + ue * x0, v0 + ve * (1f - y0));
            br.vertex(x + x1, y + y0, z + z0, u0 + ue * x1, v0 + ve * (1f - y0));
            br.vertex(x + x1, y + y1, z + z0, u0 + ue * x1, v0 + ve * (1f - y1));
            br.end(vertices);
        }

        // NORTH face - U along X, V along Y
        if (z1 == 1f || !Block.fullBlock[br.getBlockCached(0, 0, 1).getID()]) {
            br.applyFaceLighting(RawDirection.NORTH);
            br.begin();
            br.vertex(x + x1, y + y1, z + z1, u0 + ue * x0, v0 + ve * (1f - y1));
            br.vertex(x + x1, y + y0, z + z1, u0 + ue * x0, v0 + ve * (1f - y0));
            br.vertex(x + x0, y + y0, z + z1, u0 + ue * x1, v0 + ve * (1f - y0));
            br.vertex(x + x0, y + y1, z + z1, u0 + ue * x1, v0 + ve * (1f - y1));
            br.end(vertices);
        }

        // DOWN face - rotated: U along Z, V along X
        if (y0 == 0f || !Block.fullBlock[br.getBlockCached(0, -1, 0).getID()]) {
            br.applyFaceLighting(RawDirection.DOWN);
            br.begin();
            br.vertex(x + x1, y + y0, z + z1, u0 + ue * z0, v0 + ve * (1f - x0));
            br.vertex(x + x1, y + y0, z + z0, u0 + ue * z1, v0 + ve * (1f - x0));
            br.vertex(x + x0, y + y0, z + z0, u0 + ue * z1, v0 + ve * (1f - x1));
            br.vertex(x + x0, y + y0, z + z1, u0 + ue * z0, v0 + ve * (1f - x1));
            br.end(vertices);
        }

        // UP face - rotated: U along Z, V along X
        if (y1 == 1f || !Block.fullBlock[br.getBlockCached(0, 1, 0).getID()]) {
            br.applyFaceLighting(RawDirection.UP);
            br.begin();
            br.vertex(x + x0, y + y1, z + z1, u0 + ue * z0, v0 + ve * (1f - x1));
            br.vertex(x + x0, y + y1, z + z0, u0 + ue * z1, v0 + ve * (1f - x1));
            br.vertex(x + x1, y + y1, z + z0, u0 + ue * z1, v0 + ve * (1f - x0));
            br.vertex(x + x1, y + y1, z + z1, u0 + ue * z0, v0 + ve * (1f - x0));
            br.end(vertices);
        }
    }

    // facing: 0=high+X, 1=high-X, 2=high+Z, 3=high-Z
    static (float x1, float z1, float x2, float z2) getHalfBounds(byte facing) => facing switch {
        0 => (0.5f, 0f, 1f, 1f),   // high +X
        1 => (0f, 0f, 0.5f, 1f),   // high -X
        2 => (0f, 0.5f, 1f, 1f),   // high +Z
        _ => (0f, 0f, 1f, 0.5f)    // high -Z
    };

    static (float x1, float z1, float x2, float z2) getQuarterBounds(byte facing, int cornerDir) {
        // Quarter is at intersection of facing's high side and cornerDir's side
        // facing high: 0=+X, 1=-X, 2=+Z, 3=-Z
        // cornerDir: 0=-X, 1=+X, 2=+Z, 3=-Z
        float x1 = 0, z1 = 0, x2 = 1, z2 = 1;

        // X bounds from facing (if X-axis) or cornerDir (if Z-axis)
        if (facing <= 1) {
            (x1, x2) = facing == 0 ? (0.5f, 1f) : (0f, 0.5f);
        } else {
            (x1, x2) = cornerDir == 1 ? (0.5f, 1f) : (0f, 0.5f);
        }

        // Z bounds from facing (if Z-axis) or cornerDir (if X-axis)
        if (facing >= 2) {
            (z1, z2) = facing == 2 ? (0.5f, 1f) : (0f, 0.5f);
        } else {
            (z1, z2) = cornerDir == 2 ? (0.5f, 1f) : (0f, 0.5f);
        }

        return (x1, z1, x2, z2);
    }

    static ((float x1, float z1, float x2, float z2), (float x1, float z1, float x2, float z2)) getThreeQuarterBounds(byte facing, int cornerDir) {
        // Three quarters - Render as two rectangles: the half from facing + adjacent quarter
        var half = getHalfBounds(facing);

        // The "other" half on the opposite side, but only the quarter NOT cut by corner
        float ox1 = 0, oz1 = 0, ox2 = 1, oz2 = 1;

        // Opposite half bounds
        if (facing <= 1) {
            (ox1, ox2) = facing == 0 ? (0f, 0.5f) : (0.5f, 1f);
        } else {
            (oz1, oz2) = facing == 2 ? (0f, 0.5f) : (0.5f, 1f);
        }

        // Cut the quarter based on cornerDir - keep side where neighbor's step is
        if (facing <= 1) {
            // cornerDir is Z axis (2 or 3)
            (oz1, oz2) = cornerDir == 2 ? (0.5f, 1f) : (0f, 0.5f);
        } else {
            // cornerDir is X axis (0 or 1)
            (ox1, ox2) = cornerDir == 1 ? (0.5f, 1f) : (0f, 0.5f);
        }

        return (half, (ox1, oz1, ox2, oz2));
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var facing = getFacing(metadata);
        var flipped = isFlipped(metadata);
        var (cornerType, cornerDir) = detectCorner(world, x, y, z, facing);

        float yFull1 = flipped ? 0.5f : 0f;
        float yFull2 = flipped ? 1f : 0.5f;
        float yPart1 = flipped ? 0f : 0.5f;
        float yPart2 = flipped ? 0.5f : 1f;

        // full half
        aabbs.Add(new AABB(x, y + yFull1, z, x + 1, y + yFull2, z + 1));

        switch (cornerType) {
            case 0: {
                var (x1, z1, x2, z2) = getHalfBounds(facing);
                aabbs.Add(new AABB(x + x1, y + yPart1, z + z1, x + x2, y + yPart2, z + z2));
                break;
            }
            case 1: {
                var (x1, z1, x2, z2) = getQuarterBounds(facing, cornerDir);
                aabbs.Add(new AABB(x + x1, y + yPart1, z + z1, x + x2, y + yPart2, z + z2));
                break;
            }
            default:
                var (b1, b2) = getThreeQuarterBounds(facing, cornerDir);
                aabbs.Add(new AABB(x + b1.x1, y + yPart1, z + b1.z1, x + b1.x2, y + yPart2, z + b1.z2));
                aabbs.Add(new AABB(x + b2.x1, y + yPart1, z + b2.z1, x + b2.x2, y + yPart2, z + b2.z2));
                break;
        }
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        return !isStairs(world.getBlock(x, y, z));
    }

    public override bool same(ItemStack self, ItemStack other) => self.id == other.id;

    public override ItemStack getCanonical(byte metadata) => new ItemStack(getItem(), 1, 0);

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        if (canBreak) drops.Add(new ItemStack(getItem(), 1, 0));
    }

    public override byte maxValidMetadata() => 7; // 3 bits: facing (2) + flipped (1)
}
