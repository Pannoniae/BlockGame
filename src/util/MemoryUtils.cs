using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using BlockGame.main;
using BlockGame.util.log;
using BlockGame.world;
using BlockGame.world.chunk;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.util;

public static class MemoryUtils {

    /// <summary>
    /// Gets the alignment of the given object.
    /// </summary>
    public static unsafe int getAlignment(object o) {
        var ptr = &o;
        var alignment = BitOperations.TrailingZeroCount((uint)ptr);
        return alignment;
    }
    
    public static unsafe void crash(string exceptionMessage) {
        Log.info("MANUAL CRASH: " + exceptionMessage);
        *(int*)0 = 42;
    }

    public static void cleanGC() {

        ArrayBlockData.blockPool.clear();
        ArrayBlockData.lightPool.clear();
        WorldIO.saveBlockPool.clear();
        WorldIO.saveLightPool.clear();
        HeightMap.heightPool.clear();
        // probably a noop
        Game.GL.ReleaseShaderCompiler();


        //Console.WriteLine("Forcing blocking GC collection and compacting of gen2 LOH and updating OS process working set size...");
        var sw = Stopwatch.StartNew();
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(generation: 2, GCCollectionMode.Aggressive, blocking: true, compacting: true);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            // todo this swaps everything out of the working set, so it will be reloaded from disk on next access
            // 100% ssd usage hello
            // implement some smarter memory heuristic or something next time so it doesn't swap *everything* because on higher render distance
            // it takes tens of SECONDS to reload, even from a fast NVMe SSD
            WindowsMemoryUtility.ReleaseUnusedProcessWorkingSetMemory();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMallocTrim();
            LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_DONTNEED();
            LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_PAGEOUT();
        }

        Log.info($"Released memory in {sw.Elapsed.TotalMilliseconds} ms");
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
                Log.error(exc);
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
                Log.error(exc);
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
                    Log.error($"malloc_trim errno: {Marshal.GetLastSystemError()}");
                }
            }
            catch (Exception exc) {
                Log.error(exc);
            }
        }
    }

    /// Get VRAM usage in bytes. Returns -1 if not supported.
    public static long getVRAMUsage(out int stat) {
        var gl = Game.GL;
        if (gl.IsExtensionPresent("GL_NVX_gpu_memory_info")) {
            // Hell yeah, NVidia card
            const uint NVX_GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX = 0x9047;
            const uint NVX_GPU_MEMORY_INFO_CURRENT_AVAILABLE_VIDMEM_NVX = 0x9049;
            const uint NVX_GPU_MEMORY_INFO_EVICTED_MEMORY_NVX = 0x904B;

            gl.GetInteger((GetPName)NVX_GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX, out int totalMemKb);
            gl.GetInteger((GetPName)NVX_GPU_MEMORY_INFO_CURRENT_AVAILABLE_VIDMEM_NVX, out int freeMemKb);

            stat = 1;
            // KB to bytes
            return (totalMemKb - freeMemKb) * 1024L;
        }

        if (gl.IsExtensionPresent("GL_ATI_meminfo")) {
            const uint ATI_MEMINFO_VBO_FREE_MEMORY_ATI = 0x87FB;

            Span<int> memInfo = stackalloc int[4];
            gl.GetInteger((GetPName)ATI_MEMINFO_VBO_FREE_MEMORY_ATI, memInfo);

            int totalMemKb = memInfo[0]; // Total free memory

            // This is a bit of a hack since we can only get free mem, not total
            // Most AMD GPUs have 4-16GB VRAM, let's guesstimate 8GB
            const int APPROX_TOTAL_MB = 8 * 1024;
            stat = 2;
            return (APPROX_TOTAL_MB - (totalMemKb / 1024)) * 1024L * 1024L;
        }
        stat = 0;
        // Welp, we're fucked. No supported method found.
        return -1;
    }

    public class WindowsMemoryUtility {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetProcessWorkingSetSize(IntPtr proc, int minSize, int maxSize);

        public static void ReleaseUnusedProcessWorkingSetMemory() {
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
        }
    }
}
