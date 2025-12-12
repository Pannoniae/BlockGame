using System.Numerics;
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

    /** Check if wire should connect to a neighbour in given direction */
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

    public override byte maxValidMetadata() {
        return 1;
    }

    /** Determine which texture to use based on connection pattern */
    private static int getTex(byte metadata, byte connectionMask) {
        var count = BitOperations.PopCount(connectionMask);

        int tex = count switch {
            0 => 0, // dot texture (uvs[0])
            _ => 1
        };
        return metadata != 0 ? tex + 3 : tex;
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        x &= 15;
        y &= 15;
        z &= 15;

        const float h = 2f / 16f;
        const float w = 2f / 16f;
        const float c = 0.5f;
        const float hw = w / 2f;

        byte metadata = br.getBlock().getMetadata();

        byte conn = mask(br, x, y, z);
        var m = getTex(metadata, conn);
        var tex = uvs[m];
        var uv0 = UVPair.texCoords(tex);
        var uv1 = UVPair.texCoords(tex + new UVPair(1, 1));

        var duv0 = UVPair.texCoords(tex + new UVPair(c - w));
        var duv1 = UVPair.texCoords(tex + new UVPair(c + w));

        var dw = UVPair.texCoords(tex + new UVPair(c - w - w, c - w));
        var de = UVPair.texCoords(tex + new UVPair(c + w + w, c + w));
        var dn = UVPair.texCoords(tex + new UVPair(c - w, c - w - w));
        var ds = UVPair.texCoords(tex + new UVPair(c + w, c + w + w));

        var dl0 = UVPair.texCoords(tex + new UVPair(c - hw, c - hw));
        var dl1 = UVPair.texCoords(tex + new UVPair(c + hw, c + hw));

        // core
        var count = BitOperations.PopCount(conn);

        if (count == 0) {
            br.quad(vertices, x, y, z,
                c - w, h, c + w,
                c - w, h, c - w,
                c + w, h, c - w,
                c + w, h, c + w,
                duv0.X, duv0.Y,
                duv1.X, duv1.Y, RawDirection.UP);

            br.quad(vertices, x, y, z,
                c + w, 0, c + w,
                c + w, 0, c - w,
                c - w, 0, c - w,
                c - w, 0, c + w,
                duv0.X, duv0.Y,
                duv1.X, duv1.Y, RawDirection.DOWN);

            br.quad(vertices, x, y, z,
                c - w, 0, c + w,
                c - w, 0, c - w,
                c - w, h, c - w,
                c - w, h, c + w,
                dw.X, dw.Y,
                duv0.X, duv1.Y, RawDirection.WEST);

            br.quad(vertices, x, y, z,
                c + w, h, c + w,
                c + w, h, c - w,
                c + w, 0, c - w,
                c + w, 0, c + w,
                duv1.X, duv0.Y,
                de.X, de.Y, RawDirection.EAST);

            br.quad(vertices, x, y, z,
                c - w, h, c - w,
                c - w, 0, c - w,
                c + w, 0, c - w,
                c + w, h, c - w,
                duv0.X, duv1.Y,
                ds.X, ds.Y, RawDirection.SOUTH);

            br.quad(vertices, x, y, z,
                c + w, h, c + w,
                c + w, 0, c + w,
                c - w, 0, c + w,
                c - w, h, c + w,
                duv1.X, duv0.Y,
                dn.X, dn.Y, RawDirection.NORTH);
        }
        else {
            // line

            // todo none of this is culled properly - needs to be fixed later

            bool west = (conn & 0b000001) != 0;
            bool east = (conn & 0b000010) != 0;
            bool south = (conn & 0b000100) != 0;
            bool north = (conn & 0b001000) != 0;
            bool up = (conn & 0b100000) != 0;

            float lx0 = west ? 0 : c - hw;
            float lx1 = east ? 1 : c + hw;
            float lz0 = south ? 0 : c - hw;
            float lz1 = north ? 1 : c + hw;
            float ly1 = up ? 1 : h;

            float lu0 = west ? uv0.X : dl0.X;
            float lu1 = east ? uv1.X : dl1.X;
            float lv0 = dl0.Y;
            float lv1 = dl1.Y;

            float nu0 = south ? uv0.X : dl0.X;
            float nu1 = north ? uv1.X : dl1.X;
            float nv0 = dl0.Y;
            float nv1 = dl1.Y;

            // todo fix ALL the UVs

            if (west || east) {
                // up
                br.applySimpleLighting(RawDirection.UP);
                br.begin();
                br.vertex(x + lx0, y + h, z + c + hw, lu0, lv0);
                br.vertex(x + lx0, y + h, z + c - hw, lu0, lv1);
                br.vertex(x + lx1, y + h, z + c - hw, lu1, lv1);
                br.vertex(x + lx1, y + h, z + c + hw, lu1, lv0);
                br.end(vertices);

                // down
                br.applySimpleLighting(RawDirection.DOWN);
                br.begin();
                br.vertex(x + lx1, y + 0, z + c + hw, lu1, lv0);
                br.vertex(x + lx1, y + 0, z + c - hw, lu1, lv1);
                br.vertex(x + lx0, y + 0, z + c - hw, lu0, lv1);
                br.vertex(x + lx0, y + 0, z + c + hw, lu0, lv0);
                br.end(vertices);

                // sides
                br.applySimpleLighting(RawDirection.WEST);
                br.begin();
                br.vertex(x + lx0, y + h, z + c + hw, tex + new UVPair(w, c - hw));
                br.vertex(x + lx0, y + 0, z + c + hw, tex + new UVPair(0, c - hw));
                br.vertex(x + lx0, y + 0, z + c - hw, tex + new UVPair(0, c + hw));
                br.vertex(x + lx0, y + h, z + c - hw, tex + new UVPair(w, c + hw));
                br.end(vertices);

                br.applySimpleLighting(RawDirection.EAST);
                br.begin();
                br.vertex(x + lx1, y + h, z + c - hw, tex + new UVPair(1 - w, c + hw));
                br.vertex(x + lx1, y + 0, z + c - hw, tex + new UVPair(1, c + hw));
                br.vertex(x + lx1, y + 0, z + c + hw, tex + new UVPair(1, c - hw));
                br.vertex(x + lx1, y + h, z + c + hw, tex + new UVPair(1 - w, c - hw));
                br.end(vertices);

                br.applySimpleLighting(RawDirection.SOUTH);
                br.begin();
                br.vertex(x + lx0, y + h, z + c - hw, lu0, lv0);
                br.vertex(x + lx0, y + 0, z + c - hw, lu0, lv1);
                br.vertex(x + lx1, y + 0, z + c - hw, lu1, lv1);
                br.vertex(x + lx1, y + h, z + c - hw, lu1, lv0);
                br.end(vertices);

                br.applySimpleLighting(RawDirection.NORTH);
                br.begin();
                br.vertex(x + lx1, y + h, z + c + hw, lu1, lv0);
                br.vertex(x + lx1, y + 0, z + c + hw, lu1, lv1);
                br.vertex(x + lx0, y + 0, z + c + hw, lu0, lv1);
                br.vertex(x + lx0, y + h, z + c + hw, lu0, lv0);
                br.end(vertices);
            }

            // N-S
            if (south || north) {
                // up
                br.applySimpleLighting(RawDirection.UP);
                br.begin();
                br.vertex(x + c + hw, y + h, z + lz1, nu0, nv0);
                br.vertex(x + c - hw, y + h, z + lz1, nu0, nv1);
                br.vertex(x + c - hw, y + h, z + lz0, nu1, nv1);
                br.vertex(x + c + hw, y + h, z + lz0, nu1, nv0);
                br.end(vertices);

                // down
                br.applySimpleLighting(RawDirection.DOWN);
                br.begin();
                br.vertex(x + c + hw, y + 0, z + lz0, nu1, nv0);
                br.vertex(x + c - hw, y + 0, z + lz0, nu1, nv1);
                br.vertex(x + c - hw, y + 0, z + lz1, nu0, nv1);
                br.vertex(x + c + hw, y + 0, z + lz1, nu0, nv0);
                br.end(vertices);

                // sides
                br.applySimpleLighting(RawDirection.WEST);
                br.begin();
                br.vertex(x + c - hw, y + h, z + lz1, nu0, nv0);
                br.vertex(x + c - hw, y + 0, z + lz1, nu0, nv1);
                br.vertex(x + c - hw, y + 0, z + lz0, nu1, nv1);
                br.vertex(x + c - hw, y + h, z + lz0, nu1, nv0);
                br.end(vertices);

                br.applySimpleLighting(RawDirection.EAST);
                br.begin();
                br.vertex(x + c + hw, y + h, z + lz0, nu1, nv0);
                br.vertex(x + c + hw, y + 0, z + lz0, nu1, nv1);
                br.vertex(x + c + hw, y + 0, z + lz1, nu0, nv1);
                br.vertex(x + c + hw, y + h, z + lz1, nu0, nv0);
                br.end(vertices);

                br.applySimpleLighting(RawDirection.SOUTH);
                br.begin();
                br.vertex(x + c - hw, y + h, z + lz0, tex + new UVPair(w, c - hw));
                br.vertex(x + c - hw, y + 0, z + lz0, tex + new UVPair(0, c - hw));
                br.vertex(x + c + hw, y + 0, z + lz0, tex + new UVPair(0, c + hw));
                br.vertex(x + c + hw, y + h, z + lz0, tex + new UVPair(w, c + hw));
                br.end(vertices);

                br.applySimpleLighting(RawDirection.NORTH);
                br.begin();
                br.vertex(x + c + hw, y + h, z + lz1, tex + new UVPair(1 - w, c + hw));
                br.vertex(x + c + hw, y + 0, z + lz1, tex + new UVPair(1, c + hw));
                br.vertex(x + c - hw, y + 0, z + lz1, tex + new UVPair(1, c - hw));
                br.vertex(x + c - hw, y + h, z + lz1, tex + new UVPair(1 - w, c - hw));
                br.end(vertices);
            }

            // do upwards stub if needed
            if (up) {
                br.applySimpleLighting(RawDirection.UP);
                br.begin();
                br.vertex(x + c - hw, y + 1 + h, z + c + hw, dl0.X, dl0.Y);
                br.vertex(x + c - hw, y + 1 + h, z + c - hw, dl0.X, dl1.Y);
                br.vertex(x + c + hw, y + 1 + h, z + c - hw, dl1.X, dl1.Y);
                br.vertex(x + c + hw, y + 1 + h, z + c + hw, dl1.X, dl0.Y);
                br.end(vertices);

                br.applySimpleLighting(RawDirection.DOWN);
                br.begin();
                br.vertex(x + c + hw, y + h, z + c + hw, dl1.X, dl0.Y);
                br.vertex(x + c + hw, y + h, z + c - hw, dl1.X, dl1.Y);
                br.vertex(x + c - hw, y + h, z + c - hw, dl0.X, dl1.Y);
                br.vertex(x + c - hw, y + h, z + c + hw, dl0.X, dl0.Y);
                br.end(vertices);

                br.applySimpleLighting(RawDirection.WEST);
                br.begin();
                br.vertex(x + c - hw, y + 1 + h, z + c + hw, uv0.X, dl1.Y);
                br.vertex(x + c - hw, y + h, z + c + hw, uv1.X, dl1.Y);
                br.vertex(x + c - hw, y + h, z + c - hw, uv1.X, dl0.Y);
                br.vertex(x + c - hw, y + 1 + h, z + c - hw, uv0.X, dl0.Y);
                br.end(vertices);

                br.applySimpleLighting(RawDirection.EAST);
                br.begin();
                br.vertex(x + c + hw, y + 1 + h, z + c - hw, uv0.X, dl1.Y);
                br.vertex(x + c + hw, y + h, z + c - hw, uv1.X, dl1.Y);
                br.vertex(x + c + hw, y + h, z + c + hw, uv1.X, dl0.Y);
                br.vertex(x + c + hw, y + 1 + h, z + c + hw, uv0.X, dl0.Y);
                br.end(vertices);

                br.applySimpleLighting(RawDirection.SOUTH);
                br.begin();
                br.vertex(x + c - hw, y + 1 + h, z + c - hw, uv0.X, dl1.Y);
                br.vertex(x + c - hw, y + h, z + c - hw, uv1.X, dl1.Y);
                br.vertex(x + c + hw, y + h, z + c - hw, uv1.X, dl0.Y);
                br.vertex(x + c + hw, y + 1 + h, z + c - hw, uv0.X, dl0.Y);
                br.end(vertices);

                br.applySimpleLighting(RawDirection.NORTH);
                br.begin();
                br.vertex(x + c + hw, y + 1 + h, z + c + hw, uv0.X, dl1.Y);
                br.vertex(x + c + hw, y + h, z + c + hw, uv1.X, dl1.Y);
                br.vertex(x + c - hw, y + h, z + c + hw, uv1.X, dl0.Y);
                br.vertex(x + c - hw, y + 1 + h, z + c + hw, uv0.X, dl0.Y);
                br.end(vertices);
            }
        }
    }
}