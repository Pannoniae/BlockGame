using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.entity;
using Molten;

namespace BlockGame.world.block;

public class Farmland : Block {
    public Farmland(string name) : base(name) {

    }

    protected override void onRegister(int id) {
        // custom AABB
        customAABB[id] = true;
        setCustomRender();
        partialBlock();
        tick(); // for hydration
    }

    public override void onBreak(World world, int x, int y, int z, byte metadata) {
        // break crop above if present
        var above = world.getBlock(x, y + 1, z);
        if (above != 0 && get(above) is Crop) {
            world.setBlock(x, y + 1, z, AIR.id);
        }
    }

    public override void randomUpdate(World world, int x, int y, int z) {
        byte currentMeta = world.getBlockMetadata(x, y, z);
        bool hasWater = isNearWater(world, x, y, z);

        if (hasWater && currentMeta == 0) {
            // hydrate
            world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(1));
        } else if (!hasWater && currentMeta > 0) {
            // dry out
            world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(0));
        }
    }

    public override void onStepped(World world, int x, int y, int z, Entity entity) {
        // only if mob! we don't want items and shit to trample
        if (entity is not Mob) {
            return;
        }

        // trample (unless sneaking)
        if (!entity.sneaking) {
            if (world.random.NextDouble() < 1 / 16f) {
                world.setBlock(x, y, z, DIRT.id);
            }
        }
    }

    /** check if water is within 4 blocks horizontally */
    private static bool isNearWater(World world, int x, int y, int z) {
        for (int dx = -4; dx <= 4; dx++) {
            for (int dz = -4; dz <= 4; dz++) {
                for (int dy = -1; dy <= 1; dy++) {
                    var block = world.getBlock(x + dx, y + dy, z + dz);
                    if (block == WATER.id) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        aabbs.Add(new AABB(x + 0, y + 0, z + 0, x + 1, y + 15 / 16f, z + 1)); // 15/16 height
    }

    public override UVPair getTexture(int faceIdx, int metadata) {
        var top = metadata > 0 ? uvs[6] : uvs[5];
        return faceIdx switch {
            5 => top,    // top
            _ => uvs[faceIdx]
        };
    }

    public override byte maxValidMetadata() {
        return 1;
    }

    public override void render(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices) {
        x &= 15;
        y &= 15;
        z &= 15;

        var metadata = br.getBlock().getMetadata();
        var topTex = metadata > 0 ? uvs[6] : uvs[5];
        var sideTex = uvs[0];

        var sideUV0 = UVPair.texCoords(sideTex);
        var sideUV1 = UVPair.texCoords(sideTex + 1);
        var topUV0 = UVPair.texCoords(topTex);
        var topUV1 = UVPair.texCoords(topTex + 1);

        float su0 = sideUV0.X, sv0 = sideUV0.Y, su1 = sideUV1.X, sv1 = sideUV1.Y;
        float tu0 = topUV0.X, tv0 = topUV0.Y, tu1 = topUV1.X, tv1 = topUV1.Y;

        const float h = 15f / 16f;
        float ue = su1 - su0;
        float ve = sv1 - sv0;

        // adjust side v coords for 15/16 height
        float sv0adj = sv0 + ve / 16f;

        // WEST (-X)
        if (!br.shouldCullFace(RawDirection.WEST)) {
            br.applyFaceLighting(RawDirection.WEST);
            br.begin();
            br.vertex(x, y + h, z + 1, su0, sv0adj);
            br.vertex(x, y, z + 1, su0, sv1);
            br.vertex(x, y, z, su1, sv1);
            br.vertex(x, y + h, z, su1, sv0adj);
            br.end(vertices);
        }

        // EAST (+X)
        if (!br.shouldCullFace(RawDirection.EAST)) {
            br.applyFaceLighting(RawDirection.EAST);
            br.begin();
            br.vertex(x + 1, y + h, z, su0, sv0adj);
            br.vertex(x + 1, y, z, su0, sv1);
            br.vertex(x + 1, y, z + 1, su1, sv1);
            br.vertex(x + 1, y + h, z + 1, su1, sv0adj);
            br.end(vertices);
        }

        // SOUTH (-Z)
        if (!br.shouldCullFace(RawDirection.SOUTH)) {
            br.applyFaceLighting(RawDirection.SOUTH);
            br.begin();
            br.vertex(x, y + h, z, su0, sv0adj);
            br.vertex(x, y, z, su0, sv1);
            br.vertex(x + 1, y, z, su1, sv1);
            br.vertex(x + 1, y + h, z, su1, sv0adj);
            br.end(vertices);
        }

        // NORTH (+Z)
        if (!br.shouldCullFace(RawDirection.NORTH)) {
            br.applyFaceLighting(RawDirection.NORTH);
            br.begin();
            br.vertex(x + 1, y + h, z + 1, su0, sv0adj);
            br.vertex(x + 1, y, z + 1, su0, sv1);
            br.vertex(x, y, z + 1, su1, sv1);
            br.vertex(x, y + h, z + 1, su1, sv0adj);
            br.end(vertices);
        }

        // DOWN (-Y)
        if (!br.shouldCullFace(RawDirection.DOWN)) {
            br.applyFaceLighting(RawDirection.DOWN);
            br.begin();
            br.vertex(x + 1, y, z + 1, su0, sv0);
            br.vertex(x + 1, y, z, su0, sv1);
            br.vertex(x, y, z, su1, sv1);
            br.vertex(x, y, z + 1, su1, sv0);
            br.end(vertices);
        }

        // UP (+Y) - uses farmland texture
        if (!br.shouldCullFace(RawDirection.UP)) {
            br.applyFaceLighting(RawDirection.UP);
            br.begin();
            br.vertex(x, y + h, z + 1, tu0, tv0);
            br.vertex(x, y + h, z, tu0, tv1);
            br.vertex(x + 1, y + h, z, tu1, tv1);
            br.vertex(x + 1, y + h, z + 1, tu1, tv0);
            br.end(vertices);
        }
    }
}