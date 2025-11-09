using System;
using BlockGame.main;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.item;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * Floating items/blocks on the ground.
 */
public class ItemEntity : Entity {
    public ItemEntity(World world) : base(world, Entities.ITEM_ENTITY) {

    }

    /** funny number */
    public const int DESPAWN = 36900;

    public ItemStack stack;
    public int age;
    public int plotArmour;

    /** funny sine wave */
    public float hover;

    public static ItemEntity create(World world, Vector3D pos, Item item, int count) {
        return create(world, pos, new ItemStack(item, count));

    }

    public static ItemEntity create(World world, Vector3D pos, ItemStack stack) {
        var itemEntity = new ItemEntity(world) {
            stack = stack.copy(),
            position = pos,
        };

        // add some random velocity
        var random = Game.clientRandom;
        itemEntity.velocity = new Vector3D(
            (random.NextSingle() - 0.5) * 0.3,
            random.NextSingle() * 0.3 + 0.1,
            (random.NextSingle() - 0.5) * 0.3
        );

        return itemEntity;
    }

    public override void update(double dt) {
        base.update(dt);

        prevPosition = position;
        prevRotation = rotation;

        // age the item
        age++;
        plotArmour++; // increment pickup timer

        if (age >= DESPAWN) {
            // remove from world
            remove();
            return;
        }

        // hover animation
        hover = float.Sin(age * 12f) * 0.15f;
        velocity.Y += hover;

        // update AABB for collision system
        aabb = calcAABB(position);

        // apply physics
        updatePhysics(dt);

        // collision detection
        collide(dt);

        // if stuck in a block, try to get unstuck
        if (isStuckInBlock()) {
            yeet();
        }

        // update position from velocity
        position += velocity * dt;
    }

    /** If stuck in a block, try to get unstuck by finding nearest air block */
    public void yeet() {
        var currentPos = position.toBlockPos();
        var nearestAir = findNearestAirBlock(currentPos);

        if (nearestAir.HasValue) {
            // move towards the nearest air block
            var dir = (Vector3D)nearestAir.Value + new Vector3D(0.5, 0.5, 0.5) - position;
            dir = dir.norm() * 0.2; // gentle movement
            position += dir;
        } else {
            // fallback: try moving up
            position.Y += 0.2;
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
        // find nearby players for attraction
        var nearbyPlayer = findNearestPlayer();
        if (nearbyPlayer != null) {
            applyPlayerAttraction(nearbyPlayer, dt);
        }

        // apply gravity
        if (!onGround) {
            velocity.Y -= 20 * dt; // gravity
        }

        // apply friction/drag
        velocity.X *= 0.98; // air resistance
        velocity.Z *= 0.98;

        if (onGround) {
            velocity.X *= 0.8; // ground friction
            velocity.Z *= 0.8;
            velocity.Y *= 0.8;
        }

        // terminal velocity
        if (velocity.Y < -20.0) {
            velocity.Y = -20.0;
        }
    }

    private Player? findNearestPlayer() {
        if (plotArmour < 10) {
            return null; // no attraction during grace period
        }

        var entities = new List<Entity>();
        var min = position.toBlockPos() - new Vector3I(6, 6, 6);
        var max = position.toBlockPos() + new Vector3I(6, 6, 6);
        world.getEntitiesInBox(entities, min, max);

        Player? nearest = null;
        double nearestDist = double.MaxValue;

        foreach (var entity in entities) {
            if (entity is Player player) {
                var dist = (position - player.position).Length();
                if (dist < nearestDist && dist <= 6.0) {
                    nearest = player;
                    nearestDist = dist;
                }
            }
        }

        return nearest;
    }

    private void applyPlayerAttraction(Player player, double dt) {
        var toPlayer = player.position - position;
        var distance = toPlayer.Length();
        
        if (distance < 0.001) {
            return;
        }

        // different attraction ranges
        if (distance <= 1.0) {
            // close enough - strong attraction for pickup
            var attractionForce = toPlayer.norm() * 8.0 * dt;
            velocity += attractionForce;
        } else if (distance <= 3.0) {
            // medium range - gentle attraction
            var attractionForce = toPlayer.norm() * 2.0 * dt;
            velocity += attractionForce;
        }
        // beyond 3 blocks, no attraction
    }

    private bool isStuckInBlock() {
        var blockPos = position.toBlockPos();
        var block = world.getBlock(blockPos);
        return Block.collision[block] && Block.fullBlock[block];
    }

    public override AABB calcAABB(Vector3D pos) {
        // small AABB for item entities
        const double size = 0.25;
        const double height = 0.25;
        return AABB.fromSize(
            new Vector3D(pos.X - size / 2, pos.Y, pos.Z - size / 2),
            new Vector3D(size, height, size)
        );
    }

    /** Try to be picked up by the given player. Returns true if successful. */
    public bool pickup(Player player) {
        if (plotArmour < 10) {
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
        return distance <= 1.5; // pickup radius
    }

    private void remove() {
        active = false;
    }
}
