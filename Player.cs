using System.Diagnostics.Contracts;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace BlockGame;

public class Player {
    public const double gravity = 15;
    public const double maxAccel = 50;
    public const double maxhSpeed = 2.5;
    public const double maxhAirSpeed = 2.5;
    public const double jumpSpeed = 6;
    public const double maxVSpeed = 0.2;
    public const double friction = 0.55;
    public const double airFriction = 0.98;
    public const double epsilon = 0.0001;
    public const double epsilonGroundCheck = 0.01;
    public const double playerHeight = 1.8;
    public const double eyeHeight = 1.6;

    public static double moveSpeed = 3;
    public const double groundMoveSpeed = 3;
    public const double airMoveSpeed = 0.5;

    // is player walking on (colling with) ground
    public bool onGround;

    // is the player in the process of jumping
    public bool jumping;

    // jump cooldown to prevent player jumping immediately again
    public double jumpCD;

    public Camera camera;

    public AABB aabb;

    // entity positions are at feet
    public Vector3D<double> prevPosition;
    public Vector3D<double> position;
    public Vector3D<double> velocity;
    public Vector3D<double> accel;

    public Vector3D<double> forward;

    public int pickBlock;
    public World world;
    private Vector2D<double> strafeVector = new(0, 0);
    public bool pressedMovementKey;


    // positions are feet positions
    public Player(World world, int x, int y, int z) {
        position = new Vector3D<double>(x, y, z);
        prevPosition = position;
        camera = new Camera(new Vector3(x, (float)(y + eyeHeight), z), Vector3.UnitZ * 1, Vector3.UnitY,
            (float)Game.initialWidth / Game.initialHeight);

        this.world = world;
        pickBlock = 1;
        aabb = calcAABB(position);
    }


    public void update(double dt) {
        updateInputVelocity(dt);
        clamp(dt);
        velocity += accel * dt;
        //position += velocity * dt;


        collision(dt);
        updateGravity(dt);
        applyFriction();
        clamp(dt);

        camera.position = new Vector3((float)position.X, (float)(position.Y + eyeHeight), (float)position.Z);
        camera.prevPosition = new Vector3((float)prevPosition.X, (float)(prevPosition.Y + eyeHeight),
            (float)prevPosition.Z);
        var f = camera.CalculateForwardVector();
        forward = new Vector3D<double>(f.X, f.Y, f.Z);
        aabb = calcAABB(position);

        // after everything is done
        prevPosition = position;
    }

    private void resetFrameVars() {
        pressedMovementKey = false;
    }

    private void clamp(double dt) {
        // clamp max speed
        var hVel = new Vector3D<double>(velocity.X, 0, velocity.Z);
        if (onGround) {
            if (hVel.Length > maxhSpeed) {
                var cappedVel = Vector3D.Normalize(hVel) * maxhSpeed;
                velocity = new Vector3D<double>(cappedVel.X, velocity.Y, cappedVel.Z);
            }
        }
        else {
            if (hVel.Length > maxhAirSpeed) {
                var cappedVel = Vector3D.Normalize(hVel) * maxhAirSpeed;
                velocity = new Vector3D<double>(cappedVel.X, velocity.Y, cappedVel.Z);
            }
        }

        // clamp fallspeed
        if (Math.Abs(velocity.Y) > maxVSpeed) {
            var cappedVel = maxVSpeed;
            velocity.Y = cappedVel * Math.Sign(velocity.Y);
        }

        // clamp accel (only Y for now, other axes aren't used)
        if (Math.Abs(accel.Y) > maxAccel) {
            accel.Y = maxAccel * Math.Sign(accel.Y);
        }

        // clamp
        if (Math.Abs(velocity.X) < epsilon) {
            velocity.X = 0;
        }

        if (Math.Abs(velocity.Y) < epsilon) {
            velocity.Y = 0;
        }

        if (Math.Abs(velocity.Z) < epsilon) {
            velocity.Z = 0;
        }
    }

    private void applyFriction() {
        // ground friction
        if (!pressedMovementKey) {
            if (onGround) {
                var f = friction;
                velocity *= f;
            }
            else {
                var f = airFriction;
                velocity *= f;
            }
        }
    }

    private void updateGravity(double dt) {
        if (!onGround) {
            accel.Y = -gravity;
        }
        else {
            accel.Y = 0;
        }
    }

    private void updateInputVelocity(double dt) {
        if (!Game.instance.focused) {
            return;
        }
        // convert strafe vector into actual movement

        if (strafeVector.X != 0 || strafeVector.Y != 0) {
            // if air, lessen control
            moveSpeed = onGround ? groundMoveSpeed : airMoveSpeed;

            // first, normalise (v / v.length) then multiply with movespeed
            strafeVector = Vector2D.Normalize(strafeVector) * moveSpeed;

            Vector3D<double> moveVector = strafeVector.Y * forward +
                                          strafeVector.X *
                                          Vector3D.Normalize(Vector3D.Cross(Vector3D<double>.UnitY, forward));
            moveVector.Y = 0;
            // specialcase for stopping
            //if (velocity.Length == 0) {
                velocity += new Vector3D<double>(moveVector.X, 0, moveVector.Z);
            //}
            //else {
            //    velocity += moveVector;
            //}

        }

        if (jumping && onGround) {
            velocity.Y = jumpSpeed;
            onGround = false;
            jumping = false;
        }
    }

    [Pure]
    private AABB calcAABB(Vector3D<double> pos) {
        var size = 0.5;
        var sizehalf = 0.25;
        var height = 1.75;
        return AABB.fromSize(new Vector3D<double>(pos.X - sizehalf, pos.Y, pos.Z - sizehalf),
            new Vector3D<double>(size, height, size));
    }

    private void collision(double dt) {
        var blockPos = world.toBlockPos(position);
        // collect potential collision targets
        List<AABB> collisionTargets = new List<AABB>();
        foreach (Vector3D<int> target in (Vector3D<int>[]) [blockPos, new Vector3D<int>(blockPos.X, blockPos.Y + 1, blockPos.Z)]) {
            foreach (var neighbour in world.getBlocksInBox(target + new Vector3D<int>(-1, -1, -1), target + new Vector3D<int>(1, 1, 1))) {
                var block = world.getBlock(neighbour);
                var blockAABB = world.getAABB(neighbour.X, neighbour.Y, neighbour.Z, block);
                if (blockAABB == null) {
                    continue;
                }

                collisionTargets.Add(blockAABB);
            }
        }

        // Y axis resolution
        position.Y += velocity.Y * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabbY = calcAABB(new Vector3D<double>(position.X, position.Y, position.Z));
            if (AABB.isCollision(aabbY, blockAABB)) {
                // left side
                if (velocity.Y > 0 && aabbY.maxY >= blockAABB.minY) {
                    var diff = blockAABB.minY - aabbY.maxY;
                    if (diff < velocity.Y) {
                        position.Y += diff;
                        velocity.Y = 0;
                    }
                }

                else if (velocity.Y < 0 && aabbY.minY <= blockAABB.maxY) {
                    var diff = blockAABB.maxY - aabbY.minY;
                    if (diff > velocity.Y) {
                        position.Y += diff;
                        velocity.Y = 0;
                    }
                }
            }
        }

        // X axis resolution
        position.X += velocity.X * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabbX = calcAABB(new Vector3D<double>(position.X, position.Y, position.Z));

            if (AABB.isCollision(aabbX, blockAABB)) {
                // left side
                if (velocity.X > 0 && aabbX.maxX >= blockAABB.minX) {
                    var diff = blockAABB.minX - aabbX.maxX;
                    if (diff < velocity.X) {
                        position.X += diff;
                        velocity.X = 0;
                    }
                }

                else if (velocity.X < 0 && aabbX.minX <= blockAABB.maxX) {
                    var diff = blockAABB.maxX - aabbX.minX;
                    if (diff > velocity.X) {
                        position.X += diff;
                        velocity.X = 0;
                    }
                }
            }
        }

        position.Z += velocity.Z * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabbZ = calcAABB(new Vector3D<double>(position.X, position.Y, position.Z));

            if (AABB.isCollision(aabbZ, blockAABB)) {
                if (velocity.Z > 0 && aabbZ.maxZ >= blockAABB.minZ) {
                    var diff = blockAABB.minZ - aabbZ.maxZ;
                    //Console.Out.WriteLine("d: " + diff);
                    if (diff < velocity.Z) {
                        position.Z += diff;
                        velocity.Z = 0;
                    }
                }

                else if (velocity.Z < 0 && aabbZ.minZ <= blockAABB.maxZ) {
                    var diff = blockAABB.maxZ - aabbZ.minZ;
                    if (diff > velocity.Z) {
                        position.Z += diff;
                        velocity.Z = 0;
                    }
                }
            }
        }

        // is player on ground? check slightly below
        var groundCheck = calcAABB(new Vector3D<double>(position.X, position.Y - epsilonGroundCheck, position.Z));
        onGround = false;
        foreach (var blockAABB in collisionTargets) {
            if (AABB.isCollision(blockAABB, groundCheck)) {
                onGround = true;
            }
        }


    }

    public void updatePickBlock(IKeyboard keyboard, Key key, int scancode) {
        if (key >= Key.Number0 && key <= Key.Number9) {
            pickBlock = key - Key.Number0;
        }
    }

    public void updateInput(double dt) {
        pressedMovementKey = false;
        var keyboard = Game.instance.keyboard;

        if (keyboard.IsKeyPressed(Key.ShiftLeft)) {
            moveSpeed *= 5;
        }

        strafeVector = new Vector2D<double>(0, 0);

        if (keyboard.IsKeyPressed(Key.W)) {
            // Move forwards
            strafeVector.Y += 1;
            pressedMovementKey = true;
        }

        if (keyboard.IsKeyPressed(Key.S)) {
            //Move backwards
            strafeVector.Y -= 1;
            pressedMovementKey = true;
        }

        if (keyboard.IsKeyPressed(Key.A)) {
            //Move left
            strafeVector.X -= 1;
            pressedMovementKey = true;
        }

        if (keyboard.IsKeyPressed(Key.D)) {
            //Move right
            strafeVector.X += 1;
            pressedMovementKey = true;
        }

        if (keyboard.IsKeyPressed(Key.Space) && onGround) {
            jumping = true;
            pressedMovementKey = true;
        }
    }
}