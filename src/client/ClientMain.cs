// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BlockGame.render;
using BlockGame.util.log;
using BlockGame.world;
using ppy;

namespace BlockGame.main;

public partial class ClientMain {
    public static Game game = null!;

    public static void Main(string[] args) {
        var devMode = args.Length > 0 && args[0] == "--dev";

        #if DEBUG
        Log.init(minLevel: LogLevel.DEBUG);
        #else
        Log.init(minLevel: LogLevel.INFO);
        #endif

        Log.info("Launching client...");

        // check for sudo/root - game doesn't work under elevated privileges
        if (isSudo()) {
            Log.error("ERROR: This game cannot be run as root/sudo or with elevated privileges!");
            Log.error("Please run without sudo or administrator privileges.");
            Environment.Exit(1);
            return;
        }

        unsafe {
            Log.info($"The correct answer is {sizeof(BlockRenderer.RenderContext)}! What was the question?");
        }

        // name the thread
        Thread.CurrentThread.Name = "Main";

        // IMPORTANT PART
        Console.OutputEncoding = Encoding.UTF8;

        // thx osu/ppy!
        if (OperatingSystem.IsWindows() && NVAPI.Available) {
            //NVAPI.ThreadedOptimisations = NvThreadControlSetting.OGL_THREAD_CONTROL_DEFAULT;
            if (NVAPI.applyOptimalSettings() && !NVAPI.errored) {
                msgBox("NVIDIA Settings Applied",
                    "NVIDIA GPU settings have been configured. Please restart the game for the changes to take effect.");
                return;
            }
        }
        else {
            Log.info("NVAPI", "NVAPI not available.");
        }

        AppDomain.CurrentDomain.UnhandledException += handleCrash;

        // I'm tired of lagspikes
        try {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
            Process.GetCurrentProcess().PriorityBoostEnabled = true;
        }
        catch (Exception e) {
            Log.warn("Could not set process priority: ");
            Log.warn(e);

            if (OperatingSystem.IsLinux()) {
                try {
                    var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;

                    // Check if we have the capability
                    var check = Process.Start(new ProcessStartInfo {
                        FileName = "getcap",
                        Arguments = exePath,
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    });
                    check?.WaitForExit();
                    var output = check?.StandardOutput.ReadToEnd();

                    if (!output?.Contains("cap_sys_nice") ?? true) {
                        Log.info("Need to set capabilities. Please enter password:");
                        var setcap = Process.Start(new ProcessStartInfo {
                            FileName = "sudo",
                            Arguments = $"setcap cap_sys_nice=eip {exePath}",
                            UseShellExecute = false
                        });
                        setcap.WaitForExit();

                        if (setcap.ExitCode == 0) {
                            Log.info("Capabilities set. Please restart the game.");
                            Environment.Exit(0);
                        }
                    }
                }
                catch (Exception capEx) {
                    Log.debug("Could not check/set capabilities (missing getcap/setcap?): " + capEx.Message);
                }
            }
        }

        game = new Game(devMode);
    }

    private static void msgBox(string title, string txt) {
        if (OperatingSystem.IsWindows()) {
            // use windows api to show a message box
            MessageBoxW(IntPtr.Zero, txt, title, 0);
        }
    }

    private static bool isSudo() {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
            // check EUID = 0 or SUDO_USER env var
            if (Environment.GetEnvironmentVariable("SUDO_USER") != null) return true;

            try {
                return geteuid() == 0;
            }
            catch {
                return false;
            }
        }

        return false;
    }

    [LibraryImport("libc", SetLastError = true)]
    private static partial uint geteuid();

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    public static void handleCrash(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs) {
        unsafe {
            var e = (Exception)unhandledExceptionEventArgs.ExceptionObject;

            // delete world lock file!!
            var path = WorldIO.getLockFilePath(Game.world.name);
            if (File.Exists(path)) {
                File.Delete(path);
            }

            Log.info("Your game crashed! Here are some relevant details:");
            if (!Game.devMode) {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    // call glxinfo
                    using var process = new Process {
                        StartInfo = new ProcessStartInfo {
                            FileName = "glxinfo",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    // read its output
                    process.Start();
                    Log.info("OpenGL info:");
                    Log.info(process.StandardOutput.ReadToEnd());
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    // call wglinfo
                    using var process = new Process {
                        StartInfo = new ProcessStartInfo {
                            FileName = "wglinfo64.exe",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    // read its output
                    process.Start();
                    Log.info("OpenGL info:");
                    Log.info(process.StandardOutput.ReadToEnd());
                }
            }

            Log.error(e);

            // save crash report to crashes/
            Log.saveCrashReport(e);

            Console.WriteLine("Exiting...");
            Console.Out.Flush();
        }
    }
}