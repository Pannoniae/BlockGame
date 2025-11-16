using Spectre.Console;

namespace BlockGame.util.log;

/**
 * Outputs formatted log messages to console with colours using Spectre.Console.
 */
public class ConsoleAppender {

    private readonly Lock consoleLock = new();

    // callbacks for input preservation (set by ServerConsole)
    private static Func<string>? getInputBuffer;
    private static Action<string>? restoreInputLine;

    /** register callbacks for preserving console input */
    public static void registerInputHandler(Func<string> getBuffer, Action<string> restoreLine) {
        getInputBuffer = getBuffer;
        restoreInputLine = restoreLine;
    }

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
            // save and clear current input line
            string savedInput = "";
            if (getInputBuffer != null) {
                savedInput = getInputBuffer();
                if (savedInput.Length > 0) {
                    // clear line: carriage return + spaces to cover prompt and input + carriage return
                    Console.Write("\r" + new string(' ', savedInput.Length + 2) + "\r");
                }
            }

            // write log message
            AnsiConsole.MarkupLine($"[{color.ToMarkup()}]{message.EscapeMarkup()}[/]");

            // restore input line
            if (restoreInputLine != null && savedInput.Length > 0) {
                restoreInputLine(savedInput);
            }
        }
    }


}