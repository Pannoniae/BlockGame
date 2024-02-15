using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace BlockGame;

public class Player {
    public const double maxSpeed = 5;
    public const double friction = 0.55;
    public const double epsilon = 0.0001;
    public const double playerHeight = 1.75;

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
    public static double moveSpeed = 4;
    private bool pressedMovementKey;


    // positions are feet positions
    public Player(World world, int x, int y, int z) {
        position = new Vector3D<double>(x, y, z);
        prevPosition = position;
        camera = new Camera(new Vector3(x, (float)(y + playerHeight), z), Vector3.UnitZ * 1, Vector3.UnitY,
            (float)Game.initialWidth / Game.initialHeight);

        this.world = world;
        pickBlock = 1;
        calcAABB();
    }


    public void update(double dt) {
        updatePhysics(dt);

        velocity += accel * dt;
        //position += velocity * dt;

        var newPos = position + velocity * dt;
        position = collision(newPos);

        camera.position = new Vector3((float)position.X, (float)(position.Y + playerHeight), (float)position.Z);
        camera.prevPosition = new Vector3((float)prevPosition.X, (float)(prevPosition.Y + playerHeight),
            (float)prevPosition.Z);
        var f = camera.CalculateForwardVector();
        forward = new Vector3D<double>(f.X, f.Y, f.Z);
        calcAABB();

        // after everything is done
        prevPosition = position;
    }

    private void updatePhysics(double dt) {
        // convert strafe vector into actual movement

        if (strafeVector.X != 0 || strafeVector.Y != 0) {
            // first, normalise (v / v.length) then multiply with movespeed
            strafeVector = Vector2D.Normalize(strafeVector) * moveSpeed;

            Vector3D<double> moveVector = strafeVector.Y * forward +
                                          strafeVector.X *
                                          Vector3D.Normalize(Vector3D.Cross(Vector3D<double>.UnitY, forward));

            velocity = moveVector;
        }


        // clamp max speed
        if (velocity.Length > maxSpeed) {
            velocity = Vector3D.Normalize(velocity) * maxSpeed;
        }

        if (!pressedMovementKey) {
            if (velocity.Length != 0) {
                var f = friction;
                //var fVec = f * -Vector3D.Normalize(velocity) * dt;
                //Console.Out.WriteLine(fVec);
                //if (f < velocity.Length) {
                velocity *= f;
                //}
                //else {
                //    velocity = new Vector3D<double>(0, 0, 0);
                //}
            }
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

    private void calcAABB() {
        var size = 0.5;
        var sizehalf = 0.25;
        var height = 1.75;
        aabb = AABB.fromSize(new Vector3D<double>(position.X - sizehalf, position.Y, position.Z - sizehalf),
            new Vector3D<double>(size, height, size));
    }

    private Vector3D<double> collision(Vector3D<double> newPos) {
        var blockPos = world.toBlockPos(newPos);
        // get neighbours
        foreach (Vector3D<int> target in (Vector3D<int>[]) [blockPos, new Vector3D<int>(blockPos.X, blockPos.Y + 1, blockPos.Z)]) {
            foreach (var direction in Direction.directions) {
                var neighbour = target + direction;
                var block = world.getBlock(neighbour);
                var blockAABB = world.getAABB(neighbour.X, neighbour.Y, neighbour.Z, block);
                if (blockAABB == null) {
                    continue;
                }

                var collide = AABB.isCollision(aabb, blockAABB);
                //Console.Out.WriteLine(collide);
                // time to resolve!
                if (collide) {
                    // don't let player into the block

                    // check each separate axis

                }
            }
        }

        return newPos;
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
    }
}