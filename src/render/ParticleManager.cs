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
        for (int i = 0; i < 20; i++) {
            add(new TerrainParticle(new Vector3D<double>(0, 100, 0), "textures/blocks.png", 0, 0, 0.5, 9_000_000));
        }
    }

    public void add(Particle particle) {
        particles.Add(particle);
    }

    public void update(double dt) {
        //Console.Out.WriteLine(particles[0].position);
        for (var i = 0; i < particles.Count; i++) {
            var particle = particles[i];
            particle.update(dt);

            if (!particle.active) {
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

            var billboard = Matrix4F.BillboardLH(particle.position.toVec3F(),
                world.player.camera.position.toVec3FM(), world.player.camera.up.toVec3FM(), world.player.camera.forward.toVec3FM());
            InstantDraw.instantShader.setUniform(InstantDraw.uMVP, world.player.camera.getViewMatrix(interp) * world.player.camera.getProjectionMatrix());
            var vert = new InstantVertex((float)particle.position.X, (float)particle.position.Y, (float)particle.position.Z,
                (Half)particle.u, (Half)particle.v, 255, 255, 255, 255);
            drawer.addVertex(vert);
            vert = new InstantVertex((float)particle.position.X, (float)(particle.position.Y - particle.size), (float)particle.position.Z,
                (Half)particle.u, (Half)(particle.v + particle.size), 255, 255, 255, 255);
            drawer.addVertex(vert);
            vert = new InstantVertex((float)(particle.position.X + particle.size), (float)(particle.position.Y - particle.size), (float)particle.position.Z,
                (Half)(particle.u + particle.size), (Half)(particle.v + particle.size), 255, 255, 255, 255);
            drawer.addVertex(vert);
            //vert = new InstantVertex((float)(particle.position.X + particle.size), (float)particle.position.Y, (float)particle.position.Z,
            //    (Half)(particle.u + particle.size), (Half)particle.v, 255, 255, 255, 255);
            //drawer.addVertex(vert);
        }

        drawer.finish();
    }
}