using System.Numerics;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.block;

public partial class Block {
    public virtual void shatter(World world, int x, int y, int z) {
        UVPair uv;

        if (model == null || model.faces.Length == 0) {
            // no model, no particles

            // unless there's textures!

            // UNLESS it's custom texture
            var custom = renderType[id] == RenderType.CUSTOM || renderType[id] == RenderType.CUBE_DYNTEXTURE;
            if (!custom && (uvs == null || uvs.Length == 0)) {
                return;
            }
        }

        var factor = 1f / particleCount;
        for (var x1 = 0; x1 < particleCount; x1++) {
            for (var y1 = 0; y1 < particleCount; y1++) {
                for (var z1 = 0; z1 < particleCount; z1++) {
                    var particleX = x + (x1 + 0.5f) * factor + (Game.clientRandom.NextSingle() - 0.5f) * 0.15f;
                    var particleY = y + (y1 + 0.5f) * factor + (Game.clientRandom.NextSingle() - 0.5f) * 0.15f;
                    var particleZ = z + (z1 + 0.5f) * factor + (Game.clientRandom.NextSingle() - 0.5f) * 0.15f;
                    var particlePosition = new Vector3D(particleX, particleY, particleZ);

                    var size = Game.clientRandom.NextSingle() * 0.1f + 0.05f;
                    var ttl = (int)(5f / (Game.clientRandom.NextSingle() + 0.05f));

                    switch (renderType[id]) {
                        // if custom texture, get that
                        case RenderType.CUBE_DYNTEXTURE:
                        case RenderType.CUSTOM:
                            var meta = world.getBlockMetadata(x, y, z);
                            uv = getTexture(0, meta);
                            break;
                        case RenderType.MODEL:
                            uv = uvs[Game.clientRandom.Next(0, uvs.Length)];
                            break;
                        default:
                            // no model, just textures
                            uv = uvs[0];
                            break;
                    }

                    float u = uv.u + Game.clientRandom.NextSingle() * 0.75f;
                    float v = uv.v + Game.clientRandom.NextSingle() * 0.75f;
                    Vector2 us = UVPair.texCoords(u, v);

                    // break particles: explode outward from center, biased upward
                    var dx = (particleX - x - 0.5f);
                    var dy = (particleY - y - 0.5f);
                    var dz = (particleZ - z - 0.5f);

                    var motion = Particle.abbMotion(new Vector3(dx * 2, dy * 2 + 0.6f, dz * 2));

                    var particle = new Particle(
                        world,
                        particlePosition);
                    particle.texture = Game.textures.blockTexture;
                    particle.u = us.X;
                    particle.v = us.Y;
                    particle.size = new Vector2(size);
                    particle.uvsize = new Vector2(1 / 16f * size);
                    particle.maxAge = ttl;
                    world.particles.add(particle);

                    particle.velocity = motion.toVec3D();
                }
            }
        }
    }

    /** mining particles for when block is being broken */
    public virtual void shatter(World world, int x, int y, int z, RawDirection hitFace, AABB? hitAABB = null) {
        UVPair uv;

        if (model == null || model.faces.Length == 0) {
            var custom = renderType[id] == RenderType.CUSTOM || renderType[id] == RenderType.CUBE_DYNTEXTURE;
            if (!custom && (uvs == null || uvs.Length == 0)) {
                return;
            }
        }

        // spawn fewer particles for mining (2-4 particles)
        var count = Game.clientRandom.Next(2, 5);

        // use hit AABB if provided, otherwise default to full block
        var bbn = hitAABB?.min ?? new Vector3D(x, y, z);
        var bbx = hitAABB?.max ?? new Vector3D(x + 1, y + 1, z + 1);

        for (var i = 0; i < count; i++) {
            // spawn particles just outside the hit face to avoid collision with block
            float px = 0;
            float py = 0;
            float pz = 0;

            const float offset = 0.08f;

            // constrain particles to the actual AABB bounds
            switch (hitFace) {
                case RawDirection.UP:
                    px = (float)(bbn.X + Game.clientRandom.NextSingle() * (bbx.X - bbn.X));
                    py = (float)bbx.Y + offset;
                    pz = (float)(bbn.Z + Game.clientRandom.NextSingle() * (bbx.Z - bbn.Z));
                    break;
                case RawDirection.DOWN:
                    px = (float)(bbn.X + Game.clientRandom.NextSingle() * (bbx.X - bbn.X));
                    py = (float)bbn.Y - offset;
                    pz = (float)(bbn.Z + Game.clientRandom.NextSingle() * (bbx.Z - bbn.Z));
                    break;
                case RawDirection.NORTH:
                    px = (float)(bbn.X + Game.clientRandom.NextSingle() * (bbx.X - bbn.X));
                    py = (float)(bbn.Y + Game.clientRandom.NextSingle() * (bbx.Y - bbn.Y));
                    pz = (float)bbx.Z + offset;
                    break;
                case RawDirection.SOUTH:
                    px = (float)(bbn.X + Game.clientRandom.NextSingle() * (bbx.X - bbn.X));
                    py = (float)(bbn.Y + Game.clientRandom.NextSingle() * (bbx.Y - bbn.Y));
                    pz = (float)bbn.Z - offset;
                    break;
                case RawDirection.EAST:
                    px = (float)bbx.X + offset;
                    py = (float)(bbn.Y + Game.clientRandom.NextSingle() * (bbx.Y - bbn.Y));
                    pz = (float)(bbn.Z + Game.clientRandom.NextSingle() * (bbx.Z - bbn.Z));
                    break;
                case RawDirection.WEST:
                    px = (float)bbn.X - offset;
                    py = (float)(bbn.Y + Game.clientRandom.NextSingle() * (bbx.Y - bbn.Y));
                    pz = (float)(bbn.Z + Game.clientRandom.NextSingle() * (bbx.Z - bbn.Z));
                    break;
            }

            var particlePosition = new Vector3D(px, py, pz);
            var size = Game.clientRandom.NextSingle() * 0.08f + 0.04f;
            var ttl = (int)(14f / (Game.clientRandom.NextSingle() + 0.05f));

            switch (renderType[id]) {
                case RenderType.CUBE_DYNTEXTURE:
                case RenderType.CUSTOM:
                    var meta = world.getBlockMetadata(x, y, z);
                    uv = getTexture(0, meta);
                    break;
                case RenderType.MODEL:
                    uv = uvs[Game.clientRandom.Next(0, uvs.Length)];
                    break;
                default:
                    uv = uvs[0];
                    break;
            }

            float u = uv.u + Game.clientRandom.NextSingle() * 0.75f;
            float v = uv.v + Game.clientRandom.NextSingle() * 0.75f;
            Vector2 us = UVPair.texCoords(u, v);

            // mining particles: fall down with slight horizontal drift
            var rx = (Game.clientRandom.NextSingle() - 0.5f) * 0.3f;
            var rz = (Game.clientRandom.NextSingle() - 0.5f) * 0.3f;
            var motion = Particle.abbMotion(new Vector3(rx, 0.5f, rz));

            var particle = new Particle(world, particlePosition);
            particle.texture = Game.textures.blockTexture;
            particle.u = us.X;
            particle.v = us.Y;
            particle.size = new Vector2(size);
            particle.uvsize = new Vector2(1 / 16f * size);
            particle.maxAge = ttl;
            world.particles.add(particle);
            particle.velocity = motion.toVec3D();
        }
    }
}