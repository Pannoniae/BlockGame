using Silk.NET.OpenGL;

namespace BlockGame;

public class ParticleManager {
    private readonly List<Particle> particles = [];

    public void add(Particle particle) {
        particles.Add(particle);
    }

    public void update(double dt) {
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

    public void render(double dt, double interp) {

        var currentTexture = "textures/blocks.png";
        Game.GL.ActiveTexture(TextureUnit.Texture0);
        Game.GL.BindTexture(TextureTarget.Texture2D, Game.textureManager.blockTexture.handle);

        foreach (var particle in particles) {
            if (particle.texture != currentTexture) {
                Game.textureManager.load(particle.texture, particle.texture);
                var tex = Game.textureManager.get(particle.texture);
                Game.GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
            }
            particle.render(dt, interp);
        }
    }
}