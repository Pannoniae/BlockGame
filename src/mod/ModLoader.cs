using System.Reflection;
using BlockGame.util.log;
using BlockGame.util.xNBT;

namespace BlockGame.mod;

/**
 * Discovers and loads normal mods from Mods/ directory.
 */
public static class ModLoader {
    private static List<ModInfo> mods = [];
    public const string MOD_DIR = "Mods";

    /**
     * Discover and load mods from Mods/ directory.
     * Called early during game initialization.
     */
    public static void discover() {
        if (!Directory.Exists(MOD_DIR)) {
            Log.info("ModLoader", "No mods/ directory found, skipping mod loading");
            return;
        }

        // discover mods
        foreach (var modDir in Directory.GetDirectories(MOD_DIR)) {
            var dirName = Path.GetFileName(modDir);

            // skip coremods - they're loaded by launcher
            if (dirName == "coremods") {
                continue;
            }

            var metadataPath = Path.Combine(modDir, "mod.snbt");
            if (!File.Exists(metadataPath)) {
                Log.warn("ModLoader", $"Skipping {dirName}: no mod.snbt found");
                continue;
            }

            try {
                var metadata = SNBT.readFromFile(metadataPath);
                if (metadata is not NBTCompound compound) {
                    Log.warn("ModLoader", $"Skipping {dirName}: mod.snbt is not a compound");
                    continue;
                }

                var info = new ModInfo(compound, modDir);
                mods.Add(info);
                Log.info("ModLoader", $"Discovered mod: {info.name} ({info.internalname}) v{info.version}");
            } catch (Exception e) {
                Log.warn("ModLoader", $"Failed to load metadata for {dirName}:");
                Log.warn(e);
            }
        }

        // TODO: topological sort by dependencies

        // load DLLs
        foreach (var mod in mods) {
            var dllPath = Path.Combine(mod.path, mod.internalname + ".dll");
            if (!File.Exists(dllPath)) {
                Log.warn("ModLoader", $"Mod {mod.internalname} has no DLL at {dllPath}");
                continue;
            }

            try {
                var asm = Assembly.LoadFrom(dllPath);
                var modType = asm.GetTypes().FirstOrDefault(t => typeof(Mod).IsAssignableFrom(t) && !t.IsInterface);
                if (modType != null) {
                    mod.instance = (Mod)Activator.CreateInstance(modType)!;
                    Log.info("ModLoader", $"Loaded mod assembly: {mod.internalname}");
                } else {
                    Log.warn("ModLoader", $"Mod {mod.internalname} DLL contains no Mod implementation");
                }
            } catch (Exception e) {
                Log.warn("ModLoader", $"Failed to load DLL for {mod.internalname}:");
                Log.warn(e);
            }
        }

        // TODO: register asset sources
    }

    /**
     * Called from Block.preLoad() after vanilla blocks are registered.
     * Invokes onInit() on all loaded mods.
     */
    public static void invokeInit() {
        foreach (var mod in mods) {
            if (mod.instance == null) continue;

            try {
                Log.info("ModLoader", $"Initializing mod: {mod.internalname}");
                mod.instance.onInit();
            } catch (Exception e) {
                // TODO: use ErrorHandler.crash instead of just logging
                Log.error("ModLoader", $"Mod {mod.internalname} failed to initialize:");
                Log.error(e);
                throw;
            }
        }
    }

    /**
     * Get list of loaded mods (for UI, debugging, etc.)
     */
    public static List<ModInfo> getMods() => mods;
}