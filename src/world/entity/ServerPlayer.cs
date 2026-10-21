using System.Numerics;
using BlockGame.main;
using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.util;
using BlockGame.world.item;
using BlockGame.world.item.inventory;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * Server-side player entity.
 * Position/rotation is set by incoming packets, no input/physics/rendering.
 */
public class ServerPlayer : Player {

    public ServerConnection conn;

    public ServerPlayer(World world, int x, int y, int z) : base(world, x, y, z) {
        hasmovement = false;
    }

    private double fallSpeed;

    public void resetFall() => fallSpeed = 0;

    public void applyMove(Vector3D pos, Vector3 rot, bool ground) {
        velocity = (pos - position) * Game.tps;
        position = pos;
        rotation = rot;
        aabb = calcAABB(position);

        // dead clients don't run physics
        if (dead || flyMode || inLiquid || onLadder || !gameMode.gameplay) {
            fallSpeed = 0;
            return;
        }

        if (!ground) {
            fallSpeed = double.Max(fallSpeed, -velocity.Y);
            return;
        }

        if (fallSpeed > SAFE_FALL_SPEED) {
            dmg((float)((fallSpeed - SAFE_FALL_SPEED) * FALL_DAMAGE_MULTIPLIER));
        }

        fallSpeed = 0;
    }

    public override void teleport(Vector3D pos) {
        base.teleport(pos);
        fallSpeed = 0;

        // send teleport packet to client
        conn?.send(new TeleportPacket {
            position = pos,
            rotation = rotation
        }, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }

    public override void dmg(double damage, Entity source) {
        // pvp check
        if (!GameServer.instance.pvp) {
            // direct player attack
            if (source is Player) {
                return;
            }
            // projectile from player
            if (source is ProjectileEntity proj && proj.owner is Player) {
                return;
            }
        }

        base.dmg(damage, source);
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

    protected override void onDeath() {
        if (currentInventoryID != Constants.INV_ID_PLAYER) {
            var id = currentInventoryID;
            closeInventory();
            conn?.send(new InventoryClosePacket { invID = id }, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }

        base.onDeath();

        dropDeathItems();
        syncInventory();

        // also see EntityTracker for hp/death flag
    }

    protected override void updateTimers(double dt) {
        base.updateTimers(dt);
        interactBlock(dt);
    }

    [ThreadStatic] private static List<Entity>? _nearby;

    protected override void pickup() {
        var nearby = _nearby ??= [];
        var min = position.toBlockPos() - new Molten.Vector3I(2, 2, 2);
        var max = position.toBlockPos() + new Molten.Vector3I(2, 2, 2);
        world.getEntitiesInBox(nearby, min, max);

        bool changed = false;
        foreach (var entity in nearby) {
            if (entity is ItemEntity item && item.active && item.pickup(this)) {
                changed = true;
            }
        }

        // the despawn goes out through the tracker when the item is removed
        if (changed) {
            syncInventory();
        }
    }

    private int windowCtr;

    public int nextWindowID() {
        windowCtr = windowCtr == int.MaxValue ? 1 : windowCtr + 1;
        return windowCtr;
    }

    // ============ INVENTORY SYNC ============

    public void syncSlot(int invIdx) {
        conn?.send(new SetSlotPacket {
            invID = Constants.INV_ID_PLAYER,
            slotIndex = (ushort)invIdx,
            stack = inventory.getStack(invIdx)
        }, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }

    public void syncSelected() {
        syncSlot(inventory.selected);
        broadcastHeld();
    }

    public void broadcastHeld() {
        GameServer.instance.send(
            position,
            128.0,
            new HeldItemChangePacket {
                entityID = id,
                slotIndex = (byte)inventory.selected,
                heldItem = inventory.getSelected()
            },
            LiteNetLib.DeliveryMethod.ReliableOrdered,
            exclude: conn
        );
    }

    public void syncCursor() {
        conn?.send(new SetSlotPacket {
            invID = Constants.INV_ID_CURSOR,
            slotIndex = 0,
            stack = inventory.cursor
        }, LiteNetLib.DeliveryMethod.ReliableOrdered);
    }

    private ItemStack[] syncBuf = [];

    /** full player inventory + 2x2 grid + cursor */
    public void syncInventory() {
        if (conn == null) {
            return;
        }

        var grid = (inventoryCtx as SurvivalInventoryContext)?.getCraftingGrid().grid;
        var n = PlayerInventory.TOTAL_SIZE + (grid?.Length ?? 0);
        if (syncBuf.Length != n) {
            syncBuf = new ItemStack[n];
        }

        var items = syncBuf;
        for (int i = 0; i < PlayerInventory.TOTAL_SIZE; i++) {
            items[i] = inventory.getStack(i);
        }

        grid?.CopyTo(items, PlayerInventory.TOTAL_SIZE);

        conn.send(new InventorySyncPacket {
            invID = Constants.INV_ID_PLAYER,
            items = items
        }, LiteNetLib.DeliveryMethod.ReliableOrdered);

        syncCursor();
        broadcastHeld();
    }

    // override sendMessage to send ChatMessagePacket back to client
    public override void sendMessage(string msg) {
        if (conn != null) {
            conn.send(new ChatMessagePacket { message = msg }, LiteNetLib.DeliveryMethod.ReliableOrdered);
        }
    }

}