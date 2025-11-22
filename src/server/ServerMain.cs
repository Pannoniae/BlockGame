using System.Diagnostics;
using System.Reflection;
using System.Text;
using BlockGame.net.srv;
using BlockGame.render;
using BlockGame.util.log;

namespace BlockGame.main;

public class ServerMain {

    public static GameServer server = null!;

    public static void Main(string[] args) {
        var devMode = args.Length > 0 && args[0] == "--dev";

        #if DEBUG
        Log.init(minLevel: LogLevel.DEBUG);
        #else
        Log.init(minLevel: LogLevel.INFO);
        #endif

        Log.info("Launching server...");

        unsafe {
            Log.info($"The correct answer is {sizeof(BlockRenderer.RenderContext)}! What was the question?");
        }

        // name the thread
        Thread.CurrentThread.Name = "Main";

        // IMPORTANT PART
        Console.OutputEncoding = Encoding.UTF8;

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
        }

        server = new GameServer(devMode);
    }

    public static void handleCrash(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs) {
        unsafe {
            var e = (Exception)unhandledExceptionEventArgs.ExceptionObject;

            Log.info("Your game crashed! Here are some relevant details:");

            Log.error(e);

            // save crash report to crashes/
            Log.saveCrashReport(e);

            Console.WriteLine("Exiting...");
            Console.Out.Flush();
        }
    }
}