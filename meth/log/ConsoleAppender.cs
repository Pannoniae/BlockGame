using Spectre.Console;

namespace BlockGame.util.log;

/**
 * Outputs formatted log messages to console with colors using Spectre.Console.
 */
public class ConsoleAppender {
    
    
    private readonly Lock consoleLock = new();
    
    public void append(LogEvent logEvent) {
        var timeStr = logEvent.timestamp.ToString("HH:mm:ss");
        var levelStr = logEvent.level.getShortName();
        var threadStr = logEvent.threadID;
        var color = logEvent.level.getLevelColor();
        
        string message;
        if (logEvent.category != null) {
            message = $"[{timeStr}] [{threadStr}/{levelStr}] [{logEvent.category}]: {logEvent.message}";
        } else {
            message = $"[{timeStr}] [{threadStr}/{levelStr}]: {logEvent.message}";
        }
        
        lock (consoleLock) {
            AnsiConsole.MarkupLine($"[{color.ToMarkup()}]{message.EscapeMarkup()}[/]");
        }
    }
    
    
}