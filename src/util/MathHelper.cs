using System;

namespace BlockGame.util;

public static class MathHelper
{
    public static float ToRadians(double degrees)
    {
        return (float)(degrees * Math.PI / 180.0);
    }

    public static float ToDegrees(double radians)
    {
        return (float)(radians * 180.0 / Math.PI);
    }

    public static float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * Math.Clamp(amount, 0.0f, 1.0f);
    }
}
