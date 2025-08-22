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

    public Particle(World world, Vector3D position, string texture, float u, float v, double size, double uvsize, int ttl) {
        this.world = world;
        this.position = position;
        this.prevPosition = position;
        this.texture = texture;
        this.u = u;
        this.v = v;
        this.size = size;
        this.uvsize = uvsize;
        this.ttl = ttl;
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
                if (velocity.Y > 0 && aabb.maxY >= blockAABB.minY) {
                    position.Y += blockAABB.minY - aabb.maxY;
                    velocity.Y = 0;
                }
                else if (velocity.Y < 0 && aabb.minY <= blockAABB.maxY) {
                    position.Y += blockAABB.maxY - aabb.minY;
                    velocity.Y = 0;
                }
            }
        }

        // X axis collision
        position.X += velocity.X * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabb = calcAABB(position);
            if (AABB.isCollision(aabb, blockAABB)) {
                if (velocity.X > 0 && aabb.maxX >= blockAABB.minX) {
                    position.X += blockAABB.minX - aabb.maxX;
                }
                else if (velocity.X < 0 && aabb.minX <= blockAABB.maxX) {
                    position.X += blockAABB.maxX - aabb.minX;
                }
                velocity.X = 0;
            }
        }

        // Z axis collision
        position.Z += velocity.Z * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabb = calcAABB(position);
            if (AABB.isCollision(aabb, blockAABB)) {
                if (velocity.Z > 0 && aabb.maxZ >= blockAABB.minZ) {
                    position.Z += blockAABB.minZ - aabb.maxZ;
                }
                else if (velocity.Z < 0 && aabb.minZ <= blockAABB.maxZ) {
                    position.Z += blockAABB.maxZ - aabb.minZ;
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