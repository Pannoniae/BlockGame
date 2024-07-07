using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class ParticleManager {
    private Particle[] particles;

    public ParticleManager(int maxParticles) {
        particles = new Particle[maxParticles];
    }

    public void update(double dt) {
        for (int i = 0; i < particles.Length; i++) {
            if (particles[i] != null) {
                particles[i].update(dt);
                if (particles[i].ttl <= 0) {
                    particles[i].active = false;
                }
            }
        }
    }

    public void render(double interp) {
        for (int i = 0; i < particles.Length; i++) {
            if (particles[i].active) {
                particles[i].render(interp);
            }
        }
    }

    // TODO implement pooling
    public Particle newParticle(Vector3D<double> position, Color4b color, double size, int ttl) {
        return new Particle(position, color, size, ttl);
    }

    public void addParticle(Particle particle) {
        for (int i = 0; i < particles.Length; i++) {
            if (!particles[i].active) {
                particles[i] = particle;
                return;
            }
        }
    }

    public void clear() {
        for (int i = 0; i < particles.Length; i++) {
            particles[i].reset();
        }
    }
}