using System.Numerics;
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
        // dir is the face we clicked on, which indicates where the supporting block is
        if (canAttachTo(world, x, y, z, dir)) {
            byte attachment = dir switch {
                RawDirection.DOWN => GROUND,      // clicking bottom face = torch on ground
                RawDirection.WEST => WEST_WALL,   // clicking west face = torch on west wall
                RawDirection.EAST => EAST_WALL,   // clicking east face = torch on east wall  
                RawDirection.NORTH => NORTH_WALL, // clicking north face = torch on north wall
                RawDirection.SOUTH => SOUTH_WALL, // clicking south face = torch on south wall
                RawDirection.UP => GROUND,        // clicking top face = fallback to ground
                _ => GROUND
            };
            
            byte metadata = 0;
            metadata = setAttachment(metadata, attachment);
            return metadata;
        }
        
        // fallback: try to place on ground if there's a block below
        if (canAttachTo(world, x, y, z, RawDirection.DOWN)) {
            return setAttachment(0, GROUND);
        }
        
        return 255; // can't place
    }

    private bool canAttachTo(World world, int x, int y, int z, RawDirection dir) {
        var offset = Direction.getDirection(dir);
        var supportX = x + offset.X;
        var supportY = y + offset.Y;
        var supportZ = z + offset.Z;
        
        var supportBlock = world.getBlockRaw(supportX, supportY, supportZ);
        var supportBlockId = supportBlock.getID();
        
        if (supportBlockId == Blocks.AIR) return false;
        
        var support = get(supportBlockId);
        return support != null && !translucent[supportBlockId];
    }

    public override void place(World world, int x, int y, int z, byte metadata, RawDirection dir) {
        var meta = calculatePlacementMetadata(world, x, y, z, dir);
        if (meta == 255) {
            return; // can't place
        }

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

        var uv = uvs[0];
        // TODO
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var attachment = getAttachment(metadata);
        
        // TODO
        aabbs.Add(new AABB(x + 0.4f, y + 0.0f, z + 0.4f, x + 0.6f, y + 0.6f, z + 0.6f)); // placeholder
    }
    
    public override bool canPlace(World world, int x, int y, int z, RawDirection dir) {
        return calculatePlacementMetadata(world, x, y, z, dir) != 255;
    }
    
    public override byte maxValidMetadata() {
        return 4; // 0-4 for the 5 attachment types
    }
}