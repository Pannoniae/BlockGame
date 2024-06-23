// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.InteropServices;
using BlockGame;

public class Program {
    public static void Main(string[] args) {

        AppDomain.CurrentDomain.UnhandledException += handleCrash;
        Game.initDedicatedGraphics();
        _ = new Game();
    }
    public static void handleCrash(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs) {
        var e = (Exception)unhandledExceptionEventArgs.ExceptionObject;
        // call CrashReporter
        using var crashReporterProc = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = "CrashReporter",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };
        crashReporterProc.Start();

        Console.Out.WriteLine("Your game crashed! Here are some relevant details:");
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
            var process = new Process {
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