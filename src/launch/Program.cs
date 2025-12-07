using BlockGame.launch.shared;
using BlockGame.util.log;

namespace BlockGame.launch;

/**
 * Launcher bootstrapper for BlockGame client.
 */
public class Program {
    public static void Main(string[] args) {
        Log.init();
        CoremodLoader.loadAndLaunch("BlockGame.main.ClientMain", "Main", args);
    }
}