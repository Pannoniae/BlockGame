using System.Runtime.CompilerServices;

namespace BlockGame.util.log;

/**
 * Simple, fast logging system.
 * No factories, no DI, no bullshit - just logs.
 *
 * API Usage Examples:
 * 
 * // initialize at startup  
 * Log.init("logs", LogLevel.DEBUG);
 *
 * // basic logging
 * Log.info("Server started");
 * Log.error("Connection failed");
 * Log.debug("Movement", "Player position updated");
 *
 * // "one-shot" object logging
 * Log.info(player);
 *
 * // zero-alloc interpolated strings (C# 10+)  
 * Log.debug("WorldGen", $"Chunk loaded at {x},{z} in {elapsed}ms");
 *
 * // exception logging
 * Log.error(ex, "Failed to save world");
 * 
 * // cleanup
 * Log.shutdown();
 */
public static class Log {
    private static volatile bool initialized = false;
    private static LogLevel minLevel = LogLevel.INFO;
    private static ConsoleAppender? consoleAppender;
    private static FileAppender? fileAppender;
    private static readonly Lock initLock = new();
    
    // simple init - no config objects
    public static void init(string logDir = "logs", LogLevel minLevel = LogLevel.INFO) {
        if (initialized) return;
        
        lock (initLock) {
            if (initialized) return;
            
            Log.minLevel = minLevel;
            consoleAppender = new ConsoleAppender();
            fileAppender = new FileAppender(logDir);
            
            initialized = true;
        }
    }
    
    public static void shutdown() {
        if (!initialized) {
            return;
        }

        lock (initLock) {
            if (!initialized) {
                return;
            }

            fileAppender?.shutdown();
            fileAppender = null;
            consoleAppender = null;
            initialized = false;
        }
    }
    
    // level checks for perf
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isDebugEnabled() => initialized && minLevel <= LogLevel.DEBUG;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isInfoEnabled() => initialized && minLevel <= LogLevel.INFO;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isWarnEnabled() => initialized && minLevel <= LogLevel.WARNING;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool isErrorEnabled() => initialized && minLevel <= LogLevel.ERROR;
    
    // basic logging
    public static void debug(string msg) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, null, msg);
    }
    
    public static void debug(string category, string msg) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, category, msg);
    }
    
    // generic object logging
    public static void debug<T>(T obj) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, null, obj?.ToString() ?? "null");
    }
    
    public static void debug<T>(string category, T obj) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, category, obj?.ToString() ?? "null");
    }
    
    public static void info(string msg) {
        if (isInfoEnabled()) log(LogLevel.INFO, null, msg);
    }
    
    public static void info(string category, string msg) {
        if (isInfoEnabled()) log(LogLevel.INFO, category, msg);
    }
    
    // generic object logging
    public static void info<T>(T obj) {
        if (isInfoEnabled()) log(LogLevel.INFO, null, obj?.ToString() ?? "null");
    }
    
    public static void info<T>(string category, T obj) {
        if (isInfoEnabled()) log(LogLevel.INFO, category, obj?.ToString() ?? "null");
    }
    
    public static void warn(string msg) {
        if (isWarnEnabled()) log(LogLevel.WARNING, null, msg);
    }
    
    public static void warn(string category, string msg) {
        if (isWarnEnabled()) log(LogLevel.WARNING, category, msg);
    }
    
    // generic object logging
    public static void warn<T>(T obj) {
        if (isWarnEnabled()) log(LogLevel.WARNING, null, obj?.ToString() ?? "null");
    }
    
    public static void warn(Exception ex) {
        if (isErrorEnabled()) log(LogLevel.WARNING, null, $"{ex}");
    }
    
    public static void warn(string msg, Exception ex) {
        if (isErrorEnabled()) log(LogLevel.WARNING, null, $"{msg}: {ex}");
    }
    
    public static void warn(Exception ex, string category, string msg) {
        if (isErrorEnabled()) log(LogLevel.WARNING, category, $"{msg}: {ex}");
    }
    
    public static void error(string msg) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, msg);
    }
    
    public static void error(string category, string msg) {
        if (isErrorEnabled()) log(LogLevel.ERROR, category, msg);
    }
    
    // generic object logging
    public static void error<T>(T obj) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, obj?.ToString() ?? "null");
    }
    
    public static void error(Exception ex) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, $"{ex}");
    }
    
    public static void error(string msg, Exception ex) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, $"{msg}: {ex}");
    }
    
    public static void error(string category, string msg, Exception ex) {
        if (isErrorEnabled()) log(LogLevel.ERROR, category, $"{msg}: {ex}");
    }
    
    // zero-alloc interpolated string logging
    
    public static void debug(ref DebugLogInterpolatedStringHandler handler) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, null, handler.GetFormattedText());
    }
    
    public static void debug(string category, ref DebugLogInterpolatedStringHandler handler) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, category, handler.GetFormattedText());
    }
    
    public static void info(ref InfoLogInterpolatedStringHandler handler) {
        if (isInfoEnabled()) log(LogLevel.INFO, null, handler.GetFormattedText());
    }
    
    public static void info(string category, ref InfoLogInterpolatedStringHandler handler) {
        if (isInfoEnabled()) log(LogLevel.INFO, category, handler.GetFormattedText());
    }
    
    public static void warn(ref WarnLogInterpolatedStringHandler handler) {
        if (isWarnEnabled()) log(LogLevel.WARNING, null, handler.GetFormattedText());
    }
    
    public static void warn(string category, ref WarnLogInterpolatedStringHandler handler) {
        if (isWarnEnabled()) log(LogLevel.WARNING, category, handler.GetFormattedText());
    }
    
    public static void error(ref ErrorLogInterpolatedStringHandler handler) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, handler.GetFormattedText());
    }
    
    public static void error(string category, ref ErrorLogInterpolatedStringHandler handler) {
        if (isErrorEnabled()) log(LogLevel.ERROR, category, handler.GetFormattedText());
    }
    
    public static void error(ref ErrorLogInterpolatedStringHandler handler, Exception ex) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, $"{handler.GetFormattedText()}: {ex}");
    }
    
    public static void error(string category, ref ErrorLogInterpolatedStringHandler handler, Exception ex) {
        if (isErrorEnabled()) log(LogLevel.ERROR, category, $"{handler.GetFormattedText()}: {ex}");
    }
    
    public static void log(LogLevel level, string category, [InterpolatedStringHandlerArgument("level")] ref LogInterpolatedStringHandler handler) {
        log(level, category, handler.GetFormattedText());
    }
    
    public static void log(LogLevel level, [InterpolatedStringHandlerArgument("level")] ref LogInterpolatedStringHandler handler) {
        log(level, null, handler.GetFormattedText());
    }
    
    public static void log(LogLevel level, string message) {
        log(level, null, message);
    }
    
    // core logging method
    public static void log(LogLevel level, string? category, string message) {
        if (!initialized) {
            return;
        }

        var logEvent = new LogEvent(
            DateTime.Now,
            level,
            Thread.CurrentThread.Name ?? $"Thread-{Environment.CurrentManagedThreadId.ToString()}",
            message,
            category
        );
        
        consoleAppender?.append(logEvent);
        fileAppender?.append(logEvent);
    }
}

// simple log event struct
public readonly struct LogEvent {
    public readonly DateTime timestamp;
    public readonly LogLevel level;
    public readonly string threadID;
    public readonly string message;
    public readonly string? category;
    
    public LogEvent(DateTime timestamp, LogLevel level, string threadId, string message, string? category) {
        this.timestamp = timestamp;
        this.level = level;
        this.threadID = threadId;
        this.message = message;
        this.category = category;
    }
}