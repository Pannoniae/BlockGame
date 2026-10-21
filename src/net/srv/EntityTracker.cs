using System.Numerics;
using BlockGame.net.packet;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.world.entity;
using BlockGame.world.item;
using LiteNetLib;
using Molten.DoublePrecision;

namespace BlockGame.net.srv;

/**
 * tracks which entities each client can see and broadcasts updates
 * TODO open questions. Should we sync the bodyrot or drop it + recalculate on client?
 *
 */
public class EntityTracker {
    private readonly Dictionary<int, TrackedEntity> tracked = new();
    private readonly GameServer server;

    public class TrackedEntity {
        public Entity entity;
        public Vector3D lastSentPos;
        public Vector3 lastSentRot;
        public int ticksSinceUpdate;

        /** players only */
        public double lastSentHp = double.NaN;
        public int hpTicks;

        public bool wasMoving;

        /** so a permanently broken entity logs once, not sixty times a second */
        public bool warnedNaN;

        /** clients that have this entity loaded */
        public HashSet<ServerConnection> viewers = [];

        /** reusable temp set for updateViewers, totally not an implementation detail */
        public HashSet<ServerConnection> tempViewers = [];
    }

    public EntityTracker(GameServer server) {
        this.server = server;
    }

    /** start tracking an entity (broadcasts spawn to clients in range) */
    public void trackEntity(Entity entity) {
        if (tracked.ContainsKey(entity.id)) {
            return; // already tracking
        }

        var t = new TrackedEntity {
            entity = entity,
            lastSentPos = entity.position,
            lastSentRot = entity.rotation,
            ticksSinceUpdate = 0,
        };

        // updateViewers handles sending spawn packets to all viewers
        tracked[entity.id] = t;
        updateViewers(t);
    }

    /** stop tracking an entity (broadcasts despawn to clients) */
    public void untrackEntity(int entityID) {
        if (!tracked.Remove(entityID, out var t)) {
            return; // not tracking
        }

        flushEvents(t, owner(t.entity));

        // send despawn to all viewers
        var despawnPacket = new DespawnEntityPacket { entityID = entityID };
        foreach (var viewer in t.viewers) {
            viewer.send(despawnPacket, DeliveryMethod.ReliableOrdered);
        }
    }

    /** update all tracked entities - called every tick */
    public void update() {
        // update tracked entities
        foreach (var t in tracked.Values) {
            updateEntity(t);
        }

        // every 1/3s. once a second
        if (server.simTick % 20 == 0) {
            foreach (var t in tracked.Values) {
                updateViewers(t);
            }
        }
    }

    private static ServerConnection? owner(Entity e) => (e as ServerPlayer)?.conn;

    private void updateEntity(TrackedEntity t) {
        var o = owner(t.entity);
        syncMovement(t);
        syncState(t, o);
        flushEvents(t, o);
    }

    private void syncMovement(TrackedEntity t) {
        var entity = t.entity;

        // A NaN position encodes to something plausible-looking rather than failing, so every client
        // would snap the entity to wherever that lands and never correct it. Drop the update instead -
        // one stale tick, and a log line pointing at whatever produced the NaN.
        if (!double.IsFinite(entity.position.X) || !double.IsFinite(entity.position.Y) ||
            !double.IsFinite(entity.position.Z)) {
            if (!t.warnedNaN) {
                t.warnedNaN = true;
                Log.error($"Entity {entity.id} ({entity.type}) has a non-finite position {entity.position}, not syncing it");
            }
            return;
        }

        t.warnedNaN = false;

        // threshold: 0.0625 blocks (1/16) or 0.5deg
        const double MOVE_THRESHOLD_SQ = 0.0625 * 0.0625;
        const float ROT_THRESHOLD_SQ = 0.5f * 0.5f;
        const int KEEPALIVE = 40;

        t.ticksSinceUpdate++;

        bool moved = Vector3D.DistanceSquared(entity.position, t.lastSentPos) > MOVE_THRESHOLD_SQ;
        bool rotated = Vector3.DistanceSquared(entity.rotation, t.lastSentRot) > ROT_THRESHOLD_SQ;

        // came to rest under the threshold: send where it actually stopped, or the client coasts up to 1/16 past it
        // and sits there until the keepalive. (server players always land here - packets beat savePrevVars)
        bool settled = !moved && entity.position == entity.prevPosition && entity.position != t.lastSentPos;

        // hard stop (wall, landing): the stop was sent as a normal move, so without this the client extrapolates
        // a full tick past it and holds there until the keepalive. half a block into the wall for an arrow
        bool stopped = !moved && t.wasMoving;

        if (!moved && !rotated && !settled && !stopped && t.ticksSinceUpdate < KEEPALIVE) {
            return;
        }

        t.wasMoving = moved || settled;

        var packet = new EntityPositionRotationPacket {
            entityID = entity.id,
            tick = server.simTick,
            position = entity.position,
            rotation = entity.rotation,
        };

        // unreliable is fine
        foreach (var viewer in t.viewers) {
            viewer.send(packet, DeliveryMethod.Unreliable);
        }

        t.lastSentPos = entity.position;
        t.lastSentRot = entity.rotation;
        t.ticksSinceUpdate = 0;
    }

    private static void syncState(TrackedEntity t, ServerConnection? o) {
        var e = t.entity;
        e.syncState();

        if (e.state.isDirty()) {
            var packet = new EntityStatePacket {
                entityID = e.id,
                data = e.state.serialize()
            };

            foreach (var viewer in t.viewers) {
                viewer.send(packet, DeliveryMethod.ReliableOrdered);
            }

            o?.send(packet, DeliveryMethod.ReliableOrdered);
        }

        t.hpTicks++;
        if (o != null && e.hp != t.lastSentHp && (e.hp < t.lastSentHp || t.hpTicks >= 10 || double.IsNaN(t.lastSentHp))) {
            t.lastSentHp = e.hp;
            t.hpTicks = 0;
            o.send(new PlayerHealthPacket {
                health = e.hp,
                damageTime = e.dmgTime
            }, DeliveryMethod.ReliableOrdered);
        }
    }

    private static void flushEvents(TrackedEntity t, ServerConnection? o) {
        var e = t.entity;

        if (e.pendingHurt > 0) {
            // every damage source, not just melee - arrows, fire, falls used to be invisible to clients
            var packet = new EntityDamagePacket {
                entityID = e.id,
                attackerID = e.pendingHurtSrc,
                damage = e.pendingHurt
            };

            foreach (var viewer in t.viewers) {
                viewer.send(packet, DeliveryMethod.ReliableOrdered);
            }

            o?.send(packet, DeliveryMethod.ReliableOrdered);
        }

        if (e.hasImpulse) {
            o?.send(new EntityImpulsePacket {
                entityID = e.id,
                impulse = e.pendingImpulse
            }, DeliveryMethod.ReliableOrdered);
        }

        e.clearEvents();
    }

    /** recalculate which clients can see this entity (based on distance) */
    private void updateViewers(TrackedEntity t) {
        // reuse temp set instead of allocating a new one ALL THE TIME
        t.tempViewers.Clear();

        // find all connections in range
        foreach (var conn in server.connections.Values) {
            if (conn.player == null) {
                continue;
            }

            // don't send player's own entity to themselves (they have ClientPlayer)
            if (t.entity == conn.player) {
                continue;
            }

            var hys = t.viewers.Contains(conn) ? 16.0 : 0.0;
            if (conn.isInRange(t.entity.position, hys)) {
                t.tempViewers.Add(conn);
            }
        }

        // send spawn to new viewers (in tempViewers but not in viewers)
        foreach (var viewer in t.tempViewers) {
            if (t.viewers.Contains(viewer)) {
                continue;
            }

            if (t.entity is Player sp) {
                viewer.send(new SpawnPlayerPacket {
                    entityID = sp.id,
                    username = sp.name,
                    position = sp.position,
                    rotation = sp.rotation,
                    sneaking = sp.sneaking,
                    flying = sp.flyMode
                }, DeliveryMethod.ReliableOrdered);


                viewer.send(new HeldItemChangePacket {
                    entityID = sp.id,
                    slotIndex = (byte)sp.inventory.selected,
                    heldItem = sp.inventory.getSelected()
                }, DeliveryMethod.ReliableOrdered);
            }
            else {
                viewer.send(new SpawnEntityPacket {
                    entityID = t.entity.id,
                    entityType = Entities.getID(t.entity.type),
                    position = t.entity.position,
                    rotation = t.entity.rotation,
                    velocity = t.entity.velocity,
                    extraData = serializeExtraData(t.entity)
                }, DeliveryMethod.ReliableOrdered);
            }

            // send current state to new viewer
            t.entity.syncState();
            var stateData = t.entity.state.serializeAll();
            var statePacket = new EntityStatePacket {
                entityID = t.entity.id,
                data = stateData
            };
            viewer.send(statePacket, DeliveryMethod.ReliableOrdered);
        }

        // send despawn to old viewers no longer in range (in viewers but not in tempViewers)
        foreach (var viewer in t.viewers) {
            if (t.tempViewers.Contains(viewer)) {
                continue;
            }

            var despawnPacket = new DespawnEntityPacket { entityID = t.entity.id };
            viewer.send(despawnPacket, DeliveryMethod.ReliableOrdered);
        }

        // swap: viewers becomes tempViewers, tempViewers becomes old viewers (reused next time)
        (t.viewers, t.tempViewers) = (t.tempViewers, t.viewers);
    }

    /** serialize entity-specific data for spawn packet */
    public static byte[] serializeExtraData(Entity entity) {
        var buf = PacketWriter.get();

        switch (entity) {
            case ItemEntity item:
                buf.writeInt(item.stack.id);
                buf.writeInt(item.stack.metadata);
                buf.writeInt(item.stack.quantity);
                break;

            case FallingBlockEntity fb:
                buf.writeUShort(fb.blockID);
                buf.writeByte(fb.blockMeta);
                break;

            case ArrowEntity arrow:
                buf.writeInt(arrow.owner?.id ?? -1);
                break;

            case Mob:
                // mobs don't need extraData (state handled by EntityState)
                break;
        }

        return PacketWriter.getBytes();
    }

    /** deserialize entity-specific data from spawn packet (client-side helper) */
    public static void deserializeExtraData(Entity entity, byte[] data) {
        using var ms = new MemoryStream(data);
        var buf = new PacketBuffer(new BinaryReader(ms));

        switch (entity) {
            case ItemEntity item:
                var itemID = buf.readInt();
                var metadata = buf.readInt();
                var quantity = buf.readInt();
                item.stack = new ItemStack(Item.get(itemID), quantity, metadata);
                break;

            case FallingBlockEntity fb:
                fb.blockID = buf.readUShort();
                fb.blockMeta = buf.readByte();
                break;

            case ArrowEntity arrow:
                var ownerID = buf.readInt();
                if (ownerID >= 0) {
                    arrow.owner = entity.world.getEntity(ownerID);
                }
                break;
        }
    }
}