using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using Molten;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render;

public sealed partial class WorldRenderer {
    private void renderSky(double interp) {
        if (Settings.instance.renderDistance <= 4) {
            //return;
        }

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);

        var viewProj = Game.camera.getStaticViewMatrix(interp) * Game.camera.getProjectionMatrix();
        var modelView = Game.camera.getStaticViewMatrix(interp);

        float dayPercent = world.getDayPercentage(world.worldTick);

        float sunAngle = world.getSunAngle(world.worldTick);

        var horizonColour = world.getHorizonColour(world.worldTick).toColor();
        var skyColour = world.getSkyColour(world.worldTick).toColor();
        var underSkyColour = new Color(skyColour.R / 255f * 0.3f, skyColour.G / 255f * 0.3f, skyColour.B / 255f * 0.4f);

        // Setup fog
        //idc.enableFog(true);
        //idc.fogColor(horizonColor.toVec4());
        //idc.setFogType(FogType.Exp2);
        //idc.setFogDensity(0.02f);
        idc.enableFog(true);
        idc.fogColor(horizonColour.toVec4());
        idc.setFogType(FogType.Linear);
        //idc.setFogDensity(0.002f);
        idc.fogDistance(0f, 128f);

        var mat = Game.graphics.modelView;
        mat.push();

        // tilt should be 1 at sunrise (0)
        // 1 at sunset (pi)
        float tiltAngle = MathF.Cos(sunAngle) * 15f; // ±15
        mat.rotate(tiltAngle, 0, 0, 1);


        idc.setMVP(mat.top * viewProj);
        idc.setMV(mat.top * modelView);

        renderSkyDome(horizonColour, skyColour, underSkyColour);

        mat.pop();

        idc.enableFog(false);

        // Render sun and moon
        renderSunnyMoony(dayPercent, sunAngle, viewProj, modelView);

        // Render stars
        renderStars(dayPercent, viewProj, modelView);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private void renderSkyDome(Color horizonColour, Color skyColour, Color underSkyColour) {
        const float radius = 128f;
        const float topHeight = 16f;
        const float bottomHeight = -64f;
        const int segments = 24;

        idc.begin(PrimitiveType.Triangles);

        // top cone
        for (int i = 0; i < segments; i++) {
            float v = (MathF.PI * 2f / segments) * i;
            float vn = (MathF.PI * 2f / segments) * (i + 1);

            var v1 = new Vector3(MathF.Sin(v) * radius, 0, MathF.Cos(v) * radius);
            var v2 = new Vector3(0, topHeight, 0);
            var v3 = new Vector3(MathF.Sin(vn) * radius, 0, MathF.Cos(vn) * radius);

            idc.addVertex(new VertexTinted(v1.X, v1.Y, v1.Z, horizonColour));
            idc.addVertex(new VertexTinted(v2.X, v2.Y, v2.Z, skyColour));
            idc.addVertex(new VertexTinted(v3.X, v3.Y, v3.Z, horizonColour));
        }

        // bottom cone
        for (int i = 0; i < segments; i++) {
            float v = (MathF.PI * 2f / segments) * i;
            float vn = (MathF.PI * 2f / segments) * (i + 1);

            var v1 = new Vector3(0, bottomHeight, 0);
            var v2 = new Vector3(MathF.Sin(v) * radius, 0, MathF.Cos(v) * radius);
            var v3 = new Vector3(MathF.Sin(vn) * radius, 0, MathF.Cos(vn) * radius);

            idc.addVertex(new VertexTinted(v1.X, v1.Y, v1.Z, underSkyColour));
            idc.addVertex(new VertexTinted(v2.X, v2.Y, v2.Z, horizonColour));
            idc.addVertex(new VertexTinted(v3.X, v3.Y, v3.Z, horizonColour));
        }

        idc.end();
    }

    private void renderSunnyMoony(float dayPercent, float sunAngle, Matrix4x4 viewProj, Matrix4x4 modelView) {
        const float sunDistance = 96f;
        const float sunSize = 8f;
        const float moonSize = sunSize * 0.75f;

        var mat = Game.graphics.modelView;
        mat.push();

        float sunElevation = world.getSunElevation(world.worldTick);
        float sunIntensity = MathF.Max(0, (sunElevation + (MathF.PI / 6f)) / (MathF.PI / 6f));
        float moonIntensity = MathF.Max(0, -(sunElevation - (MathF.PI / 6f)) / (MathF.PI / 6f));

        // Sunny
        Game.graphics.tex(0, Game.textures.sunTexture);
        mat.rotate(Meth.rad2deg(sunAngle), 0, 0, 1);

        idt.setMVP(mat.top * viewProj);
        idt.setMV(mat.top * modelView);
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
        idt.setMVP(mat.top * viewProj);
        idt.setMV(mat.top * modelView);
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

    private void renderStars(float dayPercent, Matrix4x4 viewProj, Matrix4x4 modelView) {
        float starAlpha = 0f;

        if (dayPercent >= 0.65f && dayPercent <= 0.9f) {
            //night
            starAlpha = 1.0f;
        }
        else if (dayPercent >= 0.5f && dayPercent < 0.65f) {
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

        var starColour = new Color(1f, 1f, 1f, starAlpha);
        const float starSize = 0.15f;

        float continuousTime = dayPercent * 360;
        var mat = Game.graphics.modelView;

        mat.push();

        mat.rotate(continuousTime, 0.7f, 1f, 0.1f); // tilted celestial axis

        idc.setMVP(mat.top * viewProj);
        idc.setMV(mat.top * modelView);
        idc.enableFog(false);

        idc.begin(PrimitiveType.Quads);

        for (int i = 0; i < starPositions.Length; i++) {
            Vector3 starPos = starPositions[i];
            // create billboard quad that faces the camera (like the sun)
            var toCamera = Vector3.Normalize(-starPos); // direction from star to camera (at origin)
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, toCamera));
            var up = Vector3.Cross(toCamera, right);


            var v1 = starPos + (-right - up) * starSize;
            var v2 = starPos + (right - up) * starSize;
            var v3 = starPos + (right + up) * starSize;
            var v4 = starPos + (-right + up) * starSize;

            // generate flicker
            // so we do it per star
            var time = world.worldTick;

            var hash = XHash.hash(i);

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

            var sc = starColour * (1 - flicker);


            idc.addVertex(new VertexTinted(v1.X, v1.Y, v1.Z, sc));
            idc.addVertex(new VertexTinted(v2.X, v2.Y, v2.Z, sc));
            idc.addVertex(new VertexTinted(v3.X, v3.Y, v3.Z, sc));
            idc.addVertex(new VertexTinted(v4.X, v4.Y, v4.Z, sc));
        }

        idc.end();

        mat.pop();
    }
}