using System.Diagnostics.Contracts;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace BlockGame;

public class Player {
    public const double playerHeight = 1.8;
    public const double eyeHeight = 1.6;

    // is player walking on (colling with) ground
    public bool onGround;

    // is the player in the process of jumping
    public bool jumping;

    // jump cooldown to prevent player jumping immediately again
    // which we don't have
    public double jumpCD;

    public bool sneaking;

    public Camera camera;

    public AABB aabb;

    // entity positions are at feet
    public Vector3D<double> prevPosition;
    public Vector3D<double> position;
    public Vector3D<double> velocity;
    public Vector3D<double> accel;

    public Vector3D<double> forward;

    public Vector3D<double> inputVector;


    /// <summary>
    /// Used for transparent chunk sorting
    /// </summary>
    public Vector3D<double> lastSort = new(double.MinValue, double.MinValue, double.MinValue);

    public ushort pickBlock;
    public World world;
    public Vector2D<double> strafeVector = new(0, 0);
    public bool pressedMovementKey;

    public double lastPlace;
    public double lastBreak;


    // positions are feet positions
    public Player(World world, int x, int y, int z) {
        position = new Vector3D<double>(x, y, z);
        prevPosition = position;
        camera = new Camera(new Vector3(x, (float)(y + eyeHeight), z), Vector3.UnitZ * 1, Vector3.UnitY,
            (float)Constants.initialWidth / Constants.initialHeight);

        this.world = world;
        pickBlock = 1;
        var f = camera.CalculateForwardVector();
        forward = new Vector3D<double>(f.X, f.Y, f.Z);
        aabb = calcAABB(position);
    }


    public void update(double dt) {
        updateInputVelocity(dt);
        Console.Out.WriteLine("d: " + dt);
        velocity += accel * dt;
        //position += velocity * dt;
        clamp(dt);


        collision(dt);
        Console.Out.WriteLine(position);
        Console.Out.WriteLine(velocity);
        Console.Out.WriteLine(accel);
        //position.X += velocity.X * dt;
        //position.Y += velocity.Y * dt;
        //position.Z += velocity.Z * dt;
        applyInputMovement(dt);
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

    private void applyInputMovement(double dt) {
        velocity += inputVector;
    }

    private void resetFrameVars() {
        pressedMovementKey = false;
    }

    private void clamp(double dt) {
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

        if (velocity != Vector3D<double>.Zero) {
            // clamp max speed
            // If speed velocity is 0, we are fucked so check for that
            var hVel = new Vector3D<double>(velocity.X, 0, velocity.Z);
            if (onGround) {
                var maxSpeed = Constants.maxhSpeed;
                if (sneaking) {
                    maxSpeed = Constants.maxhSpeedSneak;
                }
                if (hVel.Length > maxSpeed) {
                    var cappedVel = Vector3D.Normalize(hVel) * maxSpeed;
                    velocity = new Vector3D<double>(cappedVel.X, velocity.Y, cappedVel.Z);
                }
            }
            else {
                var maxSpeed = Constants.maxhAirSpeed;
                if (sneaking) {
                    maxSpeed = Constants.maxhAirSpeedSneak;
                }
                if (hVel.Length > maxSpeed) {
                    var cappedVel = Vector3D.Normalize(hVel) * maxSpeed;
                    velocity = new Vector3D<double>(cappedVel.X, velocity.Y, cappedVel.Z);
                }
            }
        }

        // clamp fallspeed
        if (Math.Abs(velocity.Y) > Constants.maxVSpeed) {
            var cappedVel = Constants.maxVSpeed;
            velocity.Y = cappedVel * Math.Sign(velocity.Y);
        }

        // clamp accel (only Y for now, other axes aren't used)
        if (Math.Abs(accel.Y) > Constants.maxAccel) {
            accel.Y = Constants.maxAccel * Math.Sign(accel.Y);
        }

        // world bounds check
        var s = world.getWorldSize();
        position.X = Math.Clamp(position.X, 0, s.X);
        //position.Y = Math.Clamp(position.Y, 0, s.Y);
        position.Z = Math.Clamp(position.Z, 0, s.Z);
    }

    private void applyFriction() {
        // ground friction
        if (!pressedMovementKey) {
            if (onGround) {
                if (sneaking) {
                    velocity = Vector3D<double>.Zero;
                }
                else {
                    var f = Constants.friction;
                    velocity *= f;
                }
            }
            else {
                var f = Constants.airFriction;
                velocity *= f;
            }
        }
    }

    private void updateGravity(double dt) {
        if (!onGround) {
            accel.Y = -Constants.gravity;
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
            Constants.moveSpeed = onGround ? Constants.groundMoveSpeed : Constants.airMoveSpeed;

            // first, normalise (v / v.length) then multiply with movespeed
            strafeVector = Vector2D.Normalize(strafeVector) * Constants.moveSpeed;

            Vector3D<double> moveVector = strafeVector.Y * forward +
                                          strafeVector.X *
                                          Vector3D.Normalize(Vector3D.Cross(Vector3D<double>.UnitY, forward));


            moveVector.Y = 0;
            inputVector = new Vector3D<double>(moveVector.X, 0, moveVector.Z);

        }

        if (jumping && onGround) {
            velocity.Y = Constants.jumpSpeed;
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
                        velocity.Z = inputVector.Z;
                    }
                }

                else if (velocity.X < 0 && aabbX.minX <= blockAABB.maxX) {
                    var diff = blockAABB.maxX - aabbX.minX;
                    if (diff > velocity.X) {
                        position.X += diff;
                        velocity.X = 0;
                        velocity.Z = inputVector.Z;
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
                        velocity.X = inputVector.X;
                    }
                }

                else if (velocity.Z < 0 && aabbZ.minZ <= blockAABB.maxZ) {
                    var diff = blockAABB.maxZ - aabbZ.minZ;
                    if (diff > velocity.Z) {
                        position.Z += diff;
                        velocity.Z = 0;
                        velocity.X = inputVector.X;
                    }
                }
            }
        }

        // is player on ground? check slightly below
        var groundCheck = calcAABB(new Vector3D<double>(position.X, position.Y - Constants.epsilonGroundCheck, position.Z));
        onGround = false;
        foreach (var blockAABB in collisionTargets) {
            if (AABB.isCollision(blockAABB, groundCheck)) {
                onGround = true;
            }
        }


    }

    public void updatePickBlock(IKeyboard keyboard, Key key, int scancode) {
        if (key >= Key.Number0 && key <= Key.Number9) {
            pickBlock = (ushort)(key - Key.Number0);
        }
    }

    public void updateInput(double dt) {
        pressedMovementKey = false;
        var keyboard = Game.instance.keyboard;
        var mouse = Game.instance.mouse;

        sneaking = keyboard.IsKeyPressed(Key.ShiftLeft);

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

        if (mouse.IsButtonPressed(MouseButton.Left) && world.worldTime - lastBreak > Constants.breakDelay) {
            breakBlock();
        }

        if (mouse.IsButtonPressed(MouseButton.Right) && world.worldTime - lastPlace > Constants.placeDelay) {
            placeBlock();
        }
    }

    public void placeBlock() {
        if (Game.instance.previousPos.HasValue) {
            var pos = Game.instance.previousPos.Value;
            // don't intersect the player
            var aabb = world.getAABB(pos.X, pos.Y, pos.Z, world.player.pickBlock);
            if (aabb == null || !AABB.isCollision(world.player.aabb, aabb)) {
                world.setBlock(pos.X, pos.Y, pos.Z, world.player.pickBlock);
                world.player.lastPlace = world.worldTime;
            }
        }
    }

    public void breakBlock() {
        if (Game.instance.targetedPos.HasValue) {
            var pos = Game.instance.targetedPos.Value;
            world.setBlock(pos.X, pos.Y, pos.Z, 0);
            world.player.lastBreak = world.worldTime;
        }
    }
}