using System.Numerics;
using BlockGame.ui;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame;

public class PlayerCamera {
    private Player player;

    public Vector3D prevPosition;
    public Vector3D position;
    public Vector3D forward;

    public Vector3D renderPosition(double interp) => Vector3D.Lerp(prevPosition, position, interp);

    public float renderBob(double interp) => float.Lerp(prevBob, bob, (float)interp);

    public Vector3D up;
    public float viewportWidth;
    public float viewportHeight;

    public float yaw { get; set; } = 90f;
    public float pitch { get; set; }

    public float vfov => Settings.instance.FOV;

    // FOV properties
    private float normalFov = 70.0f;
    private float underwaterFov = 60.0f; // Wider FOV underwater to simulate refraction
    private float currentFov;
    private float targetFov;

    // in degrees
    public float bob;
    public float prevBob;
    private float aspectRatio;

    public BoundingFrustum frustum;

    public PlayerCamera(Player player, Vector3D position, Vector3D forward, Vector3D up, float viewportWidth, float viewportHeight) {
        this.player = player;
        prevPosition = position;
        this.position = position;
        this.viewportWidth = viewportWidth;
        this.viewportHeight = viewportHeight;
        aspectRatio = this.viewportWidth / this.viewportHeight;
        this.forward = forward;
        this.up = up;
        
        currentFov = normalFov;
        targetFov = normalFov;
        
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

    public void updateFOV(bool isUnderwater, double dt) {
        // Set target FOV based on underwater status
        targetFov = isUnderwater ? underwaterFov : normalFov;
        
        // Smooth transition between FOVs
        float transitionSpeed = 12.0f;
        float step = (float)(transitionSpeed * dt);
        
        if (Math.Abs(currentFov - targetFov) > 0.01f) {
            currentFov = Meth.lerp(currentFov, targetFov, step);
        }
    }

    public void ModifyDirection(float xOffset, float yOffset) {
        yaw -= xOffset;
        pitch -= yOffset;

        //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
        pitch = Math.Clamp(pitch, -Constants.maxPitch, Constants.maxPitch);

        var cameraDirection = Vector3D.Zero;
        cameraDirection.X = MathF.Cos(Meth.deg2rad(yaw)) *
                            MathF.Cos(Meth.deg2rad(pitch));
        cameraDirection.Y = MathF.Sin(Meth.deg2rad(pitch));
        cameraDirection.Z = MathF.Sin(Meth.deg2rad(yaw)) *
                            MathF.Cos(Meth.deg2rad(pitch));

        forward = Vector3D.Normalize(cameraDirection);
        up = Vector3D.Normalize(Vector3D.Cross(Vector3D.Cross(forward, Vector3D.UnitY), forward));
    }

    public Vector3 CalculateForwardVector() {

        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Cos(Meth.deg2rad(yaw));
        cameraDirection.Y = 0;
        cameraDirection.Z = MathF.Sin(Meth.deg2rad(yaw));

        return Vector3.Normalize(cameraDirection);
    }



    /// <summary>
    /// Returns a view matrix that moves with the player.
    /// </summary>
    public Matrix4x4 getViewMatrix(double interp) {
        var interpPos = renderPosition(interp);
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = (float)double.Lerp(player.prevTotalTraveled, player.totalTraveled, interp);
        var factor = 0.4f;
        var factor2 = 0.15f;
        return Matrix4x4.CreateLookAtLeftHanded(interpPos.toVec3(), interpPos.toVec3() + forward.toVec3(), up.toVec3())
               * Matrix4x4.CreateRotationZ(MathF.Sin(tt) * iBob * factor)
               * Matrix4x4.CreateRotationX(-Math.Abs(MathF.Cos(tt)) * iBob * factor)
               * Matrix4x4.CreateRotationY(MathF.Sin(tt) * iBob * factor2);
    }

    /// <summary>
    /// Returns a view matrix that doesn't move with the player.
    /// </summary>
    public Matrix4x4 getStaticViewMatrix(double interp) {
        var interpPos = Vector3.Zero;
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = (float)double.Lerp(player.prevTotalTraveled, player.totalTraveled, interp);
        var factor = 0.4f;
        var factor2 = 0.15f;
        return Matrix4x4.CreateLookAtLeftHanded(interpPos, interpPos + forward.toVec3(), up.toVec3())
               * Matrix4x4.CreateRotationZ(MathF.Sin(tt) * iBob * factor)
               * Matrix4x4.CreateRotationX(-Math.Abs(MathF.Cos(tt)) * iBob * factor)
               * Matrix4x4.CreateRotationY(MathF.Sin(tt) * iBob * factor2);
    }

    public Matrix4x4 getViewMatrixRH(double interp) {
        var interpPos = renderPosition(interp);
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = (float)double.Lerp(player.prevTotalTraveled, player.totalTraveled, interp);
        var factor = 0.4f;
        var factor2 = 0.15f;
        return Matrix4x4.CreateLookAt(interpPos.toVec3(), interpPos.toVec3() + forward.toVec3(), up.toVec3())
               * Matrix4x4.CreateRotationZ(MathF.Sin(tt) * iBob * factor)
               * Matrix4x4.CreateRotationX(-Math.Abs(MathF.Cos(tt)) * iBob * factor)
               * Matrix4x4.CreateRotationY(MathF.Sin(tt) * iBob * factor2);
    }


    /// <summary>
    /// Gets the view matrix for the held hand/block.
    /// </summary>
    public Matrix4x4 getHandViewMatrix(double interp) {
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = double.Lerp(player.prevTotalTraveled, player.totalTraveled, interp);
        var factor = 2f;
        //var axisZ = new Vector3(1f, 0, 1f);
        //var axisX = new Vector3(1f, 0, -1f);
        // why 30 degrees? no bloody idea
        var axisZ = Vector3.Normalize(Vector3.Transform(new Vector3(0, 0, 1), Matrix4x4.CreateRotationY(Meth.deg2rad(30f))));
        var axisX = Vector3.Normalize(Vector3.Transform(new Vector3(1, 0, 0), Matrix4x4.CreateRotationY(Meth.deg2rad(30f))));

        return Matrix4x4.CreateLookAtLeftHanded(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY)
               * Matrix4x4.CreateFromAxisAngle(axisZ, (float)(Math.Sin(tt) * iBob * factor))
               * Matrix4x4.CreateFromAxisAngle(axisX, (float)(Math.Abs(Math.Cos(tt)) * iBob * factor));

    }

    public Matrix4x4 getProjectionMatrix() {
        // render distance, or minimum 128/8chunks (so depthtest isn't completely inaccurate)
        var maxPlane = Math.Max(128, (Settings.instance.renderDistance * 2) * Chunk.CHUNKSIZE);
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Meth.deg2rad(currentFov), aspectRatio, 0.1f, maxPlane);
    }

    public Matrix4x4 getFixedProjectionMatrix() {
        // render distance, or minimum 128/8chunks (so depthtest isn't completely inaccurate)
        var maxPlane = Math.Max(128, (Settings.instance.renderDistance * 2) * Chunk.CHUNKSIZE);
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Meth.deg2rad(normalFov), aspectRatio, 0.1f, maxPlane);
    }


    /// <summary>
    /// Converts horizontal FOV to vertical FOV.
    /// </summary>
    public float hfov2vfov(float hfov) {
        return 2 * MathF.Atan(MathF.Tan(Meth.deg2rad(hfov) * 0.5f) / aspectRatio);
    }
}