using BlockGame.logic;
using BlockGame.main;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class DemonEye : Mob {
    private const double DETECT_RADIUS = 24.0;
    private const float ATTACK_DAMAGE = 4.0f;
    private const int ATTACK_COOLDOWN = 40;
    private const double FLIGHT_SPEED = 0.8;
    private const double HOVER_HEIGHT = 8.0; // preferred height above ground

    private int attackTime;
    private Vector3D? flyTarget;
    private int retargetCooldown;

    // don't take fall damage
    protected override bool needsFallDamage => false;

    protected override bool burnInSunlight => true;
    protected override double eyeHeight => 0; // it's a fucking floating eye

    public DemonEye(World world) : base(world, "eye") {
        tex = Game.textures.eye;
        flyMode = true;
        hp = 30;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.25, pos.Y - 0.25, pos.Z - 0.25,
            pos.X + 0.25, pos.Y + 0.25, pos.Z + 0.25
        );
    }

    public override void AI(double dt) {
        updateSunlightBurn();
        
        var nearestPlayer = findNearestPlayer(DETECT_RADIUS);

        if (nearestPlayer != null) {
            target = nearestPlayer;
            flyTowardsTarget(dt);

            var dist = Vector3D.Distance(position, nearestPlayer.position);
            if (dist < reach && attackTime <= 0) {
                attackPlayer();
            }
        } else {
            idleFlight(dt);
        }

        // no base.AI(dt), we don't need pathfinding bullshit
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

    private void attackPlayer() {
        if (target is Player player) {
            player.dmg(ATTACK_DAMAGE, position);
        }
        attackTime = ATTACK_COOLDOWN;
    }

    protected override void updateTimers(double dt) {
        base.updateTimers(dt);

        if (attackTime > 0) {
            attackTime--;
        }
    }

    // disable gravity
    protected override void prePhysics(double dt) {
        AI(dt);

        // apply drag
        velocity *= 0.85;
    }
}