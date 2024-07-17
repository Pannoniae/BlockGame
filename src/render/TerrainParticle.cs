using Silk.NET.Maths;

namespace BlockGame;

public class TerrainParticle : Particle {

    public TerrainParticle(Vector3D<double> position, string texture, float u, float v, double size, int ttl) : base(position, texture, u, v, size, ttl) {
    }
}