namespace BlockGame;

public static class Constants {
    public const int crosshairSize = 10;
    public const int crosshairThickness = 2;
    public const string fontFile = "guifont.fnt";
    public const long MEGABYTES = 1 * 1024 * 1024;
    public const float maxPitch = 89;
    public const int initialWidth = 1200;
    public const int initialHeight = 800;
    public const double gravity = 20;
    public const double maxAccel = 50;
    public const double maxhSpeed = 4;
    public const double maxhSpeedSneak = 1.5;
    public const double maxhAirSpeed = 4;
    public const double jumpSpeed = 6.5;
    public const double maxVSpeed = 200;
    public const double friction = 0.55;
    public const double airFriction = 0.98;
    public const double epsilon = 0.0001;
    public const double epsilonGroundCheck = 0.01;
    public static double moveSpeed = 1.5;
    public const double groundMoveSpeed = 1.5;
    public const double airMoveSpeed = 0.5;
    public const float RAYCASTSTEP = 1 / 32f;
    public const float RAYCASTDIST = 20f;
}