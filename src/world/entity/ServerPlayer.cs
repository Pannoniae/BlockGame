using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.util;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * Server-side player entity.
 * Position/rotation is set by incoming packets, no input/physics/rendering.
 */
public class ServerPlayer : Player {

    public ServerConnection conn;

    public ServerPlayer(World world, int x, int y, int z) : base(world, x, y, z) {
    }

    public override void update(double dt) {
        // only update essential systems
        savePrevVars();
        updateTimers(dt);

        // derive velocity from position change for body rotation
        // (position is set by packets, not physics)
        //velocity = (position - prevPosition) / dt;

        updateBodyRotation(dt);  // needed for broadcasting correct bodyRotation
        updateAnimation(dt);      // needed for animation state
        aabb = calcAABB(position);

        // item pickup
        if (!dead) {
            pickup();
        }
    }

    protected override void updateBodyRotation(double dt) {
        // use velocity for body rotation (like Mob does), not strafeVector
        var vel = velocity.withoutY();
        var velLength = vel.Length();

        const double IDLE_VELOCITY_THRESHOLD = 0.05;
        const float BODY_ROTATION_SNAP = 45f;
        const float ROTATION_SPEED = 1.8f;

        bool moving = velLength > IDLE_VELOCITY_THRESHOLD;

        float targetYaw;
        float rotSpeed;

        if (moving) {
            targetYaw = rotation.Y;
            rotSpeed = ROTATION_SPEED * 2;
        }
        else {
            targetYaw = bodyRotation.Y;
            rotSpeed = ROTATION_SPEED * 2;
        }

        bodyRotation.Y = Meth.lerpAngle(bodyRotation.Y, targetYaw, rotSpeed * (float)dt);

        float angleDiff = Meth.angleDiff(bodyRotation.Y, rotation.Y);

        if (angleDiff is > 70 or < -70) {
            bodyRotation.Y = rotation.Y - float.CopySign(70, angleDiff);
            angleDiff = float.CopySign(70, angleDiff);
        }

        var a = Math.Abs(angleDiff);
        if (a > BODY_ROTATION_SNAP) {
            bodyRotation.Y = Meth.lerpAngle(bodyRotation.Y, rotation.Y, rotSpeed * 0.6f * (float)dt * (a / BODY_ROTATION_SNAP));
        }

        bodyRotation.X = 0;
        bodyRotation.Z = 0;

        bodyRotation = Meth.clampAngle(bodyRotation);
        rotation = Meth.clampAngle(rotation);
    }

    // sync HP to client on damage
    public override void dmg(float damage) {
        base.dmg(damage);
        syncHealth();
    }

    public override void dmg(double damage, Vector3D source) {
        base.dmg(damage, source);
        syncHealth();
    }

    private void syncHealth() {
        if (conn != null && !dead) {
            conn.send(new PlayerHealthPacket {
                health = hp,
                damageTime = dmgTime
            }, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
    }

    // sync effects to client
    public override void addEffect(Effect effect) {
        base.addEffect(effect);

        if (conn != null) {
            // send add effect packet
            var packet = new AddEffectPacket {
                entityID = id,
                effectID = effect.getID(),
                duration = effect.duration,
                amplifier = effect.amplifier,
                value = 0
            };

            // special handling for regen effect value
            if (effect is RegenEffect regen) {
                packet.value = regen.value;
            }

            conn.send(packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
    }

    public override void removeEffect(int effectID) {
        base.removeEffect(effectID);

        if (conn != null) {
            conn.send(new RemoveEffectPacket {
                entityID = id,
                effectID = effectID
            }, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
    }

    public override void teleport(Vector3D pos) {
        base.teleport(pos);

        // send teleport packet to client
        if (conn != null) {
            conn.send(new TeleportPacket {
                position = pos,
                rotation = rotation
            }, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
    }

    public override void die() {
        base.die();

        // close any open inv
        if (currentInventoryID != -1) {
            closeInventory();
        }

        // drop inventory items on death (survival only, blocks only)
        if (gameMode.gameplay) {
            dropInventoryOnDeath();
        }

        // notify client of death and broadcast to nearby players
        var server = GameServer.instance;
        if (conn != null) {
            // send respawn packet to client (triggers death screen)
            conn.send(new PlayerHealthPacket {
                health = 0,
                damageTime = dmgTime
            }, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        // broadcast death action to nearby players
        server.send(
            position,
            128.0,
            new EntityActionPacket {
                entityID = id,
                action = EntityActionPacket.Action.DEATH
            },
            LiteNetLib.DeliveryMethod.ReliableOrdered
        );
    }

    private void dropInventoryOnDeath() {
        // drop all materials (blocks used for building, not tools)
        for (int i = 0; i < inventory.slots.Length; i++) {
            var stack = inventory.slots[i];
            if (stack != ItemStack.EMPTY && stack.quantity > 0) {
                var item = stack.getItem();
                // only drop materials (blocks), not tools
                if (item.isBlock() && Item.material[item.id]) {
                    dropItemStack(stack, false);
                    inventory.slots[i] = ItemStack.EMPTY;
                }
            }
        }

        // sync inventory clear to client
        var server = GameServer.instance;
        if (conn != null) {
            var inventorySlots = new List<ItemStack>();
            inventorySlots.AddRange(inventory.slots);
            inventorySlots.AddRange(inventory.armour);
            inventorySlots.AddRange(inventory.accessories);

            conn.send(new InventorySyncPacket {
                invID = Constants.INV_ID_PLAYER,
                items = inventorySlots.ToArray()
            }, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
    }

    private void pickup() {
        // get nearby entities
        var entities = new List<Entity>();
        var min = position.toBlockPos() - new Molten.Vector3I(2, 2, 2);
        var max = position.toBlockPos() + new Molten.Vector3I(2, 2, 2);
        world.getEntitiesInBox(entities, min, max);

        // try to pickup any ItemEntities
        foreach (var entity in entities) {
            if (entity is ItemEntity itemEntity) {
                if (itemEntity.pickup(this)) {
                    // successfully picked up - notify client and broadcast
                    var server = GameServer.instance;

                    if (conn != null) {
                        // find which slot was updated and send SetSlotPacket
                        // (inventory.addItem already modified the inventory)
                        // for now, just sync the entire inventory - optimise later..
                        var inventorySlots = new List<ItemStack>();
                        inventorySlots.AddRange(inventory.slots);
                        inventorySlots.AddRange(inventory.armour);
                        inventorySlots.AddRange(inventory.accessories);

                        conn.send(new InventorySyncPacket {
                            invID = Constants.INV_ID_PLAYER,
                            items = inventorySlots.ToArray()
                        }, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    }

                    // broadcast entity despawn to all nearby players
                    server.send(
                        position,
                        128.0,
                        new DespawnEntityPacket {
                            entityID = itemEntity.id
                        },
                        LiteNetLib.DeliveryMethod.ReliableOrdered
                    );
                }
            }
        }
    }

    // override sendMessage to send ChatMessagePacket back to client
    public override void sendMessage(string msg) {
        if (conn != null) {
            conn.send(new ChatMessagePacket { message = msg }, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
    }

}