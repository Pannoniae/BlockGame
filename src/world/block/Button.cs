using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;

namespace BlockGame.world.block;

public class Button(string name) : Block(name) {
    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        //base.render(br, x, y, z, vertices);
        x &= 15;
        y &= 15;
        z &= 15;
        var min = uvs?[0] ?? new UVPair(0, 0);
        if (br.forceTex.u >= 0 && br.forceTex.v >= 0) {
            min = br.forceTex;
        }

        var uv0 = UVPair.texCoords(min);
        var uv1 = UVPair.texCoords(min + 4/16f);

        // 4-pixel button at pixels 1-4 in a 16x16 tile
        var uvWidth = uv1.X - uv0.X;
        var u0 = uv0.X + uvWidth * (0f / 16f);
        var u1 = uv0.X + uvWidth * (4f / 16f);
        var v0 = uv0.Y;
        var v1 = uv1.Y;

        // 4x4 pixels centered = 6 pixels offset from each side
        const float offset = 6f / 16f;
        const float size = 4f / 16f;
        const float x0 = offset;
        const float x1 = offset + size;
        const float z0 = offset;
        const float z1 = offset + size;
        const float h = 2f / 16f;

        // north face (+Z)
        br.begin();
        br.vertex(x + x1, y + h, z + z1, u0, v0);
        br.vertex(x + x1, y, z + z1, u0, v1);
        br.vertex(x + x0, y, z + z1, u1, v1);
        br.vertex(x + x0, y + h, z + z1, u1, v0);
        br.end(vertices);

        // south face (-Z)
        br.begin();
        br.vertex(x + x0, y + h, z + z0, u0, v0);
        br.vertex(x + x0, y, z + z0, u0, v1);
        br.vertex(x + x1, y, z + z0, u1, v1);
        br.vertex(x + x1, y + h, z + z0, u1, v0);
        br.end(vertices);

        // east face (+X)
        br.begin();
        br.vertex(x + x1, y + h, z + z0, u0, v0);
        br.vertex(x + x1, y, z + z0, u0, v1);
        br.vertex(x + x1, y, z + z1, u1, v1);
        br.vertex(x + x1, y + h, z + z1, u1, v0);
        br.end(vertices);

        // west face (-X)
        br.begin();
        br.vertex(x + x0, y + h, z + z1, u0, v0);
        br.vertex(x + x0, y, z + z1, u0, v1);
        br.vertex(x + x0, y, z + z0, u1, v1);
        br.vertex(x + x0, y + h, z + z0, u1, v0);
        br.end(vertices);

        // top face (+Y)
        br.begin();
        br.vertex(x + x0, y + h, z + z1, u0, v0);
        br.vertex(x + x0, y + h, z + z0, u0, v1);
        br.vertex(x + x1, y + h, z + z0, u1, v1);
        br.vertex(x + x1, y + h, z + z1, u1, v0);
        br.end(vertices);

        // bottom face (-Y)
        br.begin();
        br.vertex(x + x0, y, z + z0, u0, v0);
        br.vertex(x + x0, y, z + z1, u0, v1);
        br.vertex(x + x1, y, z + z1, u1, v1);
        br.vertex(x + x1, y, z + z0, u1, v0);
        br.end(vertices);
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();

        // 4x4x2 pixels centered in 16x16x16 block
        const float offset = 6f / 16f;
        const float size = 4f / 16f;
        const float h = 2f / 16f;

        aabbs.Add(new AABB(
            x + offset, y, z + offset,
            x + offset + size, y + h, z + offset + size
        ));
    }

    public override UVPair getTexture(int faceIdx, int metadata) => uvs?[0] ?? new UVPair(0, 0);
}

