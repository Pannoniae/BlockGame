using BlockGame.GL.vertexformats;
using BlockGame.util;

namespace BlockGame.world.block;

public class Torch : Block {
    public Torch(ushort id, string name) : base(id, name) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
        partialBlock();
        transparency();
        light(14);
        noCollision();
    }

    /**
     * Metadata encoding for torch:
     * Bits 0-2: Attachment type (0=ground, 1=west wall, 2=east wall, 3=south wall, 4=north wall)
     * Bits 3-7: Reserved
     */
    public static byte getAttachment(byte metadata) => (byte)(metadata & 0b111);
    public static byte setAttachment(byte metadata, byte attachment) => (byte)((metadata & ~0b111) | (attachment & 0b111));
    
    public const byte GROUND = 0;
    public const byte WEST_WALL = 1;
    public const byte EAST_WALL = 2;
    public const byte SOUTH_WALL = 3;
    public const byte NORTH_WALL = 4;

    private byte calculatePlacementMetadata(World world, int x, int y, int z, RawDirection dir) {
        // check if we can place on the target surface
        if (canAttachTo(world, x, y, z, dir)) {
            byte attachment = dir switch {
                RawDirection.UP => GROUND,
                RawDirection.WEST => WEST_WALL,
                RawDirection.EAST => EAST_WALL,
                RawDirection.SOUTH => SOUTH_WALL,
                RawDirection.NORTH => NORTH_WALL,
                _ => GROUND
            };
            
            byte metadata = 0;
            metadata = setAttachment(metadata, attachment);
            return metadata;
        }
        
        // fallback: try to place on ground
        if (canAttachTo(world, x, y, z, RawDirection.UP)) {
            return setAttachment(0, GROUND);
        }
        
        return 0; // can't place
    }

    private bool canAttachTo(World world, int x, int y, int z, RawDirection dir) {
        var offset = Direction.getDirection(dir);
        var supportX = x + offset.X;
        var supportY = y + offset.Y;
        var supportZ = z + offset.Z;
        
        var supportBlock = world.getBlockRaw(supportX, supportY, supportZ);
        var supportBlockId = supportBlock.getID();
        
        if (supportBlockId == Blocks.AIR) return false;
        
        var support = Block.get(supportBlockId);
        return support != null && !Block.translucent[supportBlockId];
    }

    public override void place(World world, int x, int y, int z, byte metadata, RawDirection dir) {
        var meta = calculatePlacementMetadata(world, x, y, z, dir);
        if (meta == 0) return; // can't place
        
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
        var attachment = getAttachment(metadata);

        var min = uvs[0];
        var max = uvs[0] + 1;
        var u0 = texU(min.u);
        var v0 = texV(min.v);
        var u1 = texU(max.u);
        var v1 = texV(max.v);

        const float torchWidth = 2f/16f;  // 2 pixels wide
        const float torchHeight = 10f/16f; // 10 pixels tall
        const float torchDepth = 2f/16f;   // 2 pixels deep
        
        switch (attachment) {
            case GROUND:
                renderGroundTorch(br, x, y, z, vertices, torchWidth, torchHeight, torchDepth, u0, v0, u1, v1);
                break;
            case WEST_WALL:
                renderWallTorch(br, x, y, z, vertices, torchWidth, torchHeight, torchDepth, u0, v0, u1, v1, -0.4f, 0f);
                break;
            case EAST_WALL:
                renderWallTorch(br, x, y, z, vertices, torchWidth, torchHeight, torchDepth, u0, v0, u1, v1, 0.4f, 0f);
                break;
            case SOUTH_WALL:
                renderWallTorch(br, x, y, z, vertices, torchWidth, torchHeight, torchDepth, u0, v0, u1, v1, 0f, 0.4f);
                break;
            case NORTH_WALL:
                renderWallTorch(br, x, y, z, vertices, torchWidth, torchHeight, torchDepth, u0, v0, u1, v1, 0f, -0.4f);
                break;
        }
    }

    private void renderGroundTorch(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices, 
        float width, float height, float depth, float u0, float v0, float u1, float v1) {
        
        float centerX = 0.5f;
        float centerZ = 0.5f;
        float x1 = centerX - width / 2;
        float x2 = centerX + width / 2;
        float z1 = centerZ - depth / 2;
        float z2 = centerZ + depth / 2;
        
        BlockRenderer.renderCube(br, x, y, z, vertices, x1, 0f, z1, x2, height, z2, u0, v0, u1, v1);
    }

    private void renderWallTorch(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices,
        float width, float height, float depth, float u0, float v0, float u1, float v1, 
        float offsetX, float offsetZ) {
        
        float centerX = 0.5f + offsetX;
        float centerZ = 0.5f + offsetZ;
        float baseY = 0.2f; // slightly raised from ground
        
        float x1 = centerX - width / 2;
        float x2 = centerX + width / 2;
        float z1 = centerZ - depth / 2;
        float z2 = centerZ + depth / 2;
        
        BlockRenderer.renderCube(br, x, y, z, vertices, x1, baseY, z1, x2, baseY + height, z2, u0, v0, u1, v1);
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        // torches have no collision
    }
    
    public override bool canPlace(World world, int x, int y, int z, RawDirection dir) {
        return calculatePlacementMetadata(world, x, y, z, dir) != 0;
    }
    
    public override byte maxValidMetadata() {
        return 4; // 0-4 for the 5 attachment types
    }
}