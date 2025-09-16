// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BlockGame.util.log;
using Silk.NET.GLFW;

namespace BlockGame.main;

public class Program {

    public static Game game;
    
    public static void Main(string[] args) {
        var devMode = args.Length > 0 && args[0] == "--dev";

        Console.Out.WriteLine(Vector<int>.Count);
        Console.Out.WriteLine(Vector256<int>.Count);
        Console.Out.WriteLine(Vector512<int>.Count);
        Console.Out.WriteLine(Vector256<int>.IsSupported);
        Console.Out.WriteLine(Vector512<int>.IsSupported);
        Console.Out.WriteLine(Vector256.IsHardwareAccelerated);
        Console.Out.WriteLine(Vector512.IsHardwareAccelerated);
        
        // name the thread
        Thread.CurrentThread.Name = "Main";

        AppDomain.CurrentDomain.UnhandledException += handleCrash;
        Game.initDedicatedGraphics();
        Game.cc();

        game = new Game(devMode);
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
        
            // kill everything off
            Game.window.Close();
            Game.window.Dispose();
        
            
            Environment.Exit(1);
        }
    }

    public static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        Console.Out.WriteLine(libraryName);

        // Otherwise, fallback to default import resolver.
        return IntPtr.Zero;
    }
}