using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace BlockGame.util;

public static class MemoryUtils {
    public static void cleanGC() {
        //Console.WriteLine("Forcing blocking GC collection and compacting of gen2 LOH and updating OS process working set size...");
        var sw = Stopwatch.StartNew();
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(generation: 2, GCCollectionMode.Aggressive, blocking: true, compacting: true);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            WindowsMemoryUtility.ReleaseUnusedProcessWorkingSetMemory();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMallocTrim();
            LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_DONTNEED();
            LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_PAGEOUT();
        }

        Console.WriteLine($"Released memory in {sw.Elapsed.TotalMilliseconds} ms");
    }

    public class LinuxMemoryUtility {
        [DllImport("libc", SetLastError = true)]
        private static extern int madvise(IntPtr addr, UIntPtr length, int advice);

        // https://github.com/torvalds/linux/blob/1a4e58cce84ee88129d5d49c064bd2852b481357/arch/alpha/include/uapi/asm/mman.h
        private const int MADV_DONTNEED = 6;
        private const int MADV_PAGEOUT = 21;

        /// <summary>
        /// https://linux.die.net/man/2/madvise
        /// </summary>
        public static void ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_DONTNEED() {
            // https://linux.die.net/man/2/madvise

            try {
                var startMemoryAddress = Process.GetCurrentProcess().MainModule.BaseAddress;
                var memoryLength = new UIntPtr((ulong)Process.GetCurrentProcess().WorkingSet64);

                //Console.WriteLine($"Calling madvise with start: {startMemoryAddress} and length: {memoryLength}");

                int result = madvise(startMemoryAddress, memoryLength, MADV_DONTNEED);
                //Console.WriteLine($"Result: {result}");

                // On success madvise() returns zero. On error, it returns -1 and errno is set appropriately.
                //if (result != 0) {
                //    Console.WriteLine($"madvise errno: {Marshal.GetLastSystemError()}");
                //}
            }
            catch (Exception exc) {
                Console.WriteLine(exc);
            }
        }

        /// <summary>
        /// https://linux.die.net/man/2/madvise
        /// </summary>
        public static void ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_PAGEOUT() {

            try {
                var startMemoryAddress = Process.GetCurrentProcess().MainModule.BaseAddress;
                var memoryLength = new UIntPtr((ulong)Process.GetCurrentProcess().WorkingSet64);

                //Console.WriteLine($"Calling madvise with start: {startMemoryAddress} and length: {memoryLength}");

                int result = madvise(startMemoryAddress, memoryLength, MADV_PAGEOUT);
                //Console.WriteLine($"Result: {result}");

                // On success madvise() returns zero. On error, it returns -1 and errno is set appropriately.
                //if (result != 0) {
                //    Console.WriteLine($"madvise errno: {Marshal.GetLastSystemError()}");
                //}
            }
            catch (Exception exc) {
                Console.WriteLine(exc);
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int malloc_trim(uint pad);

        public static void ReleaseUnusedProcessWorkingSetMemoryWithMallocTrim() {
            // https://man7.org/linux/man-pages/man3/malloc_trim.3.html

            try {
                //Console.WriteLine($"Calling malloc_trim(0)");
                int result = malloc_trim(0);
                //Console.WriteLine($"Result: {result}");

                // The malloc_trim() function returns 1 if memory was actually
                // released back to the system, or 0 if it was not possible to
                // release any memory.
                if (result != 1) {
                    Console.WriteLine($"malloc_trim errno: {Marshal.GetLastSystemError()}");
                }
            }
            catch (Exception exc) {
                Console.WriteLine(exc);
            }
        }
    }

    public class WindowsMemoryUtility {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetProcessWorkingSetSize(IntPtr proc, int minSize, int maxSize);

        public static void ReleaseUnusedProcessWorkingSetMemory() {
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
        }
    }
}