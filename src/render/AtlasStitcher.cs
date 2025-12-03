using BlockGame.util;
using BlockGame.util.log;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TrippyGL.Fonts.Rectpack;

namespace BlockGame.render;

/**
 * Result of stitching multiple source atlases into one
 */
public struct StitchResult {
    public Image<Rgba32> image;
    public int width;
    public int height;
    public Dictionary<(string source, int tx, int ty), Rectangle> tilePositions;
    public Dictionary<string, Rectangle> protectedRegions;
}

/**
 * Stitches multiple source atlases into a single optimised atlas
 */
public static class AtlasStitcher {
    /**
     * Stitch multiple source atlases into one, skipping empty tiles and respecting protected regions
     */
    public static StitchResult stitch(List<AtlasSource> sources, List<ProtectedRegion> protectedRegions) {
        var rects = new List<PackingRectangle>();
        var rectMetadata = new List<RectMetadata>();

        int nextId = 0;

        // track which tiles are empty
        var emptyTiles = new HashSet<(string, int, int)>();

        // 1. Add protected regions first (they're rigid constraints)
        foreach (var pr in protectedRegions) {
            var rect = new PackingRectangle(0, 0, (uint)pr.srcRect.Width, (uint)pr.srcRect.Height) {
                Id = nextId++
            };
            rects.Add(rect);
            rectMetadata.Add(new RectMetadata {
                type = RectType.PROTECTED_REGION,
                protectedRegion = pr
            });
        }

        // 2. Extract individual tiles from non-protected areas
        foreach (var source in sources) {
            for (int ty = 0; ty < source.tilesHigh; ty++) {
                for (int tx = 0; tx < source.tilesWide; tx++) {
                    var tileRect = new Rectangle(
                        tx * source.tileSize,
                        ty * source.tileSize,
                        source.tileSize,
                        source.tileSize
                    );

                    // skip if this tile is inside a protected region
                    if (isInProtectedRegion(tileRect, source.filepath, protectedRegions))
                        continue;

                    // skip if tile is fully empty (all alpha == 0)
                    if (source.isTileEmpty(tx, ty)) {
                        emptyTiles.Add((source.filepath, tx, ty));
                        continue;
                    }

                    // add this tile to packing
                    var rect = new PackingRectangle(0, 0, (uint)source.tileSize, (uint)source.tileSize) {
                        Id = nextId++
                    };
                    rects.Add(rect);
                    rectMetadata.Add(new RectMetadata {
                        type = RectType.TILE,
                        source = source,
                        tileX = tx,
                        tileY = ty
                    });
                }
            }
        }

        // 3. Add ONE shared empty tile
        rects.Add(new PackingRectangle(0, 0, 16, 16) {
            Id = nextId++
        });
        rectMetadata.Add(new RectMetadata {
            type = RectType.EMPTY_TILE
        });

        // 4. Pack using RectanglePacker
        var rectsArray = rects.ToArray();
        RectanglePacker.Pack(rectsArray, out var bounds);

        // 5. Round up to power of 2 for each dimension (GPU-friendly)
        int finalWidth = Meth.nxtpow2((int)bounds.Width);
        int finalHeight = Meth.nxtpow2((int)bounds.Height);

        Log.info("AtlasStitcher", $"Packer bounds: {bounds.Width}x{bounds.Height}, final: {finalWidth}x{finalHeight}");

        // 6. Create final atlas image
        var finalImage = new Image<Rgba32>(finalWidth, finalHeight);

        // 7. Create result structures
        var tilePositions = new Dictionary<(string, int, int), Rectangle>();
        var protectedPositions = new Dictionary<string, Rectangle>();
        Rectangle sharedEmptyTile = default;

        // 8. Blit all rectangles to final atlas & record mappings
        for (int i = 0; i < rectsArray.Length; i++) {
            var rect = rectsArray[i];
            var meta = rectMetadata[rect.Id]; // Use Id, not index!

            var destRect = new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

            switch (meta.type) {
                case RectType.PROTECTED_REGION:
                    // blit entire protected region
                    var pr = meta.protectedRegion;
                    var prSource = sources.Find(s => s.filepath == pr.sourceFile)!;
                    blitRegion(prSource.image, pr.srcRect, finalImage, destRect);
                    protectedPositions[pr.name] = destRect;
                    Log.info("AtlasStitcher",
                        $"Protected region '{pr.name}': src=({pr.srcRect.X},{pr.srcRect.Y},{pr.srcRect.Width},{pr.srcRect.Height}) -> dest=({destRect.X},{destRect.Y},{destRect.Width},{destRect.Height})");

                    // Also map individual tiles within the protected region
                    int tileSize = prSource.tileSize;
                    int tilesWide = pr.srcRect.Width / tileSize;
                    int tilesHigh = pr.srcRect.Height / tileSize;
                    for (int ty = 0; ty < tilesHigh; ty++) {
                        for (int tx = 0; tx < tilesWide; tx++) {
                            int srcTileX = pr.srcRect.X / tileSize + tx;
                            int srcTileY = pr.srcRect.Y / tileSize + ty;
                            var tileDestRect = new Rectangle(
                                destRect.X + tx * tileSize,
                                destRect.Y + ty * tileSize,
                                tileSize,
                                tileSize
                            );
                            tilePositions[(prSource.filepath, srcTileX, srcTileY)] = tileDestRect;
                        }
                    }

                    break;

                case RectType.TILE:
                    // blit single tile
                    var srcRect = new Rectangle(
                        meta.tileX * meta.source!.tileSize,
                        meta.tileY * meta.source.tileSize,
                        meta.source.tileSize,
                        meta.source.tileSize
                    );
                    blitRegion(meta.source.image, srcRect, finalImage, destRect);
                    tilePositions[(meta.source.filepath, meta.tileX, meta.tileY)] = destRect;
                    break;

                case RectType.EMPTY_TILE:
                    // fill with transparent pixels (already is by default)
                    sharedEmptyTile = destRect;
                    break;
            }
        }

        // Map all empty tiles to shared empty tile
        foreach (var (source, tx, ty) in emptyTiles) {
            tilePositions[(source, tx, ty)] = sharedEmptyTile;
        }

        return new StitchResult {
            image = finalImage,
            width = finalWidth,
            height = finalHeight,
            tilePositions = tilePositions,
            protectedRegions = protectedPositions
        };
    }

    /**
     * Check if a tile rectangle is inside any protected region
     */
    private static bool isInProtectedRegion(Rectangle tileRect, string sourceFile, List<ProtectedRegion> protectedRegions) {
        foreach (var pr in protectedRegions) {
            if (pr.sourceFile != sourceFile)
                continue;

            // check if tileRect is inside or overlaps pr.srcRect
            if (tileRect.X >= pr.srcRect.X &&
                tileRect.Y >= pr.srcRect.Y &&
                tileRect.X + tileRect.Width <= pr.srcRect.X + pr.srcRect.Width &&
                tileRect.Y + tileRect.Height <= pr.srcRect.Y + pr.srcRect.Height) {
                return true;
            }
        }

        return false;
    }

    /**
     * Blit a region from source to destination
     */
    private static void blitRegion(Image<Rgba32> srcImage, Rectangle srcRect, Image<Rgba32> dstImage, Rectangle dstRect) {
        // sanity check - sizes must match
        if (srcRect.Width != dstRect.Width || srcRect.Height != dstRect.Height) {
            InputException.throwNew("Source and destination rectangles must have same dimensions");
        }

        int width = srcRect.Width;
        int height = srcRect.Height;

        if (!srcImage.DangerousTryGetSinglePixelMemory(out var srcMem)) {
            InputException.throwNew("Source image not contiguous");
        }

        if (!dstImage.DangerousTryGetSinglePixelMemory(out var dstMem)) {
            InputException.throwNew("Dest image not contiguous");
        }

        var srcSpan = srcMem.Span;
        var dstSpan = dstMem.Span;

        int srcStride = srcImage.Width;
        int dstStride = dstImage.Width;

        for (int y = 0; y < height; y++) {
            int srcOffset = (srcRect.Y + y) * srcStride + srcRect.X;
            int dstOffset = (dstRect.Y + y) * dstStride + dstRect.X;
            srcSpan.Slice(srcOffset, width).CopyTo(dstSpan.Slice(dstOffset, width));
        }
    }

    /**
     * Metadata for tracking what each packed rectangle represents
     */
    public enum RectType {
        TILE, // individual tile from source
        PROTECTED_REGION, // protected region that can't be split
        EMPTY_TILE // the shared empty tile
    }

    private struct RectMetadata {
        public RectType type;
        public AtlasSource? source;
        public int tileX, tileY; // for Tile type
        public ProtectedRegion protectedRegion; // for PROTECTED_REGION type
    }
}