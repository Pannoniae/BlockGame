using BlockGame.GL;
using BlockGame.main;
using BlockGame.render.texpack;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.world.block;
using Silk.NET.OpenGL.Legacy;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.render;

public class Textures {
    public readonly Silk.NET.OpenGL.Legacy.GL GL;

    public readonly BTexture2D background;
    public BlockTextureAtlas blockTexture = null!;
    public BTextureAtlas itemTexture = null!;
    public readonly BTexture2D lightTexture;
    public BTexture2D lightTexture2;

    public readonly Dictionary<string, BTexture2D> textures = new();
    public readonly BTexture2D waterOverlay;
    public readonly BTexture2D lavaOverlay;

    public readonly BTexture2D sunTexture;
    public readonly BTexture2D moonTexture;
    public readonly BTexture2D cloudTexture;

    public readonly BTexture2D particleTex;

    public const int LIGHTMAP_SIZE = 16;

    public readonly Rgba32[] lightmap = new Rgba32[256];
    public readonly BTexture2D human;
    public readonly BTexture2D cow;
    public readonly BTexture2D pig;
    public readonly BTexture2D zombie;
    public readonly BTexture2D eye;
    public readonly BTexture2D mummy;
    public readonly BTexture2D bigeye;

    // texture pack management
    private readonly List<TexturePack> availablePacks = [];
    private TexturePack? currentPack;

    // source registry
    private readonly List<AtlasSource> blockSources = [];
    private readonly List<AtlasSource> itemSources = [];

    public event Action? onAtlasReloaded;

    public const string PACK_DIR = "texturepacks";

    public Textures(Silk.NET.OpenGL.Legacy.GL GL) {
        this.GL = GL;

        // assign early so reload() can use Game.textures.open()
        Game.textures = this;

        // discover and load texture pack FIRST
        discoverPacks();
        var packName = Settings.instance.texturePack;
        loadPack(packName);

        // THEN load textures (will use pack system)
        background = get("textures/bg.png");
        lightTexture = get("textures/lightmap.png");
        lightTexture2 = get("textures/lightmap.png");
        waterOverlay = get("textures/water.png");
        lavaOverlay = get("textures/lava.png");
        sunTexture = get("textures/sun_03.png");
        moonTexture = get("textures/moon_01.png");
        cloudTexture = get("textures/clouds.png");

        particleTex = get("textures/particle.png");

        // load player skin from game directory (not assets/!)
        human = new BTexture2D(Settings.instance.skinPath);
        if (File.Exists(Settings.instance.skinPath)) {
            human.loadFromFile(Settings.instance.skinPath);
        }
        else {
            // fallback to the default skin in assets
            human = get("character.png");
        }

        cow = get("textures/entity/cow.png");
        pig = get("textures/entity/pig.png");
        zombie = get("textures/entity/zombie.png");
        eye = get("textures/entity/eye.png");
        mummy = get("textures/entity/mummy.png");
        bigeye = get("textures/entity/bigeye.png");

        reloadAll();
    }

    /**
     * Discover all available texture packs (folders and zips)
     */
    public void discoverPacks() {
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
                Log.info("Textures", $"Found pack: {pack.name} (folder)");
            } catch (Exception e) {
                Log.warn("Textures", $"Failed to load pack from {dir}:");
                Log.warn(e);
            }
        }

        // discover zips
        foreach (var zip in Directory.GetFiles(PACK_DIR, "*.zip")) {
            try {
                var source = new ZipPackSource(zip);
                var pack = TexturePack.load(source);
                availablePacks.Add(pack);
                Log.info("Textures", $"Found pack: {pack.name} (zip)");
            } catch (Exception e) {
                Log.warn("Textures", $"Failed to load pack from {zip}:");
                Log.warn(e);
            }
        }
    }

    /**
     * Load and apply a texture pack
     */
    public void loadPack(string packName) {
        var pack = availablePacks.FirstOrDefault(p => p.internalname == packName);

        if (pack == null) {
            // fallback to vanilla or first available
            pack = availablePacks.FirstOrDefault(p => p.internalname == "vanilla") ?? availablePacks.FirstOrDefault();
            if (pack != null) {
                Log.info("Textures", $"Pack '{packName}' not found, using '{pack.name}' instead.");
            }
        }

        currentPack = pack;
        reloadAtlases();
    }

    /**
     * Reload atlases from current pack (for hot-reload or settings changes)
     */
    public void reloadAtlases() {
        // clear old sources
        clearSources();
        Game.graphics.invalidateTextures();

        // register new sources
        if (currentPack == null) {
            Log.warn("Textures", "No texture pack loaded! Using direct file loading.");
            addBlockSource("blocks.png");
            addItemSource("items.png");
        } else {
            Log.info("Textures", $"Loading texture pack: {currentPack.name}");
            currentPack.registerSources(this);
        }

        // stitch atlases
        var (blockResult, itemResult) = stitchAtlases();

        // first load vs reload
        if (blockTexture == null) {
            blockTexture = new BlockTextureAtlas(blockResult);
            itemTexture = new BTextureAtlas(itemResult);
        } else {
            blockTexture.updateFromStitch(blockResult);
            itemTexture.updateFromStitch(itemResult);
        }

        Log.info("Textures", $"Stitched atlases: blocks={blockResult.width}x{blockResult.height}, items={itemResult.width}x{itemResult.height}");

        // reload all cached non-atlas textures
        reloadCachedTextures();

        // notify dependents
        onAtlasReloaded?.Invoke();
    }

    /**
     * Reload all cached textures (for pack hot-reload)
     */
    private void reloadCachedTextures() {
        foreach (var tex in textures.Values) {
            try {
                tex.reload();
            } catch (Exception e) {
                Log.warn("Textures", $"Failed to reload texture {tex.path}:");
                Log.warn(e);
            }
        }
    }

    /**
     * Stitch block and item atlases from registered sources
     */
    private (StitchResult blocks, StitchResult items) stitchAtlases() {
        // stitch block atlas
        var blockSourceId = blockSources.Count > 0 ? blockSources[0].filepath : "textures/blocks.png";
        var blockProtected = getProtectedRegions(blockSourceId);
        var blockResult = AtlasStitcher.stitch(blockSources, blockProtected);

        // apply fastLeaves alpha processing
        if (Settings.instance.fastLeaves) {
            applyFastLeaves(blockResult);
        }

        // update Block.atlasSize
        Block.updateAtlasSize(blockResult.width, blockResult.height);

        // dispose block sources
        foreach (var src in blockSources) {
            src.dispose();
        }
        blockSources.Clear();

        // stitch item atlas (no protected regions)
        var itemResult = AtlasStitcher.stitch(itemSources, []);

        // dispose item sources
        foreach (var src in itemSources) {
            src.dispose();
        }
        itemSources.Clear();

        return (blockResult, itemResult);
    }

    /**
     * Get protected regions for dynamic textures (water, lava, fire)
     */
    private static List<ProtectedRegion> getProtectedRegions(string blockSourceId) {
        return [
            new("waterStill", blockSourceId, 0, 13 * 16, 256, 16),
            new("waterFlowing", blockSourceId, 16, 14 * 16, 32, 32),
            new("lavaStill", blockSourceId, 0, 16 * 16, 16, 16),
            new("lavaFlowing", blockSourceId, 16, 17 * 16, 32, 32),
            new("fire", blockSourceId, 48, 14 * 16, 16, 16)
        ];
    }

    /**
     * Apply fastLeaves setting by forcing leaf texture alpha to 255
     */
    private static void applyFastLeaves(StitchResult result) {
        foreach (var (source, tx, ty) in Block.leafTextureTiles) {
            if (result.tilePositions.TryGetValue((source, tx, ty), out var rect)) {
                result.image.ProcessPixelRows(accessor => {
                    for (int y = rect.Y; y < rect.Y + rect.Height; y++) {
                        var row = accessor.GetRowSpan(y);
                        for (int x = rect.X; x < rect.X + rect.Width; x++) {
                            row[x].A = 255;
                        }
                    }
                });
            } else {
                Log.warn("Textures", $"Failed to find leaf texture tile in atlas: {source} ({tx}, {ty})");
            }
        }
    }

    /**
     * Register a block atlas source from file
     */
    public void addBlockSource(string filename, int tileSize = 16) {
        blockSources.Add(new AtlasSource("textures/" + filename, tileSize));
    }

    /**
     * Register a block atlas source from an image
     */
    public void addBlockSource(string filename, Image<Rgba32> image, int tileSize = 16) {
        blockSources.Add(new AtlasSource("textures/" + filename, image, tileSize));
    }

    /**
     * Register an item atlas source from file
     */
    public void addItemSource(string filename, int tileSize = 16) {
        itemSources.Add(new AtlasSource("textures/" + filename, tileSize));
    }

    /**
     * Register an item atlas source from an image
     */
    public void addItemSource(string filename, Image<Rgba32> image, int tileSize = 16) {
        itemSources.Add(new AtlasSource("textures/" + filename, image, tileSize));
    }

    /**
     * Clear all registered sources
     */
    private void clearSources() {
        foreach (var src in blockSources) src.dispose();
        foreach (var src in itemSources) src.dispose();
        blockSources.Clear();
        itemSources.Clear();
    }

    /**
     * Get available texture packs
     */
    public List<TexturePack> getAvailablePacks() => availablePacks;

    /**
     * Get currently loaded pack
     */
    public TexturePack? getCurrentPack() => currentPack;

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

    /**
     * Open a texture stream, checking pack first, then falling back to assets
     */
    public Stream open(string path) {
        if (currentPack?.source?.exists(path) == true) {
            return currentPack?.source?.open(path)!;
        }
        return Assets.open(path);
    }

    public BTexture2D get(string path) {
        if (!textures.TryGetValue(path, out _)) {
            textures[path] = new BTexture2D(path);
            textures[path].reload();
        }

        return textures[path];
    }

    public Rgba32 light(int x, int y) {
        return lightmap[x | (y << 4)];
    }

    /**
     * Update dynamic textures
     * <param name="dt"></param>
     */
    public void update(double dt) {
        // update animated textures in block atlas
        blockTexture.update(dt);
        itemTexture.update(dt);

        // update lightmap every 5 ticks
        // or don't, this doesn't eat our performance yet
        //if (Game.globalTick % 5 == 0) {
        if (Game.world != null) {
            float skyDarken = Game.world.getSkyDarkenFloat(Game.world.worldTick);
            updateLightmap(skyDarken);
        }
        //}
    }

    private unsafe void updateLightmap(float skyDarken) {

        const float BASE = 0.04f;
        const float BASE2 = 0.16f;
        const float INVBASE = 1 - BASE;
        const float INVBASE2 = 1 - BASE2;

        float ambientBrightness = 1f - skyDarken / 16f;

        // todo add slider
        // todo add this later
        const float userBrightness = 0f;

        fixed (Rgba32* pData = lightmap) {
            for (int i = 0; i < 256; i++) {
                int skyLevel = i >> 4; // i / 16
                int blockLevel = i & 0xF; // i % 16

                // inv sq law or some shit
                float sa = skyLevel / 15f;
                //sa = sa * sa;
                //sa = sa * sa * (3f - 2f * sa);
                //sa = 0.5f - float.Sin(float.Tan(1.05f - 2.0f * sa) / 3.0f);
                sa = sa * 0.5f * (1 - sa) + sa * sa;
                float ba = blockLevel / 15f;
                //ba = ba * ba;
                //ba = ba * ba * (3f - 2f * ba);
                // ba = 0.5f - float.Sin(float.Tan(1.05f - 2.0f * ba) / 3.0f);
                ba = ba * Meth.rhoF * (1 - ba) + ba * ba * 1.17f;

                float sl = sa * ambientBrightness;
                float bl = ba;


                float br = bl;
                float bg = bl * ((bl * Meth.psiF + Meth.rhoF) * Meth.psiF + Meth.rhoF);
                float bb = bl * (bl * bl * Meth.psiF + Meth.rhoF);

                // scale red down
                float sr = sl * (ambientBrightness * Meth.psiF + Meth.rhoF);
                float sg = sl * (ambientBrightness * Meth.psiF + Meth.rhoF);
                float sb = sl;

                // screen blend
                float r = (1f - (1f - sr) * (1f - br)) * INVBASE + BASE;
                float g = (1f - (1f - sg) * (1f - bg)) * INVBASE + BASE;
                float b = (1f - (1f - sb) * (1f - bb)) * INVBASE + BASE;

                // clamp & pack
                r = float.Clamp(r, 0f, 1f);
                g = float.Clamp(g, 0f, 1f);
                b = float.Clamp(b, 0f, 1f);

                pData[i] = new Rgba32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 255);
            }
        }

        lightTexture.updateTexture(lightmap, 0, 0, LIGHTMAP_SIZE, LIGHTMAP_SIZE);
    }

    public void dumpLightmap() {
        using var image = new Image<Rgba32>(LIGHTMAP_SIZE, LIGHTMAP_SIZE);
        for (int x = 0; x < LIGHTMAP_SIZE; x++) {
            for (int y = 0; y < LIGHTMAP_SIZE; y++) {
                image[x, y] = lightmap[x | (y << 4)];
            }
        }

        image.Save("lightmap.png");
    }

    public unsafe void dumpAtlas() {
        var width = (int)blockTexture.width;
        var height = (int)blockTexture.height;
        var pixels = new Rgba32[width * height];

        fixed (Rgba32* pixelPtr = pixels) {
            GL.GetTextureImage(blockTexture.handle, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (uint)(width * height * 4), pixelPtr);
        }

        using var image = Image.WrapMemory<Rgba32>(pixels, width, height);
        image.Save("atlas.png");

        // dump 2-3-4 mipmaps too
        for (int lvl = 1; lvl <= 4; lvl++) {
            int mipWidth = Math.Max(1, width >> lvl);
            int mipHeight = Math.Max(1, height >> lvl);
            var mipPixels = new Rgba32[mipWidth * mipHeight];
            fixed (Rgba32* mipPixelPtr = mipPixels) {
                GL.GetTextureImage(blockTexture.handle, lvl, PixelFormat.Rgba, PixelType.UnsignedByte, (uint)(mipWidth * mipHeight * 4), mipPixelPtr);
            }

            using var mipImage = Image.WrapMemory<Rgba32>(mipPixels, mipWidth, mipHeight);
            mipImage.Save($"atlas_mip_{lvl}.png");
        }
    }

    public void reloadAll() {
        Game.graphics.invalidateTextures();
        // reload all cached textures
        foreach (var tex in textures.Values) {
            tex.reload();
        }

        // reload atlases
        blockTexture.reload();
        itemTexture.reload();

        if (Game.renderer != null) {
            Game.renderer.reloadTextures();
        }

        // regenerate lightmap
        for (int i = 0; i < 256; i++) {
            lightmap[i] = lightTexture.getPixel(i & 15, i >> 4);
        }
    }
}