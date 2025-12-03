using BlockGame.util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render;

/**
 * Represents a source texture atlas image before stitching.
 * Temporary object used during atlas creation, disposed after stitching.
 */
public class AtlasSource {
    public readonly string filepath;
    public readonly Image<Rgba32> image;
    public readonly int tileSize;

    public int width => image.Width;
    public int height => image.Height;
    public int tilesWide => width / tileSize;
    public int tilesHigh => height / tileSize;

    public AtlasSource(string filepath, int tileSize) {
        this.filepath = filepath;
        this.tileSize = tileSize;
        using var stream = Assets.open(filepath);
        image = Image.Load<Rgba32>(stream);
    }

    public AtlasSource(string identifier, Image<Rgba32> image, int tileSize) {
        filepath = identifier;
        this.image = image;
        this.tileSize = tileSize;
    }

    /**
     * Check if a tile at the given position is fully transparent (all alpha == 0)
     */
    public bool isTileEmpty(int tileX, int tileY) {
        int startX = tileX * tileSize;
        int startY = tileY * tileSize;

        var b = image.DangerousTryGetSinglePixelMemory(out var data);
        if (!b) {
            for (int y = startY; y < startY + tileSize; y++) {
                for (int x = startX; x < startX + tileSize; x++) {
                    if (image[x, y].A > 0)
                        return false;
                }
            }

            return true;
        }
        var span = data.Span;
        for (int y = startY; y < startY + tileSize; y++) {
            for (int x = startX; x < startX + tileSize; x++) {
                if (span[y * image.Width + x].A > 0)
                    return false;
            }
        }
        return true;
    }

    public void dispose() {
        image?.Dispose();
    }
}

/**
 * Defines a rectangular region in a source atlas that must not be split during stitching.
 * Used for animation strips and special effects that need contiguous texture memory.
 */
public struct ProtectedRegion {
    /** Unique name for this region (e.g. "waterStill") */
    public string name;

    /** Source file containing this region */
    public string sourceFile;

    /** Pixel rectangle in source atlas */
    public Rectangle srcRect;

    public ProtectedRegion(string name, string sourceFile, Rectangle srcRect) {
        this.name = name;
        this.sourceFile = sourceFile;
        this.srcRect = srcRect;
    }

    public ProtectedRegion(string name, string sourceFile, int x, int y, int width, int height) {
        this.name = name;
        this.sourceFile = sourceFile;
        srcRect = new Rectangle(x, y, width, height);
    }
}