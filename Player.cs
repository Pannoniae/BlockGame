using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace BlockGame;

public class Player {
    public Camera camera;

    public AABB aabb;

    // entity positions are at feet
    public Vector3D<double> position;
    public Vector3D<double> velocity;

    public Vector3D<double> forward;

    public const double maxSpeed = 4;
    public const double friction = 0.05;
    public const double epsilon = 0.001;


    public Player(int x, int y, int z) {
        position = new Vector3D<double>(x, y, z);
        camera = new Camera(new Vector3(x, y, z), Vector3.UnitZ * 1, Vector3.UnitY,
            (float)Game.initialWidth / Game.initialHeight);
        var size = 0.5;
        var sizehalf = 0.25;
        var height = 1.75;
        aabb = new AABB(new Vector3D<double>(x - sizehalf, y, z - sizehalf), new Vector3D<double>(size, height, size));
    }


    public void update() {

        position = collision(position + velocity);

        camera.position = new Vector3((float)position.X, (float)position.Y, (float)position.Z);
        var f = camera.CalculateForwardVector();
        forward = new Vector3D<double>(f.X, f.Y, f.Z);
    }

    private Vector3D<double> collision(Vector3D<double> newPos) {
        return newPos;
    }

    public void updateInput(double dt) {
        var pressedMovementKey = false;

        var keyboard = Game.instance.keyboard;
        var moveSpeed = 0.05f * (float)dt;

        if (keyboard.IsKeyPressed(Key.ShiftLeft)) {
            moveSpeed *= 5;
        }

        var strafeVector = new Vector2D<double>(0, 0);

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

        // convert strafe vector into actual movement

        if (strafeVector.X != 0 || strafeVector.Y != 0) {
            // first, normalise (v / v.length) then multiply with movespeed
            strafeVector = Vector2D.Normalize(strafeVector) * moveSpeed;

            Vector3D<double> moveVector = strafeVector.Y * forward +
                                 strafeVector.X * Vector3D.Normalize(Vector3D.Cross(Vector3D<double>.UnitY, forward));

            //moveVector.Z = 0;
            velocity += moveVector;
        }

        // clamp max speed
        if (velocity.Length > maxSpeed * dt) {
            velocity = Vector3D.Normalize(velocity) * maxSpeed * dt;
        }

        if (!pressedMovementKey) {

            var f = friction * dt;
            if (f < Math.Abs(velocity.X)) {
                velocity.X -= f * Math.Sign(velocity.X);
            }
            else {
                velocity.X = 0;
            }
            if (f < Math.Abs(velocity.Z)) {
                velocity.Z -= f * Math.Sign(velocity.Z);
            }
            else {
                velocity.Z = 0;
            }
        }

        // clamp
        if (Math.Abs(velocity.X) < epsilon * dt) {
            velocity.X = 0;
        }

        if (Math.Abs(velocity.Y) < epsilon * dt) {
            velocity.Y = 0;
        }

        if (Math.Abs(velocity.Z) < epsilon * dt) {
            velocity.Z = 0;
        }
    }
}