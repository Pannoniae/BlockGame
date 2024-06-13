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
        Console.Out.WriteLine("Your game crashed! Here are some relevant details:");
        Console.WriteLine(e.ToString());
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            // call glxinfo
            var process = new Process {
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
    }
}