using Spectre.Console;

namespace BlockGame.util.log;

public enum LogLevel : byte {
    DEBUG = 0,
    INFO = 1,
    WARNING = 2,
    ERROR = 3,
    OFF = 255
}

public static class LogLevelExtensions {
    
    private static readonly Color[] levelColors = [
        Color.Grey,      // DEBUG
        Color.White,     // INFO  
        Color.Yellow,    // WARNING
        Color.Red        // ERROR
    ];
    
    public static string getShortName(this LogLevel level) => level switch {
        LogLevel.DEBUG => "DEBUG",
        LogLevel.INFO => "INFO",
        LogLevel.WARNING => "WARN", 
        LogLevel.ERROR => "ERROR",
        _ => level.ToString()
    };
    
    public static Color getLevelColor(this LogLevel level) {
        int idx = (int)level;
        return idx < levelColors.Length ? levelColors[idx] : Color.Red;
    }
}