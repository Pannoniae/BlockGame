using System.Numerics;
using BlockGame.GL;
using BlockGame.util;
using Molten.DoublePrecision;
using Silk.NET.OpenGL;

namespace BlockGame;

public class ParticleManager {

    private readonly World world;

    private readonly List<Particle> particles = [];
    private readonly InstantDrawTexture drawer = new InstantDrawTexture(1024);

    public ParticleManager(World world) {
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
            particle.update(dt);

            if (!particle.active || particle.ttl <= 0) {
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

        var world = this.world;
        var renderer = Game.renderer!;

        foreach (var particle in particles) {
            if (particle.texture != currentTexture) {
                Game.textures.get(particle.texture);
                var tex = Game.textures.get(particle.texture);
                Game.graphics.tex(0, tex);
            }
            drawer.setMVP(world.player.camera.getViewMatrix(interp) * world.player.camera.getProjectionMatrix());
            // get interp pos
            var pos = Vector3D.Lerp(particle.prevPosition, particle.position, (float)interp);
            var blockPos = pos.toBlockPos();
            var right = Vector3.Cross(world.player.camera.up.toVec3(), world.player.camera.forward.toVec3());
            var up = world.player.camera.up.toVec3();
            var ul = pos.toVec3() - right * (float)particle.size / 2 + up * (float)particle.size / 2;
            var ll = pos.toVec3() - right * (float)particle.size / 2 - up * (float)particle.size / 2;
            var lr = pos.toVec3() + right * (float)particle.size / 2 - up * (float)particle.size / 2;
            var ur = pos.toVec3() + right * (float)particle.size / 2 + up * (float)particle.size / 2;
            var skylight = world.getSkyLight(blockPos.X, blockPos.Y, blockPos.Z);
            var blocklight = world.getBlockLight(blockPos.X, blockPos.Y, blockPos.Z);
            
            var tint = WorldRenderer.getLightColour(skylight, blocklight);

            var vert = new BlockVertexTinted(ul.X, ul.Y, ul.Z,
                (Half)particle.u, (Half)particle.v, tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new BlockVertexTinted(ll.X, ll.Y, ll.Z,
                (Half)particle.u, (Half)(particle.v + particle.uvsize), tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new BlockVertexTinted(lr.X, lr.Y, lr.Z,
                (Half)(particle.u + particle.uvsize), (Half)(particle.v + particle.uvsize), tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);

            vert = new BlockVertexTinted(ul.X, ul.Y, ul.Z,
                (Half)particle.u, (Half)particle.v, tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new BlockVertexTinted(lr.X, lr.Y, lr.Z,
                (Half)(particle.u + particle.uvsize), (Half)(particle.v + particle.uvsize), tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new BlockVertexTinted(ur.X, ur.Y, ur.Z,
                (Half)(particle.u + particle.uvsize), (Half)particle.v, tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);

        }

        drawer.end();
    }
}