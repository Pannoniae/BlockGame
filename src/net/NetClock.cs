namespace BlockGame.net;

/**
 * Client's estimate of the server tick, for rendering replicated entities in the past by a small, measured amount.
 * This time actually done properly!
 *
 * The old scheme was "lerp 1/4 of the way to the latest packet, reset on every packet", which settles ~3 ticks
 * behind and turns thresholded sends into a speed wobble. This renders at <c>approx - delay</c> between real,
 * tick-stamped snapshots instead; delay tracks how late packets actually arrive (~2 ticks over the mem pipe).
 *
 * approx runs at client rate and is corrected once per tick from the freshest write that tick - not per packet
 * (that would scale with entity count), and not when nothing arrived (an idle world is not a slow server).
 */
public static class NetClock {
    /** estimated current server tick */
    public static double approx;

    private static bool started;

    /** freshest write since the last tick, int.MinValue = none */
    private static int latest = int.MinValue;

    /** EMA of how many ticks behind est the freshest write arrives */
    private static double jitter = 1;

    public const double MIN_DELAY = 1;
    public const double MAX_DELAY = 8;

    /** fraction of the error corrected per tick. small: most of it is jitter, real drift is slow */
    private const double PULL = 0.1;

    public static double delay => double.Clamp(1 + 2 * jitter, MIN_DELAY, MAX_DELAY);

    public static double renderTick => approx - delay;

    /** newest write ever seen, used for seeding snapshots of freshly spawned entities */
    public static int newest => started ? (int)approx : int.MinValue;

    public static void reset() {
        approx = 0;
        started = false;
        latest = int.MinValue;
        jitter = 1;
    }

    public static void onRecv(int tick) {
        if (!started) {
            started = true;
            approx = tick;
        }

        if (tick > latest) {
            latest = tick;
        }
    }

    /** once per client tick, after this tick's packets were handled */
    public static void tick() {
        if (!started) {
            return;
        }

        if (latest != int.MinValue) {
            // + => we're behind the server, - => writes arrive late cuz of jitter or serverlag
            var err = latest - approx;

            // small errors are eased out so we dont hitch. big ones (stall, reconnect) just snap to latest
            if (err is > 3 or < -2 * MAX_DELAY) {
                approx = latest;
            }
            else {
                approx += err * PULL;
            }

            jitter += (double.Abs(err) - jitter) * 0.05;
            latest = int.MinValue;
        }

        approx += 1;
    }
}
