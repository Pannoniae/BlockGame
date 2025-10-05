using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;

namespace BlockGame.world.block;

public class Stairs : Block {
    public Stairs(ushort id, string name) : base(id, name) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
        partialBlock();
    }

    /**
     * Metadata encoding for stairs:
     * Bits 0-1: Horizontal facing direction (0=WEST, 1=EAST, 2=SOUTH, 3=NORTH)
     * Bit 2: Upside-down (0=normal/bottom-half, 1=upside-down/top-half) (NOT USED YET)
     * Bits 3-7: Reserved
     */
    public static byte getFacing(byte metadata) => (byte)(metadata & 0b11);
    public static bool isUpsideDown(byte metadata) => (metadata & 0b100) != 0;
    public static byte setFacing(byte metadata, byte facing) => (byte)((metadata & ~0b11) | (facing & 0b11));
    public static byte setUpsideDown(byte metadata, bool upsideDown) => (byte)((metadata & ~0b100) | (upsideDown ? 0b100 : 0));
    
    private byte calculatePlacementMetadata(RawDirection dir) {
        // we need to place in the opposite direction the player is facing
        var opposite = Direction.getOpposite(dir);
        
        // Create metadata
        byte metadata = 0;
        metadata = setFacing(metadata, (byte)opposite);
        metadata = setUpsideDown(metadata, false);
        
        return metadata;
    }


    public override void place(World world, int x, int y, int z, byte metadata, RawDirection dir) {
        var meta = calculatePlacementMetadata(dir);
        uint blockValue = id;
        blockValue = blockValue.setMetadata(meta);
        
        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
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
        var max = uvs[0] + 1;

        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = new UVPair(br.forceTex.u, br.forceTex.v);
            max = min + 1;
        }

        var u0 = UVPair.texU(min.u);
        var v0 = UVPair.texV(min.v);
        var u1 = UVPair.texU(max.u);
        var v1 = UVPair.texV(max.v);

        // top step: half width/depth in facing direction, half height
        float tx1, tz1, tx2, tz2;
        
        switch (facing) {
            case 0: tx1 = 0.5f; tx2 = 1f; tz1 = 0f; tz2 = 1f; break; // WEST
            case 1: tx1 = 0f; tx2 = 0.5f; tz1 = 0f; tz2 = 1f; break; // EAST
            case 2: tx1 = 0f; tx2 = 1f; tz1 = 0.5f; tz2 = 1f; break; // SOUTH
            default: tx1 = 0f; tx2 = 1f; tz1 = 0f; tz2 = 0.5f; break; // NORTH
        }

        // render bottom
        br.renderCube(x, y, z, vertices, 0f, 0f, 0f, 1f, 0.5f, 1f, u0, v0, u1, v1);
        
        // render top
        br.renderCube(x, y, z, vertices, tx1, 0.5f, tz1, tx2, 1f, tz2, u0, v0, u1, v1);
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var facing = getFacing(metadata);

        // bottom half
        aabbs.Add(new AABB(x + 0f, y + 0f, z + 0f, x + 1f, y + 0.5f, z + 1f));
        
        // top half
        switch (facing) {
            case 0: // WEST
                aabbs.Add(new AABB(x + 0.5f, y + 0.5f, z + 0f, x + 1f, y + 1f, z + 1f));
                break;
            case 1: // EAST
                aabbs.Add(new AABB(x + 0f, y + 0.5f, z + 0f, x + 0.5f, y + 1f, z + 1f));
                break;
            case 2: // SOUTH
                aabbs.Add(new AABB(x + 0f, y + 0.5f, z + 0.5f, x + 1f, y + 1f, z + 1f));
                break;
            case 3: // NORTH
                aabbs.Add(new AABB(x + 0f, y + 0.5f, z + 0f, x + 1f, y + 1f,z +  0.5f));
                break;
        }
    }
    
    public override bool canPlace(World world, int x, int y, int z, RawDirection dir) {
        var existingBlock = world.getBlockRaw(x, y, z);
        var existingId = existingBlock.getID();
        
        // prevent placing stairs into existing stairs
        if (existingId == id || existingId == Block.MAPLE_STAIRS.id) {
            return false;
        }
        
        return true;
    }

    public override byte maxValidMetadata() {
        // 4 facing directions (0-3), upside-down not implemented yet
        return 3;
    }
}