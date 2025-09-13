using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame;

public class Particle {
    
    public World world;

    /** current position */
    public Vector3D position;

    /** previous frame position for interpolation */
    public Vector3D prevPosition;

    /** current velocity */
    public Vector3D velocity;

    /** whether particle is touching ground */
    public bool onGround;

    /** whether particle is alive */
    public bool active;

    /** time-to-live in ticks */
    public int ttl;

    /** texture path */
    public string texture;

    /** texture U coordinate */
    public float u;

    /** texture V coordinate */
    public float v;

    /** particle size in world units */
    public double size;

    /** texture size on particle (UV scale) */
    public double uvsize;
    

    /** collision detection cache */
    private readonly List<AABB> collisionTargets = [];
    private readonly List<Vector3I> collisionTargetsList = [];
    private static readonly List<AABB> AABBList = [];

    public Particle(World world, Vector3D position) {
        this.world = world;
        this.position = position;
        this.prevPosition = position;
        active = true;
        velocity = Vector3D.Zero;
    }

    private AABB calcAABB(Vector3D pos) {
        return new AABB(pos - new Vector3D(size / 2), pos + new Vector3D(size / 2));
    }

    public void update(double dt) {
        prevPosition = position;
        if (!active) {
            return;
        }

        // gravity
        velocity.Y -= 6 * dt;
        ttl -= 1;

        // apply friction
        velocity.X *= Constants.verticalFriction;
        velocity.Z *= Constants.verticalFriction;
        velocity.Y *= Constants.verticalFriction;
        if (onGround) {
            velocity.X *= Constants.airFriction;
            velocity.Z *= Constants.airFriction;
        }

        // collect collision targets
        var blockPos = position.toBlockPos();
        collisionTargets.Clear();

        World.getBlocksInBox(collisionTargetsList, blockPos + new Vector3I(-1, -1, -1),
            blockPos + new Vector3I(1, 1, 1));
        foreach (var neighbour in collisionTargetsList) {
            var block = world.getBlock(neighbour);
            world.getAABBsCollision(AABBList, neighbour.X, neighbour.Y, neighbour.Z);

            foreach (AABB aabb in AABBList) {
                collisionTargets.Add(aabb);
            }
        }

        // Y axis collision
        position.Y += velocity.Y * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabb = calcAABB(position);
            if (AABB.isCollision(aabb, blockAABB)) {
                if (velocity.Y > 0 && aabb.y1 >= blockAABB.y0) {
                    position.Y += blockAABB.y0 - aabb.y1;
                    velocity.Y = 0;
                }
                else if (velocity.Y < 0 && aabb.y0 <= blockAABB.y1) {
                    position.Y += blockAABB.y1 - aabb.y0;
                    velocity.Y = 0;
                }
            }
        }

        // X axis collision
        position.X += velocity.X * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabb = calcAABB(position);
            if (AABB.isCollision(aabb, blockAABB)) {
                if (velocity.X > 0 && aabb.x1 >= blockAABB.x0) {
                    position.X += blockAABB.x0 - aabb.x1;
                }
                else if (velocity.X < 0 && aabb.x0 <= blockAABB.x1) {
                    position.X += blockAABB.x1 - aabb.x0;
                }
                velocity.X = 0;
            }
        }

        // Z axis collision
        position.Z += velocity.Z * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabb = calcAABB(position);
            if (AABB.isCollision(aabb, blockAABB)) {
                if (velocity.Z > 0 && aabb.z1 >= blockAABB.z0) {
                    position.Z += blockAABB.z0 - aabb.z1;
                }
                else if (velocity.Z < 0 && aabb.z0 <= blockAABB.z1) {
                    position.Z += blockAABB.z1 - aabb.z0;
                }
                velocity.Z = 0;
            }
        }

        // ground check
        var groundCheck = calcAABB(new Vector3D(position.X, position.Y - Constants.epsilonGroundCheck, position.Z));
        onGround = false;
        foreach (var blockAABB in collisionTargets) {
            if (AABB.isCollision(blockAABB, groundCheck)) {
                onGround = true;
                break;
            }
        }
    }

    public void reset() {
        active = false;
    }
}

public class FlameParticle : Particle {
    public FlameParticle(World world, Vector3D position, Vector3D velocity)
        : base(world, position) {
        texture = "textures/blocks.png";
        u = 0;
        v = 0;
        size = 1;
        uvsize = 1 / 16f * size;
        ttl = 4;
    }
}