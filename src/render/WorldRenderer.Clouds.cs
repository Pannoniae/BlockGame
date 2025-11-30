using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using Molten.DoublePrecision;
using Silk.NET.OpenGL.Legacy;
using Debug = System.Diagnostics.Debug;

namespace BlockGame.render;

public sealed partial class WorldRenderer {
    // dont z-fight with blocks pretty please
    public const float cTexSize = 256f;
    public const float cheight = 128f + (Constants.epsilonF * 100);
    public const float cext = 512f; // hs of clouds
    public const float cscale = 2560f; // world units per fulltex (higher = larger)
    public const float scrollSpeed = 0.015f; // blocks per tick in +Z
    public const float cThickness = 6f; // vertical thickness of clouds


    // this is separate to not fuck the main one up!
    public readonly FastInstantDrawTexture cloudidt = new(16384);


    private readonly bool[] pixels;
    private readonly int cloudMaxVerts; // pre-calculated max verts

    private void renderClouds(double interp) {

        // if below 4 skip it
        if (Settings.instance.renderDistance <= 4) {
            return;
        }

        switch (Settings.instance.cloudMode) {
            case 1:
                renderClouds2D(interp);
                break;
            case 2:
                renderClouds3D(interp);
                break;
            case 3:
                renderClouds3DSmooth(interp);
                break;
            case 4:
                renderClouds4D(interp);
                break;
        }
    }

    private void renderClouds2D(double interp) {
        GL.Disable(EnableCap.CullFace);

        var idt = cloudidt;

        idt.batch();

        var mat = Game.graphics.model;
        var proj = Game.camera.getProjectionMatrix();
        var view = Game.camera.getViewMatrix(interp);

        // get sky colour for lighting
        var horizonColour = Game.graphics.getHorizonColour(world, world.worldTick);

        // clouds are in the sky!
        var cc = getLightColour(15, 0);

        var cloudColour = new Color(
            (byte)(horizonColour.R * 0.2f + cc.R * 0.8f),
            (byte)(horizonColour.G * 0.2f + cc.G * 0.8f),
            (byte)(horizonColour.B * 0.2f + cc.B * 0.8f),
            (byte)255
        );

        //Console.Out.WriteLine("Cloud colour: " + cloudColour);

        Game.graphics.tex(0, Game.textures.cloudTexture);

        mat.push();
        mat.loadIdentity();

        // world-space cloud plane centred at player's XZ
        var pp = Vector3D.Lerp(Game.player.prevPosition, Game.player.position, interp);
        float px = (float)pp.X;
        float pz = (float)pp.Z;

        // cap it to render distance as usual
        var cext = 64 + float.Min(WorldRenderer.cext, Settings.instance.renderDistance * 16);

        // snap to cloud plane grid to avoid seams
        float gridSize = cext;
        float baseX = MathF.Floor(px / gridSize) * gridSize;
        float baseZ = MathF.Floor(pz / gridSize) * gridSize;

        idt.model(mat);
        idt.view(view);
        idt.proj(proj);
        idt.setColour(cloudColour);
        idt.enableFog(true);
        // we do a little trickery...
        idt.fogColor(new Vector4(cloudColour.R / 255f, cloudColour.G / 255f, cloudColour.B / 255f, 0));
        idt.setFogType(FogType.Linear);
        // it needs to be dependant on render distance!
        idt.fogDistance(64, 64 + (float.Min(Settings.instance.renderDistance * 16, 480)));

        idt.begin(PrimitiveType.Quads);

        // single quad in world space
        float zScroll = (float)((world.worldTick + interp) * scrollSpeed);

        zScroll %= (cscale * 2);

        float x0 = baseX - cext;
        float x1 = baseX + cext * 2f;
        float z0 = baseZ - cext;
        float z1 = baseZ + cext * 2f;

        float u0 = x0 / cscale;
        float u1 = x1 / cscale;
        float v0 = (z0 + zScroll) / cscale;
        float v1 = (z1 + zScroll) / cscale;

        idt.addVertex(new BlockVertexTinted(x0, cheight, z0, u0, v0));
        idt.addVertex(new BlockVertexTinted(x1, cheight, z0, u1, v0));
        idt.addVertex(new BlockVertexTinted(x1, cheight, z1, u1, v1));
        idt.addVertex(new BlockVertexTinted(x0, cheight, z1, u0, v1));

        idt.end();

        mat.pop();

        // reset colour
        idt.setColour(Color.White);
        idt.enableFog(false);

        GL.Enable(EnableCap.CullFace);
    }

    private void renderClouds3D(double interp) {
        GL.Disable(EnableCap.CullFace);

        var idt = cloudidt;

        idt.batch();

        var mat = Game.graphics.model;
        var proj = Game.camera.getProjectionMatrix();
        var view = Game.camera.getViewMatrix(interp);

        // lighting
        var horizonColour = Game.graphics.getHorizonColour(world, world.worldTick);
        // clouds are in the sky!
        var cc = getLightColour(15, 0);

        var cloudColour = new Color(
            (byte)(horizonColour.R * 0.2f + cc.R * 0.8f),
            (byte)(horizonColour.G * 0.2f + cc.G * 0.8f),
            (byte)(horizonColour.B * 0.2f + cc.B * 0.8f),
            (byte)255
        );

        Game.graphics.tex(0, Game.textures.cloudTexture);

        mat.push();
        mat.loadIdentity();

        // centre box on player
        var pp = Vector3D.Lerp(Game.player.prevPosition, Game.player.position, interp);
        float px = (float)pp.X;
        float pz = (float)pp.Z;

        float zScroll = (float)((world.worldTick + interp) * scrollSpeed);

        // tile zScroll so we don't get precision issues at extreme distances
        zScroll %= (cscale * 2);

        // cap it to render distance as usual
        var cext = 64 + float.Min(WorldRenderer.cext, Settings.instance.renderDistance * 16);

        // box bounds - actually centred on player this time!
        float x0 = px - cext;
        float x1 = px + cext;
        float y0 = cheight;
        float y1 = cheight + cThickness;
        float z0 = pz - cext;
        float z1 = pz + cext;

        idt.model(mat);
        idt.view(view);
        idt.proj(proj);

        // get translucent
        cloudColour.A = 192;

        idt.setColour(cloudColour);
        idt.enableFog(true);
        idt.fogColor(new Vector4(cloudColour.R / 255f, cloudColour.G / 255f, cloudColour.B / 255f, 0));
        idt.setFogType(FogType.Linear);
        idt.fogDistance(64, 64 + float.Min(Settings.instance.renderDistance * 16, 480));

        // PASS 1: depth-only (write depth, no colour)
        GL.ColorMask(false, false, false, false);

        idt.begin(PrimitiveType.Quads);
        addCloud(x0, y0, z0, x1, y1, z1, cscale, zScroll, cloudColour);
        idt.endReuse(false);

        // PASS 2: colour (with depth test)
        GL.ColorMask(true, true, true, true);

        //idt.begin(PrimitiveType.Quads);
        //addCloud(x0, y0, z0, x1, y1, z1, cloudScale, zScroll, cloudColour);
        idt.endReuse(true);

        mat.pop();

        idt.setColour(Color.White);
        idt.enableFog(false);
        GL.Enable(EnableCap.CullFace);
    }

    private void renderClouds3DSmooth(double interp) {
        GL.Disable(EnableCap.CullFace);

        var idt = cloudidt;

        idt.batch();

        var mat = Game.graphics.model;
        var proj = Game.camera.getProjectionMatrix();
        var view = Game.camera.getViewMatrix(interp);

        // lighting
        var horizonColour = Game.graphics.getHorizonColour(world, world.worldTick);
        // clouds are in the sky!
        var cc = getLightColour(15, 0);

        var cloudColour = new Color(
            (byte)(horizonColour.R * 0.2f + cc.R * 0.8f),
            (byte)(horizonColour.G * 0.2f + cc.G * 0.8f),
            (byte)(horizonColour.B * 0.2f + cc.B * 0.8f),
            (byte)255
        );

        Game.graphics.tex(0, Game.textures.cloudTexture);

        mat.push();
        mat.loadIdentity();

        // centre box on player
        var pp = Vector3D.Lerp(Game.player.prevPosition, Game.player.position, interp);
        float px = (float)pp.X;
        float pz = (float)pp.Z;

        float zScroll = (float)((world.worldTick + interp) * scrollSpeed);

        // tile zScroll so we don't get precision issues at extreme distances
        zScroll %= (cscale * 2);

        // cap it to render distance as usual
        var cext = 64 + float.Min(WorldRenderer.cext, Settings.instance.renderDistance * 16);

        // box bounds - actually centred on player this time!
        float x0 = px - cext;
        float x1 = px + cext;
        float y0 = cheight;
        float y1 = cheight + cThickness;
        float z0 = pz - cext;
        float z1 = pz + cext;

        idt.model(mat);
        idt.view(view);
        idt.proj(proj);

        // get translucent
        cloudColour.A = 192;

        idt.setColour(cloudColour);
        idt.enableFog(true);
        idt.fogColor(new Vector4(cloudColour.R / 255f, cloudColour.G / 255f, cloudColour.B / 255f, 0));
        idt.setFogType(FogType.Linear);
        idt.fogDistance(64, 64 + float.Min(Settings.instance.renderDistance * 16, 480));

        // PASS 1: depth-only (write depth, no colour)
        GL.ColorMask(false, false, false, false);

        idt.begin(PrimitiveType.Quads);
        addCloudSmooth(x0, y0, z0, x1, y1, z1, cscale, zScroll, cloudColour);
        idt.endReuse(false);

        // PASS 2: colour (with depth test)
        GL.ColorMask(true, true, true, true);

        //idt.begin(PrimitiveType.Quads);
        //addCloud(x0, y0, z0, x1, y1, z1, cloudScale, zScroll, cloudColour);
        idt.endReuse(true);

        mat.pop();

        idt.setColour(Color.White);
        idt.enableFog(false);
        GL.Enable(EnableCap.CullFace);
    }

    private void addCloud(float x0, float y0, float z0, float x1, float y1, float z1,
        float scale, float zScroll, Color cloudColour) {
        // shades
        Span<float> shades = [
            1.0f, // top
            0.8f, // north/south
            0.7f, // east/west
            0.6f // bottom
        ];

        Span<Color> cc = [
            cloudColour * new Color(shades[0], shades[0], shades[0], 1.0f),
            cloudColour * new Color(shades[1], shades[1], shades[1], 1.0f),
            cloudColour * new Color(shades[2], shades[2], shades[2], 1.0f),
            cloudColour * new Color(shades[3], shades[3], shades[3], 1.0f)
        ];

        var idt = cloudidt;

        idt.batch();

        var tex = Game.textures.cloudTexture;
        int w = (int)tex.width;
        int h = (int)tex.height;

        // calc UVs for entire cloud area
        float u0 = x0 / scale;
        float u1 = x1 / scale;
        float v0 = (z0 + zScroll) / scale;
        float v1 = (z1 + zScroll) / scale;

        // top face - single quad
        idt.setColour(cc[0]);
        idt.addVertex(new BlockVertexTinted(x0, y1, z0, u0, v0));
        idt.addVertex(new BlockVertexTinted(x1, y1, z0, u1, v0));
        idt.addVertex(new BlockVertexTinted(x1, y1, z1, u1, v1));
        idt.addVertex(new BlockVertexTinted(x0, y1, z1, u0, v1));

        // bottom face - single quad
        idt.setColour(cc[3]);
        idt.addVertex(new BlockVertexTinted(x0, y0, z1, u0, v1));
        idt.addVertex(new BlockVertexTinted(x1, y0, z1, u1, v1));
        idt.addVertex(new BlockVertexTinted(x1, y0, z0, u1, v0));
        idt.addVertex(new BlockVertexTinted(x0, y0, z0, u0, v0));


        int x0o = (int)float.Floor(u0 * w);
        int x1o = (int)float.Ceiling(u1 * w);
        int y0o = (int)float.Floor(v0 * h);
        int y1o = (int)float.Ceiling(v1 * h);

        float pw = scale / w;
        float pd = scale / h;

        var img = pixels;

        for (int tx = x0o; tx < x1o; tx++) {
            for (int ty = y0o; ty < y1o; ty++) {
                // wrap
                int xx = tx % w;
                int yy = ty % h;
                if (xx < 0) xx += w;
                if (yy < 0) yy += h;

                var pixel = img[yy * w + xx];
                if (!pixel) {
                    continue; // transparent
                }

                // worldpos
                float wx = tx * pw;
                float wz = ty * pd - zScroll;
                float wx0 = wx;
                float wx1 = wx + pw;
                float wz0 = wz;
                float wz1 = wz + pd;

                float u = (tx + 0.5f) / w;
                float v = (ty + 0.5f) / h;

                // TODO don't shrink if the adjacent is also a cloud? this works but breaks at diagonals.
                // i dont think this is solvable without subdividing or some shit, good enough for now

                // north
                idt.setColour(cc[2]);
                int adj = yy + 1;
                adj = adj >= h ? 0 : adj;
                if (!img[adj * w + xx]) {
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz1, u, v));

                }

                // south
                idt.setColour(cc[2]);
                adj = yy - 1;
                adj = adj < 0 ? h - 1 : adj;
                if (!img[adj * w + xx]) {
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz0, u, v));
                }

                // east
                idt.setColour(cc[1]);
                adj = xx + 1;
                adj = adj >= w ? 0 : adj;
                if (!img[yy * w + adj]) {
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz0, u, v));
                }

                // west
                idt.setColour(cc[1]);
                adj = xx - 1;
                adj = adj < 0 ? w - 1 : adj;
                if (!img[yy * w + adj]) {
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz1, u, v));
                }
            }
        }
    }

    private void addCloudSmooth(float x0, float y0, float z0, float x1, float y1, float z1,
        float scale, float zScroll, Color cloudColour) {
        // shades
        Span<float> shades = [
            1.0f, // top
            0.8f, // north/south
            0.7f, // east/west
            0.6f // bottom
        ];

        Span<Color> cc = [
            cloudColour * new Color(1f, 1f, 1f, 0.01f),
            cloudColour * new Color(shades[1], shades[1], shades[1], 1.0f),
            cloudColour * new Color(shades[2], shades[2], shades[2], 1.0f),
            cloudColour * new Color(shades[3], shades[3], shades[3], 1.0f)
        ];

        var idt = cloudidt;

        idt.batch();

        var tex = Game.textures.cloudTexture;
        int w = (int)tex.width;
        int h = (int)tex.height;

        // calc UVs for entire cloud area
        float u0 = x0 / scale;
        float u1 = x1 / scale;
        float v0 = (z0 + zScroll) / scale;
        float v1 = (z1 + zScroll) / scale;

        // top face - single quad
        idt.setColour(cc[0]);
        idt.addVertex(new BlockVertexTinted(x0, y1, z0, u0, v0));
        idt.addVertex(new BlockVertexTinted(x1, y1, z0, u1, v0));
        idt.addVertex(new BlockVertexTinted(x1, y1, z1, u1, v1));
        idt.addVertex(new BlockVertexTinted(x0, y1, z1, u0, v1));

        // bottom face - single quad
        idt.setColour(cc[3]);
        idt.addVertex(new BlockVertexTinted(x0, y0, z1, u0, v1));
        idt.addVertex(new BlockVertexTinted(x1, y0, z1, u1, v1));
        idt.addVertex(new BlockVertexTinted(x1, y0, z0, u1, v0));
        idt.addVertex(new BlockVertexTinted(x0, y0, z0, u0, v0));


        int x0o = (int)float.Floor(u0 * w);
        int x1o = (int)float.Ceiling(u1 * w);
        int y0o = (int)float.Floor(v0 * h);
        int y1o = (int)float.Ceiling(v1 * h);

        float pw = scale / w;
        float pd = scale / h;

        var img = pixels;

        for (int tx = x0o; tx < x1o; tx++) {
            for (int ty = y0o; ty < y1o; ty++) {
                // wrap
                int xx = tx % w;
                int yy = ty % h;
                if (xx < 0) xx += w;
                if (yy < 0) yy += h;

                var pixel = img[yy * w + xx];
                if (!pixel) {
                    continue; // transparent
                }

                // worldpos
                float wx = tx * pw;
                float wz = ty * pd - zScroll;
                float wx0 = wx;
                float wx1 = wx + pw;
                float wz0 = wz;
                float wz1 = wz + pd;

                float u = (tx + 0.5f) / w;
                float v = (ty + 0.5f) / h;

                // TODO don't shrink if the adjacent is also a cloud? this works but breaks at diagonals.
                // i dont think this is solvable without subdividing or some shit, good enough for now

                // north
                idt.setColour(cc[2]);
                int adj = yy + 1;
                adj = adj >= h ? 0 : adj;
                if (!img[adj * w + xx]) {
                    idt.setColour(cc[0]);
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz1, u, v));
                    idt.setColour(cc[2]);
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz1, u, v));
                }

                // south
                idt.setColour(cc[2]);
                adj = yy - 1;
                adj = adj < 0 ? h - 1 : adj;
                if (!img[adj * w + xx]) {
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz0, u, v));
                    idt.setColour(cc[0]);
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz0, u, v));
                }

                // east
                idt.setColour(cc[1]);
                adj = xx + 1;
                adj = adj >= w ? 0 : adj;
                if (!img[yy * w + adj]) {
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz1, u, v));
                    idt.setColour(cc[0]);
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz0, u, v));
                }

                // west
                idt.setColour(cc[1]);
                adj = xx - 1;
                adj = adj < 0 ? w - 1 : adj;
                if (!img[yy * w + adj]) {
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz0, u, v));
                    idt.setColour(cc[0]);
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz1, u, v));
                }
            }
        }
    }

    private void addCloud4D(float x0, float y0, float z0, float x1, float y1, float z1,
        float scale, float zScroll, Color cloudColour, float ex = 1f) {
        // shades
        Span<Color> cc = [
            cloudColour * new Color(1.0f, 1.0f, 1.0f, 1.0f),
            cloudColour * new Color(0.8f, 0.8f, 0.8f, 1.0f),
            cloudColour * new Color(0.7f, 0.7f, 0.7f, 1.0f),
            cloudColour * new Color(0.6f, 0.6f, 0.6f, 1.0f)
        ];

        Span<bool> adjs = stackalloc bool[4];

        var idt = cloudidt;

        idt.batch();

        var tex = Game.textures.cloudTexture;
        //int w = (int)tex.width;
        //int h = (int)tex.height;

        // if not, this code is fucked
        Debug.Assert(tex.width == 256);
        Debug.Assert(tex.height == 256);

        // calc visible texture pixel bounds
        float u0 = x0 / scale;
        float u1 = x1 / scale;
        float v0 = (z0 + zScroll) / scale;
        float v1 = (z1 + zScroll) / scale;

        int x0o = (int)float.Floor(u0 * 256);
        int x1o = (int)float.Ceiling(u1 * 256);
        int y0o = (int)float.Floor(v0 * 256);
        int y1o = (int)float.Ceiling(v1 * 256);

        float pw = scale * (1 / 256f);
        float pd = scale * (1 / 256f);

        var img = pixels;

        for (int tx = x0o; tx < x1o; tx++) {
            for (int ty = y0o; ty < y1o; ty++) {
                // wrap
                int xx = tx & 0xFF;
                int yy = ty & 0xFF;
                xx = xx < 0 ? 256 + xx : xx;
                yy = yy < 0 ? 256 + yy : yy;

                var pixel = img[(yy << 8) + xx];
                if (!pixel) {
                    continue; // transparent
                }

                // set up adjacencies

                // west
                int adj = xx - 1;
                adj = adj < 0 ? (256 - 1) : adj;
                adjs[0] = !img[(yy << 8) + adj];

                // east
                adj = xx + 1;
                adj = adj >= 256 ? 0 : adj;
                adjs[1] = !img[(yy << 8) + adj];

                // south
                adj = yy - 1;
                adj = adj < 0 ? (256 - 1) : adj;
                adjs[2] = !img[(adj << 8) + xx];

                // north
                adj = yy + 1;
                adj = adj >= 256 ? 0 : adj;
                adjs[3] = !img[(adj << 8) + xx];

                float wx = tx * pw;
                float wz = ty * pd - zScroll;
                float wx0 = wx;
                float wx1 = wx + pw;
                float wz0 = wz;
                float wz1 = wz + pd;

                // shrink to ex times in the pixel
                float wwx0 = (wx + (pw * (1 - ex) / 2));
                float wwx1 = (wx + pw - (pw * (1 - ex) / 2));
                float wwz0 = (wz + (pd * (1 - ex) / 2));
                float wwz1 = (wz + pd - (pd * (1 - ex) / 2));

                // if adjacent is cloud, DON'T SHRINK, otherwise do
                wwx0 = adjs[0] ? wwx0 : wx0;
                wwx1 = adjs[1] ? wwx1 : wx1;
                wwz0 = adjs[2] ? wwz0 : wz0;
                wwz1 = adjs[3] ? wwz1 : wz1;


                // also shrink y! (between cloudHeight and cloudHeight + cloudThickness)
                float wy0 = y0 + (cThickness) * ((1 - ex) / 2);
                float wy1 = y1 - (cThickness) * ((1 - ex) / 2);

                // UVs for horizontal faces (just sample centre of pixel)
                float u = (tx + 0.5f) * (1 / 256f);
                float v = (ty + 0.5f) * (1 / 256f);

                // TODO don't shrink if the adjacent is also a cloud

                //idt.setColour();
                var tint = cc[0];

                // top face

                ref BlockVertexTinted vt = ref idt.getRefE();
                vt.x = wwx0; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                vt = ref idt.getRefE();
                vt.x = wwx1; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                vt = ref idt.getRefE();
                vt.x = wwx1; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                vt = ref idt.getRefE();
                vt.x = wwx0; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;

                //idt.setColour(cc[3]);
                tint = cc[3];

                // bottom face
                vt = ref idt.getRefE();
                vt.x = wwx0; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                vt = ref idt.getRefE();
                vt.x = wwx1; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                vt = ref idt.getRefE();
                vt.x = wwx1; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                vt = ref idt.getRefE();
                vt.x = wwx0; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;

                // west

                idt.setColour(cc[1]);
                int adjX = xx - 1;
                adjX = adjX < 0 ? (256 - 1) : adjX;
                if (!img[(yy << 8) + adjX]) {
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                }

                // east

                idt.setColour(cc[1]);
                adjX = xx + 1;
                adjX = adjX >= 256 ? 0 : adjX;
                if (!img[(yy << 8) + adjX]) {
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                }

                // check adjacent pixels for side faces

                // south
                //idt.setColour(cc[2]);
                tint = cc[2];

                idt.setColour(cc[2]);
                int adjY = yy - 1;
                adjY = adjY < 0 ? (256 - 1) : adjY;
                if (!img[(adjY << 8) + xx]) {
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = tint;
                }

                // north

                idt.setColour(cc[2]);
                adjY = yy + 1;
                adjY = adjY >= 256 ? 0 : adjY;
                if (!img[(adjY << 8) + xx]) {
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = tint;

                }
            }
        }
    }

    /**
     * Same shit as 3D but hyper
     */
    private void renderClouds4D(double interp) {
        GL.Disable(EnableCap.CullFace);

        var idt = cloudidt;

        idt.batch();

        var mat = Game.graphics.model;
        var proj = Game.camera.getProjectionMatrix();
        var view = Game.camera.getViewMatrix(interp);

        // lighting with unified fog handling
        var horizonColour = Game.graphics.getHorizonColour(world, world.worldTick);
        // clouds are in the sky!
        var cc = getLightColour(15, 0);

        var cloudColour = new Color(
            (byte)(horizonColour.R * 0.2f + cc.R * 0.8f),
            (byte)(horizonColour.G * 0.2f + cc.G * 0.8f),
            (byte)(horizonColour.B * 0.2f + cc.B * 0.8f),
            (byte)255
        );

        Game.graphics.tex(0, Game.textures.cloudTexture);

        mat.push();
        mat.loadIdentity();

        // centre box on player
        var pp = Vector3D.Lerp(Game.player.prevPosition, Game.player.position, interp);
        float px = (float)pp.X;
        float pz = (float)pp.Z;

        float zScroll = (float)((world.worldTick + interp) * scrollSpeed);

        // tile zScroll so we don't get precision issues at extreme distances
        zScroll %= (cscale * 2);

        // cap it to render distance as usual
        var cext = 64 + float.Min(WorldRenderer.cext, Settings.instance.renderDistance * 16);

        // box bounds - actually centred on player this time!
        float x0 = px - cext;
        float x1 = px + cext;
        float y0 = cheight;
        float y1 = cheight + cThickness;
        float z0 = pz - cext;
        float z1 = pz + cext;

        idt.model(mat);
        idt.view(view);
        idt.proj(proj);

        // get translucent
        cloudColour.A = 128;

        idt.setColour(cloudColour);
        idt.enableFog(true);
        idt.fogColor(new Vector4(cloudColour.R / 255f, cloudColour.G / 255f, cloudColour.B / 255f, 0));
        idt.setFogType(FogType.Linear);
        idt.fogDistance(0, 64 + float.Min(Settings.instance.renderDistance * 16, 480));

        // build all 4 layers into one buffer, then draw with offsets
        idt.begin(PrimitiveType.Quads);

        // calc visible pixel range
        int x0o = (int)float.Floor((x0 * (1 / cscale * 256)));
        int x1o = (int)float.Ceiling((x1 * (1 / cscale * 256)));
        int y0o = (int)float.Floor(((z0 + zScroll) * (1 / cscale * 256)));
        int y1o = (int)float.Ceiling(((z1 + zScroll) * (1 / cscale * 256)));

        int visiblePixels = (x1o - x0o) * (y1o - y0o);
        const float totalPixels = 256 * 256;

        //Console.Out.WriteLine($"Cloud visible pixels: {visiblePixels} / {totalPixels} {cloudMaxVerts}");

        // scale cloudMaxVerts by visible area ratio
        int estimatedVerts = (int)(((long)cloudMaxVerts * visiblePixels) / (double)totalPixels * 1.33f);
        idt.reserve(estimatedVerts * 4);

        Span<int> counts = stackalloc int[4];
        int li = 0;

        for (float i = 0.4f; i <= 1 + Constants.epsilonF; i += 0.2f) {
            int startVertex = idt.currentVertex;
            addCloud4D(x0, y0, z0, x1, y1, z1, cscale, zScroll, cloudColour, i);
            counts[li++] = idt.currentVertex - startVertex;
        }

        // upload once
        idt.reuseUpload();

        // draw 4 times with offsets
        int offset = 0;
        for (int layer = 0; layer < 4; layer++) {
            // PASS 1: depth-only
            GL.ColorMask(false, false, false, false);
            idt.renderRange(offset, counts[layer]);

            // PASS 2: colour
            GL.ColorMask(true, true, true, true);
            idt.renderRange(offset, counts[layer]);

            offset += counts[layer];
        }

        idt.finishReuse();

        mat.pop();

        idt.setColour(Color.White);
        idt.enableFog(false);
        GL.Enable(EnableCap.CullFace);
    }
}