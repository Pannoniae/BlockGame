using System.Runtime.InteropServices;
using BlockGame.util;

class Program {
    static void Main(string[] args) {
        if (args.Contains("-h") || args.Contains("--help")) {
            showHelp();
            return;
        }

        Log.init("launchLogs");

        bool force = args.Contains("-f") || args.Contains("--force");
        if (force) {
            Log.info("Force-patching!");
        }

        // Check Windows version
        bool needsFix = force || checkWindowsVersion();
        Log.info($"Windows version check: {(needsFix ? "NEEDS FIX" : "OK, nothing to be done")}");

        if (needsFix) {
            // Patch BlockGame.exe subsystem from CUI to GUI
            string exePath = Path.Combine(Directory.GetCurrentDirectory(), "BlockGame.exe");
            if (File.Exists(exePath)) {
                Log.info($"Patching {exePath}...");
                bool success = patchPESubsystem(exePath);
                Log.info($"Patch result: {(success ? "SUCCESS" : "FAILED")}");
            }
            else {
                Log.error($"BlockGame.exe not found at {exePath}");
            }
        }

        Log.shutdown();
    }

    static bool checkWindowsVersion() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Log.info("Not running on Windows, what are you doing?");
            return false;
        }

        var osVersion = Environment.OSVersion;
        Log.info($"OS Version: {osVersion}");

        // Windows 10 = version 10.0, Windows 11 = version 10.0 with build >= 22000
        // Windows 11 24H2 = build 26100+
        if (osVersion.Version.Major == 10 && osVersion.Version.Minor == 0) {
            int buildNumber = osVersion.Version.Build;
            Log.info($"Build number: {buildNumber}");

            switch (buildNumber) {
                case < 22000:
                    Log.info("Windows 10 detected");
                    return true; // Win10 needs the fix
                case < 26100:
                    Log.info("Windows 11 (pre-24H2) detected");
                    return true; // Win11 before 24H2 needs the fix
                default:
                    Log.info("Windows 11 24H2+ detected");
                    return false; // Win11 24H2+ doesn't need the fix
            }
        }

        Log.warn("Unknown Windows version");
        return false;
    }

    public static bool patchPESubsystem(string exePath) {
        try {
            // Read PE file
            byte[] peData = File.ReadAllBytes(exePath);

            // DOS header check
            if (peData.Length < 64 || peData[0] != 0x4D || peData[1] != 0x5A) {
                Log.error("Invalid DOS header");
                return false;
            }

            // Get PE header offset
            int peOffset = BitConverter.ToInt32(peData, 60);

            // PE signature check
            if (peOffset + 4 >= peData.Length ||
                peData[peOffset] != 0x50 || peData[peOffset + 1] != 0x45 ||
                peData[peOffset + 2] != 0x00 || peData[peOffset + 3] != 0x00) {
                Log.error("Invalid PE signature");
                return false;
            }

            // Optional header starts after PE signature (4 bytes) + COFF header (20 bytes)
            int optionalHeaderOffset = peOffset + 24;

            // Magic number check for PE32/PE32+ (2 bytes)
            if (optionalHeaderOffset + 2 >= peData.Length) {
                Log.error("PE file too small");
                return false;
            }

            ushort magic = BitConverter.ToUInt16(peData, optionalHeaderOffset);
            int subsystemOffset;

            if (magic == 0x10b) // PE32
            {
                subsystemOffset = optionalHeaderOffset + 68;
            }
            else if (magic == 0x20b) // PE32+
            {
                subsystemOffset = optionalHeaderOffset + 68;
            }
            else {
                Log.error($"Unknown PE magic: 0x{magic:X}");
                return false;
            }

            if (subsystemOffset + 2 >= peData.Length) {
                Log.error("Subsystem field out of bounds");
                return false;
            }

            // Read current subsystem
            ushort currentSubsystem = BitConverter.ToUInt16(peData, subsystemOffset);
            Log.info($"Current subsystem: {currentSubsystem} ({getSubsystemName(currentSubsystem)})");

            // Check if already GUI
            const ushort IMAGE_SUBSYSTEM_WINDOWS_GUI = 2;
            const ushort IMAGE_SUBSYSTEM_WINDOWS_CUI = 3;

            if (currentSubsystem == IMAGE_SUBSYSTEM_WINDOWS_GUI) {
                Log.info("Already GUI subsystem, no change needed");
                return true;
            }

            if (currentSubsystem != IMAGE_SUBSYSTEM_WINDOWS_CUI) {
                Log.warn($"Unexpected subsystem {currentSubsystem}, patching anyway");
            }

            // Create backup
            string backupPath = exePath + ".cui_backup";
            if (!File.Exists(backupPath)) {
                File.Copy(exePath, backupPath);
                Log.info($"Backup created: {backupPath}");
            }

            // Patch subsystem to GUI
            peData[subsystemOffset] = (byte)(IMAGE_SUBSYSTEM_WINDOWS_GUI & 0xFF);
            peData[subsystemOffset + 1] = (byte)((IMAGE_SUBSYSTEM_WINDOWS_GUI >> 8) & 0xFF);

            // Write patched file
            File.WriteAllBytes(exePath, peData);
            Log.info(
                $"Patched subsystem from {getSubsystemName(currentSubsystem)} to {getSubsystemName(IMAGE_SUBSYSTEM_WINDOWS_GUI)}");

            return true;
        }
        catch (Exception ex) {
            Log.error("PE patching failed", ex);
            return false;
        }
    }

    static void showHelp() {
        Console.WriteLine("win10fix - Windows 10/11 console window fix");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  win10fix [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("DESCRIPTION:");
        Console.WriteLine("  Patches BlockGame.exe to prevent console windows from appearing");
        Console.WriteLine("  on Windows 10 and Windows 11 before 24H2 (build 26100).");
        Console.WriteLine("  Changes PE subsystem from CUI to GUI.");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  -f, --force    Force patching even on Windows 11 24H2+");
        Console.WriteLine("  -h, --help     Show this help message");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  win10fix              # Normal usage, patch if needed");
        Console.WriteLine("  win10fix -f           # Force patch regardless of OS version");
    }

    public static string getSubsystemName(ushort subsystem) => subsystem switch {
        0 => "UNKNOWN",
        1 => "NATIVE",
        2 => "WINDOWS_GUI",
        3 => "WINDOWS_CUI",
        5 => "OS2_CUI",
        7 => "POSIX_CUI",
        _ => $"UNKNOWN({subsystem})"
    };
}