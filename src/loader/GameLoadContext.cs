using System.Reflection;
using System.Runtime.Loader;

namespace BlockGame.launch.shared;

/**
 * The load context the coremod-transformed game assembly lives in.
 *
 * The untransformed core.dll is still on disk and still listed in the launcher's dependencies, so the default context
 * would load THAT when loading "core". So time to make a custom ALC which returns the transformed game for any lookup.
 * Not entirely sure whether this is the right way of doing it but it works
 */
public sealed class GameLoadContext : AssemblyLoadContext {
    private Assembly? game;
    private string? gameName;

    public GameLoadContext() : base("game") {
    }

    public Assembly loadGame(Stream assembly) {
        game = LoadFromStream(assembly);
        gameName = game.GetName().Name;
        return game;
    }

    protected override Assembly? Load(AssemblyName assemblyName) {
        // everything else falls through to the default context
        return assemblyName.Name == gameName ? game : null;
    }
}
