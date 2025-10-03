using System;
using BlockGame.util;
using BlockGame.world.block;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * Floating items/blocks on the ground.
 */
public class ItemEntity : Entity {
    const double size = 0.25;

    public ItemEntity(World world) : base(world, Entities.ITEM_ENTITY) {
    }

    /** funny number */
    public const int DESPAWN = 36900;

    public ItemStack stack;
    public int age;
    public int plotArmour;

    /** funny sine wave */
    public float hover;

    public override void update(double dt) {
        base.update(dt);

        prevPosition = position;
        prevRotation = rotation;

        // age the item
        age++;
        plotArmour++; // increment pickup timer

        if (age >= DESPAWN) {
            remove();
            return;
        }

        // update AABB for collision system
        aabb = calcAABB(position);

        // apply physics
        updatePhysics(dt);

        applyFriction();

        // collision detection (applies movement!)
        collide(dt);

        // if stuck in a block, try to get unstuck
        if (isStuckInBlock()) {
            yeet();
        }
    }

    /** If stuck in a block, apply velocity towards nearest escape */
    public void yeet() {
        var currentPos = position.toBlockPos();
        var nearestAir = findNearestAirBlock(currentPos);

        if (nearestAir.HasValue) {
            // apply velocity towards the nearest air block
            var dir = (Vector3D)nearestAir.Value + new Vector3D(0.5, 0.5, 0.5) - position;
            var escape = dir.norm() * 2.0;
            velocity += escape;
        }
        else {
            // fallback: push up
            velocity.Y += 2.0;
        }
    }

    /** Find the nearest air block within a 3x3x3 area */
    private Vector3I? findNearestAirBlock(Vector3I center) {
        Vector3I? nearest = null;
        double nearestDist = double.MaxValue;

        // check 3x3x3 area around current position
        for (int dx = -1; dx <= 1; dx++) {
            for (int dy = -1; dy <= 1; dy++) {
                for (int dz = -1; dz <= 1; dz++) {
                    var checkPos = center + new Vector3I(dx, dy, dz);
                    var block = world.getBlock(checkPos);

                    // if this block is air (not solid)
                    if (!Block.collision[block]) {
                        var dist = (checkPos - center).LengthSquared();
                        if (dist < nearestDist) {
                            nearest = checkPos;
                            nearestDist = dist;
                        }
                    }
                }
            }
        }

        return nearest;
    }

    private void updatePhysics(double dt) {
        // apply gravity
        if (!onGround) {
            velocity.Y -= 20 * dt; // gravity
        }

        // terminal velocity
        if (velocity.Y < -20.0) {
            velocity.Y = -20.0;
        }

        // find nearby players for attraction
        var nearbyPlayer = findNearestPlayer();
        if (nearbyPlayer != null) {
            applyPlayerAttraction(nearbyPlayer, dt);
        }
    }

    private Player? findNearestPlayer() {
        if (plotArmour < 30) {
            return null; // no attraction during grace period
        }

        var entities = new List<Entity>();
        var min = position.toBlockPos() - new Vector3I(2, 2, 2);
        var max = position.toBlockPos() + new Vector3I(2, 2, 2);
        world.getEntitiesInBox(entities, min, max);

        Player? nearest = null;
        double nearestDist = double.MaxValue;

        foreach (var entity in entities) {
            if (entity is Player player) {
                var dist = (position - player.position).Length();
                if (dist < nearestDist && dist <= 2.0) {
                    nearest = player;
                    nearestDist = dist;
                }
            }
        }

        return nearest;
    }

    private void applyPlayerAttraction(Player player, double dt) {
        var toPlayer = player.position - position;
        var distSq = toPlayer.LengthSquared();

        if (distSq < 0.001) {
            return;
        }

        var dist = Math.Sqrt(distSq);
        var dir = toPlayer / dist;

        double strength;
        if (dist <= 2.0) {
            var a = (2.0 - dist);
            strength = 22.0 * a * a * a;
        }
        else {
            return;
        }

        velocity += dir * strength * dt;
    }

    private bool isStuckInBlock() {
        var blockPos = position.toBlockPos();
        var block = world.getBlock(blockPos);
        return Block.collision[block] && Block.fullBlock[block];
    }

    public override AABB calcAABB(Vector3D pos) {
        return AABB.fromSize(
            new Vector3D(pos.X - size / 2, pos.Y, pos.Z - size / 2),
            new Vector3D(size, size, size)
        );
    }

    /** Try to be picked up by the given player. Returns true if successful. */
    public bool pickup(Player player) {
        if (plotArmour < 30) {
            return false; // grace period to prevent immediate pickup
        }

        // check if player can pick up this item
        if (canPickup(player)) {
            // try to add to inventory
            if (player.survivalInventory.addItem(stack)) {
                // successfully added to inventory, remove from world
                remove();
                return true;
            }
        }

        return false;
    }

    private bool canPickup(Player player) {
        // check distance
        var distance = (position - player.position).Length();
        return distance <= 0.5; // pickup radius
    }

    private void remove() {
        active = false;
    }
}