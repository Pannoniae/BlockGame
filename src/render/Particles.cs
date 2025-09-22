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

    public readonly List<Particle> particles = [];
    private readonly InstantDrawTexture drawer = new InstantDrawTexture(1024);

    public Particles(World world) {
        this.world = world;
        drawer.setup();
    }

    public void add(Particle particle) {
        particles.Add(particle);
    }

    public void update(double dt) {
        //Console.Out.WriteLine(particles[0].position);
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
        var currentTexture = "textures/blocks.png";
        Game.graphics.tex(0, Game.textures.blockTexture);

        drawer.begin(PrimitiveType.Triangles);
        drawer.setMVP(Game.camera.getViewMatrix(interp) * Game.camera.getProjectionMatrix());

        var world = this.world;

        foreach (var particle in particles) {
            if (particle.texture != currentTexture) {
                // draw everything!
                drawer.end();

                drawer.begin(PrimitiveType.Triangles);
                currentTexture = particle.texture;
                var tex = Game.textures.get(particle.texture);
                Game.graphics.tex(0, tex);
            }
            // get interp pos
            var pos = Vector3D.Lerp(particle.prevPosition, particle.position, (float)interp);
            var blockPos = pos.toBlockPos();
            var right = Vector3.Cross(Game.camera.up.toVec3(), Game.camera.forward.toVec3());
            var up = Game.camera.up.toVec3();
            var ul = pos.toVec3() - right * particle.size.X / 2 + up * particle.size.Y / 2;
            var ll = pos.toVec3() - right * particle.size.X / 2 - up * particle.size.Y / 2;
            var lr = pos.toVec3() + right * particle.size.X / 2 - up * particle.size.Y / 2;
            var ur = pos.toVec3() + right * particle.size.X / 2 + up * particle.size.Y / 2;
            var skylight = world.getSkyLight(blockPos.X, blockPos.Y, blockPos.Z);
            var blocklight = world.getBlockLight(blockPos.X, blockPos.Y, blockPos.Z);
            
            
            var tint = WorldRenderer.getLightColour(blocklight, skylight);

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
    }
}