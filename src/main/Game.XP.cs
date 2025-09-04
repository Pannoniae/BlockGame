using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.util;

namespace BlockGame;

/* Random experiments in this class */
public partial class Game {
    private static PosixSignalRegistration reg;
    private static PosixSignalRegistration reg2;
    private static sigaction_t sa;
    private static sigaction_t sa2;

    private static void sigSegvHandler(PosixSignalContext psc) {
        //psc.Cancel = true;
        Console.WriteLine("SIGSEGV in managed thread");
        Console.Out.Flush();
        //Environment.Exit(139);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct sigaction_t {
        public IntPtr sa_handler;
        public ulong sa_mask;
        public int sa_flags;
        public IntPtr sa_restorer;
    }

    [LibraryImport("libc", SetLastError = true)]
    private static partial int sigaction(int signum, ref sigaction_t act, IntPtr oldact);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void sigSegvHandler(int signal) {
        // can't reliably use Console.WriteLine here, use write() syscall
        byte[] msg = "SIGSEGV caught\n"u8.ToArray();
        write(2, in msg[0], (nuint)msg.Length); // stderr
        fsync(2); // flush stderr 
        //_exit(139);
    }

    [LibraryImport("libc")]
    private static partial int fsync(int fd);

    [LibraryImport("libc")]
    private static partial nint write(int fd, in byte buf, nuint count);

    [LibraryImport("libc")]
    private static partial void _exit(int status);

    private unsafe void sigHandler() {
        if (OperatingSystem.IsLinux()) {
            /*
            // todo this shit doesn't work..... why?
            //reg = PosixSignalRegistration.Create((PosixSignal)11, sigSegvHandler);
            //reg2 = PosixSignalRegistration.Create((PosixSignal)6, sigSegvHandler);

            sa = new sigaction_t {
                sa_handler = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&sigSegvHandler,
                sa_flags = 0x40000000 // SA_RESTART
            };
            sigaction(11, ref sa, IntPtr.Zero);
            sa2 = new sigaction_t {
                sa_handler = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&sigSegvHandler,
                sa_flags = 0x40000000 // SA_RESTART
            };
            sigaction(6, ref sa2, IntPtr.Zero);
            */
        }
    }

    private static void regNativeLib() {
        //NativeLibrary.SetDllImportResolver(typeof(Game).Assembly, nativeLibPath);
        //NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly, nativeLibPath);
    }

    private static IntPtr nativeLibPath(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
        string arch = RuntimeInformation.OSArchitecture == Architecture.X64 ? "x64" : "x86";

        // Create path to libs
        string libsPath = Path.Combine(AppContext.BaseDirectory, "libs", arch);
        string libraryPath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            libraryPath = Path.Combine(libsPath, $"{libraryName}.dll");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            // macOS has *one* path regardless of architecture
            libsPath = Path.Combine(AppContext.BaseDirectory, "libs", "osx");
            libraryPath = Path.Combine(libsPath, $"lib{libraryName}.dylib");
        }
        // Linux
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            libraryPath = Path.Combine(libsPath, $"lib{libraryName}.so");
        }
        else {
            throw new PlatformNotSupportedException($"Platform {RuntimeInformation.OSDescription} is not supported.");
        }

        // Try to load the library if it exists
        if (File.Exists(libraryPath)) {
            if (!shutUp) {
                Log.info($"Loading native library: {libraryName} {libraryPath}");
            }

            shutUp = true;
            return NativeLibrary.Load(libraryPath);
        }

        // Fallback to default resolution
        Log.warn($"Couldn't find native library {libraryName}, falling back to default resolution");
        return IntPtr.Zero;
    }
}