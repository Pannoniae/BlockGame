using System.Numerics;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.meth;
using BlockGame.world.chunk;
using Molten.DoublePrecision;

namespace BlockGame.world;

public enum CameraMode {
    FirstPerson,
    ThirdPersonBehind,
    ThirdPersonFront
}

public class Camera {
    private Entity? player;

    public Vector3D renderPosition(double interp) {
        if (player is not Player p) return Vector3D.Zero;
        var basePos = Vector3D.Lerp(p.prevPosition, p.position, interp);
        var trueEyeHeight = p.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
        basePos.Y += trueEyeHeight;

        return mode switch {
            CameraMode.FirstPerson => basePos,
            CameraMode.ThirdPersonBehind => clip(basePos, basePos + playerForward() * -3.0),
            CameraMode.ThirdPersonFront => clip(basePos, basePos + playerForward() * 2.0),
            _ => basePos
        };
    }

    private Vector3D playerForward() {
        if (player is not Player p) return Vector3D.UnitZ;
        var yaw = p.rotation.Y;
        var pitch = p.rotation.X;

        var cameraDirection = Vector3D.Zero;
        cameraDirection.X = MathF.Cos(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch));
        cameraDirection.Y = MathF.Sin(Meth.deg2rad(pitch));
        cameraDirection.Z = MathF.Sin(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch));

        return Vector3D.Normalize(cameraDirection);
    }

    private Vector3D clip(Vector3D pivot, Vector3D pos) {
        if (player is not Player p) return pos;

        // Raycast from player eye position towards intended camera position
        var dir = Vector3D.Normalize(pos - pivot);
        var dist = (pos - pivot).Length();
        var currentPos = pivot;
        const double stepSize = 0.003;
        const double cameraRadius = 0.2; // Small buffer to avoid clipping

        for (double d = 0; d < dist; d += stepSize) {
            currentPos = pivot + dir * d;
            var blockPos = currentPos.toBlockPos();

            // todo this isn't ACTUAL AABB checking, add it if necessary! or just leave it like this, idk
            if (p.world.isSelectableBlock(blockPos.X, blockPos.Y, blockPos.Z)) {
                // Hit a block, move camera back by the step size plus buffer
                return pivot + dir * Math.Max(0, d - stepSize - cameraRadius);
            }
        }

        return pos; // No collision, use position
    }

    public float renderBob(double interp) => float.Lerp(prevBob, bob, (float)interp);

    public float renderAirBob(double interp) => float.Lerp(prevAirBob, airBob, (float)interp);

    public Vector3D forward() {
        if (player is not Player p) return Vector3D.UnitZ;

        // For front-facing camera, look at the player's eye position
        if (mode == CameraMode.ThirdPersonFront) {
            var cameraPos = renderPosition(1.0); // Get current camera position
            var playerEyePos = p.position;
            var trueEyeHeight = p.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
            playerEyePos.Y += trueEyeHeight;

            return Vector3D.Normalize(playerEyePos - cameraPos);
        }

        // For normal cameras, use player rotation
        var yaw = p.rotation.Y;
        var pitch = p.rotation.X;

        var cameraDirection = Vector3D.Zero;
        cameraDirection.X = MathF.Cos(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch));
        cameraDirection.Y = MathF.Sin(Meth.deg2rad(pitch));
        cameraDirection.Z = MathF.Sin(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch));

        return Vector3D.Normalize(cameraDirection);
    }

    public Vector3D forward(double interp) {
        if (player is not Player p) return Vector3D.UnitZ;

        // For front-facing camera, look at the player's eye position
        if (mode == CameraMode.ThirdPersonFront) {
            var cameraPos = renderPosition(interp);
            var playerEyePos = p.position;
            var trueEyeHeight = p.sneaking ? Player.sneakingEyeHeight : Player.eyeHeight;
            playerEyePos.Y += trueEyeHeight;

            return Vector3D.Normalize(playerEyePos - cameraPos);
        }

        // For normal cameras, use player rotation
        var yaw = p.rotation.Y;
        var pitch = p.rotation.X;

        var cameraDirection = Vector3D.Zero;
        cameraDirection.X = MathF.Cos(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch));
        cameraDirection.Y = MathF.Sin(Meth.deg2rad(pitch));
        cameraDirection.Z = MathF.Sin(Meth.deg2rad(yaw)) * MathF.Cos(Meth.deg2rad(pitch));

        return Vector3D.Normalize(cameraDirection);
    }

    public Vector3D up() {
        var forward = this.forward();
        return Vector3D.Normalize(Vector3D.Cross(Vector3D.Cross(forward, Vector3D.UnitY), forward));
    }

    public Vector3D up(double interp) {
        var forward = this.forward(interp);
        return Vector3D.Normalize(Vector3D.Cross(Vector3D.Cross(forward, Vector3D.UnitY), forward));
    }

    public float viewportWidth;
    public float viewportHeight;

    public float vfov => Settings.instance.FOV;

    public Entity? player1 {
        set { player = value; }
        get { return player; }
    }

    // FOV properties
    private float normalFov = 70.0f;
    private float underwaterFov = 60.0f; // Wider FOV underwater to simulate refraction
    private float currentFov;
    private float targetFov;

    /** in degrees */
    public float bob;
    public float prevBob;

    /** It's like bobbing but in the air and affecting pitch */
    public float airBob;
    public float prevAirBob;

    private float aspectRatio;

    public BoundingFrustum frustum;
    public bool frustumFrozen = false;
    
    public CameraMode mode = CameraMode.FirstPerson;
    
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
    
    public void setPlayer(Entity player) {
        this.player = player;
        calculateFrustum(1);
    }
    
    public void cycleMode() {
        mode = mode switch {
            CameraMode.FirstPerson => CameraMode.ThirdPersonBehind,
            CameraMode.ThirdPersonBehind => CameraMode.ThirdPersonFront,
            CameraMode.ThirdPersonFront => CameraMode.FirstPerson,
            _ => CameraMode.FirstPerson
        };
    }
    
    public void updatePosition(double dt) {
        if (player is not Player p) return;

        // Update bob (ground movement)
        prevBob = bob;
        if (Math.Abs(p.velocity.withoutY().Length()) > 0.0001 && p.onGround) {
            bob = Math.Clamp((float)(p.velocity.Length() / 4), 0, 1) * 0.935f;
        } else {
            bob *= 0.935f;
        }

        // Update airBob (air movement affecting pitch)
        prevAirBob = airBob;
        if (!p.onGround && !p.flyMode) {
            // Base on vertical velocity, stronger effect when falling/jumping
            var verticalSpeed = Math.Abs(p.velocity.Y);
            airBob = Math.Clamp((float)(verticalSpeed / 8), 0, 1) * Math.Sign(p.velocity.Y) * 0.8f;
        } else {
            airBob *= 0.8f; // Faster decay than regular bob
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
        const float speed = 12.0f;
        float step = (float)(speed * dt);
        
        if (Math.Abs(currentFov - targetFov) > 0.01f) {
            currentFov = Meth.lerp(currentFov, targetFov, step);
        }
    }


    /// <summary>
    /// Returns a view matrix that moves with the player.
    /// </summary>
    public Matrix4x4 getViewMatrix(double interp) {
        var interpPos = renderPosition(interp);
        var interpForward = forward(interp);
        var interpUp = up(interp);
        var iBob = float.DegreesToRadians(renderBob(interp));
        var iAirBob = float.DegreesToRadians(renderAirBob(interp) * 0.8f);
        var tt = 0f;
        if (player is Player p) {
            tt = (float)double.Lerp(p.prevTotalTraveled, p.totalTraveled, interp);
        }
        var factor = 0.4f;
        var factor2 = 0.15f;

        // Use standard look direction - forward vector already handles front camera rotation
        Vector3 lookTarget = interpPos.toVec3() + interpForward.toVec3();

        return Matrix4x4.CreateLookAtLeftHanded(interpPos.toVec3(), lookTarget, interpUp.toVec3())
               * Matrix4x4.CreateRotationZ(MathF.Sin(tt) * iBob * factor)
               * Matrix4x4.CreateRotationX(-Math.Abs(MathF.Cos(tt)) * iBob * factor + iAirBob) // Add airBob to pitch
               * Matrix4x4.CreateRotationY(MathF.Sin(tt) * iBob * factor2);
    }

    /// <summary>
    /// Returns a view matrix that doesn't move with the player.
    /// </summary>
    public Matrix4x4 getStaticViewMatrix(double interp) {
        var interpPos = Vector3.Zero;
        var interpForward = forward(interp);
        var interpUp = up(interp);
        var iBob = float.DegreesToRadians(renderBob(interp));
        var iAirBob = float.DegreesToRadians(renderAirBob(interp) * 0.8f);
        var tt = 0f;
        if (player is Player p) {
            tt = (float)double.Lerp(p.prevTotalTraveled, p.totalTraveled, interp);
        }
        const float factor = 0.4f;
        const float factor2 = 0.15f;
        return Matrix4x4.CreateLookAtLeftHanded(interpPos, interpPos + interpForward.toVec3(), interpUp.toVec3())
               * Matrix4x4.CreateRotationZ(MathF.Sin(tt) * iBob * factor)
               * Matrix4x4.CreateRotationX(-Math.Abs(MathF.Cos(tt)) * iBob * factor + iAirBob) // Add airBob to pitch
               * Matrix4x4.CreateRotationY(MathF.Sin(tt) * iBob * factor2);
    }


    /// <summary>
    /// Gets the view matrix for the held hand/block.
    /// </summary>
    public Matrix4x4 getHandViewMatrix(double interp) {
        var iBob = float.DegreesToRadians(renderBob(interp));
        var iAirBob = float.DegreesToRadians(renderAirBob(interp) * 0.8f);
        var tt = 0.0;
        if (player is Player p) {
            tt = double.Lerp(p.prevTotalTraveled, p.totalTraveled, interp);
        }
        const float factor = 2f;
        //var axisZ = new Vector3(1f, 0, 1f);
        //var axisX = new Vector3(1f, 0, -1f);
        // why 30 degrees? no bloody idea
        var axisZ = Vector3.Normalize(Vector3.Transform(new Vector3(0, 0, 1), Matrix4x4.CreateRotationY(Meth.deg2rad(30f))));
        var axisX = Vector3.Normalize(Vector3.Transform(new Vector3(1, 0, 0), Matrix4x4.CreateRotationY(Meth.deg2rad(30f))));

        return Matrix4x4.CreateLookAtLeftHanded(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY)
               * Matrix4x4.CreateFromAxisAngle(axisZ, (float)(Math.Sin(tt) * iBob * factor))
               * Matrix4x4.CreateFromAxisAngle(axisX, (float)(Math.Abs(Math.Cos(tt)) * iBob * factor) + iAirBob);

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

        //normalFov = 110;
        
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