using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BlockGame.util;

/**
 * this exists so we dont slopcode win32 into the main class
 */
public sealed partial class TickTimer : IDisposable {
    private readonly nint timer;
    private readonly bool periodRaised;

    /** ms before the deadline where we stop sleeping and spin */
    private readonly double spin;

    public TickTimer() {
        spin = 0.25;
        if (!OperatingSystem.IsWindows()) {
            return;
        }

        timer = CreateWaitableTimerExW(0, 0, CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, TIMER_ALL_ACCESS);
        if (timer == 0) {
            periodRaised = timeBeginPeriod(1) == 0;
            spin = 1.5;
        }
    }

    /** returns once <paramref name="sw"/> reaches <paramref name="deadline"/> (ms) */
    public void sleepUntil(Stopwatch sw, double deadline) {
        while (true) {
            var left = deadline - sw.Elapsed.TotalMilliseconds;
            if (left <= 0) {
                return;
            }

            if (left <= spin) {
                Thread.SpinWait(32);
                continue;
            }

            sleep(left - spin);
        }
    }

    private void sleep(double ms) {
        if (timer != 0) {
            // negative = relative, in 100ns units
            long due = -(long)(ms * 10_000);
            if (SetWaitableTimer(timer, ref due, 0, 0, 0, 0) != 0) {
                WaitForSingleObject(timer, INFINITE);
                return;
            }
        }

        Thread.Sleep(int.Max((int)ms, 1));
    }

    public void Dispose() {
        if (timer != 0) {
            CloseHandle(timer);
        }

        if (periodRaised) {
            timeEndPeriod(1);
        }
    }

    private const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x2;
    private const uint TIMER_ALL_ACCESS = 0x1F0003;
    private const uint INFINITE = 0xFFFFFFFF;

    [LibraryImport("kernel32.dll")]
    private static partial nint CreateWaitableTimerExW(nint attributes, nint name, uint flags, uint access);

    [LibraryImport("kernel32.dll")]
    private static partial int SetWaitableTimer(nint timer, ref long dueTime, int period, nint completion, nint arg, int resume);

    [LibraryImport("kernel32.dll")]
    private static partial uint WaitForSingleObject(nint handle, uint ms);

    [LibraryImport("kernel32.dll")]
    private static partial int CloseHandle(nint handle);

    [LibraryImport("winmm.dll")]
    private static partial uint timeBeginPeriod(uint ms);

    [LibraryImport("winmm.dll")]
    private static partial uint timeEndPeriod(uint ms);
}
