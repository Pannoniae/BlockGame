using System.Diagnostics;
using System.Runtime.InteropServices;
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
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        Process.GetCurrentProcess().PriorityBoostEnabled = true;

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