using System.Collections;
using BlockGame.main;
using BlockGame.util.log;

namespace BlockGame.util;

public abstract class YieldInstruction {
    public abstract bool isComplete { get; }

    public virtual void update(double dt) {
    }
}

public sealed class WaitForFrames : YieldInstruction {
    private int remainingFrames;

    public WaitForFrames(int frameCount) {
        remainingFrames = frameCount;
    }

    public override bool isComplete => remainingFrames <= 0;

    internal void decrement() {
        remainingFrames--;
    }
}

public sealed class WaitForTicks : YieldInstruction {
    private int remainingTicks;

    public WaitForTicks(int tickCount) {
        remainingTicks = tickCount;
    }

    public override bool isComplete => remainingTicks <= 0;

    internal void decrement() {
        remainingTicks--;
    }
}

public sealed class WaitForSeconds : YieldInstruction {
    private readonly long targetTime;

    public WaitForSeconds(double seconds) {
        targetTime = Game.permanentStopwatch.ElapsedMilliseconds + (long)(seconds * 1000);
    }

    public override bool isComplete => Game.permanentStopwatch.ElapsedMilliseconds >= targetTime;
}

public sealed class WaitForNextFrame : YieldInstruction {
    private bool frameElapsed = false;

    public override bool isComplete => frameElapsed;

    internal void MarkComplete() {
        frameElapsed = true;
    }
}

public sealed class WaitForMinimumTime : YieldInstruction {
    private readonly long startTime;
    private readonly long minimumDurationMs;
    
    public WaitForMinimumTime(double minimumSeconds) {
        startTime = Game.permanentStopwatch.ElapsedMilliseconds;
        minimumDurationMs = (long)(minimumSeconds * 1000);
    }
    
    public override bool isComplete => 
        Game.permanentStopwatch.ElapsedMilliseconds - startTime >= minimumDurationMs;
}

// Coroutine interfaces
public interface Coroutineish {
    bool isRunning { get; }
    bool isCompleted { get; }
    object result { get; }
    Exception ex { get; }
    void cancel();
}

public interface Coroutineish<out T> : Coroutineish {
    new T result { get; }
}

public class Coroutine : Coroutineish {
    public readonly IEnumerator enumerator;
    public YieldInstruction currentYield;
    public bool isCanceled;

    public bool isRunning { get; protected set; }
    public bool isCompleted { get; protected set; }
    public object result { get; protected set; }
    public Exception ex { get; protected set; }

    public Coroutine(IEnumerator enumerator) {
        this.enumerator = enumerator;
        isRunning = true;
    }

    public void cancel() {
        isCanceled = true;
        isRunning = false;
    }

    public virtual bool MoveNext() {
        if (isCanceled || isCompleted) {
            return false;
        }

        if (!enumerator.MoveNext()) {
            isRunning = false;
            isCompleted = true;
            return false;
        }

        currentYield = (enumerator.Current as YieldInstruction)!;

        return true;
    }
}

public class Coroutine<T> : Coroutine, Coroutineish<T> {
    private readonly IEnumerator<T> typedEnumerator;

    public new T result { get; private set; }
    object Coroutineish.result => result;

    public Coroutine(IEnumerator<T> enumerator) : base(enumerator) {
        typedEnumerator = enumerator;
    }

    public override bool MoveNext() {
        if (isCanceled || isCompleted) {
            return false;
        }

        try {
            if (!typedEnumerator.MoveNext()) {
                // Check if Current is the return value
                if (!isCompleted) {
                    result = typedEnumerator.Current;
                }

                isRunning = false;
                isCompleted = true;
                return false;
            }

            // Check if it's a yield return value (not a YieldInstruction)
            if (typedEnumerator.Current is not YieldInstruction && typedEnumerator.Current != null) {
                result = typedEnumerator.Current;
                isRunning = false;
                isCompleted = true;
                return false;
            }

            currentYield = (typedEnumerator.Current as YieldInstruction)!;
            return true;
        }
        catch (Exception ex) {
            throw;
            this.ex = ex;
            isRunning = false;
            isCompleted = true;
            return false;
        }
    }
}

public class TypedCoroutine<R> : Coroutine, Coroutineish<R> {
    private readonly IEnumerator typedEnumerator;

    public new R result { get; private set; }
    R Coroutineish<R>.result => result;

    public TypedCoroutine(IEnumerator enumerator) : base(enumerator) {
        typedEnumerator = enumerator;
    }

    public override bool MoveNext() {
        if (isCanceled || isCompleted) {
            return false;
        }

        try {
            if (!typedEnumerator.MoveNext()) {
                // Check if Current is the return value
                if (!isCompleted) {
                    result = (R)typedEnumerator.Current; // Cast to R
                }

                isRunning = false;
                isCompleted = true;
                return false;
            }

            // Check if it's a yield return value (not a YieldInstruction)
            if (typedEnumerator.Current is not YieldInstruction && typedEnumerator.Current != null) {
                result = (R)typedEnumerator.Current; // Cast to R
                isRunning = false;
                isCompleted = true;
                return false;
            }

            currentYield = typedEnumerator.Current as YieldInstruction;
            return true;
        }
        catch (Exception ex) {
            throw;
            this.ex = ex;
            isRunning = false;
            isCompleted = true;
            return false;
        }
    }
}

// Coroutine Manager
public class Coroutines {
    private readonly List<Coroutine> activeCoroutines = new();
    private readonly List<Coroutine> coroutinesToStart = new();
    private readonly List<Coroutine> tempList = new();

    public Coroutine start(IEnumerator coroutine) {
        var c = new Coroutine(coroutine);
        activeCoroutines.Add(c);
        c.MoveNext(); // Start immediately
        return c;
    }

    public Coroutine<T> start<T>(IEnumerator<T> coroutine) {
        var c = new Coroutine<T>(coroutine);
        activeCoroutines.Add(c);
        c.MoveNext(); // Start immediately
        return c;
    }
    
    public TypedCoroutine<R> start<R>(IEnumerator coroutine) {
        var c = new TypedCoroutine<R>(coroutine);
        activeCoroutines.Add(c);
        c.MoveNext(); // Start immediately
        return c;
    }

    public Coroutine startNextFrame(IEnumerator coroutine) {
        var c = new Coroutine(coroutine);
        coroutinesToStart.Add(c);
        return c;
    }

    public Coroutine<T> startNextFrame<T>(IEnumerator<T> coroutine) {
        var c = new Coroutine<T>(coroutine);
        coroutinesToStart.Add(c);
        return c;
    }
    
    public TypedCoroutine<R> startNextFrame<R>(IEnumerator coroutine) {
        var c = new TypedCoroutine<R>(coroutine);
        coroutinesToStart.Add(c);
        return c;
    }

    public void StopAll() {
        foreach (var c in activeCoroutines) {
            c.cancel();
        }

        activeCoroutines.Clear();
        coroutinesToStart.Clear();
    }

    public void update(double dt) {
        tempList.Clear();
        tempList.AddRange(activeCoroutines);

        foreach (var coroutine in tempList) {
            if (!coroutine.isRunning) continue;

            if (coroutine.currentYield is WaitForTicks wait) {
                wait.decrement();
            }
        }

        updateCoroutines(dt);
    }

    public void updateFrame(double dt) {
        // Start queued coroutines
        if (coroutinesToStart.Count > 0) {
            foreach (var c in coroutinesToStart) {
                activeCoroutines.Add(c);
                c.MoveNext();
            }

            coroutinesToStart.Clear();
        }

        tempList.Clear();
        tempList.AddRange(activeCoroutines);

        foreach (var coroutine in tempList) {
            if (!coroutine.isRunning) continue;

            if (coroutine.currentYield is WaitForFrames wait) {
                wait.decrement();
            }
            else if (coroutine.currentYield is WaitForNextFrame nextFrame) {
                nextFrame.MarkComplete();
            }
        }

        updateCoroutines(dt);
    }

    private void updateCoroutines(double dt) {
        for (int i = activeCoroutines.Count - 1; i >= 0; i--) {
            var coroutine = activeCoroutines[i];
            
            // print exceptions
            if (coroutine.ex != null) {
                Log.error("Coroutine exception", coroutine.ex);
                throw coroutine.ex;
            }

            if (!coroutine.isRunning) {
                activeCoroutines.RemoveAt(i);
                continue;
            }

            // Update time-based yields
            coroutine.currentYield?.update(dt);

            if (coroutine.currentYield == null || coroutine.currentYield.isComplete) {
                if (!coroutine.MoveNext()) {
                    
                    // check for exceptions again! why? I dunno, but it seems to improve things! (not silently swallowing exceptions
                    // and just early-exiting/corrupting state is nice)
                    if (coroutine.ex != null) {
                        Log.error("Coroutine exception", coroutine.ex);
                        throw coroutine.ex;
                    }
                    
                    // Coroutine is completely finished
                    activeCoroutines.RemoveAt(i);
                }
            }
        }
    }

    /** Random examples:
     *
     */
    // Simple coroutine
    IEnumerator FadeInEffect() {
        float alpha = 0;
        while (alpha < 1) {
            alpha += 0.02f;
            // Apply alpha to something
            yield return new WaitForNextFrame();
        }
    }

    // Coroutine with return value
    IEnumerator CheckSomethingAsync() {
        yield return new WaitForSeconds(1.0);
        // Do check
        yield return true; // Return result
    }

    private void example() {
        // Usage
        var fade = Game.startCoroutine(FadeInEffect());

        var check = Game.startCoroutine<bool>(CheckSomethingAsync());

        // Later...
        if (check.isCompleted && check.result) {
            // Handle success
        }
    }
}