using System.Reflection;
using BlockGame.main;
using BlockGame.mod;
using BlockGame.util.log;
using BlockGame.util.xNBT;
using Mono.Cecil;

namespace BlockGame.launch;

/**
 * Launcher bootstrapper for BlockGame client.
 *
 * Discovers and loads coremods, transforms game assembly, then launches client.
 */
public class Program {
    private const string COREMOD_DIR = "mods/coremods";

    public static void Main(string[] args) {
        Log.init();

        // fast path: no coremods → zero overhead
        if (!Directory.Exists(COREMOD_DIR) || !Directory.EnumerateFileSystemEntries(COREMOD_DIR).Any()) {
            Log.info("Launcher", "No coremods, loading game normally...");
            ClientMain.Main(args);
            return;
        }

        try {
            Log.info("Launcher", "Coremods detected, starting transformation...");

            // discover coremods
            var coremods = discoverCoremods();
            if (coremods.Count == 0) {
                Log.info("Launcher", "No valid coremods found, launching normally");
                ClientMain.Main(args);
                return;
            }

            // load game assembly as raw IL
            using var game = ModuleDefinition.ReadModule("core.dll");

            // apply patches
            foreach (var coremod in coremods) {
                try {
                    var asm = Assembly.LoadFrom(coremod.dllPath);
                    var coremodType = asm.GetTypes()
                        .FirstOrDefault(t => typeof(Coremod).IsAssignableFrom(t) && !t.IsInterface);

                    if (coremodType != null) {
                        var instance = (Coremod)Activator.CreateInstance(coremodType)!;
                        instance.patch(game);
                        Log.info("Launcher", $"Applied coremod: {coremod.internalname}");
                    } else {
                        Log.warn("Launcher", $"Coremod {coremod.internalname} has no Coremod implementation");
                    }
                } catch (Exception e) {
                    Log.error("Launcher", $"Failed to apply coremod {coremod.internalname}:");
                    Log.error(e);
                    throw;
                }
            }

            // write transformed assembly to memory
            using var ms = new MemoryStream();
            game.Write(ms);
            var transformedBytes = ms.ToArray();

            // load and execute
            var transformedAsm = Assembly.Load(transformedBytes);
            var entryPoint = transformedAsm.GetType("BlockGame.client.ClientMain")?.GetMethod("Main");
            if (entryPoint == null) {
                throw new Exception("Failed to find ClientMain.Main in transformed assembly");
            }

            entryPoint.Invoke(null, [args]);

        } catch (Exception e) {
            Log.error("Launcher", "Coremod loading failed:");
            Log.error(e);
            throw;
        }
    }

    private static List<CoremodInfo> discoverCoremods() {
        var coremods = new List<CoremodInfo>();

        foreach (var modDir in Directory.GetDirectories(COREMOD_DIR)) {
            var dirName = Path.GetFileName(modDir);
            var metadataPath = Path.Combine(modDir, "mod.snbt");

            if (!File.Exists(metadataPath)) {
                Log.warn("Launcher", $"Skipping coremod {dirName}: no mod.snbt found");
                continue;
            }

            try {
                var metadata = SNBT.readFromFile(metadataPath);
                if (metadata is not NBTCompound compound) {
                    Log.warn("Launcher", $"Skipping coremod {dirName}: mod.snbt is not a compound");
                    continue;
                }

                var internalname = compound.getString("internalname");
                if (internalname == null) {
                    Log.warn("Launcher", $"Skipping coremod {dirName}: missing internalname field");
                    continue;
                }

                var dllPath = Path.Combine(modDir, internalname + ".dll");
                if (!File.Exists(dllPath)) {
                    Log.warn("Launcher", $"Skipping coremod {dirName}: no DLL at {dllPath}");
                    continue;
                }

                coremods.Add(new CoremodInfo {
                    internalname = internalname,
                    dllPath = dllPath
                });
                Log.info("Launcher", $"Discovered coremod: {internalname}");
            } catch (Exception e) {
                Log.warn("Launcher", $"Failed to load coremod metadata for {dirName}:");
                Log.warn(e);
            }
        }

        return coremods;
    }

    private class CoremodInfo {
        public string internalname = null!;
        public string dllPath = null!;
    }
}