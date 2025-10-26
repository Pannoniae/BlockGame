using System.Reflection;
using BlockGame.main;
using BlockGame.util.log;

namespace BlockGame.launchsv;

/**
 * Launcher bootstrapper for BlockGame server.
 *
 * Eventually this will:
 * 1. Discover and load coremods from coremods/ folder
 * 2. Allow mods to transform assemblies (probably Cecil or MonoMod..)
 * 3. Invoke the *actual* server entry point
 *
 * For now, it's a simple pass-through to the server main.
 */
public class Program {
    public static void Main(string[] args) {
        // TODO: coremod loading and transformation goes here

        Log.init("launchLogs");

        // print whether dll can be loaded
        var a = Assembly.Load("core");
        Log.info($"Loaded core.dll: {a.FullName}");

        // invoke server entry point
        ServerMain.Main(args);
    }
}