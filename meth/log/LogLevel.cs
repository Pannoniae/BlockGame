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
    
    private static readonly ColorS[] levelColours = [
        ColorS.Grey,      // DEBUG
        ColorS.White,     // INFO
        ColorS.Yellow,    // WARNING
        ColorS.Red        // ERROR
    ];

    extension(LogLevel level) {
        public string getShortName() => level switch {
            LogLevel.DEBUG => "DEBUG",
            LogLevel.INFO => "INFO",
            LogLevel.WARNING => "WARN",
            LogLevel.ERROR => "ERROR",
            _ => level.ToString()
        };

        public ColorS getLevelColor() {
            int idx = (int)level;
            return idx < levelColours.Length ? levelColours[idx] : ColorS.Red;
        }
    }
}