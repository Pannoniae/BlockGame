using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public partial class Entity {
    /**
     * Swept collision - adjust velocity per axis, apply movement immediately.
     * Reduces velocity to exact contact point instead of preventing escape.
     *
     * This used to be YXZ order but I changed it to match the blocks' order to be YZX.
     */
    protected virtual void collide(double dt) {
        var di = 1 / dt;
        var oldPos = position;
        var blockPos = position.toBlockPos();

        // collect potential collision targets
        collisions.Clear();

        // if we aren't noclipping
        if (!noClip) {
            ReadOnlySpan<Vector3I> targets = [
                blockPos, new(blockPos.X, blockPos.Y + 1, blockPos.Z)
            ];
            // for each block we might collide with
            foreach (Vector3I target in targets) {
                // first, collide with the block the player is in
                var blockPos2 = feetPosition.toBlockPos();
                world.getAABBsCollision(AABBList, blockPos2.X, blockPos2.Y, blockPos2.Z);

                // for each AABB of the block the player is in
                foreach (AABB aa in AABBList) {
                    collisions.Add(aa);
                }

                // gather neighbouring blocks
                World.getBlocksInBox(neighbours, target + new Vector3I(-1, -1, -1),
                    target + new Vector3I(1, 1, 1));
                // for each neighbour block
                foreach (var neighbour in neighbours) {
                    var block = world.getBlock(neighbour);
                    world.getAABBsCollision(AABBList, neighbour.X, neighbour.Y, neighbour.Z);
                    foreach (AABB aa in AABBList) {
                        collisions.Add(aa);
                    }
                }
            }
        }

        // tl;dr: the point is that we *prevent* collisions instead of resolving them after the fact
        // and if there's already a collision, we don't do shit. We just prevent new ones.
        // This prevents the "ejecting out" behaviour and it also prevents stuff glitching where it REALLY shouldn't.

        // hehe
        double d = 0.0;

        // Y axis
        var p = calcAABB(position);

        foreach (var b in collisions) {
            // wtf are we even doing here then?
            if (!AABB.isCollisionX(p, b) || !AABB.isCollisionZ(p, b)) {
                continue;
            }

            switch (velocity.Y) {
                case > 0: {
                    d = b.y0 - p.y1;
                    if (p.y1 <= b.y0 && d < velocity.Y * dt) {
                        velocity.Y = d * di;
                    }

                    break;
                }
                case < 0: {
                    d = b.y1 - p.y0;
                    if (p.y0 >= b.y1 && d > velocity.Y * dt) {
                        velocity.Y = d * di;
                    }

                    break;
                }
            }
        }

        position.Y += velocity.Y * dt;
        // recalc aabb after Y movement!
        p = calcAABB(position);

        // Z axis
        foreach (var b in collisions) {
            if (!AABB.isCollisionX(p, b) || !AABB.isCollisionY(p, b)) {
                continue;
            }

            // try stepping up if on ground
            bool canStepUp = false;
            if (onGround && velocity.Z != 0) {
                for (double stepY = Constants.epsilon; stepY <= STEP_HEIGHT; stepY += 0.1) {
                    var stepAABB = calcAABB(new Vector3D(position.X, position.Y + stepY, position.Z + velocity.Z * dt));
                    bool cb = false;

                    foreach (var testAABB in collisions) {
                        if (AABB.isCollision(stepAABB, testAABB)) {
                            cb = true;
                            break;
                        }
                    }

                    if (!cb) {
                        position.Y += stepY;
                        canStepUp = true;
                        break;
                    }
                }
            }

            if (!canStepUp) {
                switch (velocity.Z) {
                    case > 0: {
                        d = b.z0 - p.z1;
                        if (p.z1 <= b.z0 && d < velocity.Z * dt) {
                            velocity.Z = d * di;
                            collz = true;
                        }

                        break;
                    }
                    case < 0: {
                        d = b.z1 - p.z0;
                        if (p.z0 >= b.z1 && d > velocity.Z * dt) {
                            velocity.Z = d * di;
                            collz = true;
                        }

                        break;
                    }
                }
            }
        }

        // sneaking edge prevention for Z
        if (sneaking && onGround) {
            var sneakAABB = calcAABB(new Vector3D(position.X, position.Y - 0.1, position.Z + velocity.Z * dt));
            bool hasEdge = false;
            foreach (var blockAABB in collisions) {
                if (AABB.isCollision(sneakAABB, blockAABB)) {
                    hasEdge = true;
                    break;
                }
            }

            if (!hasEdge) {
                velocity.Z = 0;
            }
        }

        position.Z += velocity.Z * dt;
        // recalc aabb after Z movement!
        p = calcAABB(position);

        // X axis
        foreach (var b in collisions) {
            if (!AABB.isCollisionY(p, b) || !AABB.isCollisionZ(p, b)) {
                continue;
            }

            // try stepping up if on ground
            bool canStepUp = false;
            if (onGround && velocity.X != 0) {
                for (double stepY = Constants.epsilon; stepY <= STEP_HEIGHT; stepY += 0.1) {
                    var stepAABB = calcAABB(new Vector3D(position.X + velocity.X * dt, position.Y + stepY, position.Z));
                    bool cb = false;

                    foreach (var testAABB in collisions) {
                        if (AABB.isCollision(stepAABB, testAABB)) {
                            cb = true;
                            break;
                        }
                    }

                    if (!cb) {
                        position.Y += stepY;
                        canStepUp = true;
                        break;
                    }
                }
            }

            if (!canStepUp) {
                switch (velocity.X) {
                    case > 0: {
                        d = b.x0 - p.x1;
                        if (p.x1 <= b.x0 && d < velocity.X * dt) {
                            velocity.X = d * di;
                            collx = true;
                        }

                        break;
                    }
                    case < 0: {
                        d = b.x1 - p.x0;
                        if (p.x0 >= b.x1 && d > velocity.X * dt) {
                            velocity.X = d * di;
                            collx = true;
                        }

                        break;
                    }
                }
            }
        }

        // sneaking edge prevention for X
        if (sneaking && onGround) {
            var sneakAABB = calcAABB(new Vector3D(position.X + velocity.X * dt, position.Y - 0.1, position.Z));
            bool hasEdge = false;
            foreach (var blockAABB in collisions) {
                if (AABB.isCollision(sneakAABB, blockAABB)) {
                    hasEdge = true;
                    break;
                }
            }

            if (!hasEdge) {
                velocity.X = 0;
            }
        }

        position.X += velocity.X * dt;
        // recalc aabb after X movement!
        p = calcAABB(position);

        // zero out velocity on collision?
        //if (hasXCollision) velocity.X = 0;
        //if (hasZCollision) velocity.Z = 0;

        // is player on ground? check slightly below
        var groundCheck = calcAABB(new Vector3D(position.X, position.Y - EPSILON_GROUND_CHECK, position.Z));
        onGround = false;
        foreach (var blockAABB in collisions) {
            if (AABB.isCollision(blockAABB, groundCheck)) {
                onGround = true;
                flyMode = false;
            }
        }
    }

    protected virtual void clamp(double dt) {
        // clamp
        if (Math.Abs(velocity.X) < Constants.epsilon) {
            velocity.X = 0;
        }

        if (Math.Abs(velocity.Y) < Constants.epsilon) {
            velocity.Y = 0;
        }

        if (Math.Abs(velocity.Z) < Constants.epsilon) {
            velocity.Z = 0;
        }

        // clamp fallspeed
        if (Math.Abs(velocity.Y) > MAX_VSPEED) {
            var cappedVel = MAX_VSPEED;
            velocity.Y = cappedVel * Math.Sign(velocity.Y);
        }

        // clamp accel (only Y for now, other axes aren't used)
        if (Math.Abs(accel.Y) > MAX_ACCEL) {
            accel.Y = MAX_ACCEL * Math.Sign(accel.Y);
        }
    }

    protected virtual void applyFriction() {
        if (flyMode) {
            velocity.X *= FLY_FRICTION;
            velocity.Z *= FLY_FRICTION;
            velocity.Y *= FLY_FRICTION;
            return;
        }

        // ground friction
        if (!inLiquid) {
            if (onGround) {
                //if (sneaking) {
                //    velocity = Vector3D.Zero;
                //}
                //else {
                velocity.X *= FRICTION;
                velocity.Z *= FRICTION;
                velocity.Y *= VERTICAL_FRICTION;
                //}
            }
            else {
                velocity.X *= AIR_FRICTION;
                velocity.Z *= AIR_FRICTION;
                velocity.Y *= VERTICAL_FRICTION;
            }
        }

        // liquid friction
        if (inLiquid) {
            velocity.X *= LIQUID_FRICTION;
            velocity.Z *= LIQUID_FRICTION;
            velocity.Y *= LIQUID_FRICTION;
            velocity.Y -= 0.25;
        }

        if (jumping && !wasInLiquid && inLiquid) {
            velocity.Y -= 2.5;
        }

        //Console.Out.WriteLine(level);
        if (jumping && (onGround || inLiquid)) {
            velocity.Y += inLiquid ? LIQUID_SWIM_UP_SPEED : JUMP_SPEED;

            // if on the edge of water, boost
            if (inLiquid && (collx || collz)) {
                velocity.Y += LIQUID_SURFACE_BOOST;
            }

            onGround = false;
            jumping = false;
        }
    }

    protected virtual void updateGravity(double dt) {
        // if in liquid, don't apply gravity
        if (inLiquid) {
            accel.Y = 0;
            return;
        }

        if (!onGround && !flyMode) {
            accel.Y = -GRAVITY;
        }
        else {
            accel.Y = 0;
        }
    }
}