using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using BlockGame.GL.vertexformats;
using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.render;

public partial class BlockRenderer {
    /**
     * Cube renderer with dynamic per-face textures based on metadata.
     */
    public void renderCubeDynamic(Block bl, int x, int y, int z, List<BlockVertexPacked> vertices, byte metadata) {
        // render each face with its own texture
        for (int faceIdx = 0; faceIdx < 6; faceIdx++) {
            var tex = bl.getTexture(faceIdx, metadata);
            var texm = tex + 1;

            if (forceTex.u >= 0 && forceTex.v >= 0) {
                tex = forceTex;
                texm = new UVPair(forceTex.u + 1, forceTex.v + 1);
            }

            var uvd = UVPair.texCoords(tex);
            var uvdm = UVPair.texCoords(texm);

            // render the specific face
            renderCubeFace(x, y, z, vertices, (RawDirection)faceIdx, uvd.X, uvd.Y, uvdm.X, uvdm.Y);
        }
    }

    /**
     * Cube renderer with dynamic per-face textures based on metadata. Also applies grass tinting.
     */
    public void renderGrass(Block bl, int x, int y, int z, List<BlockVertexPacked> vertices, byte metadata) {
        // render each face with its own texture
        for (int faceIdx = 0; faceIdx < 6; faceIdx++) {
            var tex = bl.getTexture(faceIdx, metadata);
            var texm = tex + 1;

            if (forceTex.u >= 0 && forceTex.v >= 0) {
                tex = forceTex;
                texm = new UVPair(forceTex.u + 1, forceTex.v + 1);
            }

            var uvd = UVPair.texCoords(tex);
            var uvdm = UVPair.texCoords(texm);
        }
    }

    public void renderCrop(Block bl, int x, int y, int z, List<BlockVertexPacked> vertices, byte metadata) {
        var tex = bl.getTexture(0, metadata);
        var texm = tex + 1;

        if (forceTex.u >= 0 && forceTex.v >= 0) {
            tex = forceTex;
            texm = new UVPair(forceTex.u + 1, forceTex.v + 1);
        }

        var uvd = UVPair.texCoords(tex);
        var uvdm = UVPair.texCoords(texm);

        // 2x2 planes, "hash" pattern
        const float d = 0.25f; // offset from edge

        // north-south plane at x=0.25 (west side)
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + d, y + 1f, z + 0f, uvd.X, uvd.Y);
        vertex(x + d, y + 0f, z + 0f, uvd.X, uvdm.Y);
        vertex(x + d, y + 0f, z + 1f, uvdm.X, uvdm.Y);
        vertex(x + d, y + 1f, z + 1f, uvdm.X, uvd.Y);
        endTwo(vertices);

        // north-south plane at x=0.75 (east side)
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 1f - d, y + 1f, z + 0f, uvd.X, uvd.Y);
        vertex(x + 1f - d, y + 0f, z + 0f, uvd.X, uvdm.Y);
        vertex(x + 1f - d, y + 0f, z + 1f, uvdm.X, uvdm.Y);
        vertex(x + 1f - d, y + 1f, z + 1f, uvdm.X, uvd.Y);
        endTwo(vertices);

        // east-west plane at z=0.25 (south side)
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 0f, y + 1f, z + d, uvd.X, uvd.Y);
        vertex(x + 0f, y + 0f, z + d, uvd.X, uvdm.Y);
        vertex(x + 1f, y + 0f, z + d, uvdm.X, uvdm.Y);
        vertex(x + 1f, y + 1f, z + d, uvdm.X, uvd.Y);
        endTwo(vertices);

        // east-west plane at z=0.75 (north side)
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 0f, y + 1f, z + 1f - d, uvd.X, uvd.Y);
        vertex(x + 0f, y + 0f, z + 1f - d, uvd.X, uvdm.Y);
        vertex(x + 1f, y + 0f, z + 1f - d, uvdm.X, uvdm.Y);
        vertex(x + 1f, y + 1f, z + 1f - d, uvdm.X, uvd.Y);
        endTwo(vertices);
    }

    /**
     * Cross renderer for plants and similar blocks.
     */
    public void renderCross(Block bl, int x, int y, int z, List<BlockVertexPacked> vertices, byte metadata) {
        var tex = bl.getTexture(0, metadata);
        var texm = tex + 1;

        if (forceTex.u >= 0 && forceTex.v >= 0) {
            tex = forceTex;
            texm = new UVPair(forceTex.u + 1, forceTex.v + 1);
        }

        var uvd = UVPair.texCoords(tex);
        var uvdm = UVPair.texCoords(texm);

        // first
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 0f, y + 1f, z + 0f, uvd.X, uvd.Y);
        vertex(x + 0f, y + 0f, z + 0f, uvd.X, uvdm.Y);
        vertex(x + 1f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
        vertex(x + 1f, y + 1f, z + 1f, uvdm.X, uvd.Y);
        end(vertices);

        // second
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 1f, y + 1f, z + 0f, uvd.X, uvd.Y);
        vertex(x + 1f, y + 0f, z + 0f, uvd.X, uvdm.Y);
        vertex(x + 0f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
        vertex(x + 0f, y + 1f, z + 1f, uvdm.X, uvd.Y);
        end(vertices);

        // do the backsides
        // first backside
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 1f, y + 1f, z + 1f, uvd.X, uvd.Y);
        vertex(x + 1f, y + 0f, z + 1f, uvd.X, uvdm.Y);
        vertex(x + 0f, y + 0f, z + 0f, uvdm.X, uvdm.Y);
        vertex(x + 0f, y + 1f, z + 0f, uvdm.X, uvd.Y);
        end(vertices);

        // second backside
        applySimpleLighting(RawDirection.NONE);
        begin();
        vertex(x + 0f, y + 1f, z + 1f, uvd.X, uvd.Y);
        vertex(x + 0f, y + 0f, z + 1f, uvd.X, uvdm.Y);
        vertex(x + 1f, y + 0f, z + 0f, uvdm.X, uvdm.Y);
        vertex(x + 1f, y + 1f, z + 0f, uvdm.X, uvd.Y);
        end(vertices);
    }

    /** 4-plane radial pattern, tapered inward at top */
    public void renderFire(Block bl, int x, int y, int z, List<BlockVertexPacked> vertices, byte metadata) {
        var tex = bl.getTexture(0, metadata);
        var texm = tex + 1;

        if (forceTex.u >= 0 && forceTex.v >= 0) {
            tex = forceTex;
            texm = new UVPair(forceTex.u + 1, forceTex.v + 1);
        }

        float inset = 0 / 16f;
        const float topInset = 4 / 16f;

        var uvd = UVPair.texCoords(tex);
        var uvdm = UVPair.texCoords(texm);

        applySimpleLightingNoDir();

        // check if block below can support fire (is solid)
        uint below = getBlockCached(0, -1, 0);
        bool b = Block.collision[below.getID()];

        if (b) {
            begin();
            vertex(x + 0f + inset, y + 1f, z + 1f - inset, uvd.X, uvd.Y);
            vertex(x + 0f, y + 0f, z + 0f, uvd.X, uvdm.Y);
            vertex(x + 1f, y + 0f, z + 0f, uvdm.X, uvdm.Y);
            vertex(x + 1f - inset, y + 1f, z + 1f - inset, uvdm.X, uvd.Y);
            endTwo(vertices);

            begin();
            vertex(x + 0f + inset, y + 1f, z + 0f + inset, uvd.X, uvd.Y);
            vertex(x + 0f, y + 0f, z + 1f, uvd.X, uvdm.Y);
            vertex(x + 1f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 1f - inset, y + 1f, z + 0f + inset, uvdm.X, uvd.Y);
            endTwo(vertices);

            begin();
            vertex(x + 1f - inset, y + 1f, z + 0f + inset, uvd.X, uvd.Y);
            vertex(x + 0f, y + 0f, z + 0f, uvd.X, uvdm.Y);
            vertex(x + 0f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 1f - inset, y + 1f, z + 1f - inset, uvdm.X, uvd.Y);
            endTwo(vertices);

            begin();
            vertex(x + 0f + inset, y + 1f, z + 0f + inset, uvd.X, uvd.Y);
            vertex(x + 1f, y + 0f, z + 0f, uvd.X, uvdm.Y);
            vertex(x + 1f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 0f + inset, y + 1f, z + 1f - inset, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        if (!b) {
            inset = 1 / 16f;
        }

        // north (+Z)
        uint block = getBlockCached(0, 0, 1);
        if (b || Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x, y + 1f, z + 1f - inset, uvd.X, uvd.Y);
            vertex(x, y + 0f, z + 1f, uvd.X, uvdm.Y);
            vertex(x + 1, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 1, y + 1f, z + 1f - inset, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // south (-Z)
        block = getBlockCached(0, 0, -1);
        if (b || Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x + 1, y + 1f, z + 0f + inset, uvd.X, uvd.Y);
            vertex(x + 1, y + 0f, z + 0f, uvd.X, uvdm.Y);
            vertex(x, y + 0f, z + 0f, uvdm.X, uvdm.Y);
            vertex(x, y + 1f, z + 0f + inset, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // west (-X)
        block = getBlockCached(-1, 0, 0);
        if (b || Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x + 0f + inset, y + 1f, z, uvd.X, uvd.Y);
            vertex(x + 0f, y + 0f, z, uvd.X, uvdm.Y);
            vertex(x + 0f, y + 0f, z + 1f, uvdm.X, uvdm.Y);
            vertex(x + 0f + inset, y + 1f, z + 1f, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // east (+X)
        block = getBlockCached(1, 0, 0);
        if (b || Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x + 1f - inset, y + 1f, z + 1f, uvd.X, uvd.Y);
            vertex(x + 1f, y + 0f, z + 1f, uvd.X, uvdm.Y);
            vertex(x + 1f, y + 0f, z + 0f, uvdm.X, uvdm.Y);
            vertex(x + 1f - inset, y + 1f, z + 0f, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // up (+Y)
        block = getBlockCached(0, 1, 0);
        if (Block.flammable[block.getID()] > 0) {
            begin();
            vertex(x, y + 1f - inset, z, uvd.X, uvd.Y);
            vertex(x, y + 1f - inset, z + 1, uvd.X, uvdm.Y);
            vertex(x + 1, y + 1f - inset, z + 1, uvdm.X, uvdm.Y);
            vertex(x + 1, y + 1f - inset, z, uvdm.X, uvd.Y);
            endTwo(vertices);
        }

        // down (-Y)
        if (Block.flammable[below.getID()] > 0) {
            begin();
            vertex(x, y + inset, z, uvd.X, uvd.Y);
            vertex(x + 1, y + inset, z, uvdm.X, uvd.Y);
            vertex(x + 1, y + inset, z + 1, uvdm.X, uvdm.Y);
            vertex(x, y + inset, z + 1, uvd.X, uvdm.Y);
            endTwo(vertices);
        }
    }

    public static void renderTorchCube(BlockRenderer br, int x, int y, int z, List<BlockVertexPacked> vertices,
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
                    v0 = new Vector3(x + x0, y + y1, z + z1);
                    v1 = new Vector3(x + x0, y + y0, z + z1);
                    v2 = new Vector3(x + x0, y + y0, z + z0);
                    v3 = new Vector3(x + x0, y + y1, z + z0);
                    u0_ = su0;
                    v0_ = sv0;
                    u1_ = su1;
                    v1_ = sv1;

                    break;
                case RawDirection.EAST:
                    v0 = new Vector3(x + x1, y + y1, z + z0);
                    v1 = new Vector3(x + x1, y + y0, z + z0);
                    v2 = new Vector3(x + x1, y + y0, z + z1);
                    v3 = new Vector3(x + x1, y + y1, z + z1);
                    u0_ = su0;
                    v0_ = sv0;
                    u1_ = su1;
                    v1_ = sv1;
                    break;
                case RawDirection.SOUTH:
                    v0 = new Vector3(x + x0, y + y1, z + z0);
                    v1 = new Vector3(x + x0, y + y0, z + z0);
                    v2 = new Vector3(x + x1, y + y0, z + z0);
                    v3 = new Vector3(x + x1, y + y1, z + z0);
                    u0_ = su0;
                    v0_ = sv0;
                    u1_ = su1;
                    v1_ = sv1;
                    break;
                case RawDirection.NORTH:
                    v0 = new Vector3(x + x1, y + y1, z + z1);
                    v1 = new Vector3(x + x1, y + y0, z + z1);
                    v2 = new Vector3(x + x0, y + y0, z + z1);
                    v3 = new Vector3(x + x0, y + y1, z + z1);
                    u0_ = su0;
                    v0_ = sv0;
                    u1_ = su1;
                    v1_ = sv1;
                    break;
                case RawDirection.DOWN:
                    v0 = new Vector3(x + x1, y + y0, z + z1);
                    v1 = new Vector3(x + x1, y + y0, z + z0);
                    v2 = new Vector3(x + x0, y + y0, z + z0);
                    v3 = new Vector3(x + x0, y + y0, z + z1);
                    u0_ = bu0;
                    v0_ = bv0;
                    u1_ = bu1;
                    v1_ = bv1;
                    break;
                case RawDirection.UP:
                    v0 = new Vector3(x + x0, y + y1, z + z1);
                    v1 = new Vector3(x + x0, y + y1, z + z0);
                    v2 = new Vector3(x + x1, y + y1, z + z0);
                    v3 = new Vector3(x + x1, y + y1, z + z1);
                    u0_ = tu0;
                    v0_ = tv0;
                    u1_ = tu1;
                    v1_ = tv1;
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

    /**
     * Render a standing sign at the given position.
     */
    public void renderSign(int x, int y, int z, List<BlockVertexPacked> vertices, float u0, float v0, float u1, float v1, byte rot) {
        const float width = 1f;
        const float depth = 1f / 16f;
        const float y0 = 6 / 16f;
        const float y1 = 1f;

        const float cx = 0.5f;
        const float cz = 0.5f;

        float a = -rot * (float.Pi / 8.0f); // 22.5deg
        float ca = float.Cos(a);
        float sa = float.Sin(a);

        // ext
        const float hw = width / 2f;
        const float hd = depth / 2f;

        // corners
        const float x0 = cx - hw;
        const float x1 = cx + hw;
        const float z0 = cz - hd;
        const float z1 = cz + hd;

        rp(x0, z0, out float rx0z0, out float rz0z0);
        rp(x1, z0, out float rx1z0, out float rz1z0);
        rp(x0, z1, out float rx0z1, out float rz0z1);
        rp(x1, z1, out float rx1z1, out float rz1z1);

        // UVs
        const float height = y1 - y0;
        float du = u1 - u0;
        float dv = v1 - v0;

        // I fucked with the UVs yes. They very much don't match up. But it works for the wood. If you unwood this, adjust (or not, I don't think anyone will care)
        float fv1 = v0 + dv * height;
        float fu0 = u0 + du * depth;
        float eu1 = u1 - du * depth;
        float ev0 = v0 + dv * height;
        float ev1 = v1 - dv * depth;
        float dv0 = fv1;
        float dv1 = fv1 + dv * depth;


        // we cheat and we do it blatantly
        applySimpleLighting(RawDirection.EAST);

        // front
        begin();
        vertex(x + rx0z0, y + y1, z + rz0z0, u0, v0);
        vertex(x + rx0z0, y + y0, z + rz0z0, u0, fv1);
        vertex(x + rx1z0, y + y0, z + rz1z0, u1, fv1);
        vertex(x + rx1z0, y + y1, z + rz1z0, u1, v0);
        end(vertices);

        applySimpleLighting(RawDirection.DOWN);

        // back
        begin();
        vertex(x + rx1z1, y + y1, z + rz1z1, u0, v0);
        vertex(x + rx1z1, y + y0, z + rz1z1, u0, fv1);
        vertex(x + rx0z1, y + y0, z + rz0z1, u1, fv1);
        vertex(x + rx0z1, y + y1, z + rz0z1, u1, v0);
        end(vertices);

        applySimpleLighting(RawDirection.NORTH);

        // left
        begin();
        vertex(x + rx0z1, y + y1, z + rz0z1, u0, v0);
        vertex(x + rx0z1, y + y0, z + rz0z1, u0, ev0);
        vertex(x + rx0z0, y + y0, z + rz0z0, fu0, ev0);
        vertex(x + rx0z0, y + y1, z + rz0z0, fu0, v0);
        end(vertices);

        applySimpleLighting(RawDirection.SOUTH);

        // right
        begin();
        vertex(x + rx1z0, y + y1, z + rz1z0, eu1, v0);
        vertex(x + rx1z0, y + y0, z + rz1z0, eu1, ev0);
        vertex(x + rx1z1, y + y0, z + rz1z1, u1, ev0);
        vertex(x + rx1z1, y + y1, z + rz1z1, u1, v0);
        end(vertices);

        applySimpleLighting(RawDirection.UP);

        // top
        begin();
        vertex(x + rx0z0, y + y1, z + rz0z0, u0, dv0);
        vertex(x + rx1z0, y + y1, z + rz1z0, u1, dv0);
        vertex(x + rx1z1, y + y1, z + rz1z1, u1, dv1);
        vertex(x + rx0z1, y + y1, z + rz0z1, u0, dv1);
        end(vertices);

        applySimpleLighting(RawDirection.DOWN);

        // bottom
        begin();
        vertex(x + rx0z1, y + y0, z + rz0z1, u0, ev1);
        vertex(x + rx1z1, y + y0, z + rz1z1, u1, ev1);
        vertex(x + rx1z0, y + y0, z + rz1z0, u1, v1);
        vertex(x + rx0z0, y + y0, z + rz0z0, u0, v1);
        end(vertices);
        return;

        void rp(float px, float pz, out float rx, out float rz) {
            float dx = px - cx;
            float dz = pz - cz;
            rx = dx * ca - dz * sa + cx;
            rz = dx * sa + dz * ca + cz;
        }
    }

    /**
     * Render a single cube face with culling and lighting.
     */
    private void renderCubeFace(int x, int y, int z, List<BlockVertexPacked> vertices,
        RawDirection dir, float u0, float v0, float u1, float v1) {
        var vec = Direction.getDirection(dir);
        var nb = getBlockCached(vec.X, vec.Y, vec.Z).getID();

        // check if we should render based on neighbour
        if (Block.fullBlock[nb]) {
            return;
        }

        applyFaceLighting(dir);
        begin();

        switch (dir) {
            case RawDirection.WEST: // 0
                vertex(x + 0, y + 1, z + 1, u0, v0);
                vertex(x + 0, y + 0, z + 1, u0, v1);
                vertex(x + 0, y + 0, z + 0, u1, v1);
                vertex(x + 0, y + 1, z + 0, u1, v0);
                break;
            case RawDirection.EAST: // 1
                vertex(x + 1, y + 1, z + 0, u0, v0);
                vertex(x + 1, y + 0, z + 0, u0, v1);
                vertex(x + 1, y + 0, z + 1, u1, v1);
                vertex(x + 1, y + 1, z + 1, u1, v0);
                break;
            case RawDirection.SOUTH: // 2
                vertex(x + 0, y + 1, z + 0, u0, v0);
                vertex(x + 0, y + 0, z + 0, u0, v1);
                vertex(x + 1, y + 0, z + 0, u1, v1);
                vertex(x + 1, y + 1, z + 0, u1, v0);
                break;
            case RawDirection.NORTH: // 3
                vertex(x + 1, y + 1, z + 1, u0, v0);
                vertex(x + 1, y + 0, z + 1, u0, v1);
                vertex(x + 0, y + 0, z + 1, u1, v1);
                vertex(x + 0, y + 1, z + 1, u1, v0);
                break;
            case RawDirection.DOWN: // 4
                vertex(x + 1, y + 0, z + 1, u0, v0);
                vertex(x + 1, y + 0, z + 0, u0, v1);
                vertex(x + 0, y + 0, z + 0, u1, v1);
                vertex(x + 0, y + 0, z + 1, u1, v0);
                break;
            case RawDirection.UP: // 5
                vertex(x + 0, y + 1, z + 1, u0, v0);
                vertex(x + 0, y + 1, z + 0, u0, v1);
                vertex(x + 1, y + 1, z + 0, u1, v1);
                vertex(x + 1, y + 1, z + 1, u1, v0);
                break;
        }

        end(vertices);
    }

    /**
     * All-in-one chungus cube renderer. Claude helped me a bit with it so it's not very great but hey, if you don't need anything fancy, it's alright.
     * (Assumptions: the lighting is what you'd expect from the faces, you need a properly culled cube, you don't need extra tint, your texture maps 1:1 with world pixels)
     */
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public void renderCube(int x, int y, int z, List<BlockVertexPacked> vertices,
        float x0, float y0, float z0, float x1, float y1, float z1,
        float u0, float v0, float u1, float v1) {
        var ue = u1 - u0;
        var ve = v1 - v0;

        // WEST face
        var westUMin = u0 + ue * z0;
        var westUMax = u0 + ue * z1;
        var westVMin = v0 + ve * (1f - y1);
        var westVMax = v0 + ve * (1f - y0);
        var nb = getBlockCached(-1, 0, 0).getID();

        bool edge = x0 == 0f;
        bool render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.WEST);
            begin();
            vertex(x + x0, y + y1, z + z1, westUMin, westVMin);
            vertex(x + x0, y + y0, z + z1, westUMin, westVMax);
            vertex(x + x0, y + y0, z + z0, westUMax, westVMax);
            vertex(x + x0, y + y1, z + z0, westUMax, westVMin);
            end(vertices);
        }

        // EAST face
        var eastUMin = u0 + ue * z0;
        var eastUMax = u0 + ue * z1;
        var eastVMin = v0 + ve * (1f - y1);
        var eastVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(1, 0, 0).getID();

        edge = x1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.EAST);
            begin();
            vertex(x + x1, y + y1, z + z0, eastUMin, eastVMin);
            vertex(x + x1, y + y0, z + z0, eastUMin, eastVMax);
            vertex(x + x1, y + y0, z + z1, eastUMax, eastVMax);
            vertex(x + x1, y + y1, z + z1, eastUMax, eastVMin);
            end(vertices);
        }

        // SOUTH face
        var southUMin = u0 + ue * x0;
        var southUMax = u0 + ue * x1;
        var southVMin = v0 + ve * (1f - y1);
        var southVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(0, 0, -1).getID();

        edge = z0 == 0f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.SOUTH);
            begin();
            vertex(x + x0, y + y1, z + z0, southUMin, southVMin);
            vertex(x + x0, y + y0, z + z0, southUMin, southVMax);
            vertex(x + x1, y + y0, z + z0, southUMax, southVMax);
            vertex(x + x1, y + y1, z + z0, southUMax, southVMin);
            end(vertices);
        }

        // NORTH face
        var northUMin = u0 + ue * x0;
        var northUMax = u0 + ue * x1;
        var northVMin = v0 + ve * (1f - y1);
        var northVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(0, 0, 1).getID();

        edge = z1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.NORTH);
            begin();
            vertex(x + x1, y + y1, z + z1, northUMin, northVMin);
            vertex(x + x1, y + y0, z + z1, northUMin, northVMax);
            vertex(x + x0, y + y0, z + z1, northUMax, northVMax);
            vertex(x + x0, y + y1, z + z1, northUMax, northVMin);
            end(vertices);
        }

        // DOWN face
        var downUMin = u0 + ue * x0;
        var downUMax = u0 + ue * x1;
        var downVMin = v0 + ve * (1f - z1);
        var downVMax = v0 + ve * (1f - z0);
        nb = getBlockCached(0, -1, 0).getID();

        edge = y0 == 0f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.DOWN);
            begin();
            vertex(x + x1, y + y0, z + z1, downUMin, downVMin);
            vertex(x + x1, y + y0, z + z0, downUMin, downVMax);
            vertex(x + x0, y + y0, z + z0, downUMax, downVMax);
            vertex(x + x0, y + y0, z + z1, downUMax, downVMin);
            end(vertices);
        }

        // UP face
        var upUMin = u0 + ue * x0;
        var upUMax = u0 + ue * x1;
        var upVMin = v0 + ve * (1f - z1);
        var upVMax = v0 + ve * (1f - z0);
        nb = getBlockCached(0, 1, 0).getID();

        edge = y1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applyFaceLighting(RawDirection.UP);
            begin();
            vertex(x + x0, y + y1, z + z1, upUMin, upVMin);
            vertex(x + x0, y + y1, z + z0, upUMin, upVMax);
            vertex(x + x1, y + y1, z + z0, upUMax, upVMax);
            vertex(x + x1, y + y1, z + z1, upUMax, upVMin);
            end(vertices);
        }
    }

    public void renderSimpleCube(int x, int y, int z, List<BlockVertexPacked> vertices,
        float x0, float y0, float z0, float x1, float y1, float z1,
        float u0, float v0, float u1, float v1) {
        var ue = u1 - u0;
        var ve = v1 - v0;

        // WEST face
        var westUMin = u0 + ue * z0;
        var westUMax = u0 + ue * z1;
        var westVMin = v0 + ve * (1f - y1);
        var westVMax = v0 + ve * (1f - y0);
        var nb = getBlockCached(-1, 0, 0).getID();

        bool edge = x0 == 0f;
        bool render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.WEST);
            begin();
            vertex(x + x0, y + y1, z + z1, westUMin, westVMin);
            vertex(x + x0, y + y0, z + z1, westUMin, westVMax);
            vertex(x + x0, y + y0, z + z0, westUMax, westVMax);
            vertex(x + x0, y + y1, z + z0, westUMax, westVMin);
            end(vertices);
        }

        // EAST face
        var eastUMin = u0 + ue * z0;
        var eastUMax = u0 + ue * z1;
        var eastVMin = v0 + ve * (1f - y1);
        var eastVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(1, 0, 0).getID();

        edge = x1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.EAST);
            begin();
            vertex(x + x1, y + y1, z + z0, eastUMin, eastVMin);
            vertex(x + x1, y + y0, z + z0, eastUMin, eastVMax);
            vertex(x + x1, y + y0, z + z1, eastUMax, eastVMax);
            vertex(x + x1, y + y1, z + z1, eastUMax, eastVMin);
            end(vertices);
        }

        // SOUTH face
        var southUMin = u0 + ue * x0;
        var southUMax = u0 + ue * x1;
        var southVMin = v0 + ve * (1f - y1);
        var southVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(0, 0, -1).getID();

        edge = z0 == 0f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.SOUTH);
            begin();
            vertex(x + x0, y + y1, z + z0, southUMin, southVMin);
            vertex(x + x0, y + y0, z + z0, southUMin, southVMax);
            vertex(x + x1, y + y0, z + z0, southUMax, southVMax);
            vertex(x + x1, y + y1, z + z0, southUMax, southVMin);
            end(vertices);
        }

        // NORTH face
        var northUMin = u0 + ue * x0;
        var northUMax = u0 + ue * x1;
        var northVMin = v0 + ve * (1f - y1);
        var northVMax = v0 + ve * (1f - y0);
        nb = getBlockCached(0, 0, 1).getID();

        edge = z1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.NORTH);
            begin();
            vertex(x + x1, y + y1, z + z1, northUMin, northVMin);
            vertex(x + x1, y + y0, z + z1, northUMin, northVMax);
            vertex(x + x0, y + y0, z + z1, northUMax, northVMax);
            vertex(x + x0, y + y1, z + z1, northUMax, northVMin);
            end(vertices);
        }

        // DOWN face
        var downUMin = u0 + ue * x0;
        var downUMax = u0 + ue * x1;
        var downVMin = v0 + ve * (1f - z1);
        var downVMax = v0 + ve * (1f - z0);
        nb = getBlockCached(0, -1, 0).getID();

        edge = y0 == 0f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.DOWN);
            begin();
            vertex(x + x1, y + y0, z + z1, downUMin, downVMin);
            vertex(x + x1, y + y0, z + z0, downUMin, downVMax);
            vertex(x + x0, y + y0, z + z0, downUMax, downVMax);
            vertex(x + x0, y + y0, z + z1, downUMax, downVMin);
            end(vertices);
        }

        // UP face
        var upUMin = u0 + ue * x0;
        var upUMax = u0 + ue * x1;
        var upVMin = v0 + ve * (1f - z1);
        var upVMax = v0 + ve * (1f - z0);
        nb = getBlockCached(0, 1, 0).getID();

        edge = y1 == 1f;
        render = !Block.fullBlock[nb] || !edge;

        if (render) {
            applySimpleLighting(RawDirection.UP);
            begin();
            vertex(x + x0, y + y1, z + z1, upUMin, upVMin);
            vertex(x + x0, y + y1, z + z0, upUMin, upVMax);
            vertex(x + x1, y + y1, z + z0, upUMax, upVMax);
            vertex(x + x1, y + y1, z + z1, upUMax, upVMin);
            end(vertices);
        }
    }
}