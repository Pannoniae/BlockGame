using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame;

public class Particle : Entity {

    /// <summary>
    /// The texture coordinates of the particle.
    /// </summary>
    public float u;

    /// <summary>
    /// The texture coordinates of the particle.
    /// </summary>
    public float v;

    /// <summary>
    /// The texture the particle uses.
    /// </summary>
    public string texture;

    /// <summary>
    /// The size of the particle. (world coords)
    /// </summary>
    public double size;

    /// <summary>
    /// The size of the texture on the particle.
    /// </summary>
    public double uvsize;

    /// <summary>
    /// The time-to-live of the particle in ticks.
    /// </summary>
    public int ttl;

    /// <summary>
    /// Is this particle valid?
    /// </summary>
    public bool active;

    public Particle(World world, Vector3D position, string texture, float u, float v, double size, double uvsize, int ttl) : base(world) {
        this.position = position;
        this.texture = texture;
        this.u = u;
        this.v = v;
        this.size = size;
        this.uvsize = uvsize;
        this.ttl = ttl;
        active = true;
    }

    protected override AABB calcAABB(Vector3D pos) {
        return new AABB(pos - new Vector3D(size / 2), pos + new Vector3D(size / 2));
    }

    public virtual void update(double dt) {
        prevPosition = position;
        if (active) {
            // gravity
            velocity.Y -= 6 * dt;
            ttl -= 1;
        }
        // cursed logic?
        velocity.X *= Constants.verticalFriction;
        velocity.Z *= Constants.verticalFriction;
        velocity.Y *= Constants.verticalFriction;
        if (onGround) {
            velocity.X *= Constants.airFriction;
            velocity.Z *= Constants.airFriction;
        }
        var blockPos = position.toBlockPos();
        collisionTargets.Clear();
        var currentAABB = world.getAABB(blockPos.X, blockPos.Y, blockPos.Z, world.getBlock(blockPos));
        if (currentAABB != null) {
            collisionTargets.Add(currentAABB.Value);
        }
        foreach (var neighbour in world.getBlocksInBox(blockPos + new Vector3I(-1, -1, -1), blockPos + new Vector3I(1, 1, 1))) {
            var block = world.getBlock(neighbour);
            var blockAABB = world.getAABB(neighbour.X, neighbour.Y, neighbour.Z, block);
            if (blockAABB == null) {
                continue;
            }

            collisionTargets.Add(blockAABB.Value);
        }

        // Y axis resolution
        position.Y += velocity.Y * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabbY = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            if (AABB.isCollision(aabbY, blockAABB)) {
                // left side
                if (velocity.Y > 0 && aabbY.maxY >= blockAABB.minY) {
                    var diff = blockAABB.minY - aabbY.maxY;
                    position.Y += diff;
                    velocity.Y = 0;
                }

                else if (velocity.Y < 0 && aabbY.minY <= blockAABB.maxY) {
                    var diff = blockAABB.maxY - aabbY.minY;
                    position.Y += diff;
                    velocity.Y = 0;
                }
            }
        }


        // X axis resolution
        position.X += velocity.X * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabbX = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            var sneakaabbX = calcAABB(new Vector3D(position.X, position.Y - 0.1, position.Z));
            if (AABB.isCollision(aabbX, blockAABB)) {
                collisionXThisFrame = true;
                // left side
                if (velocity.X > 0 && aabbX.maxX >= blockAABB.minX) {
                    var diff = blockAABB.minX - aabbX.maxX;
                    position.X += diff;
                }

                else if (velocity.X < 0 && aabbX.minX <= blockAABB.maxX) {
                    var diff = blockAABB.maxX - aabbX.minX;
                    position.X += diff;
                }
            }
        }

        position.Z += velocity.Z * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabbZ = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            if (AABB.isCollision(aabbZ, blockAABB)) {
                collisionZThisFrame = true;
                if (velocity.Z > 0 && aabbZ.maxZ >= blockAABB.minZ) {
                    var diff = blockAABB.minZ - aabbZ.maxZ;
                    position.Z += diff;
                }

                else if (velocity.Z < 0 && aabbZ.minZ <= blockAABB.maxZ) {
                    var diff = blockAABB.maxZ - aabbZ.minZ;
                    position.Z += diff;
                }
            }
        }
        var groundCheck = calcAABB(new Vector3D(position.X, position.Y - Constants.epsilonGroundCheck, position.Z));
        onGround = false;
        foreach (var blockAABB in collisionTargets) {
            if (AABB.isCollision(blockAABB, groundCheck)) {
                onGround = true;
            }
        }
    }

    public void reset() {
        active = false;
    }
}