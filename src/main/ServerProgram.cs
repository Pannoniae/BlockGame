using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BlockGame.render;
using BlockGame.util.log;

namespace BlockGame.main;

public class ServerProgram {

    public static Server server = null!;

    public static void Main(string[] args) {
        var devMode = args.Length > 0 && args[0] == "--dev";

        #if DEBUG
        Log.init(minLevel: LogLevel.DEBUG);
        #else
        Log.init(minLevel: LogLevel.INFO);
        #endif

        unsafe {
            Log.info($"The correct answer is {sizeof(BlockRenderer.RenderContext)}! What was the question?");
        }

        // name the thread
        Thread.CurrentThread.Name = "Main";

        // IMPORTANT PART
        Console.OutputEncoding = Encoding.UTF8;

        AppDomain.CurrentDomain.UnhandledException += handleCrash;

        server = new Server(devMode);
    }

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

            Console.WriteLine("Exiting...");
            Console.Out.Flush();
        }
    }
}