using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using FontStashSharp;
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

    /** if true, particle.render() is called instead of default billboard shit */
    public bool customRender = false;


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
    
    public virtual void render(double interp) {
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

public class DamageNumber : Particle {
    public readonly string text;
    public readonly double damage;
    public float alpha = 1.0f;

    public DamageNumber(World world, Vector3D position, double damage)
        : base(world, position) {
        this.damage = damage;
        text = ((int)damage).ToString();
        maxAge = 60; // 3 sec
        noGravity = true;
        customRender = true;

        // drift upward
        velocity = new Vector3D(
            (Game.clientRandom.NextSingle() - 0.5) * 0.3,
            1.5,
            (Game.clientRandom.NextSingle() - 0.5) * 0.3
        );
    }

    public override void update(double dt) {
        prevPosition = position;
        position += velocity * dt;

        velocity.Y *= 0.96;
        velocity.X *= 0.92;
        velocity.Z *= 0.92;

        // fade out
        float ageRatio = age / (float)maxAge;
        if (ageRatio > 0.66f) {
            alpha = 1.0f - (ageRatio - 0.66f) / 0.34f;
        }
    }

    public override void render(double interp) {
        var pos = Vector3D.Lerp(prevPosition, position, interp);
        var font = Game.fontLoader.fontSystem.GetFont(16);
        var renderer = Game.fontLoader.renderer3D;
        var textBounds = font.MeasureString(text);

        var mat = Game.graphics.model;
        mat.push();
        mat.loadIdentity();

        mat.translate((float)pos.X, (float)pos.Y, (float)pos.Z);

        var fwd = Game.camera.forward(interp).toVec3();
        var up = Game.camera.up(interp).toVec3();
        var right = Vector3.Normalize(Vector3.Cross(up, fwd));
        up = Vector3.Normalize(Vector3.Cross(fwd, right)); // re-orthogonalize

        var bb = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            -fwd.X, -fwd.Y, -fwd.Z, 0,
            0, 0, 0, 1
        );

        mat.multiply(bb);

        // scale text (font is big, world is small)
        const float scale = 1 / 64f;
        mat.scale(scale, -scale, scale); // flip text the right side up!

        var textPos = new Vector2(-textBounds.X / 2, 0);

        // rn it scales from 0 to 30, maybe we will need a better formula later
        float t = (float)double.Min(damage / 30f, 1f);
        t *= t;
        float hue = 60f * (1f - t); // 60° -> 0°

        // S=1, V=1
        float c1 = hue / 60f;
        float x = 1f - Math.Abs(c1 % 2f - 1f);
        const int r = 255;
        int g = (int)(x * 255);
        const int b = 0;

        var c = new FSColor(r, g, b, (int)(alpha * 255));
        var worldMatrix = mat.top;

        // on purpose!! we already begun in the renderloop
        renderer.set(interp);
        font.DrawText(renderer, text, textPos, c, ref worldMatrix, layerDepth: 0f);
        renderer.end();
        renderer.begin();

        mat.pop();
    }
}