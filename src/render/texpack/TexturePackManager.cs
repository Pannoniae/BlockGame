using BlockGame.GL;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.ui.screen;
using BlockGame.util.log;
using BlockGame.world.block;

namespace BlockGame.render.texpack;

/**
 * Manages texture pack discovery, loading, and hot-reloading
 */
public static class TexturePackManager {
    public const string PACK_DIR = "texturepacks";

    public static readonly List<TexturePack> availablePacks = [];
    public static TexturePack? currentPack;

    /**
     * Discover all available texture packs (folders and zips)
     */
    public static List<TexturePack> discoverPacks() {
        // dispose old pack sources
        foreach (var pack in availablePacks) {
            pack.dispose();
        }
        availablePacks.Clear();

        if (!Directory.Exists(PACK_DIR)) {
            Directory.CreateDirectory(PACK_DIR);
        }

        // discover folders
        foreach (var dir in Directory.GetDirectories(PACK_DIR)) {
            try {
                var source = new FolderPackSource(dir);
                var pack = TexturePack.load(source);
                availablePacks.Add(pack);
                Log.info("TexturePack", $"Found pack: {pack.name} (folder)");
            } catch (Exception e) {
                Log.warn("TexturePack", $"Failed to load pack from {dir}: {e.Message}");
            }
        }

        // discover zips
        foreach (var zip in Directory.GetFiles(PACK_DIR, "*.zip")) {
            try {
                var source = new ZipPackSource(zip);
                var pack = TexturePack.load(source);
                availablePacks.Add(pack);
                Log.info("TexturePack", $"Found pack: {pack.name} (zip)");
            } catch (Exception e) {
                Log.warn("TexturePack", $"Failed to load pack from {zip}: {e.Message}");
            }
        }

        return availablePacks;
    }

    /**
     * Get a list of discovered packs.
     */
    public static List<TexturePack> getAvailablePacks() => availablePacks;

    /**
     * Load and apply a texture pack
     */
    public static void loadPack(string packName) {
        var pack = availablePacks.FirstOrDefault(p => p.name == packName);
        if (pack == null) {
            Log.warn("TexturePack", $"Pack not found: {packName}");
            return;
        }

        loadPack(pack);
    }

    /**
     * Load and apply a texture pack
     */
    public static void loadPack(TexturePack pack) {
        Log.info("TexturePack", $"Loading texture pack: {pack.name}");



        // re-stitch atlases
        doReloadAtlases();

        currentPack = pack;
        Log.info("TexturePack", $"Texture pack loaded: {pack.name}");
    }

    public static void reloadAtlases() {
        var pack = currentPack;
        if (pack == null) {
            Log.warn("TexturePack", "No texture pack loaded, cannot reload atlases");
            return;
        }

        // clear old sources
        TextureSources.clear();

        // register new sources
        pack.registerSources();

        doReloadAtlases();
    }

    /**
     * Reload atlases after source changes
     */
    private static void doReloadAtlases() {
        // re-stitch block atlas (copied from Textures.cs)
        // TODO STOP COPYPASTING SHIT and make it real clean lol
        var blockSources = TextureSources.getBlockSources();
        var blockSourceId = blockSources.Count > 0 ? blockSources[0].filepath : "textures/blocks.png";
        List<ProtectedRegion> blockProtectedRegions = [
            new("waterStill", blockSourceId, 0, 13 * 16, 256, 16),
            new("waterFlowing", blockSourceId, 16, 14 * 16, 32, 32),
            new("lavaStill", blockSourceId, 0, 16 * 16, 16, 16),
            new("lavaFlowing", blockSourceId, 16, 17 * 16, 32, 32),
            new("fire", blockSourceId, 48, 14 * 16, 16, 16)
        ];

        var blockResult = AtlasStitcher.stitch(blockSources, blockProtectedRegions);

        // update Block.atlasSize
        Block.updateAtlasSize(blockResult.width);

        // todo this is a giant hack, do something better?
        // post-process: force leaf alpha = 255 if fastLeaves enabled
        if (Settings.instance.fastLeaves) {
            foreach (var (source, tx, ty) in Block.leafTextureTiles) {
                if (blockResult.tilePositions.TryGetValue((source, tx, ty), out var rect)) {
                    blockResult.image.ProcessPixelRows(accessor => {
                        for (int y = rect.Y; y < rect.Y + rect.Height; y++) {
                            var row = accessor.GetRowSpan(y);
                            for (int x = rect.X; x < rect.X + rect.Width; x++) {
                                row[x].A = 255;
                            }
                        }
                    });
                }
                else {
                    Log.warn("TexturePack", $"Failed to find leaf texture tile in atlas: {source} ({tx}, {ty})");
                }
            }
        }

        // update existing texture
        if (Game.textures?.blockTexture != null) {
            ((BlockTextureAtlas)Game.textures.blockTexture).updateFromStitch(blockResult);
        }

        // dispose source images
        foreach (var src in blockSources) {
            src.dispose();
        }

        // re-stitch item atlas
        var itemSources = TextureSources.getItemSources();
        var itemResult = AtlasStitcher.stitch(itemSources, []);

        if (Game.textures?.itemTexture != null) {
            Game.textures.itemTexture.updateFromStitch(itemResult);
        }

        // dispose source images
        foreach (var src in itemSources) {
            src.dispose();
        }

        Log.info("TexturePack", $"Stitched atlases: blocks={blockResult.width}x{blockResult.height}, items={itemResult.width}x{itemResult.height}");

        // invalidate chunk meshes to use new UVs
        if (Game.world != null && Game.renderer != null) {
            Screen.GAME_SCREEN?.remeshWorld(Settings.instance.renderDistance);
        }
    }

    /**
     * Get the currently loaded pack
     */
    public static TexturePack? getCurrentPack() => currentPack;

    /**
     * Open the texture packs folder in file explorer
     */
    public static void openPackFolder() {
        if (!Directory.Exists(PACK_DIR)) {
            Directory.CreateDirectory(PACK_DIR);
        }

        try {
            System.Diagnostics.Process.Start("explorer", Path.GetFullPath(PACK_DIR));
        } catch {
            // works on Linux, right? needs testing :D
            System.Diagnostics.Process.Start("open", Path.GetFullPath(PACK_DIR));
        }
    }
}
