namespace BlockGame.util;

public static class Constants {
    public const int crosshairSize = 10;
    public const int crosshairThickness = 2;
    public const string fontFile = "guifont.fnt";
    public const string fontFileUnicode = "guifontU.fnt";
    public const long MEGABYTES = 1 * 1024 * 1024;
    public const float maxPitch = 89;
    public const int initialWidth = 1200;
    public const int initialHeight = 800;
    public const double gravity = 20;
    public const double maxAccel = 50;
    //public const double maxhSpeed = 4;
    //public const double maxhSpeedSneak = 1.5;
    //public const double maxhAirSpeed = 4;
    //public const double maxhAirSpeedSneak = 1.5;
    //public const double maxhLiquidSpeed = 4;
    //public const double maxhLiquidSpeedSneak = 1.5;
    public const double jumpSpeed = 7;
    public const double liquidSwimUpSpeed = 0.9;
    public const double liquidSurfaceBoost = 0.3;
    public const double maxVSpeed = 200;
    public const double friction = 0.57;
    public const double airFriction = 0.57;
    public const double verticalFriction = 0.98;
    public const double liquidFriction = 0.75;
    public const double epsilon = 0.0001;
    public const double epsilonGroundCheck = 0.01;
    public static double moveSpeed = 2.5;
    public const double groundMoveSpeed = 3;
    public const double airMoveSpeed = 2;
    public const double liquidMoveSpeed = 0.9;
    public const double sneakFactor = 0.28;
    public const float RAYCASTSTEP = 1 / 32f;
    public const float RAYCASTDIST = 6f;
    public const double breakDelay = 0.6;
    public const double placeDelay = 0.6;
}