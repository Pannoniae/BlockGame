namespace BlockGame.util;

/**
 * Localization system. Currently English-only, but designed for future multi-language support and expandability and *chokes*
 * Keys use dot notation: "category.subcategory.id"
 */
public static class Loc {
    private static readonly Dictionary<string, string> strings = new() {
        // world generators
        ["generator.v2"] = "Nostalgic",
        ["generator.v2.tooltip"] = "The good old terrain: crazy mountains and lots of overhangs. 3D glasses not bundled.",
        ["generator.new"] = "Plainly",
        ["generator.new.tooltip"] = "Varied and interesting terrain with mountains, plains and plenty of space for building.",
        ["generator.perlin"] = "Perlin",
        ["generator.perlin.tooltip"] = "Chaotic floating islands and fragmented terrain.",
        ["generator.overworld"] = "Overworld",
        ["generator.overworld.tooltip"] = "Old test generator with a boring landscape.",
        ["generator.simple"] = "Simple",
        ["generator.simple.tooltip"] = "(Mostly) flat testing world.",
        ["generator.flat"] = "Flat",
        ["generator.flat.tooltip"] = "Completely flat world without any resources or features."
    };

    /**
     * Get localized string for key. Returns key itself if not found.
     */
    public static string get(string key) {
        return strings.TryGetValue(key, out var value) ? value : key;
    }

    /**
     * Check if key exists in localization data.
     */
    public static bool has(string key) {
        return strings.ContainsKey(key);
    }
}