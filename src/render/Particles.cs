using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using Molten.DoublePrecision;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.render;

public class Particles {

    private readonly World world;

    public readonly XUList<Particle> particles = [];
    private readonly InstantDrawTexture drawer;

    public Particles(World world) {

        // on the server we just don't bother about rendering
        this.world = world;

        if (!Net.mode.isDed()) {
            drawer = new InstantDrawTexture(1024);
            drawer.setup();
        }
    }

    public void add(Particle particle) {
        particles.Add(particle);
    }

    public void update(double dt) {
        for (var i = 0; i < particles.Count; i++) {
            var particle = particles[i];
            
            if (!particle.active) {
                particles.RemoveAt(i);
                i--;
                continue;
            }
            particle.update(dt);
            particle.age++;

            if (particle.age >= particle.maxAge) {
                particles.RemoveAt(i);
                // don't skip the next particle
                i--;
            }
        }
    }

    public void render(double interp) {
        BTexture2D currentTexture = Game.textures.blockTexture;
        Game.graphics.tex(0, currentTexture);
        drawer.batch();

        Matrix4x4 mat = Game.camera.getViewMatrix(interp) * Game.camera.getProjectionMatrix();
        drawer.setMVP(ref mat);

        drawer.begin(PrimitiveType.Triangles);

        var world = this.world;

        foreach (var particle in particles) {
            if (particle.customRender) {
                continue; // skip, will render in second pass
            }

            if (particle.texture != currentTexture) {
                // draw everything!
                drawer.end();

                drawer.begin(PrimitiveType.Triangles);
                currentTexture = particle.texture;
                Game.graphics.tex(0,  particle.texture);
            }
            // get interp pos
            var pos = Vector3D.Lerp(particle.prevPosition, particle.position, (float)interp);
            var blockPos = pos.toBlockPos();
            var right = Vector3.Cross(Game.camera.up(interp).toVec3(), Game.camera.forward(interp).toVec3());
            var up = Game.camera.up(interp).toVec3();
            var ul = pos.toVec3() - right * particle.size.X * 0.5f + up * particle.size.Y * 0.5f;
            var ll = pos.toVec3() - right * particle.size.X * 0.5f - up * particle.size.Y * 0.5f;
            var lr = pos.toVec3() + right * particle.size.X * 0.5f - up * particle.size.Y * 0.5f;
            var ur = pos.toVec3() + right * particle.size.X * 0.5f + up * particle.size.Y * 0.5f;
            var l = world.getLightC(blockPos.X, blockPos.Y, blockPos.Z);


            var tint = WorldRenderer.getLightColour((byte)(l & 0xF), (byte)((l >> 4) & 0xF));

            var vert = new BlockVertexTinted(ul.X, ul.Y, ul.Z,
                (Half)particle.u, (Half)particle.v, tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new BlockVertexTinted(ll.X, ll.Y, ll.Z,
                (Half)particle.u, (Half)(particle.v + particle.uvsize.Y), tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new BlockVertexTinted(lr.X, lr.Y, lr.Z,
                (Half)(particle.u + particle.uvsize.X), (Half)(particle.v + particle.uvsize.Y), tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);

            vert = new BlockVertexTinted(ul.X, ul.Y, ul.Z,
                (Half)particle.u, (Half)particle.v, tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new BlockVertexTinted(lr.X, lr.Y, lr.Z,
                (Half)(particle.u + particle.uvsize.X), (Half)(particle.v + particle.uvsize.Y), tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new BlockVertexTinted(ur.X, ur.Y, ur.Z,
                (Half)(particle.u + particle.uvsize.X), (Half)particle.v, tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);

        }

        drawer.end();

        // second pass: custom render particles
        foreach (var particle in particles) {
            if (particle.customRender) {
                particle.render(interp);
            }
        }
    }
}