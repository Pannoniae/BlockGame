using System.Numerics;
using System.Runtime.Intrinsics.X86;
using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;

namespace BlockGame.world.block;

/** Wire block for circuit system.
 * Connects in all 6 directions to other wires, horizontally only to circuit blocks.
 * Metadata bit 0 stores powered state (0=OFF, 1=ON).
 * Rendering is dynamic - connections computed at render time based on neighbors.
 */
public class Wire : Block {
    public Wire(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        renderType[id] = RenderType.CUSTOM;
    }

    /** Check if wire should connect to a neighbor in given direction */
    private static bool connect(BlockRenderer br, int x, int y, int z, RawDirection dir) {
        var offset = Direction.getDirection(dir);

        var n = br.getBlockCached(offset.X, offset.Y, offset.Z);
        var bl = n.getID();

        // connect to other wires in all 6 directions
        // connect to circuit blocks horizontally only (no UP/DOWN)
        return bl == WIRE.id || dir != RawDirection.UP && dir != RawDirection.DOWN && circuit[bl];
    }

    /** Build 6-bit mask of which directions have connections */
    private static byte mask(BlockRenderer br, int x, int y, int z) {
        byte mask = 0;
        if (connect(br, x, y, z, RawDirection.WEST)) {
            mask |= 0b000001;
        }

        if (connect(br, x, y, z, RawDirection.EAST)) {
            mask |= 0b000010;
        }

        if (connect(br, x, y, z, RawDirection.SOUTH)) {
            mask |= 0b000100;
        }

        if (connect(br, x, y, z, RawDirection.NORTH)) {
            mask |= 0b001000;
        }

        if (connect(br, x, y, z, RawDirection.DOWN)) {
            mask |= 0b010000;
        }

        if (connect(br, x, y, z, RawDirection.UP)) {
            mask |= 0b100000;
        }

        return mask;
    }

    /** Determine which texture to use based on connection pattern */
    private static int getTex(byte connectionMask) {
        var count = BitOperations.PopCount(connectionMask);

        switch (count) {
            case 0:
                return 0; // dot texture (uvs[0])

            case 1:
                return 1; // single stub (uvs[1])

            case 2:
                bool isLine = (connectionMask == 0b000011) || // WEST-EAST
                              (connectionMask == 0b001100) || // SOUTH-NORTH
                              (connectionMask == 0b110000); // DOWN-UP
                return isLine ? 1 : 2; // line or L-shape

            default:
                return 2;
        }
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        x &= 15;
        y &= 15;
        z &= 15;

        byte conn = mask(br, x, y, z);
        var tex = uvs[getTex(conn)];
        var uv0 = UVPair.texCoords(tex);
        var uv1 = UVPair.texCoords(tex + new UVPair(1, 1));

        const float h = 2f / 16f;
        const float w = 2f / 16f;
        const float c = 0.5f;
        const float hw = w / 2f;

        // core
        br.boxProportional(vertices, x, y, z, c - hw, 0, c - hw, c + hw, h, c + hw, uv0.X, uv0.Y, uv1.X, uv1.Y);

        // stubs
        if ((conn & 0b000001) != 0) {
            br.boxProportional(vertices, x, y, z, 0, 0, c - hw, c - hw, h, c + hw, uv0.X, uv0.Y, uv1.X, uv1.Y);
        }

        if ((conn & 0b000010) != 0) {
            br.boxProportional(vertices, x, y, z, c + hw, 0, c - hw, 1, h, c + hw, uv0.X, uv0.Y, uv1.X, uv1.Y);
        }

        if ((conn & 0b000100) != 0) {
            br.boxProportional(vertices, x, y, z, c - hw, 0, 0, c + hw, h, c - hw, uv0.X, uv0.Y, uv1.X, uv1.Y);
        }

        if ((conn & 0b001000) != 0) {
            br.boxProportional(vertices, x, y, z, c - hw, 0, c + hw, c + hw, h, 1, uv0.X, uv0.Y, uv1.X, uv1.Y);
        }

        if ((conn & 0b100000) != 0) {
            br.boxProportional(vertices, x, y, z, c - hw, h, c - hw, c + hw, 1, c + hw, uv0.X, uv0.Y, uv1.X, uv1.Y);
        }
    }
}