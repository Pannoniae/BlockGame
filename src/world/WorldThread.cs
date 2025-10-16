using BlockGame.main;
using BlockGame.util.log;

namespace BlockGame.world;

/**
 * TODO make this stuff actually work. Probably won't happen anytime soon but oh well
 */
public class WorldThread {
    public World? world;
    public Thread? thread;

    public bool stopped = false;
    public bool started = false;


    public void run() {
        thread = new Thread(doRun) {
            IsBackground = true,
            Name = "World Thread"
        };
        thread.Start();
        started = true;
    }

    public void kill() {
        if (!started) {
            return;
        }

        thread?.Join();

        stopped = true;
    }

    public void doRun() {
        while (!stopped) {
            try {
                double dt = Game.fixeddt;
                update(dt);
            }
            catch (Exception e) {
                Log.error("Error in world thread");
                Log.error(e);
            }
        }
    }

    public void update(double dt) {
        if (!world!.paused && !Game.lockingMouse) {
            world.update(dt);
        }

        world.renderUpdate(dt);
    }
}