// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BlockGame.render;
using BlockGame.util.log;
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
            if (NVAPI.applyOptimalSettings()) {
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
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        Process.GetCurrentProcess().PriorityBoostEnabled = true;

        game = new Game(devMode);
    }

    private static void msgBox(string title, string txt) {
        if (OperatingSystem.IsWindows()) {
            // use windows api to show a message box
            MessageBoxW(IntPtr.Zero, txt, title, 0);
        }
    }

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    public static void handleCrash(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs) {
        unsafe {
            var e = (Exception)unhandledExceptionEventArgs.ExceptionObject;

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