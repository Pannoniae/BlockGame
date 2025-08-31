using BlockGame.GL.vertexformats;
using BlockGame.util;

namespace BlockGame.src.world.block;

public class Slabs : Block {
    public Slabs(ushort id, string name) : base(id, name) {
        renderType[id] = RenderType.CUSTOM;
        customCulling[id] = true;
        customAABB[id] = true;
        partialBlock();
    }

    /**
     * Metadata encoding for slabs:
     * Bit 0: Half position (0=bottom, 1=top)
     * Bit 1: Double slab (0=single slab, 1=double slab)
     * Bits 2-7: Reserved
     */
    public static bool isTop(byte metadata) => (metadata & 0b1) != 0;
    public static bool isDouble(byte metadata) => (metadata & 0b10) != 0;
    public static byte setTop(byte metadata, bool top) => (byte)((metadata & ~0b1) | (top ? 0b1 : 0));
    public static byte setDouble(byte metadata, bool doubleSlab) => (byte)((metadata & ~0b10) | (doubleSlab ? 0b10 : 0));

    private byte calculatePlacement(World world, int x, int y, int z, RawDirection dir) {
        var existingBlock = world.getBlockRaw(x, y, z);
        var existingBlockId = existingBlock.getID();
        
        // check if there's already a slab here that we can combine
        if (existingBlockId == id) {
            var existingMetadata = existingBlock.getMetadata();
            // if it's already a double slab, can't place
            if (isDouble(existingMetadata)) {
                return 0;
            }

            // would become double slab
            byte metadata = 0;
            metadata = setTop(metadata, false);
            metadata = setDouble(metadata, true);
            return metadata;
        }
        
        bool placeOnTop = determinePlacement(world, x, y, z, dir);
        
        byte newMetadata = 0;
        newMetadata = setTop(newMetadata, placeOnTop);
        newMetadata = setDouble(newMetadata, false);
        return newMetadata;
    }

    public override void place(World world, int x, int y, int z, RawDirection dir) {
        var meta = calculatePlacement(world, x, y, z, dir);
        
        uint blockValue = id;
        blockValue = blockValue.setMetadata(meta);
        
        world.setBlockMetadata(x, y, z, blockValue);
        world.blockUpdateNeighbours(x, y, z);
    }

    private bool determinePlacement(World world, int x, int y, int z, RawDirection hitFace) {
        // if placing on top face of a block, place as bottom slab
        if (hitFace == RawDirection.UP) {
            return false; // bottom slab
        }
        
        // if placing on bottom face of a block, place as top slab
        if (hitFace == RawDirection.DOWN) {
            return true; // top slab
        }
        
        // for side faces, use cursor position within the block
        var raycast = Raycast.raycast(world);
        if (raycast.hit) {
            var hitPoint = raycast.point;
            var blockY = y;
            var relativeY = hitPoint.Y - blockY;
            
            // if hit in upper half of block, place top slab
            // if hit in lower half of block, place bottom slab
            return relativeY > 0.5;
        }
        
        // fallback to bottom slab
        return false;
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        base.render(br, x, y, z, vertices);

        x &= 15;
        y &= 15;
        z &= 15;

        var block = br.getBlock();
        var metadata = block.getMetadata();
        var top = isTop(metadata);
        var doubleSlab = isDouble(metadata);

        var min = uvs[0];
        var max = uvs[0] + 1;
        var u0 = texU(min.u);
        var v0 = texV(min.v);
        var u1 = texU(max.u);
        var v1 = texV(max.v);

        float y0;
        float y1;

        if (doubleSlab) {
            y0 = 0f;
            y1 = 1f;
        } else if (top) {
            y0 = 0.5f;
            y1 = 1f;
        } else {
            y0 = 0f;
            y1 = 0.5f;
        }
        
        BlockRenderer.renderCube(br, x, y, z, vertices, 0f, y0, 0f, 1f, y1, 1f, u0, v0, u1, v1);
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var top = isTop(metadata);
        var doubleSlab = isDouble(metadata);

        if (doubleSlab) {
            // full block
            aabbs.Add(new AABB(x + 0f, y + 0f, z + 0f, x + 1f, y + 1f, z + 1f));
        } else if (top) {
            aabbs.Add(new AABB(x + 0f, y + 0.5f, z + 0f, x + 1f, y + 1f, z + 1f));
        } else {
            aabbs.Add(new AABB(x + 0f, y + 0f, z + 0f, x + 1f, y + 0.5f, z + 1f));
        }
    }
    
    public override bool canPlace(World world, int x, int y, int z, RawDirection dir) {
        var meta = calculatePlacement(world, x, y, z, dir);
        var isDoubleSlab = isDouble(meta);
        
        // if trying to place on existing double slab, can't place
        if (!isDoubleSlab && meta == 0) {
            var existingBlock = world.getBlockRaw(x, y, z);
            if (existingBlock.getID() == id && isDouble(existingBlock.getMetadata())) {
                return false;
            }
        }
        
        getAABBs(world, x, y, z, meta, AABBList);
        
        var entities = new List<Entity>();
        foreach (var aabb in AABBList) {
            world.getEntitiesInBox(entities, aabb.min.toBlockPos(),
                aabb.max.toBlockPos() + 1);
            
            foreach (var entity in entities) {
                if (util.AABB.isCollision(aabb, entity.aabb)) {
                    return false;
                }
            }
            entities.Clear();
        }
        return true;
    }
    
    public override byte maxValidMetadata() {
        // bit 0: bottom(0)/top(1), bit 1: single(0)/double(1) -> values 0,1,2,3
        return 3;
    }
}