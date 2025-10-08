namespace BlockGame.util;

/**
 * Localization system. Currently English-only, but designed for future multi-language support and expandability and *chokes*
 * Keys use dot notation: "category.subcategory.id"
 */
public static class Loc {
    private static readonly Dictionary<string, string> strings = new() {
        // world generators
        ["generator.new"] = "Default",
        ["generator.new.tooltip"] = "Varied and interesting terrain with mountains, plains and plenty of space for building.",
        ["generator.perlin"] = "Perlin",
        ["generator.perlin.tooltip"] = "Chaotic floating islands and fragmented terrain.",
        ["generator.overworld"] = "Overworld",
        ["generator.overworld.tooltip"] = "Old test generator with a boring landscape.",
        ["generator.simple"] = "Simple",
        ["generator.simple.tooltip"] = "(Mostly) flat testing world. Good for testing and building.",
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