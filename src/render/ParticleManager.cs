using System.Numerics;
using BlockGame.util;
using Molten;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class ParticleManager {

    private readonly World world;

    private readonly List<Particle> particles = [];
    private readonly InstantDraw drawer = new InstantDraw(1024);

    public ParticleManager(World world) {
        this.world = world;
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
        Game.GL.ActiveTexture(TextureUnit.Texture0);
        Game.GL.BindTexture(TextureTarget.Texture2D, Game.textureManager.blockTexture.handle);
        InstantDraw.instantShader.use();


        foreach (var particle in particles) {
            if (particle.texture != currentTexture) {
                Game.textureManager.load(particle.texture, particle.texture);
                var tex = Game.textureManager.get(particle.texture);
                Game.GL.ActiveTexture(TextureUnit.Texture0);
                Game.GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
            }
            InstantDraw.instantShader.setUniform(InstantDraw.uMVP, world.player.camera.getViewMatrix(interp) * world.player.camera.getProjectionMatrix());
            // get interp pos
            var pos = Vector3D.Lerp(particle.prevPosition, particle.position, (float)interp);
            var right = Vector3.Cross(world.player.camera.up, world.player.camera.forward);
            var up = world.player.camera.up;
            var ul = pos.toVec3() - right * (float)particle.size / 2 + up * (float)particle.size / 2;
            var ll = pos.toVec3() - right * (float)particle.size / 2 - up * (float)particle.size / 2;
            var lr = pos.toVec3() + right * (float)particle.size / 2 - up * (float)particle.size / 2;
            var ur = pos.toVec3() + right * (float)particle.size / 2 + up * (float)particle.size / 2;
            var skylight = world.getSkyLight((int)pos.X, (int)pos.Y, (int)pos.Z);
            var blocklight = world.getBlockLight((int)pos.X, (int)pos.Y, (int)pos.Z);
            var tint = Game.textureManager.lightTexture.getPixel(blocklight, skylight);

            var vert = new InstantVertex(ul.X, ul.Y, ul.Z,
                (Half)particle.u, (Half)particle.v, tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new InstantVertex(ll.X, ll.Y, ll.Z,
                (Half)particle.u, (Half)(particle.v + particle.uvsize), tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new InstantVertex(lr.X, lr.Y, lr.Z,
                (Half)(particle.u + particle.uvsize), (Half)(particle.v + particle.uvsize), tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);

            vert = new InstantVertex(ul.X, ul.Y, ul.Z,
                (Half)particle.u, (Half)particle.v, tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new InstantVertex(lr.X, lr.Y, lr.Z,
                (Half)(particle.u + particle.uvsize), (Half)(particle.v + particle.uvsize), tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);
            vert = new InstantVertex(ur.X, ur.Y, ur.Z,
                (Half)(particle.u + particle.uvsize), (Half)particle.v, tint.R, tint.G, tint.B, tint.A);
            drawer.addVertex(vert);

        }

        drawer.finish();
    }
}