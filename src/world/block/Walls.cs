using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;

public class Walls : Block {
    
    public Walls(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
        partialBlock();
    }

    /**
     * Metadata encoding for walls:
     * Bit 0: Half position (0=front, 1=back)
     * Bit 1: Double wall (0=single wall, 1=double wall)
     * Bit 2: Axis (0=Z/north-south, 1=X/east-west)
     * Bits 3-7: Reserved
     */
    public static bool isBack(byte metadata) => (metadata & 0b1) != 0;
    public static bool isDouble(byte metadata) => (metadata & 0b10) != 0;
    public static bool isXAxis(byte metadata) => (metadata & 0b100) != 0;
    public static byte setBack(byte metadata, bool back) => (byte)((metadata & ~0b1) | (back ? 0b1 : 0));
    public static byte setDouble(byte metadata, bool doubleWall) => (byte)((metadata & ~0b10) | (doubleWall ? 0b10 : 0));
    public static byte setXAxis(byte metadata, bool xAxis) => (byte)((metadata & ~0b100) | (xAxis ? 0b100 : 0));

    private byte calculatePlacement(World world, int x, int y, int z, Placement info) {
        var existingBlock = world.getBlockRaw(x, y, z);
        var existingBlockId = existingBlock.getID();

        // check if there's already a wall here that we can combine
        if (existingBlockId == id) {
            var existingMetadata = existingBlock.getMetadata();
            if (isDouble(existingMetadata)) {
                return 0;
            }

            // preserve axis, make double
            byte metadata = existingMetadata;
            metadata = setBack(metadata, false);
            metadata = setDouble(metadata, true);
            return metadata;
        }

        var (back, xAxis) = determinePlacement(world, x, y, z, info);

        byte newMetadata = 0;
        newMetadata = setBack(newMetadata, back);
        newMetadata = setDouble(newMetadata, false);
        newMetadata = setXAxis(newMetadata, xAxis);
        return newMetadata;
    }

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        var meta = calculatePlacement(world, x, y, z, info);
        
        uint blockValue = id;
        blockValue = blockValue.setMetadata(meta);
        
        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
    }

    /** determines front/back and axis from player facing and hit point */
    private (bool back, bool xAxis) determinePlacement(World world, int x, int y, int z, Placement info) {
        var hf = info.hfacing;
        bool xAxis = hf is RawDirectionH.EAST or RawDirectionH.WEST;

        if (xAxis) {
            // X-axis wall
            if (info.face == RawDirection.EAST) return (true, true);
            if (info.face == RawDirection.WEST) return (false, true);
            // use cursor position for top/bottom/other faces
            var relX = info.hitPoint.X - x;
            return (relX > 0.5, true);
        }
        else {
            // Z-axis wall
            if (info.face == RawDirection.SOUTH) return (false, false);
            if (info.face == RawDirection.NORTH) return (true, false);
            var relZ = info.hitPoint.Z - z;
            return (relZ > 0.5, false);
        }
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);

        x &= 15;
        y &= 15;
        z &= 15;

        var block = br.getBlock();
        var metadata = block.getMetadata();
        var back = isBack(metadata);
        var doubleWall = isDouble(metadata);
        var xAxis = isXAxis(metadata);

        var min = uvs[0];
        var max = uvs[0] + 1;

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

        float x0 = 0f, x1 = 1f, z0 = 0f, z1 = 1f;

        if (doubleWall) {
            // full block, both axes same
        } else if (xAxis) {
            if (back) { x0 = 0.5f; x1 = 1f; }
            else { x0 = 0f; x1 = 0.5f; }
        } else {
            if (back) { z0 = 0.5f; z1 = 1f; }
            else { z0 = 0f; z1 = 0.5f; }
        }

        br.renderCube(x, y, z, vertices, x0, 0, z0, x1, 1f, z1, u0, v0, u1, v1);
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var back = isBack(metadata);
        var doubleWall = isDouble(metadata);
        var xAxis = isXAxis(metadata);

        if (doubleWall) {
            aabbs.Add(new AABB(x + 0f, y + 0f, z + 0f, x + 1f, y + 1f, z + 1f));
        } else if (xAxis) {
            if (back) aabbs.Add(new AABB(x + 0.5f, y + 0f, z + 0f, x + 1f, y + 1f, z + 1f));
            else aabbs.Add(new AABB(x + 0f, y + 0f, z + 0f, x + 0.5f, y + 1f, z + 1f));
        } else {
            if (back) aabbs.Add(new AABB(x + 0f, y + 0f, z + 0.5f, x + 1f, y + 1f, z + 1f));
            else aabbs.Add(new AABB(x + 0f, y + 0f, z + 0f, x + 1f, y + 1f, z + 0.5f));
        }
    }
    
    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        var meta = calculatePlacement(world, x, y, z, info);
        
        // if trying to place on existing double wall, can't place
        if (meta == 0) {
            var existingBlock = world.getBlockRaw(x, y, z);
            if (existingBlock.getID() == id && isDouble(existingBlock.getMetadata())) {
                return false;
            }
        }
        
        return true;
    }
    
    public override byte maxValidMetadata() {
        // bit 0: front/back, bit 1: single/double, bit 2: Z/X axis -> values 0-7
        return 7;
    }
}