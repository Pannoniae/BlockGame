using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using Molten.DoublePrecision;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render;

public sealed partial class WorldRenderer {
    // dont z-fight with blocks pretty please
    const float cTexSize = 256f;
    const float cheight = 128f + (Constants.epsilonF * 100);
    const float cext = 512f; // hs of clouds
    const float cscale = 2560f; // world units per fulltex (higher = larger)
    const float scrollSpeed = 0.015f; // blocks per tick in +Z
    const float cThickness = 6f; // vertical thickness of clouds


    // this is separate to not fuck the main one up!
    public FastInstantDrawTexture cloudidt = new(65536);


    private Rgba32[] pixels;

    private void renderClouds(double interp) {
        switch (Settings.instance.cloudMode) {
            case 1:
                renderClouds2D(interp);
                break;
            case 2:
                renderClouds3D(interp);
                break;
            case 3:
                renderClouds4D(interp);
                break;
        }
    }

    private void renderClouds2D(double interp) {
        GL.Disable(EnableCap.CullFace);

        var idt = cloudidt;

        var mat = Game.graphics.model;
        var proj = Game.camera.getProjectionMatrix();
        var view = Game.camera.getViewMatrix(interp);

        // get sky colour for lighting
        var horizonColour = world.getHorizonColour(world.worldTick);

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

        // snap to cloud plane grid to avoid seams
        const float gridSize = cext;
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
        idt.fogDistance(0, 64 + (float.Min(Settings.instance.renderDistance * 16, 480)));

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
        var mat = Game.graphics.model;
        var proj = Game.camera.getProjectionMatrix();
        var view = Game.camera.getViewMatrix(interp);

        // lighting
        var horizonColour = world.getHorizonColour(world.worldTick);
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
        var tex = Game.textures.cloudTexture;
        int w = (int)tex.width;
        int h = (int)tex.height;

        // calc visible texture pixel bounds
        float u0 = x0 / scale;
        float u1 = x1 / scale;
        float v0 = (z0 + zScroll) / scale;
        float v1 = (z1 + zScroll) / scale;

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
                if (pixel.A == 0) {
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

                idt.setColour(cc[0]);

                // top
                idt.addVertex(new BlockVertexTinted(wx0, y1, wz0, u, v));
                idt.addVertex(new BlockVertexTinted(wx1, y1, wz0, u, v));
                idt.addVertex(new BlockVertexTinted(wx1, y1, wz1, u, v));
                idt.addVertex(new BlockVertexTinted(wx0, y1, wz1, u, v));

                idt.setColour(cc[3]);

                // bottom
                idt.addVertex(new BlockVertexTinted(wx0, y0, wz1, u, v));
                idt.addVertex(new BlockVertexTinted(wx1, y0, wz1, u, v));
                idt.addVertex(new BlockVertexTinted(wx1, y0, wz0, u, v));
                idt.addVertex(new BlockVertexTinted(wx0, y0, wz0, u, v));

                // north
                idt.setColour(cc[2]);
                int adj = yy + 1;
                if (adj >= h) adj = 0;
                if (img[adj * w + xx].A == 0) {
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz1, u, v));

                }

                // south
                idt.setColour(cc[2]);
                adj = yy - 1;
                if (adj < 0) adj = h - 1;
                if (img[adj * w + xx].A == 0) {
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz0, u, v));
                }

                // east
                idt.setColour(cc[1]);
                adj = xx + 1;
                if (adj >= w) adj = 0;
                if (img[yy * w + adj].A == 0) {
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y0, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx1, y1, wz0, u, v));
                }

                // west
                idt.setColour(cc[1]);
                adj = xx - 1;
                if (adj < 0) adj = w - 1;
                if (img[yy * w + adj].A == 0) {
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz1, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y0, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz0, u, v));
                    idt.addVertex(new BlockVertexTinted(wx0, y1, wz1, u, v));
                }
            }
        }
    }

    private void addCloud4D(float x0, float y0, float z0, float x1, float y1, float z1,
        float scale, float zScroll, Color cloudColour, float ex = 1f) {
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

        Span<bool> adjs = stackalloc bool[4];

        var idt = cloudidt;
        var tex = Game.textures.cloudTexture;
        int w = (int)tex.width;
        int h = (int)tex.height;

        // calc visible texture pixel bounds
        float u0 = x0 / scale;
        float u1 = x1 / scale;
        float v0 = (z0 + zScroll) / scale;
        float v1 = (z1 + zScroll) / scale;

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
                if (pixel.A == 0) {
                    continue; // transparent
                }

                // set up adjacencies

                // west
                int adj = xx - 1;
                if (adj < 0) adj = w - 1;
                adjs[0] = img[yy * w + adj].A == 0;

                // east
                adj = xx + 1;
                if (adj >= w) adj = 0;
                adjs[1] = img[yy * w + adj].A == 0;

                // south
                adj = yy - 1;
                if (adj < 0) adj = h - 1;
                adjs[2] = img[adj * w + xx].A == 0;

                // north
                adj = yy + 1;
                if (adj >= h) adj = 0;
                adjs[3] = img[adj * w + xx].A == 0;

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
                float u = (tx + 0.5f) / w;
                float v = (ty + 0.5f) / h;

                // TODO don't shrink if the adjacent is also a cloud

                idt.setColour(cc[0]);

                // top face
                /*idt.addVertexE(new BlockVertexTinted(wwx0, wy1, wwz0, u, v));
                idt.addVertexE(new BlockVertexTinted(wwx1, wy1, wwz0, u, v));
                idt.addVertexE(new BlockVertexTinted(wwx1, wy1, wwz1, u, v));
                idt.addVertexE(new BlockVertexTinted(wwx0, wy1, wwz1, u, v));*/

                ref BlockVertexTinted vt = ref idt.getRefE();
                vt.x = wwx0; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                vt = ref idt.getRefE();
                vt.x = wwx1; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                vt = ref idt.getRefE();
                vt.x = wwx1; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                vt = ref idt.getRefE();
                vt.x = wwx0; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;

                idt.setColour(cc[3]);

                // bottom face
                /*idt.addVertexE(new BlockVertexTinted(wwx0, wy0, wwz1, u, v));
                idt.addVertexE(new BlockVertexTinted(wwx1, wy0, wwz1, u, v));
                idt.addVertexE(new BlockVertexTinted(wwx1, wy0, wwz0, u, v));
                idt.addVertexE(new BlockVertexTinted(wwx0, wy0, wwz0, u, v));*/
                vt = ref idt.getRefE();
                vt.x = wwx0; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                vt = ref idt.getRefE();
                vt.x = wwx1; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                vt = ref idt.getRefE();
                vt.x = wwx1; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                vt = ref idt.getRefE();
                vt.x = wwx0; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;

                // west

                idt.setColour(cc[1]);
                int adjX = xx - 1;
                if (adjX < 0) adjX = w - 1;
                if (img[yy * w + adjX].A == 0) {
                    /*idt.addVertexE(new BlockVertexTinted(wwx0, wy0, wwz1, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx0, wy0, wwz0, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx0, wy1, wwz0, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx0, wy1, wwz1, u, v));*/
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                }

                // east

                idt.setColour(cc[1]);
                adjX = xx + 1;
                if (adjX >= w) adjX = 0;
                if (img[yy * w + adjX].A == 0) {
                    /*idt.addVertexE(new BlockVertexTinted(wwx1, wy0, wwz0, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx1, wy0, wwz1, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx1, wy1, wwz1, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx1, wy1, wwz0, u, v));*/
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                }

                // check adjacent pixels for side faces

                // south

                idt.setColour(cc[2]);
                int adjY = yy - 1;
                if (adjY < 0) adjY = h - 1;
                if (img[adjY * w + xx].A == 0) {
                    /*idt.addVertexE(new BlockVertexTinted(wwx0, wy0, wwz0, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx1, wy0, wwz0, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx1, wy1, wwz0, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx0, wy1, wwz0, u, v));*/
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy0; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy1; vt.z = wwz0; vt.u = u; vt.v = v; vt.c = idt.tint;
                }

                // north

                idt.setColour(cc[2]);
                adjY = yy + 1;
                if (adjY >= h) adjY = 0;
                if (img[adjY * w + xx].A == 0) {
                    /*idt.addVertexE(new BlockVertexTinted(wwx0, wy1, wwz1, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx1, wy1, wwz1, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx1, wy0, wwz1, u, v));
                    idt.addVertexE(new BlockVertexTinted(wwx0, wy0, wwz1, u, v));*/
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy1; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx1; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;
                    vt = ref idt.getRefE();
                    vt.x = wwx0; vt.y = wy0; vt.z = wwz1; vt.u = u; vt.v = v; vt.c = idt.tint;

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
        var mat = Game.graphics.model;
        var proj = Game.camera.getProjectionMatrix();
        var view = Game.camera.getViewMatrix(interp);

        // lighting
        var horizonColour = world.getHorizonColour(world.worldTick);
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

        var tex = Game.textures.cloudTexture;

        // reserve space for ALL 4 layers upfront
        int x0o = (int)float.Floor((x0 / cscale) * (int)tex.width);
        int x1o = (int)float.Ceiling((x1 / cscale) * (int)tex.width);
        int y0o = (int)float.Floor(((z0 + zScroll) / cscale) * (int)tex.height);
        int y1o = (int)float.Ceiling(((z1 + zScroll) / cscale) * (int)tex.height);

        // todo some of this is unnecessary. can you find it? :D
        // I know what it is and I didn't fix it on purpose, but if you find it, you get a free hug! <3
        int vertsPerLayer = (x1o - x0o) * (y1o - y0o) * 24;
        idt.reserve(vertsPerLayer * 4);

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