namespace BlockGame.util;

public static class Constants {
    public const int crosshairSize = 10;
    public const int crosshairThickness = 2;
    public const long MEGABYTES = 1 * 1024 * 1024;
    public const float maxPitch = 89;
    public const int initialWidth = 1440;
    public const int initialHeight = 960;
    public const int minWidth = 640;
    public const int minHeight = 480;
    public const double gravity = 30;
    public const double maxAccel = 50;
    public const double jumpSpeed = 10;
    public const double liquidSwimUpSpeed = 0.45;
    public const double liquidSurfaceBoost = 0.2;
    public const double maxVSpeed = 200;
    public const double friction = 0.80;
    public const double airFriction = 0.80;
    public const double flyFriction = 0.85;
    public const double verticalFriction = 0.99;
    public const double liquidFriction = 0.92;
    public const double epsilon = 0.0001;
    public const float epsilonF = 0.0001f;
    public const double epsilonGroundCheck = 0.01;

    public static double moveSpeed = 1.25;
    public const double groundMoveSpeed = 0.75;
    public const double airMoveSpeed = 0.5;
    public const double airFlySpeed = 1.75;
    // idk why this has to be so low??? otherwise it feels super fast
    // not anymore!
    public const double liquidMoveSpeed = 0.2;
    public const double sneakFactor = 0.28;
    public const double stepHeight = 0.51; // max height entity can step up
    public const float RAYCASTSTEP = 1 / 32f;
    public const float RAYCASTDIST = 6f;
    public const double breakDelayMs = 267; // ~16 ticks at 60fps
    public const double breakMissDelayMs = 67; // 4 ticks at 60fps
    public const double placeDelayMs = 267; // ~16 ticks at 60fps
    public const double airHitDelayMs = 67; // 4 ticks at 60fps - additional delay when hitting air
    public const double flyModeDelay = 0.4;
}