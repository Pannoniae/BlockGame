using System.Numerics;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.block;

public class Torch : Block {
    public Torch( string name) : base(name) {
    }

    protected override void onRegister(int id) {
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
                RawDirection.WEST => EAST_WALL,
                RawDirection.EAST => WEST_WALL,
                RawDirection.NORTH => SOUTH_WALL,
                RawDirection.SOUTH => NORTH_WALL,
                RawDirection.UP => GROUND,
                _ => 255
            };
            
            if (attachment == 255) {
                return 255; // can't place
            }
            
            byte metadata = 0;
            metadata = setAttachment(metadata, attachment);
            return metadata;
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
            GROUND => RawDirection.UP,
            WEST_WALL => RawDirection.EAST,
            EAST_WALL => RawDirection.WEST,
            SOUTH_WALL => RawDirection.NORTH,
            NORTH_WALL => RawDirection.SOUTH,
        };
        
        if (!canAttachTo(world, x, y, z, dir)) {
            world.setBlock(x, y, z, 0);
        }
    }
    
    

    public override void renderUpdate(World world, int x, int y, int z) {
        // add some flames
        Vector3D particlePos = Vector3D.Zero;
        
        
        var dir = world.getBlockMetadata(x, y, z);
        switch (dir) {
            case GROUND:
                particlePos = new Vector3D(x + 0.5, y + 0.725, z + 0.5);
                break;
            case WEST_WALL:
                particlePos = new Vector3D(x + 0.3, y + 0.85, z + 0.5);
                break;
            case EAST_WALL:
                particlePos = new Vector3D(x + 0.7, y + 0.85, z + 0.5);
                break;
            case SOUTH_WALL:
                particlePos = new Vector3D(x + 0.5, y + 0.85, z + 0.3);
                break;
            case NORTH_WALL:
                particlePos = new Vector3D(x + 0.5, y + 0.85, z + 0.7);
                break;
        }
        
        // only 50%!

        if (Game.clientRandom.NextSingle() > 0.5) {
            Game.world.particles.add(new FlameParticle(world, particlePos));
        }
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

        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = br.forceTex;
        }

        // torch texcoords (7/16 to 9/16 width, bottom 10 pixels)
        var su = UVPair.texCoords(new UVPair(min.u + 7/16f, min.v + 6/16f));
        var su0 = su.X;
        var sv0 = su.Y;
        var sv = UVPair.texCoords(new UVPair(min.u + 9/16f, min.v + 1f));
        var su1 = sv.X;
        var sv1 = sv.Y;

        var tu = UVPair.texCoords(new UVPair(min.u + 7/16f, min.v + 6/16f));
        var tu0 = tu.X;
        var tv0 = tu.Y;
        var tv = UVPair.texCoords(new UVPair(min.u + 9/16f, min.v + 8/16f));
        var tu1 = tv.X;
        var tv1 = tv.Y;

        var bu = UVPair.texCoords(new UVPair(min.u + 7/16f, min.v + 14/16f));
        var bu0 = bu.X;
        var bv0 = bu.Y;
        var bv = UVPair.texCoords(new UVPair(min.u + 9/16f, min.v + 1f));
        var bu1 = bv.X;
        var bv1 = bv.Y;
        
        // torch tex is 2x10
        const float tiltAngle = MathF.PI / 8f; // 22.5

        switch (attachment) {
            case GROUND:
                renderTorchCube(br, x, y, z, vertices, 7/16f, 0f, 7/16f, 9/16f, 10/16f, 9/16f,
                    su0, sv0, su1, sv1, tu0, tv0, tu1, tv1, bu0, bv0, bu1, bv1);
                break;

            case WEST_WALL:
                renderTorchCube(br,  x, y, z, vertices, 0f, 3/16f, 7/16f, 2/16f, 13/16f, 9/16f,
                    su0, sv0, su1, sv1, tu0, tv0, tu1, tv1, bu0, bv0, bu1, bv1,
                    new Vector3(0, 0, 1), -tiltAngle, new Vector3(x + 0f, y + 2/16f, z + 8/16f));
                break;

            case EAST_WALL:
                renderTorchCube(br, x, y, z, vertices, 14/16f, 3/16f, 7/16f, 1f, 13/16f, 9/16f,
                    su0, sv0, su1, sv1, tu0, tv0, tu1, tv1, bu0, bv0, bu1, bv1,
                    new Vector3(0, 0, 1), tiltAngle, new Vector3(x + 1f, y + 2/16f, z + 8/16f));
                break;

            case SOUTH_WALL:
                renderTorchCube(br,  x, y, z, vertices, 7/16f, 3/16f, 0/16f, 9/16f, 13/16f, 2/16f,
                    su0, sv0, su1, sv1, tu0, tv0, tu1, tv1, bu0, bv0, bu1, bv1,
                    new Vector3(1, 0, 0), tiltAngle, new Vector3(x + 8/16f, y + 2/16f, z + 0f));
                break;

            case NORTH_WALL:
                renderTorchCube(br, x, y, z, vertices, 7/16f, 3/16f, 14/16f, 9/16f, 13/16f, 16/16f,
                    su0, sv0, su1, sv1, tu0, tv0, tu1, tv1, bu0, bv0, bu1, bv1,
                    new Vector3(1, 0, 0), -tiltAngle, new Vector3(x + 8/16f, y + 2/16f, z + 1f));
                break;
        }
    }

    private static void renderTorchCube(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices,
        float x0, float y0, float z0, float x1, float y1, float z1,
        float su0, float sv0, float su1, float sv1,
        float tu0, float tv0, float tu1, float tv1,
        float bu0, float bv0, float bu1, float bv1,
        Vector3 ax = default, float angle = 0f, Vector3 pivot = default) {

        bool brot = angle != 0f;


        for (RawDirection i = 0; i < RawDirection.MAX; i++) {
            br.applySimpleLighting(RawDirection.NONE);
            
            
            Vector3 v0 = default;
            Vector3 v1 = default;
            Vector3 v2 = default;
            Vector3 v3 = default;

            float u0_ = 0;
            float v0_ = 0;
            float u1_ = 0;
            float v1_ = 0;

            switch (i) {
                case RawDirection.WEST:
                    v0 = new(x + x0, y + y1, z + z1);
                    v1 = new(x + x0, y + y0, z + z1);
                    v2 = new(x + x0, y + y0, z + z0);
                    v3 = new(x + x0, y + y1, z + z0);
                    u0_ = su0; v0_ = sv0;
                    u1_ = su1; v1_ = sv1;
                    
                    break;
                case RawDirection.EAST:
                    v0 = new(x + x1, y + y1, z + z0);
                    v1 = new(x + x1, y + y0, z + z0);
                    v2 = new(x + x1, y + y0, z + z1);
                    v3 = new(x + x1, y + y1, z + z1);
                    u0_ = su0; v0_ = sv0;
                    u1_ = su1; v1_ = sv1;
                    break;
                case RawDirection.SOUTH:
                    v0 = new(x + x0, y + y1, z + z0);
                    v1 = new(x + x0, y + y0, z + z0);
                    v2 = new(x + x1, y + y0, z + z0);
                    v3 = new(x + x1, y + y1, z + z0);
                    u0_ = su0; v0_ = sv0;
                    u1_ = su1; v1_ = sv1;
                    break;
                case RawDirection.NORTH:
                    v0 = new(x + x1, y + y1, z + z1);
                    v1 = new(x + x1, y + y0, z + z1);
                    v2 = new(x + x0, y + y0, z + z1);
                    v3 = new(x + x0, y + y1, z + z1);
                    u0_ = su0; v0_ = sv0;
                    u1_ = su1; v1_ = sv1;
                    break;
                case RawDirection.DOWN:
                    v0 = new(x + x1, y + y0, z + z1);
                    v1 = new(x + x1, y + y0, z + z0);
                    v2 = new(x + x0, y + y0, z + z0);
                    v3 = new(x + x0, y + y0, z + z1);
                    u0_ = bu0; v0_ = bv0;
                    u1_ = bu1; v1_ = bv1;
                    break;
                case RawDirection.UP:
                    v0 = new(x + x0, y + y1, z + z1);
                    v1 = new(x + x0, y + y1, z + z0);
                    v2 = new(x + x1, y + y1, z + z0);
                    v3 = new(x + x1, y + y1, z + z1);
                    u0_ = tu0; v0_ = tv0;
                    u1_ = tu1; v1_ = tv1;
                    break;

            }

            v0 = Meth.transformVertex(v0.X, v0.Y, v0.Z, brot, pivot, angle, ax);
            v1 = Meth.transformVertex(v1.X, v1.Y, v1.Z, brot, pivot, angle, ax);
            v2 = Meth.transformVertex(v2.X, v2.Y, v2.Z, brot, pivot, angle, ax);
            v3 = Meth.transformVertex(v3.X, v3.Y, v3.Z, brot, pivot, angle, ax);
            
            br.begin();

            br.vertex(v0.X, v0.Y, v0.Z, u0_, v0_);
                br.vertex(v1.X, v1.Y, v1.Z, u0_, v1_);
                br.vertex(v2.X, v2.Y, v2.Z, u1_, v1_);
                br.vertex(v3.X, v3.Y, v3.Z, u1_, v0_);
                
            br.end(vertices);
            
            
        }
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        var attachment = getAttachment(metadata);

        switch (attachment) {
            case GROUND:
                aabbs.Add(new AABB(x + 6/16f, y + 0f, z + 6/16f, x + 10/16f, y + 10/16f, z + 10/16f));
                break;

            case WEST_WALL:
                aabbs.Add(new AABB(x + 0f, y + 3/16f, z + 6/16f, x + 4/16f, y + 13/16f, z + 10/16f));
                break;

            case EAST_WALL:
                aabbs.Add(new AABB(x + 12/16f, y + 3/16f, z + 6/16f, x + 1f, y + 13/16f, z + 10/16f));
                break;

            case SOUTH_WALL:
                aabbs.Add(new AABB(x + 6/16f, y + 3/16f, z + 0/16f, x + 10/16f, y + 13/16f, z + 4/16f));
                break;

            case NORTH_WALL:
                aabbs.Add(new AABB(x + 6/16f, y + 3/16f, z + 12/16f, x + 10/16f, y + 13/16f, z + 16/16f));
                break;
        }
    }
    
    public override bool canPlace(World world, int x, int y, int z, RawDirection dir) {
        dir = Game.raycast.face;
        return base.canPlace(world, x, y, z, dir) && calculatePlacementMetadata(world, x, y, z, dir) != 255;
    }
    
    public override byte maxValidMetadata() {
        return 4; // 0-4 for the 5 attachment types
    }
}