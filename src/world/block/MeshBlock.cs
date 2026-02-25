using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;

namespace BlockGame.world.block;

public class MeshBlock : Ladder {
    private const byte WALL_BIT = 0b100;

    public MeshBlock(string name) : base(name) {
    }

    private static bool isWall(byte metadata) => (metadata & WALL_BIT) != 0;

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        var targetBlock = world.getBlock(x, y, z);
        if (targetBlock != 0 && targetBlock != WATER.id) return false;

        // side face: need solid block behind
        if (info.face is not RawDirection.UP and not RawDirection.DOWN) {
            var o = Direction.getDirection(info.face.opposite());
            return fullBlock[world.getBlock(x + o.X, y + o.Y, z + o.Z)];
        }

        // top/bottom: need solid or mesh below
        var below = world.getBlock(x, y - 1, z);
        return fullBlock[below] || below == id;
    }

    public override void place(World world, int x, int y, int z, byte metadata, Placement info) {
        byte facing;
        if (info.face is RawDirection.UP or RawDirection.DOWN) {
            facing = (byte)info.hfacing;
        }
        else {
            facing = (byte)((byte)info.face | WALL_BIT);
        }

        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(facing));
        world.blockUpdateNeighbours(x, y, z);
    }

    public override byte maxValidMetadata() => 7;

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        // call Block.render, not Ladder.render
        base.render(br, x, y, z, vertices);
        x &= 15;
        y &= 15;
        z &= 15;

        var block = br.getBlock();
        var metadata = block.getMetadata();
        var facing = (byte)(metadata & 0b11);

        var min = uvs[0];
        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = br.forceTex;
        }

        var uv0 = UVPair.texCoords(min);
        var uv1 = UVPair.texCoords(min + 1);
        var u0 = uv0.X;
        var v0 = uv0.Y;
        var u1 = uv1.X;
        var v1 = uv1.Y;

        const float t = 1f / 16f;

        br.applySimpleLighting(RawDirection.NONE);

        // front face
        br.begin();
        switch (facing) {
            case 0: // WEST
                br.vertex(x + 1 - t, y + 1, z + 1, u0, v0);
                br.vertex(x + 1 - t, y, z + 1, u0, v1);
                br.vertex(x + 1 - t, y, z, u1, v1);
                br.vertex(x + 1 - t, y + 1, z, u1, v0);
                break;
            case 1: // EAST
                br.vertex(x + t, y + 1, z, u0, v0);
                br.vertex(x + t, y, z, u0, v1);
                br.vertex(x + t, y, z + 1, u1, v1);
                br.vertex(x + t, y + 1, z + 1, u1, v0);
                break;
            case 2: // SOUTH
                br.vertex(x, y + 1, z + 1 - t, u0, v0);
                br.vertex(x, y, z + 1 - t, u0, v1);
                br.vertex(x + 1, y, z + 1 - t, u1, v1);
                br.vertex(x + 1, y + 1, z + 1 - t, u1, v0);
                break;
            default: // NORTH
                br.vertex(x + 1, y + 1, z + t, u0, v0);
                br.vertex(x + 1, y, z + t, u0, v1);
                br.vertex(x, y, z + t, u1, v1);
                br.vertex(x, y + 1, z + t, u1, v0);
                break;
        }
        br.end(vertices);

        // back face (reversed winding)
        br.begin();
        switch (facing) {
            case 0: // WEST
                br.vertex(x + 1 - t, y + 1, z, u0, v0);
                br.vertex(x + 1 - t, y, z, u0, v1);
                br.vertex(x + 1 - t, y, z + 1, u1, v1);
                br.vertex(x + 1 - t, y + 1, z + 1, u1, v0);
                break;
            case 1: // EAST
                br.vertex(x + t, y + 1, z + 1, u0, v0);
                br.vertex(x + t, y, z + 1, u0, v1);
                br.vertex(x + t, y, z, u1, v1);
                br.vertex(x + t, y + 1, z, u1, v0);
                break;
            case 2: // SOUTH
                br.vertex(x + 1, y + 1, z + 1 - t, u0, v0);
                br.vertex(x + 1, y, z + 1 - t, u0, v1);
                br.vertex(x, y, z + 1 - t, u1, v1);
                br.vertex(x, y + 1, z + 1 - t, u1, v0);
                break;
            default: // NORTH
                br.vertex(x, y + 1, z + t, u0, v0);
                br.vertex(x, y, z + t, u0, v1);
                br.vertex(x + 1, y, z + t, u1, v1);
                br.vertex(x + 1, y + 1, z + t, u1, v0);
                break;
        }
        br.end(vertices);
    }

    public override void update(World world, int x, int y, int z) {
        var block = world.getBlockRaw(x, y, z);
        var metadata = block.getMetadata();

        bool supported;
        if (isWall(metadata)) {
            // wall-attached: check backing block
            byte facing = (byte)(metadata & 0b11);
            RawDirection dir = facing switch {
                0 => RawDirection.WEST,
                1 => RawDirection.EAST,
                2 => RawDirection.SOUTH,
                3 => RawDirection.NORTH,
                _ => RawDirection.SOUTH
            };
            var o = Direction.getDirection(dir.opposite());
            supported = fullBlock[world.getBlock(x + o.X, y + o.Y, z + o.Z)];
        }
        else {
            // floor-placed: check below
            var below = world.getBlock(x, y - 1, z);
            supported = fullBlock[below] || below == id;
        }

        if (!supported) {
            drops.Clear();
            getDrop(drops, world, x, y, z, metadata, true);
            foreach (var drop in drops) {
                world.spawnBlockDrop(x, y, z, drop.getItem(), drop.quantity, drop.metadata);
            }
            world.setBlock(x, y, z, 0);
        }
    }
}
