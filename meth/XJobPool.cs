using System.Diagnostics;
using BlockGame.util.log;

namespace BlockGame.util;

/**
 * One unit of CPU work.
 */
public abstract class XJob {
    /**
     * The exception the job threw (if any), normally null
     */
    public Exception? error;

    /**
     * Runs on a worker thread.
     */
    public abstract void run();
}

/**
 * Persistent workers for CPU-bound work. Threads are created once and block on a semaphore when idle -
 * nothing is spun up per job and nothing polls.
 *
 * Fork-join, not a queue: run() starts a batch, wakes the workers up, and returns
 * once every job is done. Deliberately synchronous - a batch never outlives the call, so there is no
 * ownersh to track and no staleness. The caller can hand
 * a worker a buffer, get it back, and reuse it immediately.
 *
 * NOT for blocking work. A job that waits on disk or a lock parks a worker that could be crunching;
 * IO keeps its own dedicated threads.
 *
 * run() is single-caller. Two threads calling it concurrently is a bug, not a supported mode.
 */
public sealed class XJobPool : IDisposable {
    private sealed class Batch {
        public XJob[] jobs = null!;
        public int count;
        public int next;
        public int done;
    }

    private readonly Thread[] threads;
    private readonly SemaphoreSlim wake = new(0);

    private volatile Batch? current;
    private volatile bool running = true;

    public int workers => threads.Length;

    public XJobPool(string name, int workers) {
        threads = new Thread[int.Max(workers, 0)];

        for (var i = 0; i < threads.Length; i++) {
            var t = new Thread(loop) {
                IsBackground = true,
                Name = $"{name} {i}"
            };
            threads[i] = t;
            t.Start();
        }
    }

    public static int defaultWorkers() {
        return int.Clamp(Environment.ProcessorCount - 2, 0, 8);
    }

    /**
     * Run jobs[0..count) and return when all of them are done.
     */
    public void run(XJob[] jobs, int count) {
        if (count <= 0) {
            return;
        }

        if (count == 1 || threads.Length == 0) {
            for (var i = 0; i < count; i++) {
                exec(jobs[i]);
            }
            return;
        }

        var b = new Batch { jobs = jobs, count = count };
        current = b;

        wake.Release(threads.Length);

        // do our share
        work(b);

        // and wait out whoever is still going. we only get here after finishing our own claims, so this is
        // normally a straggler or two, i.e. at most one job's worth of waiting. sleep1Threshold: -1 because
        // SpinWait's escalation ends in Thread.Sleep(1), and that is 1ms with the timer resolution raised and
        // up to 15.6ms without (test host, dedicated server) - either way longer than the whole batch. Past
        // the spin phase it yields / Sleep(0)s instead, which still gives the core away if we're oversubscribed.
        var sw = new SpinWait();
        while (Volatile.Read(ref b.done) < count) {
            sw.SpinOnce(sleep1Threshold: -1);
        }
    }

    private void loop() {
        while (running) {
            wake.Wait();

            if (!running) {
                return;
            }

            var b = current;
            if (b != null) {
                work(b);
            }
        }
    }

    /** claim and run until the batch is exhausted */
    private static void work(Batch b) {
        while (true) {
            var i = Interlocked.Increment(ref b.next) - 1;
            if (i >= b.count) {
                return;
            }

            exec(b.jobs[i]);
            Interlocked.Increment(ref b.done);
        }
    }

    private static void exec(XJob j) {
        j.error = null;

        try {
            j.run();
        }
        catch (Exception e) {
            // a bad job must not kill the worker or hang the batch - record it and carry on. the
            // Interlocked on done is what publishes this write to whoever is waiting in run().
            j.error = e;
            Log.error("Error in job:");
            Log.error(e);
        }
    }

    public void Dispose() {
        if (!running) {
            return;
        }

        running = false;
        current = null;
        wake.Release(int.Max(threads.Length, 1));

        foreach (var t in threads) {
            t.Join(2000);
        }
    }
}
