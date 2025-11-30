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

        public bool sendVelocity = true;

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
            sendVelocity = shouldSendVelocity(entity),
        };

        // updateViewers handles sending spawn packets to all viewers
        tracked[entity.id] = t;
        updateViewers(t);
    }

    private static bool shouldSendVelocity(Entity entity) {
        // only send velocity for entities that actually move
        return entity switch {
            Player => true,  // players need velocity for smooth animation
            ItemEntity => true,
            FallingBlockEntity => true,
            ArrowEntity => true,
            SnowballEntity => true,
            GrenadeEntity => true,
            _ => false
        };
    }

    /** stop tracking an entity (broadcasts despawn to clients) */
    public void untrackEntity(int entityID) {
        if (!tracked.Remove(entityID, out var t)) {
            return; // not tracking
        }

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

        // periodically refresh viewers (every 60 ticks = 1 second)
        if (server.world.worldTick % 60 == 0) {
            foreach (var t in tracked.Values) {
                updateViewers(t);
            }
        }
    }

    private void updateEntity(TrackedEntity t) {
        var entity = t.entity;

        // threshold: 0.0625 blocks (1/16) or 0.5rad
        const double MOVE_THRESHOLD_SQ = 0.0625 * 0.0625;
        const float ROT_THRESHOLD_SQ = 0.5f * 0.5f;

        bool movedEnough = Vector3D.DistanceSquared(entity.position, t.lastSentPos) > MOVE_THRESHOLD_SQ;
        bool rotatedEnough = Vector3.DistanceSquared(entity.rotation, t.lastSentRot) > ROT_THRESHOLD_SQ;
        t.ticksSinceUpdate++;

        if (movedEnough || rotatedEnough) {
            // try to use delta packet for small movements
            bool usedDelta = false;
            if (EntityPositionDeltaPacket.tryCreate(entity.id, entity.position, t.lastSentPos, entity.rotation, t.lastSentRot, out var deltaPacket)) {
                // send delta
                foreach (var viewer in t.viewers) {
                    viewer.send(deltaPacket, DeliveryMethod.Unreliable);
                }
                usedDelta = true;
            }

            if (!usedDelta) {
                // delta too large, send full position packet
                var packet = new EntityPositionRotationPacket {
                    entityID = entity.id,
                    position = entity.position,
                    rotation = entity.rotation,
                };

                foreach (var viewer in t.viewers) {
                    viewer.send(packet, DeliveryMethod.Unreliable);
                }
            }

            if (t.sendVelocity) {
                // send velocity packet too
                var velPacket = new EntityVelocityPacket {
                    entityID = entity.id,
                    velocity = entity.velocity
                };
                foreach (var viewer in t.viewers) {
                    viewer.send(velPacket, DeliveryMethod.Unreliable);
                }
            }

            t.lastSentPos = entity.position;
            t.lastSentRot = entity.rotation;
            t.ticksSinceUpdate = 0;
        }
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

            if (conn.isInRange(t.entity.position)) {
                t.tempViewers.Add(conn);
            }
        }

        // send spawn to new viewers (in tempViewers but not in viewers)
        foreach (var viewer in t.tempViewers) {
            if (t.viewers.Contains(viewer)) continue;
            if (t.entity is Player sp) {

                // DON'T SEND SPAWNS, we send them in finishLogin!
                continue;

                Log.info($"[EntityTracker] Sending spawn for player {sp.name} (ID={t.entity.id}) to viewer {viewer.username}");
                /*viewer.send(new SpawnPlayerPacket {
                    entityID = t.entity.id,
                    username = sp.name,
                    position = t.entity.position,
                    rotation = t.entity.rotation
                }, DeliveryMethod.ReliableOrdered);*/

                GameServer.instance.send(
                    new SpawnPlayerPacket {
                        entityID = sp.id,
                        username = sp.name,
                        position = sp.position,
                        rotation = sp.rotation,
                        sneaking = sp.sneaking,
                        flying = sp.flyMode
                    },
                    DeliveryMethod.ReliableOrdered,
                    exclude: viewer
                );

                // broadcast initial entity state to all existing clients
                sp.state.markAllDirty();
                GameServer.instance.send(
                    new EntityStatePacket {
                        entityID = sp.id,
                        data = sp.state.serializeAll()
                    },
                    DeliveryMethod.ReliableOrdered,
                    exclude: viewer
                );

                // send their held item
                viewer.send(new HeldItemChangePacket {
                    entityID = t.entity.id,
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
            if (t.tempViewers.Contains(viewer)) continue;

            // DON'T SEND DESPAWNS for players either (todo players never despawn until disconnect, this needs to be improved later)
            if (t.entity is Player) {
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
                    // find owner entity in world
                    arrow.owner = entity.world.entities.FirstOrDefault(e => e.id == ownerID);
                }
                break;
        }
    }
}