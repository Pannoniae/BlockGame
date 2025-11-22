using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render;

/**
 * Global registry for collecting texture source files before stitching.
 * Allows base game and mods to register their texture atlases.
 */
public static class TextureSources {
    public static readonly List<AtlasSource> blockSources = [];
    public static readonly List<AtlasSource> itemSources = [];

    /**
     * Register a block atlas source from file.
     * Path will be prefixed with "textures/" automatically.
     */
    public static void addBlockSource(string filename, int tileSize = 16) {
        blockSources.Add(new AtlasSource("textures/" + filename, tileSize));
    }

    /**
     * Register a block atlas source from an image.
     * Path will be prefixed with "textures/" automatically.
     */
    public static void addBlockSource(string filename, Image<Rgba32> image, int tileSize = 16) {
        blockSources.Add(new AtlasSource("textures/" + filename, image, tileSize));
    }

    /**
     * Register an item atlas source from file.
     * Path will be prefixed with "textures/" automatically.
     */
    public static void addItemSource(string filename, int tileSize = 16) {
        itemSources.Add(new AtlasSource("textures/" + filename, tileSize));
    }

    /**
     * Register an item atlas source from an image.
     * Path will be prefixed with "textures/" automatically.
     */
    public static void addItemSource(string filename, Image<Rgba32> image, int tileSize = 16) {
        itemSources.Add(new AtlasSource("textures/" + filename, image, tileSize));
    }

    public static List<AtlasSource> getBlockSources() => blockSources;
    public static List<AtlasSource> getItemSources() => itemSources;

    /**
     * Clear all registered sources (for hot-reload)
     */
    public static void clear() {
        foreach (var src in blockSources) src.dispose();
        foreach (var src in itemSources) src.dispose();
        blockSources.Clear();
        itemSources.Clear();
    }
}