using System.Numerics;
using BlockGame.GL.vertexformats;
using BlockGame.util;
using Molten.DoublePrecision;

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
        if (canAttachTo(world, x, y, z, dir)) {
            byte attachment = dir switch {
                RawDirection.DOWN => GROUND,
                RawDirection.WEST => EAST_WALL,
                RawDirection.EAST => WEST_WALL,
                RawDirection.NORTH => SOUTH_WALL,
                RawDirection.SOUTH => NORTH_WALL,
                RawDirection.UP => GROUND,
                _ => GROUND
            };
            
            byte metadata = 0;
            metadata = setAttachment(metadata, attachment);
            if (canAttachTo(world, x, y, z, dir)) {
                return metadata;
            }
        }
        
        return 255; // can't place
    }

    private bool canAttachTo(World world, int x, int y, int z, RawDirection dir) {

        dir = dir.opposite();
        var offset = Direction.getDirection(dir);
        var supportX = x + offset.X;
        var supportY = y + offset.Y;
        var supportZ = z + offset.Z;
        
        var supportBlock = world.getBlock(supportX, supportY, supportZ);
        
        return fullBlock[supportBlock];
    }

    public override void place(World world, int x, int y, int z, byte metadata, RawDirection dir) {

        dir = Game.raycast.face;
        var meta = calculatePlacementMetadata(world, x, y, z, dir);
        if (meta == 255) {
            return; // can't place
        }

        uint blockValue = id;
        blockValue = blockValue.setMetadata(meta);
        
        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
    }

    public override void update(World world, int x, int y, int z) {
        // check if still attached to a valid block
        var block = world.getBlockRaw(x, y, z);
        var metadata = block.getMetadata();
        var attachment = getAttachment(metadata);
        
        RawDirection dir = attachment switch {
            GROUND => RawDirection.DOWN,
            WEST_WALL => RawDirection.EAST,
            EAST_WALL => RawDirection.WEST,
            SOUTH_WALL => RawDirection.NORTH,
            NORTH_WALL => RawDirection.SOUTH,
            _ => RawDirection.DOWN
        };
        
        if (!canAttachTo(world, x, y, z, dir)) {
            world.setBlock(x, y, z, 0);
        }
    }
    
    

    public override void renderUpdate(World world, int x, int y, int z) {
        // add some flames
        var particlePos = new Vector3D(x + 0.5f, y + 0.7f, z + 0.5f);
        Game.world.particles.add(new FlameParticle(world, particlePos, Vector3D.Zero));
        Console.Out.WriteLine(Game.world.particles.particles.Count);
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
        
        // we don't need AO
        var AO = br.AO;
        br.AO = false;

        switch (attachment) {
            case GROUND:
                // bottom part: 7/16 to 9/16, height 0 to 0.5
                br.renderCube(x, y, z, vertices, 7/16f, 0f, 7/16f, 9/16f, 0.5f, 9/16f, u0, v0, u1, v1);
                // top part: 6/16 to 10/16, height 0.5 to 15/16
                br.renderCube(x, y, z, vertices, 6/16f, 0.5f, 6/16f, 10/16f, 15/16f, 10/16f, u0, v0, u1, v1);
                break;
                
            case WEST_WALL: // attached to west wall, torch points east
                // bottom: against west wall
                br.renderCube(x, y, z, vertices, 0f, 0f, 7/16f, 2/16f, 0.5f, 9/16f, u0, v0, u1, v1);
                // top: angled toward east
                br.renderCube(x, y, z, vertices, 2/16f, 0.5f, 6/16f, 6/16f, 15/16f, 10/16f, u0, v0, u1, v1);
                break;
                
            case EAST_WALL: // attached to east wall, torch points west  
                // bottom: against east wall
                br.renderCube(x, y, z, vertices, 14/16f, 0f, 7/16f, 1f, 0.5f, 9/16f, u0, v0, u1, v1);
                // top: angled toward west
                br.renderCube(x, y, z, vertices, 10/16f, 0.5f, 6/16f, 14/16f, 15/16f, 10/16f, u0, v0, u1, v1);
                break;
            
            case SOUTH_WALL:
                // bottom: against north wall  
                br.renderCube(x, y, z, vertices, 7/16f, 0f, 0f, 9/16f, 0.5f, 2/16f, u0, v0, u1, v1);
                // top: angled toward south
                br.renderCube(x, y, z, vertices, 6/16f, 0.5f, 2/16f, 10/16f, 15/16f, 6/16f, u0, v0, u1, v1);
                break;
                
            case NORTH_WALL:
                // bottom: against south wall
                br.renderCube(x, y, z, vertices, 7/16f, 0f, 14/16f, 9/16f, 0.5f, 1f, u0, v0, u1, v1);
                // top: angled toward north
                br.renderCube(x, y, z, vertices, 6/16f, 0.5f, 10/16f, 10/16f, 15/16f, 14/16f, u0, v0, u1, v1);
                break;
        }
        
        br.AO = AO;
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var attachment = getAttachment(metadata);
        
        switch (attachment) {
            case GROUND:
                // encompass both bottom and top parts
                aabbs.Add(new AABB(x + 6/16f, y + 0f, z + 6/16f, x + 10/16f, y + 15/16f, z + 10/16f));
                break;
                
            case WEST_WALL: // attached to west wall, extends east
                aabbs.Add(new AABB(x + 0f, y + 0f, z + 6/16f, x + 6/16f, y + 15/16f, z + 10/16f));
                break;
                
            case EAST_WALL: // attached to east wall, extends west
                aabbs.Add(new AABB(x + 10/16f, y + 0f, z + 6/16f, x + 1f, y + 15/16f, z + 10/16f));
                break;
                
            case NORTH_WALL:
                aabbs.Add(new AABB(x + 6/16f, y + 0f, z + 10/16f, x + 10/16f, y + 15/16f, z + 1f));
                break;
                
            case SOUTH_WALL:
                aabbs.Add(new AABB(x + 6/16f, y + 0f, z + 0f, x + 10/16f, y + 15/16f, z + 6/16f));
                break;
        }
    }
    
    public override bool canPlace(World world, int x, int y, int z, RawDirection dir) {
        dir = Game.raycast.face;
        return calculatePlacementMetadata(world, x, y, z, dir) != 255;
    }
    
    public override byte maxValidMetadata() {
        return 4; // 0-4 for the 5 attachment types
    }
}