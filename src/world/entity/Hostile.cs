using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * Base class for hostile mobs that attack players.
 * Consolidates attack detection, cooldown management, and damage dealing.
 */
public abstract class Hostile : Mob {
    protected int attackTime;

    // subclass configuration
    protected virtual double detectRadius => 16.0;
    protected virtual float attackDamage => 6.0f;
    protected virtual int attackCooldown => 60;

    // subclasses can override this to disable Mob pathfinding (e.g., flying mobs)
    protected virtual bool usePathfinding => true;

    protected Hostile(World world, string name) : base(world, name) {
    }

    public override bool hostile => true;

    public override void AI(double dt) {
        // find nearest player
        Entity? nearestPlayer = target ?? findNearestPlayer(detectRadius, out _);

        if (nearestPlayer != null) {
            target = nearestPlayer;
            var dist = Vector3D.Distance(position, nearestPlayer.position);

            onTargetDetected(dt, nearestPlayer, dist);

            // check attack conditions
            if (dist < reach && attackTime <= 0 && hasLineOfSight(nearestPlayer)) {
                attack(nearestPlayer);
            }
        }
        else {
            target = null;
            onNoTargetDetected(dt);
        }

        // call base pathfinding (only if ground-based)
        if (usePathfinding) {
            base.AI(dt);
        }
    }

    protected virtual void attack(Entity target) {
        target.dmg(attackDamage, position);
        attackTime = attackCooldown;
    }

    protected virtual void onTargetDetected(double dt, Entity target, double distance) {

    }

    protected virtual void onNoTargetDetected(double dt) {

    }

    protected override void updateTimers(double dt) {
        base.updateTimers(dt);

        if (attackTime > 0) {
            attackTime--;
        }
    }
}
