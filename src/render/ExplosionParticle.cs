using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using Molten.DoublePrecision;

namespace BlockGame.render;

public class ExplosionParticle : Particle {
    public ExplosionParticle(World world, Vector3D position, Vector3D direction) : base(world, position) {
        size = new Vector2(0.4f, 0.4f);
        maxAge = 70;
        noGravity = false;

        texture = "textures/particle.png";

        var variant = Game.clientRandom.Next(3);
        u = UVPair.texCoords(Game.textures.particleTex, 0, 0).X;
        v = UVPair.texCoords(Game.textures.particleTex, 0, 0).Y;
        uvsize = UVPair.texCoords(Game.textures.particleTex, 3, 3);

        // initial velocity in explosion direction
        var speed = 2.0 + Game.clientRandom.NextDouble() * 4.0;
        velocity = direction * speed;
    }

    public override void update(double dt) {
        prevPosition = position;

        position += velocity * dt;

        if (!noGravity) {
            velocity.Y -= 9.8 * dt;
        }

        // air friction
        velocity *= 0.95;
    }
}
