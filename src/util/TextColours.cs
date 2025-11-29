using System.Text;
using BlockGame.world.block;
using FontStashSharp;

namespace BlockGame.util;

/**
 * parses &-codes into coloured text segments.
 * CandyBlock colours ftw!
 *
 * &0-9, a-f = standard 16 colours
 * &g-w = extended 8 colours (g=16, w=23)
 * &r = reset to white
 */
public static class TextColours {
    /** map &-codes to colour indices */
    private static readonly Dictionary<char, int> map = new() {
        // standard 16 colors (for compatibility;))
        { '0', 2 },  // black
        { '1', 12 }, // dark blue
        { '2', 8 },  // dark green
        { '3', 9 },  // turquoise (dark cyan)
        { '4', 3 },  // dark red
        { '5', 14 }, // purple (dark purple)
        { '6', 5 },  // orange (gold)
        { '7', 1 },  // gray
        { '8', 1 },  // dark gray (use gray)
        { '9', 11 }, // blue
        { 'a', 7 },  // light green
        { 'b', 20 }, // cyan
        { 'c', 4 },  // red
        { 'd', 13 }, // violet (light purple)
        { 'e', 6 },  // yellow
        { 'f', 0 },  // white

        // extended colors (g=16 through w=23)
        { 'g', 16 }, // beige
        { 'h', 17 }, // light orange
        { 'i', 18 }, // neon
        { 'j', 19 }, // apple green
        { 'k', 20 }, // cyan
        { 'l', 21 }, // light purple
        { 'm', 22 }, // dark violet
        { 'n', 23 }, // brown
        { 'o', 15 }, // pink
        { 'p', 10 }, // sky blue

        // aliases
        { 'r', 0 },  // reset (white)
    };

    /** segment of colored text with optional style */
    public record struct ColourSegment(string text, Color color, TextStyle style = TextStyle.None);

    /**
     * parse text with &-codes into colored segments.
     * &amp;a turns text green, &amp;c turns it red, etc.
     * &amp;I = italic, &amp;B = bold, &amp;U = underline, &amp;S = strikethrough, &amp;R = reset style
     */
    public static List<ColourSegment> parse(string text) {
        var segments = new List<ColourSegment>();
        var sb = new StringBuilder();
        var currentColour = Color.White;
        var currentStyle = TextStyle.None;

        for (int i = 0; i < text.Length; i++) {
            if (text[i] == '&' && i + 1 < text.Length) {
                char code = text[i + 1];
                char lowerCode = char.ToLower(code);

                // check for style codes (uppercase)
                if (char.IsUpper(code)) {
                    TextStyle? newStyle = code switch {
                        'I' => TextStyle.Italic,
                        'B' => TextStyle.Bold,
                        'U' => TextStyle.Underline,
                        'S' => TextStyle.Strikethrough,
                        'R' => TextStyle.None, // reset style
                        _ => null
                    };

                    if (newStyle.HasValue) {
                        // flush current segment
                        if (sb.Length > 0) {
                            segments.Add(new ColourSegment(sb.ToString(), currentColour, currentStyle));
                            sb.Clear();
                        }

                        currentStyle = newStyle.Value;
                        i++; // skip the code char
                        continue;
                    }
                }

                // check for color codes (lowercase)
                if (map.TryGetValue(lowerCode, out int colorIdx)) {
                    // flush current segment
                    if (sb.Length > 0) {
                        segments.Add(new ColourSegment(sb.ToString(), currentColour, currentStyle));
                        sb.Clear();
                    }

                    // update colour
                    currentColour = CandyBlock.colours[colorIdx];
                    i++; // skip the code char
                    continue;
                }
            }

            sb.Append(text[i]);
        }

        // flush remaining
        if (sb.Length > 0) {
            segments.Add(new ColourSegment(sb.ToString(), currentColour, currentStyle));
        }

        return segments;
    }

    /**
     * strip all &-codes from text, leaving plain text
     */
    public static string strip(string text) {
        var sb = new StringBuilder(text.Length);

        for (int i = 0; i < text.Length; i++) {
            if (text[i] == '&' && i + 1 < text.Length) {
                char code = text[i + 1];
                char lowerCode = char.ToLower(code);

                // check for style codes (uppercase)
                if (char.IsUpper(code) && (code is 'I' or 'B' or 'U' or 'S' or 'R')) {
                    i++; // skip the code char
                    continue;
                }

                // check for color codes
                if (map.ContainsKey(lowerCode)) {
                    i++; // skip the code char
                    continue;
                }
            }

            sb.Append(text[i]);
        }

        return sb.ToString();
    }
}