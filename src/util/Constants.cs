namespace BlockGame.util;

public static class Constants {
    public const int crosshairSize = 10;
    public const int crosshairThickness = 2;
    public const long MEGABYTES = 1 * 1024 * 1024;
    public const long MEGABYTES_SMOL = 1 * 1000 * 1000;
    public const long GIGABYTES = 1024L * 1024L * 1024L;
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
    public const double epsilonGroundCheck = 0.0001;

    public static double moveSpeed = 1.25;
    public const double groundMoveSpeed = 0.75;
    public const double airMoveSpeed = 0.5;
    public const double airFlySpeed = 1.75;
    // idk why this has to be so low??? otherwise it feels super fast
    // not anymore!
    public const double liquidMoveSpeed = 0.2;
    public const double sneakFactor = 0.28;
    public const double stepHeight = 0.51; // max height entity can step up
    public const float RAYCASTSTEP = 1 / 256f;
    public const float RAYCASTDIST = 6f;
    public const double breakDelayMs = 267; // ~16 ticks at 60fps
    public const double breakMissDelayMs = 67; // 4 ticks at 60fps
    public const double placeDelayMs = 267; // ~16 ticks at 60fps
    public const double airHitDelayMs = 67; // 4 ticks at 60fps - additional delay when hitting air
    public const double flyModeDelay = 0.4;

    // Networking
    public const int netVersion = 7;
    public const string connectionKey = "BlockGame";

    // Inventory
    // note changed these from 255/254 to -1/-2 because the whole system was fucked, it was sending TYPES (byte) instead of INT IDs (sequential and different for each inv opened)
    public const int INV_ID_CURSOR = -3;  // special invID for cursor
    public const int INV_ID_CREATIVE = -2; // creative mode inventory (client-side UI, server validates)
    public const int INV_ID_PLAYER = -1;    // player inventory
    
    #if DEBUG
    public static string VERSION => _ver + " SLOW DEBUG";
    #else
    public static string VERSION => _ver;
    #endif

    private const string _ver = "BlockGame v0.0.5_01";
}