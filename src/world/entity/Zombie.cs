using BlockGame.main;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class Zombie : Mob {
    private const double DETECT_RADIUS = 16.0;
    private const double ATTACK_RANGE = 3.0;
    private const float ATTACK_DAMAGE = 6.0f;
    private const int ATTACK_COOLDOWN = 60;

    private int attackTime;

    protected override bool burnInSunlight => true;

    public Zombie(World world) : base(world, "zombie") {
        tex = Game.textures.zombie;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.3, pos.Y, pos.Z - 0.3,
            pos.X + 0.3, pos.Y + 1.8, pos.Z + 0.3
        );
    }

    public override void AI(double dt) {
        // find and chase nearest player
        var nearestPlayer = findNearestPlayer(DETECT_RADIUS);
        if (nearestPlayer != null) {
            target = nearestPlayer;

            var dist = Vector3D.Distance(position, nearestPlayer.position);
            if (dist < ATTACK_RANGE && attackTime <= 0) {
                attackPlayer();
            }
        }
        
        base.AI(dt);
    }

    private void attackPlayer() {
        world.player.dmg(ATTACK_DAMAGE, position);
        attackTime = ATTACK_COOLDOWN;
    }

    protected override void updateTimers(double dt) {
        base.updateTimers(dt);

        if (attackTime > 0) {
            attackTime--;
        }
    }
}