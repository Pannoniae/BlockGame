using System.Numerics;
using BlockGame.util;

namespace BlockGame;

public class PlayerCamera {
    private Player player;

    public Vector3 prevPosition;
    public Vector3 position;
    public Vector3 forward;

    public Vector3 renderPosition(double interp) => Vector3.Lerp(prevPosition, position, (float)interp);
    public float renderBob(double interp) => float.Lerp(prevBob, bob, (float)interp);

    public Vector3 up { get; private set; }
    public float viewportWidth;
    public float viewportHeight;

    public float yaw { get; set; } = 90f;
    public float pitch { get; set; }

    public float hfov = 70;


    // in degrees
    public float bob;
    public float prevBob;
    private float aspectRatio;

    public BoundingFrustum frustum;

    public PlayerCamera(Player player, Vector3 position, Vector3 forward, Vector3 up, float viewportWidth, float viewportHeight) {
        this.player = player;
        prevPosition = position;
        this.position = position;
        this.viewportWidth = viewportWidth;
        this.viewportHeight = viewportHeight;
        aspectRatio = this.viewportWidth / this.viewportHeight;
        this.forward = forward;
        this.up = up;
        var view = getViewMatrix(1);
        var proj = getProjectionMatrix();
        var mat = view * proj;
        frustum = new BoundingFrustum(mat);
        calculateFrustum(1);
    }

    public void setViewport(float width, float height) {
        viewportWidth = width;
        viewportHeight = height;
        aspectRatio = viewportWidth / viewportHeight;
    }

    public void calculateFrustum(double interp) {
        var view = getViewMatrix(interp);
        var proj = getProjectionMatrix();
        var mat = view * proj;
        frustum.Matrix = mat;
    }

    public void modifyFOV(float fov) {
        //We don't want to be able to zoom in too close or too far away so clamp to these values
        hfov = Math.Clamp(hfov - fov, 30f, 150f);
    }

    public void ModifyDirection(float xOffset, float yOffset) {
        yaw -= xOffset;
        pitch -= yOffset;

        //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
        pitch = Math.Clamp(pitch, -Constants.maxPitch, Constants.maxPitch);

        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Cos(Utils.deg2rad(yaw)) *
                            MathF.Cos(Utils.deg2rad(pitch));
        cameraDirection.Y = MathF.Sin(Utils.deg2rad(pitch));
        cameraDirection.Z = MathF.Sin(Utils.deg2rad(yaw)) *
                            MathF.Cos(Utils.deg2rad(pitch));

        forward = Vector3.Normalize(cameraDirection);
    }

    public Vector3 CalculateForwardVector() {
        var p = 0;

        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Cos(Utils.deg2rad(yaw));
        cameraDirection.Y = 0;
        cameraDirection.Z = MathF.Sin(Utils.deg2rad(yaw));

        return Vector3.Normalize(cameraDirection);
    }

    public Matrix4x4 getViewMatrix(double interp) {
        var interpPos = renderPosition(interp);
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = (float)double.Lerp(player.prevTotalTraveled, player.totalTraveled, interp);
        var factor = 0.4f;
        var factor2 = 0.15f;
        return Matrix4x4.CreateLookAtLeftHanded(interpPos, interpPos + forward, up)
               * Matrix4x4.CreateRotationZ(MathF.Sin(tt) * iBob * factor)
               * Matrix4x4.CreateRotationX(-Math.Abs(MathF.Cos(tt)) * iBob * factor)
               * Matrix4x4.CreateRotationY(MathF.Sin(tt) * iBob * factor2);

    }

    public Matrix4x4 getHandViewMatrix(double interp) {
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = double.Lerp(player.prevTotalTraveled, player.totalTraveled, interp);
        var factor = 4f;
        var factor2 = 1.5f;
        return Matrix4x4.CreateLookAtLeftHanded(Vector3.Zero, Vector3.UnitZ, up)
               * Matrix4x4.CreateRotationZ((float)(Math.Sin(tt) * iBob * factor))
               * Matrix4x4.CreateRotationX((float)(Math.Abs(Math.Cos(tt)) * iBob * factor))
               * Matrix4x4.CreateRotationY((float)(Math.Sin(tt) * iBob * factor2));

    }

    public Matrix4x4 getProjectionMatrix() {
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(hfov2vfov(hfov), aspectRatio, 0.1f, World.RENDERDISTANCE * Chunk.CHUNKSIZE);
    }

    public static float hfov2vfov(float hfov) {
        return 1.0f / MathF.Tan(hfov * 0.017453292519943295f / 2.0f);
    }
}