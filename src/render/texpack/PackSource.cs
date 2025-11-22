using BlockGame.util.xNBT;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render.texpack;

/**
 * Abstraction for loading files from a texture pack.
 * Allows packs to be loaded from either directories or zip archives.
 */
public interface PackSource {
    /**
     * The display name of this pack (folder/zip name)
     */
    string name { get; }

    /**
     * Check if a file exists in this pack
     */
    bool exists(string path);

    /**
     * Load an image from the pack
     */
    Image<Rgba32> loadImage(string path);

    /**
     * Load pack metadata (pack.snbt), returns null if not found
     */
    NBTCompound? loadMetadata();

    /**
     * Load pack icon (pack.png), returns null if not found
     */
    Image<Rgba32>? loadIcon();

    /**
     * Dispose resources (close zip handles, etc)
     */
    void dispose();
}
