using BlockGame.util;

namespace BlockGameTesting;

using NUnit.Framework;

[TestFixture]
public class XJobPoolTest {
    private sealed class Counter : XJob {
        public int runs;
        public int value;

        public override void run() {
            Interlocked.Increment(ref runs);
            value = runs * 7;
        }
    }

    private sealed class Thrower : XJob {
        public bool bang = true;

        public override void run() {
            if (bang) {
                throw new InvalidOperationException("job went bang");
            }
        }
    }

    private sealed class Spinner : XJob {
        public int spins;

        public override void run() {
            // uneven work, so claims don't line up neatly with workers
            var acc = 0;
            for (var i = 0; i < spins; i++) {
                acc += i;
            }
            if (acc == int.MinValue) {
                throw new Exception("never");
            }
        }
    }

    private static Counter[] make(int n) {
        var jobs = new Counter[n];
        for (var i = 0; i < n; i++) {
            jobs[i] = new Counter();
        }
        return jobs;
    }

    [Test]
    public void everyJobRunsExactlyOnce([Values(0, 1, 2, 3, 4, 7, 8, 9, 64, 1000)] int count) {
        using var pool = new XJobPool("test", 4);

        var jobs = make(count);
        pool.run(jobs, count);

        for (var i = 0; i < count; i++) {
            Assert.That(jobs[i].runs, Is.EqualTo(1), $"job {i} of {count}");
        }
    }

    [Test]
    public void worksWithoutWorkers() {
        using var pool = new XJobPool("test", 0);

        var jobs = make(32);
        pool.run(jobs, 32);

        foreach (var j in jobs) {
            Assert.That(j.runs, Is.EqualTo(1));
        }
    }

    /** the straggler window: hammer batch boundaries and check nothing runs twice or gets skipped */
    [Test]
    public void backToBackBatchesDontOverlap() {
        using var pool = new XJobPool("test", 8);

        var rnd = new XRandom(4242);

        for (var round = 0; round < 3000; round++) {
            var count = rnd.Next(1, 17);
            var jobs = make(count);

            pool.run(jobs, count);

            for (var i = 0; i < count; i++) {
                Assert.That(jobs[i].runs, Is.EqualTo(1), $"round {round}, job {i}/{count}");
            }
        }
    }

    /** same, but with lopsided job durations so workers finish at very different times */
    [Test]
    public void unevenJobsStillComplete() {
        using var pool = new XJobPool("test", 6);

        var rnd = new XRandom(99);
        var jobs = new Spinner[64];
        for (var i = 0; i < jobs.Length; i++) {
            jobs[i] = new Spinner();
        }

        for (var round = 0; round < 300; round++) {
            // one job is 100x the others, so the batch has a long tail
            foreach (Spinner s in jobs) {
                s.spins = rnd.Next(10, 100);
            }
            jobs[rnd.Next(0, jobs.Length)].spins = 200_000;

            pool.run(jobs, jobs.Length);
        }

        Assert.Pass();
    }

    [Test]
    public void throwingJobDoesNotHangOrKillWorkers() {
        using var pool = new XJobPool("test", 4);

        var jobs = new XJob[8];
        for (var i = 0; i < 8; i++) {
            jobs[i] = i % 2 == 0 ? new Thrower() : new Counter();
        }

        // must return, not hang
        pool.run(jobs, 8);

        // and the pool must still work afterwards
        var after = make(16);
        pool.run(after, 16);
        foreach (var j in after) {
            Assert.That(j.runs, Is.EqualTo(1));
        }
    }

    /** a failure has to be visible to the caller, not just to the log */
    [Test]
    public void failureIsReportedOnTheJob() {
        using var pool = new XJobPool("test", 4);

        var jobs = new XJob[8];
        for (var i = 0; i < 8; i++) {
            jobs[i] = i % 2 == 0 ? new Thrower() : new Counter();
        }

        pool.run(jobs, 8);

        for (var i = 0; i < 8; i++) {
            if (i % 2 == 0) {
                Assert.That(jobs[i].error, Is.TypeOf<InvalidOperationException>(), $"job {i} should have failed");
            }
            else {
                Assert.That(jobs[i].error, Is.Null, $"job {i} should be clean");
            }
        }
    }

    /** jobs are pooled and reused, so a stale error must not linger into the next batch */
    [Test]
    public void errorIsClearedOnReuse() {
        using var pool = new XJobPool("test", 4);

        var jobs = new XJob[4];
        for (var i = 0; i < 4; i++) {
            jobs[i] = new Thrower();
        }

        pool.run(jobs, 4);
        foreach (var j in jobs) {
            Assert.That(j.error, Is.Not.Null);
        }

        foreach (Thrower j in jobs) {
            j.bang = false;
        }

        pool.run(jobs, 4);
        foreach (var j in jobs) {
            Assert.That(j.error, Is.Null, "stale error survived a clean run");
        }
    }

    /** the inline paths (count == 1, no workers) have to report failures the same way */
    [Test]
    public void inlinePathsAlsoReportFailure() {
        using (var pool = new XJobPool("test", 4)) {
            var one = new XJob[] { new Thrower() };
            pool.run(one, 1);
            Assert.That(one[0].error, Is.Not.Null, "count==1 runs inline and still has to report");
        }

        using (var pool = new XJobPool("test", 0)) {
            var none = new XJob[] { new Thrower(), new Thrower() };
            pool.run(none, 2);
            Assert.That(none[0].error, Is.Not.Null, "workerless pool still has to report");
            Assert.That(none[1].error, Is.Not.Null);
        }
    }

    [Test]
    public void disposeWithoutRunningAnything() {
        var pool = new XJobPool("test", 4);
        Assert.DoesNotThrow(pool.Dispose);
    }

    [Test]
    public void doubleDisposeIsFine() {
        var pool = new XJobPool("test", 2);
        pool.run(make(4), 4);
        pool.Dispose();
        Assert.DoesNotThrow(pool.Dispose);
    }
}
