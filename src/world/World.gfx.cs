using BlockGame.ui;

namespace BlockGame.world;

public partial class World {
    private const float TWILIGHT_ANGLE = -12f * MathF.PI / 180f; // -6 degrees WHICH WE DON'T HAVE
    private const float SUNRISE_ANGLE = 0f;
    private const float SOLAR_NOON_ANGLE = MathF.PI / 2f;
    private const float SUNSET_ANGLE = MathF.PI;

    public float getSunAngle(int ticks) {
        float dayPercent = getDayPercentage(ticks);
        // Maps 0-1 to 0-2π (full rotation)
        return dayPercent * MathF.PI * 2f;
    }

    public float getSunElevation(int ticks) {
        float angle = getSunAngle(ticks);
        return MathF.Sin(angle);
    }

    public Color getSkyColour(int ticks) {
        float e = getSunElevation(ticks);

        var nightSky = new Color(5, 5, 15);
        var daySky = new Color(100, 180, 255);

        switch (e) {
            case < TWILIGHT_ANGLE:
                // night
                return nightSky;
            case < SUNRISE_ANGLE: {
                // civil twilight
                float t = (e - TWILIGHT_ANGLE) / (SUNRISE_ANGLE - TWILIGHT_ANGLE);
                return Color.Lerp(nightSky, new Color(20, 35, 80), t);
            }
            case < MathF.PI / 12f: {
                // 15 deg, sunrise/sunset
                float t = e / (MathF.PI / 12f);
                return Color.Lerp(new Color(20, 35, 80), daySky, t);
            }
            default:
                // Full day
                return daySky;
        }
    }

    public Color getHorizonColour(int ticks) {
        float e = getSunElevation(ticks);
        float angle = getSunAngle(ticks);

        var nightHorizon = new Color(15, 15, 40);
        var dayHorizon = new Color(135, 206, 235);

        const float NIGHT_START = -18f * MathF.PI / 180f;
        const float TWILIGHT_START = -12f * MathF.PI / 180f;
        const float GOLDEN_START = -0f * MathF.PI / 180f;
        const float GOLDEN_END = 10f * MathF.PI / 180f;
        const float DAY_START = 30f * MathF.PI / 180f;

        // Smooth blend between sunrise/sunset colours based on time of day
        float isSunset;
        switch (angle) {
            // morning
            case < MathF.PI / 2f:
                isSunset = angle / (MathF.PI / 2f) * 0.5f;
                break;
            // evening
            case < MathF.PI:
                isSunset = 0.5f + (angle - MathF.PI / 2f) / (MathF.PI / 2f) * 0.5f;
                break;
            // night
            case < 3f * MathF.PI / 2f:
                isSunset = 1f - (angle - MathF.PI) / (MathF.PI / 2f) * 0.5f;
                break;
            // sunrise
            default:
                isSunset = 0.5f - (angle - 3f * MathF.PI / 2f) / (MathF.PI / 2f) * 0.5f;
                break;
        }

        var twilightColor = Color.Lerp(
            new Color(80, 40, 100), // dawn purple
            new Color(120, 50, 90), // sunset purple=pink
            isSunset);

        var goldenColor = Color.Lerp(
            new Color(255, 140, 80), // dawn orange
            new Color(255, 80, 50), // sunset red-orange-ish thingie
            isSunset);

        switch (e) {
            case <= NIGHT_START:
                return nightHorizon;
            case <= TWILIGHT_START: {
                float t = (e - NIGHT_START) / (TWILIGHT_START - NIGHT_START);
                return Color.Lerp(nightHorizon, twilightColor, t);
            }
            case <= GOLDEN_START: {
                float t = (e - TWILIGHT_START) / (GOLDEN_START - TWILIGHT_START);
                return Color.Lerp(twilightColor, goldenColor, t);
            }
            case <= GOLDEN_END: {
                float t = (e - GOLDEN_START) / (GOLDEN_END - GOLDEN_START);
                // ???
                return goldenColor;
            }
            case <= DAY_START: {
                float t = (e - GOLDEN_END) / (DAY_START - GOLDEN_END);
                return Color.Lerp(goldenColor, dayHorizon, t);
            }
            default:
                return dayHorizon;
        }
    }

    public Color getFogColour(int ticks) {
        return getHorizonColour(ticks);
    }

    public float getSkyDarkenFloat(int ticks) {
        float elevation = getSunElevation(ticks);

        float darken;

        switch (elevation) {
            case > SUNRISE_ANGLE:
                darken = 0f;
                break;
            case > TWILIGHT_ANGLE: {
                float t = (elevation - TWILIGHT_ANGLE) / (SUNRISE_ANGLE - TWILIGHT_ANGLE);
                darken = 11f * (1f - t);
                break;
            }
            default:
                darken = 11f;
                break;
        }

        return !Settings.instance.smoothDayNight ? (float)Math.Round(darken) : darken;
    }
}