using BlockGame.main;
using BlockGame.util;
using BlockGame.world.item;
using Molten.DoublePrecision;
namespace BlockGame.world.entity;

public class BigEye : Hostile {
    protected override double detectRadius => 24.0;
    protected override float attackDamage => 4.0f;
    protected override int attackCooldown => 40;
    protected override bool usePathfinding => false; // handles own flight movement

    private const double FLIGHT_SPEED = 1;
    private const double HOVER_HEIGHT = 18.0; // preferred height above ground

    private Vector3D? flyTarget;
    private int retargetCooldown;

    // don't take fall damage
    protected override bool needsFallDamage => false;

    public override bool burnInSunlight => true;
    public override double eyeHeight => 0; // it's a fucking floating eye

    public BigEye(World world) : base(world, "BigEye") {
        tex = "textures/entity/bigeye.png";
        flyMode = true;
        hp = 100;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.25, pos.Y - 0.25, pos.Z - 0.25,
            pos.X + 0.25, pos.Y + 0.25, pos.Z + 0.25
        );
    }

    public override void AI(double dt) {
        spawnTicks++;
        updateSunlightBurn();

        // call base for attack detection/management
        base.AI(dt);
    }

    protected override void onTargetDetected(double dt, Entity target, double distance) {
        flyTowardsTarget(dt);
    }

    protected override void onNoTargetDetected(double dt) {
        idleFlight(dt);
    }

    private void flyTowardsTarget(double dt) {
        if (target == null) return;

        var dir = target.position - position;
        dir.Y += 1.0;

        if (dir.Length() > 0.01) {
            dir = Vector3D.Normalize(dir);

            velocity.X += dir.X * FLIGHT_SPEED;
            velocity.Y += dir.Y * FLIGHT_SPEED;
            velocity.Z += dir.Z * FLIGHT_SPEED;

            // look at target
            var yaw = Meth.rad2deg((float)Math.Atan2(dir.X, dir.Z));
            var pitch = Meth.rad2deg((float)Math.Atan2(-dir.Y, Math.Sqrt(dir.X * dir.X + dir.Z * dir.Z)));

            rotation.Y = yaw;
            rotation.X = pitch;
        }
    }

    private void idleFlight(double dt) {
        retargetCooldown--;

        // pick new target
        if (flyTarget == null || retargetCooldown <= 0) {
            var angle = Game.random.NextSingle(0, MathF.PI * 2);
            var dist = Game.random.NextSingle(8f, 16f);
            var tx = position.X + MathF.Cos(angle) * dist;
            var tz = position.Z + MathF.Sin(angle) * dist;

            // try to maintain hover height above ground
            var groundY = findGroundBelow((int)tx, (int)tz);
            var ty = groundY + HOVER_HEIGHT + Game.random.NextSingle(-2f, 2f);

            flyTarget = new Vector3D(tx, ty, tz);
            retargetCooldown = 60 + Game.random.Next(120);
        }

        // fly towards target
        if (flyTarget != null) {
            var dir = flyTarget.Value - position;
            var dist = dir.Length();

            // reached target?
            if (dist < 2.0) {
                flyTarget = null;
                retargetCooldown = 20;
                return;
            }

            dir = Vector3D.Normalize(dir);

            velocity.X += dir.X * FLIGHT_SPEED * 0.6;
            velocity.Y += dir.Y * FLIGHT_SPEED * 0.6;
            velocity.Z += dir.Z * FLIGHT_SPEED * 0.6;

            var yaw = Meth.rad2deg((float)Math.Atan2(dir.X, dir.Z));
            rotation.Y = yaw;
        }
    }

    private double findGroundBelow(int x, int z) {
        // scan downwards to find ground level
        for (int y = (int)position.Y; y > 0; y--) {
            if (world.getBlock(x, y, z) != 0) {
                return y + 1;
            }
        }

        return 64;
    }

    // disable gravity
    protected override void prePhysics(double dt) {
        AI(dt);

        // apply drag
        velocity *= 0.85;
    }

    public override void getDrop(List<ItemStack> drops) {
        //if (id == BigEye.id && Game.random.Next(20) == 0) {
        //    drops.Add(new ItemStack(LW_BOOTS.item, 1, 0));
        //}
    }
}
