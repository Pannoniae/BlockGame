using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.render;

/**
 * Simple particle for arrow trail (3x3px from particles.png).
 */
public class ArrowParticle : Particle {
    public ArrowParticle(World world, Vector3D position) : base(world, position) {
        size = new System.Numerics.Vector2(0.1f, 0.1f);
        maxAge = 10; // short lifetime
        noGravity = true;

        texture = "textures/particle.png";
        u = UVPair.texCoords(Game.textures.particleTex, 0, 0).X;
        v = UVPair.texCoords(Game.textures.particleTex, 0, 0).Y;
        uvsize = UVPair.texCoords(Game.textures.particleTex, 3, 3);

        velocity = new Vector3D(
            (Game.clientRandom.NextSingle() - 0.5) * 0.05,
            (Game.clientRandom.NextSingle() - 0.5) * 0.05,
            (Game.clientRandom.NextSingle() - 0.5) * 0.05
        );
    }

    public override void update(double dt) {
        prevPosition = position;
        position += velocity * dt;

        // fade and shrink
        velocity *= 0.9;
    }
}
