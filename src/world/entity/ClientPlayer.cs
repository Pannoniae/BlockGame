using System.Numerics;
using BlockGame.main;
using BlockGame.net;
using BlockGame.net.packet;
using BlockGame.util;
using BlockGame.world.block;
using LiteNetLib;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class ClientPlayer : Player {
    private Vector3D lastSentPos;
    private Vector3 lastSentRot;
    private int ticksSinceUpdate;

    // block breaking tracking for multiplayer
    private Vector3I? lastBreakingBlock;
    private bool sentStartBreak;

    // state tracking for multiplayer
    private bool lastSentSneaking;
    private bool lastSentFlying;

    public ClientPlayer(World world, int x, int y, int z) : base(world, x, y, z) {
    }

    public override void update(double dt) {
        base.update(dt);

        // send position updates to server
        if (ClientConnection.instance != null && ClientConnection.instance.connected) {
            ticksSinceUpdate++;

            // send position every tick if changed significantly, or every 20 ticks regardless
            bool moved = Vector3D.Distance(position, lastSentPos) > 0.01;
            bool rotated = Vector3.Distance(rotation, lastSentRot) > 0.5f;

            if (moved || rotated || ticksSinceUpdate >= 20) {
                ClientConnection.instance.send(
                    new PlayerPositionRotationPacket {
                        position = position,
                        rotation = rotation,
                        onGround = onGround
                    },
                    DeliveryMethod.ReliableOrdered
                );

                lastSentPos = position;
                lastSentRot = rotation;
                ticksSinceUpdate = 0;
            }

            // send state changes (sneaking, flying)
            if (sneaking != lastSentSneaking) {
                state.setBool(EntityState.SNEAKING, sneaking);
                ClientConnection.instance.send(
                    new EntityStatePacket {
                        entityID = id,
                        data = state.serialize()
                    },
                    DeliveryMethod.ReliableOrdered
                );
                lastSentSneaking = sneaking;
            }

            if (flyMode != lastSentFlying) {
                state.setBool(EntityState.FLYING, flyMode);
                ClientConnection.instance.send(
                    new EntityStatePacket {
                        entityID = id,
                        data = state.serialize()
                    },
                    DeliveryMethod.ReliableOrdered
                );
                lastSentFlying = flyMode;
            }
        }
    }

    public override void breakBlock() {
        bool connected = ClientConnection.instance != null && ClientConnection.instance.connected;
        var now = Game.permanentStopwatch.ElapsedMilliseconds;

        if (connected && Game.raycast.hit && Game.raycast.type == Result.BLOCK) {
            var pos = Game.raycast.block;
            var prev = Game.raycast.previous;

            // handle fire breaking (instant break, needs packet)
            if (world.getBlock(prev.X, prev.Y, prev.Z) == Block.FIRE.id) {
                ClientConnection.instance.send(
                    new FinishBlockBreakPacket { position = prev },
                    DeliveryMethod.ReliableOrdered
                );
                base.breakBlock();
                return;
            }

            // creative instant-break (needs packet, respect delay)
            if (!gameMode.gameplay && now - lastMouseAction > Constants.breakDelayMs) {
                ClientConnection.instance.send(
                    new FinishBlockBreakPacket { position = pos },
                    DeliveryMethod.ReliableOrdered
                );
            }
            // survival mode will be handled by blockHandling()
        }

        base.breakBlock();
    }

    public override void blockHandling(double dt) {
        // check if connected to server
        bool connected = ClientConnection.instance != null && ClientConnection.instance.connected;

        // track breaking state changes for multiplayer
        if (connected) {
            if (isBreaking && Game.instance.targetedPos.HasValue) {
                var pos = Game.instance.targetedPos.Value;

                // check if we started breaking a new block
                if (!lastBreakingBlock.HasValue || lastBreakingBlock.Value != pos) {
                    // send cancel for previous block if we were breaking one
                    if (sentStartBreak) {
                        ClientConnection.instance.send(
                            new CancelBlockBreakPacket(),
                            DeliveryMethod.ReliableOrdered
                        );
                    }

                    // send start for new block
                    ClientConnection.instance.send(
                        new StartBlockBreakPacket { position = pos },
                        DeliveryMethod.ReliableOrdered
                    );

                    lastBreakingBlock = pos;
                    sentStartBreak = true;
                }
            }
            else if (sentStartBreak) {
                // stopped breaking, send cancel
                ClientConnection.instance.send(
                    new CancelBlockBreakPacket(),
                    DeliveryMethod.ReliableOrdered
                );

                lastBreakingBlock = null;
                sentStartBreak = false;
            }
        }

        // call base implementation (handles actual breaking)
        base.blockHandling(dt);

        // if we finished breaking, send finish packet
        if (connected && !isBreaking && lastBreakingBlock.HasValue && sentStartBreak) {
            // block was broken (progress reached 1.0 in base.blockHandling)
            ClientConnection.instance.send(
                new FinishBlockBreakPacket { position = lastBreakingBlock.Value },
                DeliveryMethod.ReliableOrdered
            );

            lastBreakingBlock = null;
            sentStartBreak = false;
        }
    }

    public override void placeBlock() {
        // todo we yeeted into base class. we should probably refactor this later?

        // call base implementation (applies locally for immediate feedback)
        base.placeBlock();
    }

    public override void attackEntity() {
        // check if connected to server
        bool connected = ClientConnection.instance != null && ClientConnection.instance.connected;

        if (connected && Game.raycast.entity != null) {
            var now = Game.permanentStopwatch.ElapsedMilliseconds;

            if (now - lastMouseAction <= Constants.breakDelayMs) {
                return;
            }

            // send attack packet to server
            ClientConnection.instance.send(
                new AttackEntityPacket {
                    targetEntityID = Game.raycast.entity.id
                },
                DeliveryMethod.ReliableOrdered
            );

            // play swing animation locally
            setSwinging(true);
            lastMouseAction = now;
        } else {
            // singleplayer - use base implementation
            base.attackEntity();
        }
    }

    // see base.setSwinging for impl
}