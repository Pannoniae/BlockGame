using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * Base class for projectile entities (arrows, snowballs, grenades, etc.)
 * Consolidates common projectile behavior: physics, collision detection, aging
 */
public abstract class ProjectileEntity : Entity {
    // shared state
    public Entity? owner; // who fired/threw this projectile
    public int age;
    protected const int MAX_AGE = 1200; // 60 seconds

    // subclass configuration - override these
    protected abstract double gravityMultiplier { get; }
    protected abstract double airFriction { get; }
    protected abstract double damage { get; }
    protected abstract bool canCollideWithEntities { get; }

    protected ProjectileEntity(World world, string name) : base(world, name) {
        flags &= ~(EFlag.FRICTION | EFlag.BLOCK_INTERACTION);
    }

    // common capability flags

    protected override void updateGravity(double dt) {
        velocity.Y -= GRAVITY * gravityMultiplier * dt;
    }

    protected override bool shouldContinueUpdate(double dt) {
        age++;

        if (age > MAX_AGE) {
            active = false;
            return false;
        }

        // subclass can add more checks (fuse, stuck timer, etc.)
        return shouldContinueUpdateSubclass(dt);
    }

    // hook for subclass-specific despawn logic
    protected virtual bool shouldContinueUpdateSubclass(double dt) {
        return true;
    }

    protected override void updatePhysics(double dt) {
        updateGravity(dt);

        velocity += accel * dt;
        clamp(dt);

        var from = position;
        var to = position + velocity * dt;
        var tBlock = collide(world, from, to);

        if (canCollideWithEntities) {
            var hit = collideEntities(this, owner, age, from, to, tBlock, out var tHit);
            if (hit != null) {
                position = from + (to - from) * tHit;
                aabb = calcAABB(position);
                onEntityHit(hit);
                return;
            }
        }

        if (tBlock < 1) {
            // stop at the face, not up to a step short of it
            position = from + (to - from) * tBlock;
            aabb = calcAABB(position);
            onBlockHit();
            return;
        }

        position = to;
        velocity *= airFriction;
        aabb = calcAABB(position);
        updateRotation();
    }

    protected override void updateVisual(double dt) {
        base.updateVisual(dt);
        age++;
        spawnTrailParticles();
    }

    // ============ SWEPT COLLISION (snowballs use it too) ============

    private const double SWEEP_STEP = 0.25;

    public static double collide(World world, Vector3D from, Vector3D to) {
        var d = to - from;
        var steps = int.Max(1, (int)double.Ceiling(d.Length() / SWEEP_STEP));

        for (int i = 1; i <= steps; i++) {
            var p = (from + d * ((double)i / steps)).toBlockPos();
            if (world.inWorld(p.X, p.Y, p.Z) && Block.collision[world.getBlock(p)]) {
                return (double)(i - 1) / steps;
            }
        }

        return 1;
    }
    public static Entity? collideEntities(Entity self, Entity? owner, int age, Vector3D from, Vector3D to, double maxT, out double tHit) {
        var r = (self.aabb.x1 - self.aabb.x0) * 0.5;
        var lo = Vector3D.Min(from, to) - new Vector3D(r + 1);
        var hi = Vector3D.Max(from, to) + new Vector3D(r + 1);

        var list = nearby;
        self.world.getEntitiesInBox(list, new AABB(lo, hi));

        Entity? best = null;
        tHit = maxT;
        var d = to - from;

        foreach (var e in list) {
            // plot armour for the shooter
            if (e == self || e.dead || e.GetType() == self.GetType() || (e == owner && age < 8)) {
                continue;
            }

            if (segment(e.aabb, from, d, r, out var t) && t <= tHit) {
                tHit = t;
                best = e;
            }
        }

        return best;
    }

    private static bool segment(in AABB b, Vector3D from, Vector3D d, double r, out double t) {
        double t0 = 0, t1 = 1;
        var hit = slab(from.X, d.X, b.x0 - r, b.x1 + r, ref t0, ref t1) &&
                  slab(from.Y, d.Y, b.y0 - r, b.y1 + r, ref t0, ref t1) &&
                  slab(from.Z, d.Z, b.z0 - r, b.z1 + r, ref t0, ref t1);
        t = t0;
        return hit;
    }

    private static bool slab(double o, double v, double min, double max, ref double t0, ref double t1) {
        if (double.Abs(v) < 1e-12) {
            return o >= min && o <= max;
        }

        var inv = 1 / v;
        var a = (min - o) * inv;
        var b = (max - o) * inv;
        if (a > b) {
            (a, b) = (b, a);
        }

        t0 = double.Max(t0, a);
        t1 = double.Min(t1, b);
        return t0 <= t1;
    }

    // hooks for subclass behavior
    protected virtual void onEntityHit(Entity entity) {
        entity.dmg(damage, this);
        active = false; // default: despawn on hit
    }

    protected virtual void onBlockHit() {
        active = false; // default: despawn on block collision
    }

    protected virtual void spawnTrailParticles() {
        // optional override for particle trails
    }

    protected virtual void updateRotation() {
        // optional override for velocity-aligned rotation (arrows)
    }

    // NBT serialization
    protected override void readx(NBTCompound data) {
        if (data.has("age")) {
            age = data.getInt("age");
        }
    }

    public override void writex(NBTCompound data) {
        data.addInt("age", age);
    }
}
