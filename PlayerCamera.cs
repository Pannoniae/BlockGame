using System.Numerics;

namespace BlockGame;

public class PlayerCamera {
    private Player player;

    public Vector3 prevPosition;
    public Vector3 position;
    public Vector3 forward;

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
        calculateFrustum();
    }

    public void setViewport(float width, float height) {
        viewportWidth = width;
        viewportHeight = height;
        aspectRatio = viewportWidth / viewportHeight;
    }

    public void calculateFrustum() {
        var view = getViewMatrix(1);
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
        cameraDirection.X = MathF.Cos(DegreesToRadians(yaw)) *
                            MathF.Cos(DegreesToRadians(pitch));
        cameraDirection.Y = MathF.Sin(DegreesToRadians(pitch));
        cameraDirection.Z = MathF.Sin(DegreesToRadians(yaw)) *
                            MathF.Cos(DegreesToRadians(pitch));

        forward = Vector3.Normalize(cameraDirection);
    }

    public Vector3 CalculateForwardVector() {
        var p = 0;

        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Cos(DegreesToRadians(yaw));
        cameraDirection.Y = 0;
        cameraDirection.Z = MathF.Sin(DegreesToRadians(yaw));

        return Vector3.Normalize(cameraDirection);
    }

    public Matrix4x4 getViewMatrix(double interp) {
        var interpPos = Vector3.Lerp(prevPosition, position, (float)interp);
        var iBob = float.DegreesToRadians(float.Lerp(prevBob, bob, (float)interp));
        return Matrix4x4.CreateLookAtLeftHanded(interpPos, interpPos + forward, up);
        //* Matrix4x4.CreateRotationZ(MathF.Sin(iBob * 5))
        //* Matrix4x4.CreateRotationX(MathF.Cos(iBob * 5));
    }

    public Matrix4x4 getProjectionMatrix() {
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(hfov2vfov(hfov), aspectRatio, 0.1f, 400.0f);
    }

    public static float DegreesToRadians(float degrees) {
        return MathF.PI / 180f * degrees;
    }

    public float hfov2vfov(float hfov) {
        return 1.0f / MathF.Tan(hfov * 0.017453292519943295f / 2.0f);
    }
}