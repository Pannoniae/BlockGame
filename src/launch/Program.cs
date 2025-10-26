using System.Reflection;
using BlockGame.main;
using BlockGame.util.log;

namespace BlockGame.launch;

/**
 * Launcher bootstrapper for BlockGame client.
 *
 * Eventually this will:
 * 1. Discover and load coremods from coremods/ folder
 * 2. Allow mods to transform assemblies (probably Cecil or MonoMod..)
 * 3. Invoke the *actual* client entry point
 *
 * For now, it's a simple pass-through to the client main.
 */
public class Program {
    public static void Main(string[] args) {
        // TODO: coremod loading and transformation goes here

        Log.init("launchLogs");

        // print whether dll can be loaded
        var a = Assembly.Load("core");
        Log.info($"Loaded core.dll: {a.FullName}");

        // invoke client entry point
        ClientMain.Main(args);
    }
}