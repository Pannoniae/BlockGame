using System.Numerics;

namespace BlockGame;

public class Camera {
    public Vector3 prevPosition { get; set; }
    public Vector3 position { get; set; }
    public Vector3 forward { get; set; }

    public Vector3 up { get; private set; }
    public float aspectRatio { get; set; }

    public float yaw { get; set; } = 90f;
    public float pitch { get; set; }

    public float zoom = 45f;

    public Camera(Vector3 position, Vector3 forward, Vector3 up, float aspectRatio) {
        prevPosition = position;
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
        return Matrix4x4.CreateLookAtLeftHanded(interpPos, interpPos + forward, up);
    }

    public Matrix4x4 getProjectionMatrix() {
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(DegreesToRadians(zoom), aspectRatio, 0.1f, 400.0f);
    }

    public static float DegreesToRadians(float degrees) {
        return MathF.PI / 180f * degrees;
    }
}