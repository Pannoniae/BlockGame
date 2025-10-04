using Spectre.Console;

using ColorS = Spectre.Console.Color;

namespace BlockGame.util.log;

public enum LogLevel : byte {
    DEBUG = 0,
    INFO = 1,
    WARNING = 2,
    ERROR = 3,
    OFF = 255
}

public static class LogLevelExtensions {
    
    private static readonly ColorS[] levelColors = [
        ColorS.Grey,      // DEBUG
        ColorS.White,     // INFO
        ColorS.Yellow,    // WARNING
        ColorS.Red        // ERROR
    ];
    
    public static string getShortName(this LogLevel level) => level switch {
        LogLevel.DEBUG => "DEBUG",
        LogLevel.INFO => "INFO",
        LogLevel.WARNING => "WARN", 
        LogLevel.ERROR => "ERROR",
        _ => level.ToString()
    };
    
    public static ColorS getLevelColor(this LogLevel level) {
        int idx = (int)level;
        return idx < levelColors.Length ? levelColors[idx] : ColorS.Red;
    }
}