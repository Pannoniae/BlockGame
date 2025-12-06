using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.util.log;
using BlockGame.world;
using BlockGame.world.chunk;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.util;

public static partial class MemoryUtils {

    public static void init() {
        if (OperatingSystem.IsWindows()) {
            WindowsMemoryUtility.init();
        }
    }

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

    public static void cleanGC(bool period = true) {

        SharedBlockVAO.c = 0;
        SharedBlockVAO.lastTrim = Game.permanentStopwatch.ElapsedMilliseconds;

        ArrayBlockData.blockPool.clear();
        ArrayBlockData.lightPool.clear();
        PaletteBlockData.arrayPool.clear();
        PaletteBlockData.arrayPoolU.clear();
        PaletteBlockData.arrayPoolUS.clear();
        WorldIO.saveBlockPool.clear();
        WorldIO.saveLightPool.clear();
        HeightMap.heightPool.clear();
        // probably a noop
        // it doesn't do shit except crashing some games?
        if (false) {
            Game.GL.ReleaseShaderCompiler();
        }


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

            // todo re-enable these after testing
            if (!period) {
                LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMallocTrim();
                LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_DONTNEED();
                LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_PAGEOUT();
            }
        }

        Log.info($"Released memory in {sw.Elapsed.TotalMilliseconds} ms");
    }

    public static void trim() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            WindowsMemoryUtility.ReleaseUnusedProcessWorkingSetMemory();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMallocTrim();
            LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_DONTNEED();
            LinuxMemoryUtility.ReleaseUnusedProcessWorkingSetMemoryWithMadvise_MADV_PAGEOUT();
        }
    }

    public static class LinuxMemoryUtility {
        public static string getCPUName() {
            try {
                if (File.Exists("/proc/cpuinfo")) {
                    var lines = File.ReadAllLines("/proc/cpuinfo");
                    foreach (var line in lines) {
                        if (line.StartsWith("model name", StringComparison.OrdinalIgnoreCase)) {
                            var parts = line.Split(':', 2);
                            if (parts.Length == 2) {
                                return parts[1].Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.error("Failed to read CPU info from /proc/cpuinfo:");
                Log.error(ex);
            }
            return "Unknown CPU";
        }

        public static long getTotalRAM() {
            try {
                if (File.Exists("/proc/meminfo")) {
                    var lines = File.ReadAllLines("/proc/meminfo");
                    foreach (var line in lines) {
                        if (line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase)) {
                            var parts = line.Split(':', 2);
                            if (parts.Length == 2) {
                                // meminfo reports in KB
                                var numStr = parts[1].Trim().Split(' ')[0];
                                if (long.TryParse(numStr, out long kb)) {
                                    return kb * 1024L; // convert to bytes
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.error("Failed to read RAM info from /proc/meminfo:");
                Log.error(ex);
            }
            return -1;
        }

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

    private static string? cachedCPUInfo;
    private static long cachedTotalRAM = -2; // -2 = not cached yet, -1 = failed
    private static string? cachedGPURenderer;
    private static string? cachedGPUVendor;
    private static string? cachedGLVersion;

    /// <summary>
    /// Gets GPU renderer string (e.g., "NVIDIA GeForce RTX 4060").
    /// Cached after first call since GPU doesn't change at runtime.
    /// </summary>
    public static string getGPURenderer() {
        if (cachedGPURenderer != null) {
            return cachedGPURenderer;
        }
        cachedGPURenderer = Game.GL.GetStringS(StringName.Renderer);
        return cachedGPURenderer;
    }

    /// <summary>
    /// Gets GPU vendor string (e.g., "NVIDIA Corporation").
    /// Cached after first call since GPU doesn't change at runtime.
    /// </summary>
    public static string getGPUVendor() {
        if (cachedGPUVendor != null) {
            return cachedGPUVendor;
        }
        cachedGPUVendor = Game.GL.GetStringS(StringName.Vendor);
        return cachedGPUVendor;
    }

    /// <summary>
    /// Gets OpenGL version string (e.g., "4.6.0 NVIDIA 551.86").
    /// Cached after first call since OpenGL version doesn't change at runtime.
    /// </summary>
    public static string getGLVersion() {
        if (cachedGLVersion != null) {
            return cachedGLVersion;
        }
        cachedGLVersion = Game.GL.GetStringS(StringName.Version);
        return cachedGLVersion;
    }

    /// <summary>
    /// Gets CPU information including processor name and core count.
    /// Cached after first call since CPU info doesn't change at runtime.
    /// </summary>
    public static string getCPUInfo() {
        if (cachedCPUInfo != null) {
            return cachedCPUInfo;
        }

        int logicalCores = Environment.ProcessorCount;
        string cpuName = "Unknown CPU";

        try {
            if (OperatingSystem.IsWindows()) {
                cpuName = WindowsMemoryUtility.getCPUName();
            }
            else if (OperatingSystem.IsLinux()) {
                cpuName = LinuxMemoryUtility.getCPUName();
            }
        }
        catch (Exception ex) {
            Log.error("Failed to get CPU name:");
            Log.error(ex);
        }

        cachedCPUInfo = $"{cpuName} ({logicalCores}x)";
        return cachedCPUInfo;
    }

    /// <summary>
    /// Gets total physical RAM in bytes. Returns -1 if unable to determine.
    /// Cached after first call since RAM amount doesn't change at runtime.
    /// </summary>
    public static long getTotalRAM() {
        if (cachedTotalRAM != -2) {
            return cachedTotalRAM;
        }

        try {
            if (OperatingSystem.IsWindows()) {
                cachedTotalRAM = WindowsMemoryUtility.getTotalRAM();
            }
            else if (OperatingSystem.IsLinux()) {
                cachedTotalRAM = LinuxMemoryUtility.getTotalRAM();
            }
            else {
                cachedTotalRAM = -1;
            }
        }
        catch (Exception ex) {
            Log.error("Failed to get total RAM:");
            Log.error(ex);
            cachedTotalRAM = -1;
        }

        return cachedTotalRAM;
    }

    /// Get VRAM usage in bytes. Returns per-process usage on Windows, total usage on other platforms. Returns -1 if not supported.
    public static long getVRAMUsage(out int stat) {
        // On Windows, try to get per-process GPU memory usage first because WDDM means the GPU driver doesn't know per-process usage
        // and OpenGL extensions only return total usage.
        if (OperatingSystem.IsWindows()) {
            long processGpuMem = WindowsMemoryUtility.getCurrentProcessGpuMemory();
            if (processGpuMem != -1) {
                stat = 3; // stat type for Windows Performance Counters
                return processGpuMem;
            }
        }

        // Fall back to OpenGL extensions (returns total GPU memory usage)
        var gl = Game.GL;
        if (gl.IsExtensionPresent("GL_NVX_gpu_memory_info")) {
            // NVidia card
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

    [SupportedOSPlatform("windows")]
    public static partial class WindowsMemoryUtility {
        private static PerformanceCounterCategory category;
        private static PerformanceCounter counter;

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetPhysicallyInstalledSystemMemory(out ulong totalMemoryInKilobytes);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetProcessWorkingSetSize(IntPtr proc, int minSize, int maxSize);

        [LibraryImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EmptyWorkingSet(IntPtr proc);

        public static void ReleaseUnusedProcessWorkingSetMemory() {
            //SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }

        public static long getTotalRAM() {
            try {
                if (GetPhysicallyInstalledSystemMemory(out ulong memKb)) {
                    return (long)(memKb * 1024L); // convert KB to bytes
                }
            }
            catch (Exception ex) {
                Log.error("Failed to get total RAM from GetPhysicallyInstalledSystemMemory:");
                Log.error(ex);
            }
            return -1;
        }

        public static string getCPUName() {
            try {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key != null) {
                    var cpuName = key.GetValue("ProcessorNameString")?.ToString();
                    if (!string.IsNullOrWhiteSpace(cpuName)) {
                        return cpuName.Trim();
                    }
                }
            }
            catch (Exception ex) {
                Log.error("Failed to read CPU name from registry:");
                Log.error(ex);
            }
            return "Unknown CPU";
        }

        public static void init() {
            try {
                category = new PerformanceCounterCategory("GPU Process Memory");
            }
            catch (Exception ex) {
                Log.error("Failed to initialize GPU Process Memory Performance Counter Category:");
                Log.error(ex);
            }

            using var process = Process.GetCurrentProcess();
            //string processName = process.ProcessName;
            int processId = process.Id;

            string[] instanceNames = category.GetInstanceNames();

            //Console.Out.WriteLine($"Looking for GPU memory usage for process '{processName}' (PID {processId})");
            //Console.Out.WriteLine($"Available GPU Process Memory instances: {string.Join(", ", instanceNames)}");

            // Find the instance that matches our process
            string? targetInstance = null;
            foreach (string instanceName in instanceNames) {
                // Instance names are typically in format "pid_9244_luid_0x00000000_0x00010378_phys_0" or something
                if (instanceName.Contains(processId.ToString())) {
                    targetInstance = instanceName;
                    break;
                }
            }

            if (targetInstance != null) {
                counter?.Dispose();
                counter = new PerformanceCounter("GPU Process Memory", "Dedicated Usage", targetInstance);
            }
            else {
                Log.error("Failed to find matching GPU Process Memory instance for current process.");
            }
        }

        /// <summary>
        /// Gets the current process's GPU dedicated memory usage in bytes using Windows Performance Counters.
        /// Returns -1 if not available or on error.
        /// </summary>
        /// TODO clean this method up

        public static long getCurrentProcessGpuMemory() {
            try {
                if (category == null || counter == null) {
                    return -1;
                }

                var value = counter.RawValue;

                // Performance counter returns value in bytes
                return value;
            }
            catch (Exception ex) {
                Log.error("Failed to get GPU memory for current process:");
                Log.error(ex);
                return -1;
            }
        }
    }
}
