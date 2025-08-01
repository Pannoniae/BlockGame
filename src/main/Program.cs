﻿// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BlockGame;

public class Program {
    public static void Main(string[] args) {
        var devMode = args.Length > 0 && args[0] == "--dev";

        Console.Out.WriteLine(Vector<int>.Count);
        Console.Out.WriteLine(Vector256<int>.Count);
        Console.Out.WriteLine(Vector512<int>.Count);
        Console.Out.WriteLine(Vector256<int>.IsSupported);
        Console.Out.WriteLine(Vector512<int>.IsSupported);
        Console.Out.WriteLine(Vector256.IsHardwareAccelerated);
        Console.Out.WriteLine(Vector512.IsHardwareAccelerated);

        AppDomain.CurrentDomain.UnhandledException += handleCrash;
        Game.initDedicatedGraphics();

        _ = new Game(devMode);
    }
    public static void handleCrash(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs) {
        var e = (Exception)unhandledExceptionEventArgs.ExceptionObject;

        Console.Out.WriteLine("Your game crashed! Here are some relevant details:");
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
                Console.Out.WriteLine("OpenGL info:");
                Console.Out.WriteLine(process.StandardOutput.ReadToEnd());
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
                Console.Out.WriteLine("OpenGL info:");
                Console.Out.WriteLine(process.StandardOutput.ReadToEnd());
            }
            Console.WriteLine(e.ToString());
        }
    }

    public static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        Console.Out.WriteLine(libraryName);

        // Otherwise, fallback to default import resolver.
        return IntPtr.Zero;
    }
}