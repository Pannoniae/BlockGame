using System.Numerics;

namespace BlockGame;

public class Camera {
    public Vector3 position { get; set; }
    public Vector3 forward { get; set; }

    public Vector3 up { get; private set; }
    public float aspectRatio { get; set; }

    public float yaw { get; set; } = 90f;
    public float pitch { get; set; }

    public float zoom = 45f;

    public Camera(Vector3 position, Vector3 forward, Vector3 up, float aspectRatio) {
        this.position = position;
        this.aspectRatio = aspectRatio;
        this.forward = forward;
        this.up = up;
    }

    public void ModifyZoom(float zoomAmount) {
        //We don't want to be able to zoom in too close or too far away so clamp to these values
        zoom = Math.Clamp(zoom - zoomAmount, 1.0f, 45f);
    }

    public void ModifyDirection(float xOffset, float yOffset) {
        yaw -= xOffset;
        pitch -= yOffset;

        //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
        pitch = Math.Clamp(pitch, -89f, 89f);

        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Cos(DegreesToRadians(yaw)) *
                            MathF.Cos(DegreesToRadians(pitch));
        cameraDirection.Y = MathF.Sin(DegreesToRadians(pitch));
        cameraDirection.Z = MathF.Sin(DegreesToRadians(yaw)) *
                            MathF.Cos(DegreesToRadians(pitch));

        forward = Vector3.Normalize(cameraDirection);
    }

    public Matrix4x4 getViewMatrix() {
        return Matrix4x4.CreateLookAtLeftHanded(position, position + forward, up);
    }

    public Matrix4x4 getProjectionMatrix() {
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(DegreesToRadians(zoom), aspectRatio, 0.5f, 400.0f);
    }

    public static float DegreesToRadians(float degrees) {
        return MathF.PI / 180f * degrees;
    }
}