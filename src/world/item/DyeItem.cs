using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class DyeItem : Item {
    public DyeItem(string name) : base(name) {
    }

    public override string getName(ItemStack stack) {
        int meta = stack.metadata;
        return meta >= CandyBlock.colourNames.Length ? name : $"{CandyBlock.colourNames[meta]} Dye";
    }

    public override UVPair getTexture(ItemStack stack) {
        int meta = stack.metadata & 0x1F;
        return meta < 16
            ?
            // v=1, u=0 to 15
            new UVPair(meta, 1)
            :
            // v=2, u=0 to 15
            new UVPair(meta, 2);
    }

    /** Find the closest dye colour to target RGB */
    public static int findClosestDye(float r, float g, float b) {
        int closest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < CandyBlock.colours.Length; i++) {
            var c = CandyBlock.colours[i];
            float dr = c.R - r;
            float dg = c.G - g;
            float db = c.B - b;
            float dist = dr * dr + dg * dg + db * db;

            if (dist < minDist) {
                minDist = dist;
                closest = i;
            }
        }

        return closest;
    }

    /** Convert RGB (0-255) to HSL (H: 0-360, S: 0-1, L: 0-1)
     *
     * TODO migrate this to Meth class
     */
    private static (float h, float s, float l) rgbToHsl(float r, float g, float b) {
        r /= 255f;
        g /= 255f;
        b /= 255f;

        float max = float.Max(r, float.Max(g, b));
        float min = float.Min(r, float.Min(g, b));
        float delta = max - min;

        float h = 0, s = 0, l = (max + min) / 2f;

        if (delta != 0) {
            s = l > 0.5f ? delta / (2f - max - min) : delta / (max + min);

            if (max == r) h = ((g - b) / delta) + (g < b ? 6f : 0f);
            else if (max == g) h = ((b - r) / delta) + 2f;
            else h = ((r - g) / delta) + 4f;

            h *= 60f;
            //h/= 6f;
        }

        return (h, s, l);
    }

    /** Convert HSL (H: 0-360, S: 0-1, L: 0-1) to RGB (0-255)
     * Conversion formula adapted from https://en.wikipedia.org/wiki/HSL_color_space.
     */
    private static (float r, float g, float b) hslToRgb(float h, float s, float l) {
        float r, g, b;

        if (s == 0f) {
            r = g = b = l; // achromatic
        } else {
            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;
            r = hueToRgb(p, q, h + 120f);
            g = hueToRgb(p, q, h);
            b = hueToRgb(p, q, h - 120f);
        }

        return (MathF.Round(r * 255f), MathF.Round(g * 255f), MathF.Round(b * 255f));
    }

    private static float hueToRgb(float p, float q, float t) {
        if (t < 0f) t += 360f;
        if (t > 360f) t -= 360f;
        if (t < 60f) return p + (q - p) * t / 60f;
        if (t < 180f) return q;
        if (t < 240f) return p + (q - p) * (240f - t) / 60f;
        return p;
    }

    /*
    // OLD VERSION - COMMENTED OUT FOR REFERENCE
    /** Convert HSL (H: 0-360, S: 0-1, L: 0-1) to RGB (0-255) */
    /*private static (float r, float g, float b) hslToRgb(float h, float s, float l) {
        float c = (1f - float.Abs(2f * l - 1f)) * s;
        float x = c * (1f - float.Abs((h / 60f) % 2f - 1f));
        float m = l - c / 2f;

        float r1, g1, b1;
        if (h < 60) {
            r1 = c;
            g1 = x;
            b1 = 0;
        }
        else if (h < 120) {
            r1 = x;
            g1 = c;
            b1 = 0;
        }
        else if (h < 180) {
            r1 = 0;
            g1 = c;
            b1 = x;
        }
        else if (h < 240) {
            r1 = 0;
            g1 = x;
            b1 = c;
        }
        else if (h < 300) {
            r1 = x;
            g1 = 0;
            b1 = c;
        }
        else {
            r1 = c;
            g1 = 0;
            b1 = x;
        }

        return ((r1 + m) * 255f, (g1 + m) * 255f, (b1 + m) * 255f);
    }*/


    /** Average two hues (0-360) with wraparound */
    private static float avgHue(float h1, float h2) {
        float diff = float.Abs(h1 - h2);
        if (diff > 180f) {
            // wraparound!
            if (h1 < h2) h1 += 360f;
            else h2 += 360f;
        }

        float avg = (h1 + h2) / 2f;
        return avg >= 360f ? avg - 360f : avg;
    }

    /** Mix two dye colours and return result dye metadata */
    public static int mixColours(int meta1, int meta2) {
        if (meta1 < 0 || meta1 >= CandyBlock.colours.Length) meta1 = 0;
        if (meta2 < 0 || meta2 >= CandyBlock.colours.Length) meta2 = 0;

        var c1 = CandyBlock.colours[meta1];
        var c2 = CandyBlock.colours[meta2];

        // convert to HSL
        var (h1, s1, l1) = rgbToHsl(c1.R, c1.G, c1.B);
        var (h2, s2, l2) = rgbToHsl(c2.R, c2.G, c2.B);

        // average in HSL space
        // weight hue by saturation (achromatic colours have meaningless hue)
        float avgH, avgS, avgL;

        bool c1Achromatic = s1 < 0.01f;
        bool c2Achromatic = s2 < 0.01f;

        if (c1Achromatic && c2Achromatic) {
            // both achromatic, just average lightness
            avgH = 0f;
            avgS = 0f;
            avgL = (l1 + l2) / 2f;
        } else if (c1Achromatic) {
            // c1 is achromatic (white/gray/black), preserve c2's hue and saturation
            avgH = h2;
            avgS = s2 * 0.85f; // slight desaturation when mixing with achromatic
            avgL = (l1 + l2) / 2f;
        } else if (c2Achromatic) {
            // c2 is achromatic, preserve c1's hue and saturation
            avgH = h1;
            avgS = s1 * 0.85f; // slight desaturation when mixing with achromatic
            avgL = (l1 + l2) / 2f;
        } else {
            // both chromatic, average normally
            avgH = avgHue(h1, h2);
            avgS = (s1 + s2) / 2f;
            avgL = (l1 + l2) / 2f;
        }

        // convert back to RGB
        var (r, g, b) = hslToRgb(avgH, avgS, avgL);

        return findClosestDye(r, g, b);
    }

    /** Test method to verify RGB->HSL->RGB round-trip conversion */
    public static void testColourConversion() {
        int[] testIndices = [0, 8, 7]; // White, Dark Green, Light Green
        string[] names = ["White", "Dark Green", "Light Green"];

        for (int i = 0; i < testIndices.Length; i++) {
            var c = CandyBlock.colours[testIndices[i]];

            Console.WriteLine($"\n{names[i]}:");
            Console.WriteLine($"  Original RGB: ({c.R}, {c.G}, {c.B})");

            // convert to HSL
            var (h, s, l) = rgbToHsl(c.R, c.G, c.B);
            Console.WriteLine($"  HSL: (H={h:F2}°, S={s:F3}, L={l:F3})");

            // convert back to RGB
            var (r, g, b) = hslToRgb(h, s, l);
            Console.WriteLine($"  Back to RGB: ({r}, {g}, {b})");

            // check if round-trip is accurate
            bool match = MathF.Abs(r - c.R) < 1f && MathF.Abs(g - c.G) < 1f && MathF.Abs(b - c.B) < 1f;
            Console.WriteLine($"  Round-trip {(match ? "OK" : "MISMATCH")}");
        }
    }
}