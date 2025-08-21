using System.Numerics;
using BlockGame.GL.vertexformats;
using Molten;

namespace BlockGame.util;

public class Stairs : Block {
    public Stairs(ushort id, string name) : base(id, name) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
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
    
    private uint calculateMetadata(ushort blockId, RawDirection dir) {
        
        // we need to place in the opposite direction the player is facing
        var opposite = Direction.getOpposite(dir);
        
        // Create metadata
        byte metadata = 0;
        metadata = setFacing(metadata, (byte)opposite);
        metadata = setUpsideDown(metadata, false);
        
        // Create full block value with metadata
        uint blockValue = blockId;
        blockValue = blockValue.setMetadata(metadata);
        
        return blockValue;
    }


    public override void place(World world, int x, int y, int z, RawDirection dir) {
        
        var stair = calculateMetadata(id, dir);
        world.setBlockMetadataRemesh(x, y, z, stair);
        world.blockUpdateWithNeighbours(new Vector3I(x, y, z));
        
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);
        
        var block = br.getBlock();
        var metadata = block.getMetadata();
        var facing = getFacing(metadata);

        var texture = model.faces[0];
        var u0 = texU(texture.min.u);
        var v0 = texV(texture.min.v);
        var u1 = texU(texture.max.u);
        var v1 = texV(texture.max.v);

        // top step: half width/depth in facing direction, half height
        float tx1, tz1, tx2, tz2;
        
        switch (facing) {
            case 0: tx1 = 0.5f; tx2 = 1f; tz1 = 0f; tz2 = 1f; break; // WEST
            case 1: tx1 = 0f; tx2 = 0.5f; tz1 = 0f; tz2 = 1f; break; // EAST
            case 2: tx1 = 0f; tx2 = 1f; tz1 = 0.5f; tz2 = 1f; break; // SOUTH
            default: tx1 = 0f; tx2 = 1f; tz1 = 0f; tz2 = 0.5f; break; // NORTH
        }

        // render bottom cuboid (full width, half height)
        BlockRenderer.renderCuboid(br, x, y, z, vertices, 0f, 0f, 0f, 1f, 0.5f, 1f, u0, v0, u1, v1);
        
        // render top cuboid (variable size based on facing)
        BlockRenderer.renderCuboid(br, x, y, z, vertices, tx1, 0.5f, tz1, tx2, 1f, tz2, u0, v0, u1, v1);
    }
}