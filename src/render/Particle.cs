using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.render;

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
    public bool active = true;

    /** elapsed time this particle lived */
    public int age;

    /** maximum age in ticks */
    public int maxAge;

    /** texture path */
    public string texture = null!;

    /** texture U coordinate */
    public float u;

    /** texture V coordinate */
    public float v;

    /** particle size in world units */
    public Vector2 size;

    /** texture size on particle (UV scale) */
    public Vector2 uvsize;

    public bool noGravity;


    /** collision detection cache */
    private readonly List<AABB> collisionTargets = [];

    private readonly List<Vector3I> collisionTargetsList = [];
    private static readonly List<AABB> AABBList = [];

    public Particle(World world, Vector3D position) {
        this.world = world;
        this.position = position;
        this.prevPosition = position;
        velocity = Vector3D.Zero;
    }

    /**
     * BB stands for basic bitch
     */
    public static Vector3 bbMotion() {
        return new Vector3(
            (Game.clientRandom.NextSingle() - 0.5f) * 0.2f,
            (Game.clientRandom.NextSingle() - 0.5f) * 0.2f,
            (Game.clientRandom.NextSingle() - 0.5f) * 0.2f
        );
    }

    /**
     * ABB stands for...
     */
    public static Vector3 abbMotion(Vector3 direction) {
        var motion = bbMotion() + direction;
        var s = Game.clientRandom.NextSingle();
        s *= s;
        var speed = (s + 1);
        motion *= speed;
        motion.Y += 0.15f;
        return motion;
    }

    private AABB calcAABB(Vector3D pos) {
        return new AABB(pos - new Vector3D(size.X / 2), pos + new Vector3D(size.Y / 2));
    }

    public virtual void update(double dt) {
        prevPosition = position;

        // gravity
        if (!noGravity) {
            velocity.Y -= 6 * dt;
        }

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
    private Vector2 ssize;

    public FlameParticle(World world, Vector3D position)
        : base(world, position) {
        size = new Vector2(3 / 24f, 6 / 24f);
        ssize = size;
        maxAge = (int)(12f / (Game.clientRandom.NextSingle() + 0.25f) + 5f) * 4;
        noGravity = true;


        // texture maths
        texture = "textures/particle.png";
        u = UVPair.texCoords(Game.textures.particleTex, 0, 10).X;
        v = UVPair.texCoords(Game.textures.particleTex, 0, 10).Y;

        uvsize = UVPair.texCoords(Game.textures.particleTex, 3, 6);
    }

    public override void update(double dt) {
        // shrink
        const float f = 0.16f;
        // TODO COMMENT THIS BACK IN WHEN READY
        size = ssize * (1 - (age / (float)maxAge) * f);

        // change texture frame
        int frame = (int)(age / (double)maxAge * 4);
        u = UVPair.texCoords(Game.textures.particleTex, frame * 4, 10).X;
        v = UVPair.texCoords(Game.textures.particleTex, frame * 4, 10).Y;
    }
}