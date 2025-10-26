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
        int meta = stack.metadata & 0xF;
        return meta < 16
            ?
            // v=1, u=0 to 15
            new UVPair(meta, 1)
            :
            // v=2, u=0 to 3
            new UVPair(meta - 16, 2);
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

    /** Convert RGB (0-255) to HSL (H: 0-360, S: 0-1, L: 0-1) */
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
        }

        return (h, s, l);
    }

    /** Convert HSL (H: 0-360, S: 0-1, L: 0-1) to RGB (0-255) */
    private static (float r, float g, float b) hslToRgb(float h, float s, float l) {
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
    }

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
        float avgH = avgHue(h1, h2);
        float avgS = (s1 + s2) / 2f;
        float avgL = (l1 + l2) / 2f;

        // convert back to RGB
        var (r, g, b) = hslToRgb(avgH, avgS, avgL);

        return findClosestDye(r, g, b);
    }
}