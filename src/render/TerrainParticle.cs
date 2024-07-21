using Silk.NET.Maths;

namespace BlockGame;

public class TerrainParticle : Particle {

    public TerrainParticle(World world, Vector3D<double> position, string texture, float u, float v, double size, double uvsize, int ttl)
        : base(world, position, texture, u, v, size, uvsize, ttl) {
    }
}