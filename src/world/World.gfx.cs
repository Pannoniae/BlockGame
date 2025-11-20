using BlockGame.ui;
using BlockGame.world.worldgen;

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

        Color skyc;

        switch (e) {
            case < TWILIGHT_ANGLE:
                // night
                skyc = nightSky;
                break;
            case < SUNRISE_ANGLE: {
                // civil twilight
                float t = (e - TWILIGHT_ANGLE) / (SUNRISE_ANGLE - TWILIGHT_ANGLE);
                skyc = Color.Lerp(nightSky, new Color(20, 35, 80), t);
                break;
            }
            case < MathF.PI / 12f: {
                // 15 deg, sunrise/sunset
                float t = e / (MathF.PI / 12f);
                skyc = Color.Lerp(new Color(20, 35, 80), daySky, t);
                break;
            }
            default:
                // Full day
                skyc = daySky;
                break;
        }

        // biome switch
        var biome = getBiomeAtPlayer();

        //Console.Out.WriteLine(biome);

        switch (biome) {
            case BiomeType.Ocean:
                skyc += new Color(-10, 0, 25, 0);
                break;
            case BiomeType.Beach:
                break;
            case BiomeType.Desert:
                skyc += new Color(42, 24, -24, 0);
                break;
            case BiomeType.Plains:
                break;
            case BiomeType.Forest:
                break;
            case BiomeType.Taiga:
                skyc += new Color(5, 10, 10, 0);
                break;
            case BiomeType.Jungle:
                // warm
                skyc += new Color(35, 32, -16, 0);
                break;
            default:
                break;
        }

        return skyc;
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

        Color c;

        switch (e) {
            case <= NIGHT_START:
                c = nightHorizon;
                break;
            case <= TWILIGHT_START: {
                float t = (e - NIGHT_START) / (TWILIGHT_START - NIGHT_START);
                c = Color.Lerp(nightHorizon, twilightColor, t);
                break;
            }
            case <= GOLDEN_START: {
                float t = (e - TWILIGHT_START) / (GOLDEN_START - TWILIGHT_START);
                c = Color.Lerp(twilightColor, goldenColor, t);
                break;
            }
            case <= GOLDEN_END: {
                float t = (e - GOLDEN_START) / (GOLDEN_END - GOLDEN_START);
                // ???
                c = goldenColor;
                break;
            }
            case <= DAY_START: {
                float t = (e - GOLDEN_END) / (DAY_START - GOLDEN_END);
                c = Color.Lerp(goldenColor, dayHorizon, t);
                break;
            }
            default:
                c = dayHorizon;
                break;
        }

        // biome switch
        var biome = getBiomeAtPlayer();

        switch (biome) {
            case BiomeType.Ocean:
                c += new Color(-10, 0, 25, 0);
                break;
            case BiomeType.Beach:
                break;
            case BiomeType.Desert:
                c += new Color(24, 6, -24, 0);
                break;
            case BiomeType.Plains:
                break;
            case BiomeType.Forest:
                break;
            case BiomeType.Taiga:
                c += new Color(5, 10, 10, 0);
                break;
            case BiomeType.Jungle:
                // warm
                c += new Color(20, 10, -10, 0);
                break;
            default:
                break;
        }

        return c;
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