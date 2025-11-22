using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render;

public sealed partial class WorldRenderer {

    public Color currentSkyColour = new Color(100, 180, 255);
    public Color targetSkyColour = new Color(100, 180, 255);

    public Color currentHorizonColour = new Color(120, 200, 255);
    public Color targetHorizonColour = new Color(120, 200, 255);

    private void renderSky(double interp) {
        if (Settings.instance.renderDistance <= 4) {
            var clearColour = Game.graphics.getHorizonColour(world, world.worldTick);
            GL.ClearColor(clearColour.R / 255f, clearColour.G / 255f, clearColour.B / 255f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            return;
        }

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);

        var idc = Game.graphics.idc;
        var idt = Game.graphics.idt;

        var proj = Game.camera.getProjectionMatrix();
        var modelView = Game.camera.getStaticViewMatrix(interp);

        float dayPercent = world.getDayPercentage(world.worldTick);

        float sunAngle = world.getSunAngle(world.worldTick);

        var skyColour = currentSkyColour;
        var underSkyColour = new Color(skyColour.R / 255f * 0.3f, skyColour.G / 255f * 0.3f, skyColour.B / 255f * 0.4f);

        // Setup fog
        //idc.enableFog(true);
        //idc.fogColor(horizonColor.toVec4());
        //idc.setFogType(FogType.Exp2);
        //idc.setFogDensity(0.02f);

        // idk why this is needed! but otherwise it spazzes out when switching light level
        idc.setColour(Color.White);

        idc.enableFog(true);
        idc.fogColor(currentHorizonColour.toVec4());
        idc.setFogType(FogType.Linear);
        //idc.setFogDensity(0.002f);
        idc.fogDistance(0f, 128f);

        var mat = Game.graphics.model;
        mat.push();

        // tilt should be 1 at sunrise (0)
        // 1 at sunset (pi)
        float tiltAngle = MathF.Cos(sunAngle) * 15f; // ±15
        mat.rotate(tiltAngle, 0, 0, 1);


        idc.model(mat);
        idc.view(modelView);
        idc.proj(proj);

        idt.model(mat);
        idt.view(modelView);
        idt.proj(proj);

        renderSkyDome(currentHorizonColour, currentSkyColour, underSkyColour);

        mat.pop();

        idc.enableFog(false);

        renderSunnyMoony(sunAngle);
        renderStars(dayPercent);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private void renderSkyPost(double interp) {
        renderClouds(interp);
    }

    private void renderSkyDome(Color horizonColour, Color skyColour, Color underSkyColour) {
        const float radius = 124f;
        const float topHeight = 16f;
        const float bottomHeight = -64f;
        const int segments = 24;

        var idc = Game.graphics.idc;

        idc.begin(PrimitiveType.Triangles);

        // top cone
        for (int i = 0; i < segments; i++) {
            float v = (MathF.PI * 2f / segments) * i;
            float vn = (MathF.PI * 2f / segments) * (i + 1);

            var v1 = new Vector3(float.Sin(v) * radius, 0, float.Cos(v) * radius);
            var v2 = new Vector3(0, topHeight, 0);
            var v3 = new Vector3(float.Sin(vn) * radius, 0, float.Cos(vn) * radius);

            idc.addVertex(new VertexTinted(v1.X, v1.Y, v1.Z, horizonColour));
            idc.addVertex(new VertexTinted(v2.X, v2.Y, v2.Z, skyColour));
            idc.addVertex(new VertexTinted(v3.X, v3.Y, v3.Z, horizonColour));
        }

        // bottom cone
        for (int i = 0; i < segments; i++) {
            float v = (MathF.PI * 2f / segments) * i;
            float vn = (MathF.PI * 2f / segments) * (i + 1);

            var v1 = new Vector3(0, bottomHeight, 0);
            var v2 = new Vector3(float.Sin(v) * radius, 0, float.Cos(v) * radius);
            var v3 = new Vector3(float.Sin(vn) * radius, 0, float.Cos(vn) * radius);

            idc.addVertex(new VertexTinted(v1.X, v1.Y, v1.Z, underSkyColour));
            idc.addVertex(new VertexTinted(v2.X, v2.Y, v2.Z, horizonColour));
            idc.addVertex(new VertexTinted(v3.X, v3.Y, v3.Z, horizonColour));
        }

        idc.end();
    }

    private void renderSunnyMoony(float sunAngle) {
        const float sunDistance = 96f;
        const float sunSize = 8f;
        const float moonSize = sunSize * 0.75f;

        var idt = Game.graphics.idt;

        var mat = Game.graphics.model;
        mat.push();

        float sunElevation = world.getSunElevation(world.worldTick);
        float sunIntensity = MathF.Max(0, (sunElevation + (MathF.PI / 6f)) / (MathF.PI / 6f));
        float moonIntensity = MathF.Max(0, -(sunElevation - (MathF.PI / 6f)) / (MathF.PI / 6f));

        // Sunny
        Game.graphics.tex(0, Game.textures.sunTexture);
        mat.rotate(Meth.rad2deg(sunAngle), 0, 0, 1);

        idt.setColour(new Color(sunIntensity, sunIntensity, sunIntensity * 0.9f, sunIntensity));

        idt.begin(PrimitiveType.Quads);

        var v1 = new Vector3(sunDistance, -sunSize, -sunSize);
        var v2 = new Vector3(sunDistance, sunSize, -sunSize);
        var v3 = new Vector3(sunDistance, sunSize, sunSize);
        var v4 = new Vector3(sunDistance, -sunSize, sunSize);

        idt.addVertex(new BlockVertexTinted(v1.X, v1.Y, v1.Z, 0f, 0f));
        idt.addVertex(new BlockVertexTinted(v2.X, v2.Y, v2.Z, 0f, 1f));
        idt.addVertex(new BlockVertexTinted(v3.X, v3.Y, v3.Z, 1f, 1f));
        idt.addVertex(new BlockVertexTinted(v4.X, v4.Y, v4.Z, 1f, 0f));
        idt.end();

        // Moony
        Game.graphics.tex(0, Game.textures.moonTexture);

        mat.push();
        mat.rotate(180f, 0, 0, 1);
        idt.setColour(new Color(moonIntensity * 0.9f, moonIntensity * 0.9f, moonIntensity, moonIntensity));

        idt.begin(PrimitiveType.Quads);

        v1 = new Vector3(sunDistance, -moonSize, -moonSize);
        v2 = new Vector3(sunDistance, moonSize, -moonSize);
        v3 = new Vector3(sunDistance, moonSize, moonSize);
        v4 = new Vector3(sunDistance, -moonSize, moonSize);

        idt.addVertex(new BlockVertexTinted(v1.X, v1.Y, v1.Z, 0f, 0f));
        idt.addVertex(new BlockVertexTinted(v2.X, v2.Y, v2.Z, 0f, 1f));
        idt.addVertex(new BlockVertexTinted(v3.X, v3.Y, v3.Z, 1f, 1f));
        idt.addVertex(new BlockVertexTinted(v4.X, v4.Y, v4.Z, 1f, 0f));
        idt.end();

        mat.pop();
        mat.pop();

        // reset colour!
        idt.setColour(Color.White);
    }

    private static Vector3[] generateStarPositions() {
        var random = new XRandom(12345); // fixed seed for consistent stars
        var stars = new Vector3[1200];
        const float starDistance = 100f;

        for (int i = 0; i < stars.Length; i++) {
            // generate random point on sphere using proper spherical coordinates
            float u = random.NextSingle();
            float v = random.NextSingle();

            float theta = 2 * MathF.PI * u; // azimuth
            float phi = MathF.Acos(2 * v - 1); // elevation (corrected distribution)

            float x = starDistance * MathF.Sin(phi) * MathF.Cos(theta);
            float y = starDistance * MathF.Cos(phi);
            float z = starDistance * MathF.Sin(phi) * MathF.Sin(theta);

            stars[i] = new Vector3(x, y, z);
        }

        return stars;
    }

    private void renderStars(float dayPercent) {
        float starAlpha = 0f;

        if (dayPercent is >= 0.65f and <= 0.9f) {
            //night
            starAlpha = 1.0f;
        }
        else if (dayPercent is >= 0.5f and < 0.65f) {
            // evening
            starAlpha = Meth.fadeIn(dayPercent, 0.5f, 0.65f);
        }
        else if (dayPercent >= 0.9f) {
            // morning
            starAlpha = Meth.fadeOut(dayPercent, 0.9f, 1.0f);
        }

        if (starAlpha <= 0.0f) {
            return; // day
        }

        var idc = Game.graphics.idc;

        float continuousTime = dayPercent * 360;
        var mat = Game.graphics.model;

        mat.push();

        mat.rotate(continuousTime, 0.7f, 1f, 0.1f); // tilted celestial axis

        idc.enableFog(false);

        idc.begin(PrimitiveType.Quads);

        for (int i = 0; i < starPositions.Length; i++) {
            Vector3 starPos = starPositions[i];
            // create billboard quad that faces the camera (like the sun)
            var toCamera = Vector3.Normalize(-starPos); // direction from star to camera (at origin)
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, toCamera));
            var up = Vector3.Cross(toCamera, right);

            var hash = XHash.hash(i);
            var colourHash = XHash.hash(hash);
            var sizeHash = XHash.hash(colourHash);

            float ss = (sizeHash & 0xFF) / 255f;
            // 0.12 to 0.31, heavily biased to small
            float starSize = 0.12f + ss * ss * ss * 0.19f;

            var v1 = starPos + (-right - up) * starSize;
            var v2 = starPos + (right - up) * starSize;
            var v3 = starPos + (right + up) * starSize;
            var v4 = starPos + (-right + up) * starSize;

            var type = colourHash % 100;
            float r, g, b;


            switch (int.Abs(type)) {
                case < 2:
                    // MG
                    r = 1.0f;
                    g = 0.4f + ((colourHash >> 0) & 0xF) / 100f;
                    b = 0.9f + ((colourHash >> 4) & 0xF) / 150f;
                    break;
                case < 15:
                    // O/B
                    r = 0.7f + ((colourHash >> 0) & 0xF) / 60f;
                    g = 0.8f + ((colourHash >> 4) & 0xF) / 60f;
                    b = 1.0f;
                    break;
                case < 35:
                    // A
                    r = 0.9f + ((colourHash >> 0) & 0xF) / 150f;
                    g = 0.9f + ((colourHash >> 4) & 0xF) / 150f;
                    b = 1.0f;
                    break;
                case < 60:
                    // F/G
                    r = 1.0f;
                    g = 0.9f + ((colourHash >> 0) & 0xF) / 150f;
                    b = 0.7f + ((colourHash >> 4) & 0xF) / 60f;
                    break;
                case < 80:
                    // K
                    r = 1.0f;
                    g = 0.7f + ((colourHash >> 0) & 0xF) / 60f;
                    b = 0.5f + ((colourHash >> 4) & 0xF) / 90f;
                    break;
                default:
                    // M
                    r = 1.0f;
                    g = 0.5f + ((colourHash >> 0) & 0xF) / 90f;
                    b = 0.3f + ((colourHash >> 4) & 0xF) / 120f;
                    break;
            }

            const int TOTAL = 5000;
            const int THRESHOLD = 4950;
            const int REM = 50;
            // so that the flicker is between 0 and 0.3
            const float DIVIDER = (REM) / 2f / 0.3f;

            // smash the hash into an offset into the function
            // below 80% 1, above 80% sin into 0
            var pc = Meth.mod(world.worldTick + hash, TOTAL);

            // 1 to 50 (REM)
            var rem = pc - THRESHOLD;

            // 0 to rem / 2 (25)
            var pc2 = float.Max(0, float.Max(rem - REM, REM - rem));
            var pc3 = pc2 / DIVIDER; // 0 to 0.3ish???
            var flicker = pc > THRESHOLD ? pc3 : 0f;

            var sc = new Color(r * (1 - flicker), g * (1 - flicker), b * (1 - flicker), starAlpha);


            idc.addVertex(new VertexTinted(v1.X, v1.Y, v1.Z, sc));
            idc.addVertex(new VertexTinted(v2.X, v2.Y, v2.Z, sc));
            idc.addVertex(new VertexTinted(v3.X, v3.Y, v3.Z, sc));
            idc.addVertex(new VertexTinted(v4.X, v4.Y, v4.Z, sc));
        }

        idc.end();

        mat.pop();
    }
}