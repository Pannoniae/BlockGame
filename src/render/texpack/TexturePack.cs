using BlockGame.GL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render.texpack;

/**
 * Represents a texture pack with metadata.
 */
public class TexturePack {
    public string name;
    public string? author;
    public string? description;
    public string? version;
    public int tileSize = 16;

    internal PackSource source;
    internal BTexture2D? iconTexture; // cached icon for UI

    public static TexturePack load(PackSource source) {
        var pack = new TexturePack { source = source };

        // load metadata if exists
        var metadata = source.loadMetadata();
        if (metadata != null) {
            pack.name = metadata.getString("name") ?? source.name;
            pack.author = metadata.getString("author", "Unknown");
            pack.description = metadata.getString("description", "");
            pack.version = metadata.getString("version", "1.0");
            pack.tileSize = metadata.getInt("tileSize", 16);
        } else {
            // fallback to source name
            pack.name = source.name;
        }

        return pack;
    }

    /**
     * Register this pack's texture sources with TextureSources.
     * Called before stitching.
     */
    public void registerSources() {
        if (source.exists("textures/blocks.png")) {
            var img = source.loadImage("textures/blocks.png");
            TextureSources.addBlockSource("blocks.png", img, tileSize);
        }

        if (source.exists("textures/items.png")) {
            var img = source.loadImage("textures/items.png");
            TextureSources.addItemSource("items.png", img, tileSize);
        }
    }

    /**
     * Load pack icon into a texture for UI display.
     * Returns null if no icon exists.
     */
    public BTexture2D? getIconTexture() {
        if (iconTexture != null) return iconTexture;

        var icon = source.loadIcon();
        if (icon == null) return null;

        iconTexture = new BTexture2D("pack_icon_" + name);
        iconTexture.loadFromImage(icon);
        return iconTexture;
    }

    public void dispose() {
        source.dispose();
        iconTexture?.Dispose();
    }
}
