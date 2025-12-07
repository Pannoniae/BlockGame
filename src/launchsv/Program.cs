using BlockGame.launch.shared;
using BlockGame.util.log;

namespace BlockGame.launchsv;

/**
 * Launcher bootstrapper for BlockGame server.
 */
public class Program {
    public static void Main(string[] args) {
        Log.init();
        CoremodLoader.loadAndLaunch("BlockGame.main.ServerMain", "Main", args);
    }
}