using System.Diagnostics;
using BlockGame.util.log;

namespace BlockGame.util;

public abstract class XJob {
    /** the exception run() threw, if any */
    public Exception? error;

    public abstract void run();
}

/**
 * Fork-join pool for CPU-bound work. The threads are created once and block on a semaphore when idle,
 * so nothing gets spawned per job and nothing polls. run() hands out a batch and blocks until every
 * job in it is done, which means a buffer given to a job can be reused as soon as run() returns.
 *
 * Not for blocking work - a job that waits on disk or a lock parks a worker. IO gets its own threads.
 * Only call run() from one thread at a time.
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

    /** Runs jobs[0..count) and blocks until all of them are done. */
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

        // wait out whoever is still going. we only get here after finishing our own claims, so there's
        // at most one job's worth of waiting left. sleep1Threshold: -1 keeps SpinWait from escalating
        // to Thread.Sleep(1), which is 1-15.6ms depending on timer resolution - longer than the whole batch.
        var sw = new SpinWait();
        while (Volatile.Read(ref b.done) < count) {
            sw.SpinOnce(sleep1Threshold:-1);
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
            // a bad job must not kill the worker or hang the batch - record the error and move on
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