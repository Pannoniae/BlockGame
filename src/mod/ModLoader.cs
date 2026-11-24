using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using BlockGame.util.log;
using BlockGame.util.xNBT;

namespace BlockGame.mod;

/**
 * Discovers and loads normal mods from Mods/ directory.
 */
public static class ModLoader {
    private static List<ModInfo> mods = [];
    public const string MOD_DIR = "mods";

    /**
     * Discover and load mods from the mods/ directory.
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

        // Mods go into the same load context the game itself is running in. With coremods present that's NOT the
        // default one (see GameLoadContext), and a mod loaded into the default context would bind to the untransformed
        // core.dll on disk i.e. the types would be duplicated and everything would be messed up
        var context = AssemblyLoadContext.GetLoadContext(typeof(ModLoader).Assembly) ?? AssemblyLoadContext.Default;
        context.Resolving += resolveDependency;
        context.ResolvingUnmanagedDll += resolveNativeDependency;

        // load DLLs
        foreach (var mod in mods) {
            var dllPath = Path.Combine(mod.path, mod.internalname + ".dll");
            if (!File.Exists(dllPath)) {
                Log.warn("ModLoader", $"Mod {mod.internalname} has no DLL at {dllPath}");
                continue;
            }

            try {
                var asm = context.LoadFromAssemblyPath(Path.GetFullPath(dllPath));
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
     * Handle managed libraries a mod ships sit next to its DLL.
     * Only gets called when the normal lookup has already failed.
     */
    private static Assembly? resolveDependency(AssemblyLoadContext context, AssemblyName name) {
        foreach (var mod in mods) {
            var path = Path.GetFullPath(Path.Combine(mod.path, name.Name + ".dll"));
            if (File.Exists(path)) {
                return context.LoadFromAssemblyPath(path);
            }
        }

        return null;
    }

    /**
     * Same for native libraries, fallback when the normal lookup has already failed.
     */
    private static IntPtr resolveNativeDependency(Assembly assembly, string name) {
        foreach (var mod in mods) {
            if (NativeLibrary.TryLoad(Path.GetFullPath(Path.Combine(mod.path, name)), out var handle)) {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    /**
     * Called once the vanilla content is registered, before postLoad.
     * Invokes onInit() on all loaded mods.
     */
    public static void invokeInit() {
        foreach (var mod in mods) {
            if (mod.instance == null) {
                continue;
            }

            try {
                Log.info("ModLoader", $"Initializing mod: {mod.internalname}");
                mod.instance.onInit();
            } catch (Exception e) {
                // TODO: implement something like ErrorHandler.crash instead of just logging? with contextual info
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