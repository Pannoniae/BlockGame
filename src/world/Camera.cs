using System.Numerics;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.meth;
using Molten.DoublePrecision;

namespace BlockGame;

public enum CameraMode {
    FirstPerson,
    ThirdPersonBehind,
    ThirdPersonFront
}

public class Camera {
    private Entity? player;

    public Vector3D prevPosition;
    public Vector3D position;
    public Vector3D forward;
    
    
    public Vector3D pForward;
    public Vector3D prevpForward;

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
    public bool frustumFrozen = false;
    
    public CameraMode mode = CameraMode.FirstPerson;
    private const double THIRD_PERSON_DISTANCE = 4.0;
    
    public Camera(float viewportWidth, float viewportHeight) {
        this.viewportWidth = viewportWidth;
        this.viewportHeight = viewportHeight;
        aspectRatio = this.viewportWidth / this.viewportHeight;
        
        currentFov = normalFov;
        targetFov = normalFov;
        
        var view = getViewMatrix(1);
        var proj = getProjectionMatrix();
        var mat = view * proj;
        frustum = new BoundingFrustum(mat);
        calculateFrustum(1);
    }

    public Camera(Player player, Vector3D position, Vector3D forward, Vector3D up, float viewportWidth, float viewportHeight) {
        this.player = player;
        prevPosition = position;
        this.position = position;
        this.viewportWidth = viewportWidth;
        this.viewportHeight = viewportHeight;
        aspectRatio = this.viewportWidth / this.viewportHeight;
        this.forward = forward;
        pForward = forward;
        prevpForward = forward;
        this.up = up;
        
        currentFov = normalFov;
        targetFov = normalFov;
        
        var view = getViewMatrix(1);
        var proj = getProjectionMatrix();
        var mat = view * proj;
        frustum = new BoundingFrustum(mat);
        calculateFrustum(1);

        
    }
    
    public void setPlayer(Entity player) {
        this.player = player;

        forward = player.forward;
        pForward = player.forward;
        prevpForward = player.forward;
        up = Vector3D.UnitY;
        position = player.position;
        prevPosition = player.position;
        yaw = player.rotation.Y;
        pitch = player.rotation.X;
        
        // update forward vector!
        var view = getViewMatrix(1);
        var proj = getProjectionMatrix();
        var mat = view * proj;
        frustum = new BoundingFrustum(mat);
        calculateFrustum(1);
    }
    
    public void setPosition(Vector3D newPos) {
        prevPosition = position;
        position = newPos;
        
    }
    
    public void cycleMode() {
        mode = mode switch {
            CameraMode.FirstPerson => CameraMode.ThirdPersonBehind,
            CameraMode.ThirdPersonBehind => CameraMode.ThirdPersonFront,
            CameraMode.ThirdPersonFront => CameraMode.FirstPerson,
            _ => CameraMode.FirstPerson
        };
    }
    
    /**
     * TODO the huge fucking problem with this method is that we're mixing up per-frame data (forward/pitch/yaw) with per-update data (position/prevPosition which is interpolated in rendering)
     * i.e. it jitters like hell in non-first-person modes when you f5
     * to be fixed but idk how yet
     */
    public void updatePosition(double dt) {
        if (player is not Player p) return;
        
        var cameraDirection = Vector3D.Zero;
        cameraDirection.X = MathF.Cos(Meth.deg2rad(yaw)) *
                            MathF.Cos(Meth.deg2rad(pitch));
        cameraDirection.Y = MathF.Sin(Meth.deg2rad(pitch));
        cameraDirection.Z = MathF.Sin(Meth.deg2rad(yaw)) *
                            MathF.Cos(Meth.deg2rad(pitch));
        
        // if third person front, invert
        if (mode == CameraMode.ThirdPersonFront) {
            cameraDirection = -cameraDirection;
        }

        pForward = Vector3D.Normalize(cameraDirection);
        
        
        var trueEyeHeight = p.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
        var basePos = new Vector3D(p.position.X, p.position.Y + trueEyeHeight, p.position.Z);
        var basePrevPos = new Vector3D(p.prevPosition.X, p.prevPosition.Y + trueEyeHeight, p.prevPosition.Z);
        
        switch (mode) {
            case CameraMode.FirstPerson:
                position = basePos;
                prevPosition = basePrevPos;
                break;
                
            case CameraMode.ThirdPersonBehind:
                var backwardOffset = pForward * -THIRD_PERSON_DISTANCE;
                var prevBackwardOffset = prevpForward * -THIRD_PERSON_DISTANCE;
                position = basePos + backwardOffset;
                prevPosition = basePrevPos + prevBackwardOffset;
                // TODO: Add collision detection to prevent camera clipping through blocks
                break;
                
            case CameraMode.ThirdPersonFront:
                var forwardOffset = (pForward) * THIRD_PERSON_DISTANCE;
                var prevForwardOffset = (prevpForward) * THIRD_PERSON_DISTANCE;
                position = basePos + forwardOffset;
                prevPosition = basePrevPos + prevForwardOffset;
                
                // TODO: Add collision detection to prevent camera clipping through blocks
                break;
        }
        
        // Update bob
        if (Math.Abs(p.velocity.withoutY().Length()) > 0.0001 && p.onGround) {
            bob = Math.Clamp((float)(p.velocity.Length() / 4), 0, 1);
        } else {
            bob *= 0.935f;
        }
    }

    public void setViewport(float width, float height) {
        viewportWidth = width;
        viewportHeight = height;
        aspectRatio = viewportWidth / viewportHeight;
    }

    public void calculateFrustum(double interp) {
        if (!frustumFrozen) {
            var view = getViewMatrix(interp);
            var proj = getProjectionMatrix();
            var mat = view * proj;
            frustum.Matrix = mat;
        }
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
        
        // if third person front, invert
        if (mode == CameraMode.ThirdPersonFront) {
            //xOffset = -xOffset;
            yOffset = -yOffset;
        }
        
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
        
        // if third person front, invert
        if (mode == CameraMode.ThirdPersonFront) {
            cameraDirection = -cameraDirection;
        }

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
        var interpForward = mode != CameraMode.FirstPerson ? Vector3D.Lerp(prevpForward, pForward, interp) : forward;
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = 0f;
        if (player is Player p) {
            tt = (float)double.Lerp(p.prevTotalTraveled, p.totalTraveled, interp);
        }
        var factor = 0.4f;
        var factor2 = 0.15f;
        return Matrix4x4.CreateLookAtLeftHanded(interpPos.toVec3(), interpPos.toVec3() + interpForward.toVec3(), up.toVec3())
               * Matrix4x4.CreateRotationZ(MathF.Sin(tt) * iBob * factor)
               * Matrix4x4.CreateRotationX(-Math.Abs(MathF.Cos(tt)) * iBob * factor)
               * Matrix4x4.CreateRotationY(MathF.Sin(tt) * iBob * factor2);
    }

    /// <summary>
    /// Returns a view matrix that doesn't move with the player.
    /// </summary>
    public Matrix4x4 getStaticViewMatrix(double interp) {
        var interpPos = Vector3.Zero;
        var interpForward = mode != CameraMode.FirstPerson ? Vector3D.Lerp(prevpForward, pForward, interp) : forward;
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = 0f;
        if (player is Player p) {
            tt = (float)double.Lerp(p.prevTotalTraveled, p.totalTraveled, interp);
        }
        var factor = 0.4f;
        var factor2 = 0.15f;
        return Matrix4x4.CreateLookAtLeftHanded(interpPos, interpPos + interpForward.toVec3(), up.toVec3())
               * Matrix4x4.CreateRotationZ(MathF.Sin(tt) * iBob * factor)
               * Matrix4x4.CreateRotationX(-Math.Abs(MathF.Cos(tt)) * iBob * factor)
               * Matrix4x4.CreateRotationY(MathF.Sin(tt) * iBob * factor2);
    }

    public Matrix4x4 getViewMatrixRH(double interp) {
        var interpPos = renderPosition(interp);
        var interpForward = mode != CameraMode.FirstPerson ? Vector3D.Lerp(prevpForward, pForward, interp) : forward;
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = 0f;
        if (player is Player p) {
            tt = (float)double.Lerp(p.prevTotalTraveled, p.totalTraveled, interp);
        }
        var factor = 0.4f;
        var factor2 = 0.15f;
        return Matrix4x4.CreateLookAt(interpPos.toVec3(), interpPos.toVec3() + interpForward.toVec3(), up.toVec3())
               * Matrix4x4.CreateRotationZ(MathF.Sin(tt) * iBob * factor)
               * Matrix4x4.CreateRotationX(-Math.Abs(MathF.Cos(tt)) * iBob * factor)
               * Matrix4x4.CreateRotationY(MathF.Sin(tt) * iBob * factor2);
    }


    /// <summary>
    /// Gets the view matrix for the held hand/block.
    /// </summary>
    public Matrix4x4 getHandViewMatrix(double interp) {
        var iBob = float.DegreesToRadians(renderBob(interp));
        var tt = 0.0;
        if (player is Player p) {
            tt = double.Lerp(p.prevTotalTraveled, p.totalTraveled, interp);
        }
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
        const float nearPlane = 0.1f;
        
        if (Settings.instance.reverseZ) {
            // reverse-Z: swap near and far, use infinite far plane
            return createReverseZProjectionMatrix(Meth.deg2rad(currentFov), aspectRatio, nearPlane, maxPlane);
        }
        
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Meth.deg2rad(currentFov), aspectRatio, nearPlane, maxPlane);
    }

    public Matrix4x4 getFixedProjectionMatrix() {
        // render distance, or minimum 128/8chunks (so depthtest isn't completely inaccurate)
        var maxPlane = Math.Max(128, (Settings.instance.renderDistance * 2) * Chunk.CHUNKSIZE);
        const float nearPlane = 0.1f;
        
        if (Settings.instance.reverseZ) {
            // reverse-Z: swap near and far, use infinite far plane
            return createReverseZProjectionMatrix(Meth.deg2rad(normalFov), aspectRatio, nearPlane, maxPlane);
        }
        
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(Meth.deg2rad(normalFov), aspectRatio, nearPlane, maxPlane);
    }


    /// <summary>
    /// Creates a reverse-Z projection matrix with infinite far plane for optimal depth precision or something. "Try the numbers until it works" -pannie
    /// Help from https://github.com/Qendolin/farz-poc/blob/main/src/client/java/com/qendolin/farz/Util.java:
    /// </summary>
    private static Matrix4x4 createReverseZProjectionMatrix(float fov, float aspectRatio, float nearPlane, int maxPlane) {
        var f = 1.0f / MathF.Tan(fov * 0.5f);

        // Reverse-Z, [0, 1] Range (Infinite Far)
        // Near maps to 1, Far (infinity) maps to 0

        var mat = new Matrix4x4 {
            M11 = f / aspectRatio,
            M22 = f,
            M33 = 0.0f,
            M34 = 1.0f, // Maps eye-space Z into W component
            M43 = nearPlane,
            M44 = 0.0f
        };

        //mat = Matrix4x4.Transpose(mat);

        return mat;
    }

    /// <summary>
    /// Converts horizontal FOV to vertical FOV.
    /// </summary>
    public float hfov2vfov(float hfov) {
        return 2 * MathF.Atan(MathF.Tan(Meth.deg2rad(hfov) * 0.5f) / aspectRatio);
    }
}