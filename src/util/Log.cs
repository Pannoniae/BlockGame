using System.Runtime.CompilerServices;

namespace BlockGame.util;

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
 * // formatted - no boxing
 * Log.info("Player {0} joined with ID {1}", playerName, playerId);
 * Log.warn("Memory", "Low memory: {0}MB free", freeMemory);
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
    
    public static void info(string msg) {
        if (isInfoEnabled()) log(LogLevel.INFO, null, msg);
    }
    
    public static void info(string category, string msg) {
        if (isInfoEnabled()) log(LogLevel.INFO, category, msg);
    }
    
    public static void warn(string msg) {
        if (isWarnEnabled()) log(LogLevel.WARNING, null, msg);
    }
    
    public static void warn(string category, string msg) {
        if (isWarnEnabled()) log(LogLevel.WARNING, category, msg);
    }
    
    public static void error(string msg) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, msg);
    }
    
    public static void error(string category, string msg) {
        if (isErrorEnabled()) log(LogLevel.ERROR, category, msg);
    }
    
    public static void error(Exception ex, string msg) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, $"{msg}: {ex}");
    }
    
    public static void error(string category, Exception ex, string msg) {
        if (isErrorEnabled()) log(LogLevel.ERROR, category, $"{msg}: {ex}");
    }
    
    // formatted logging - no boxing with generics
    public static void debug<T>(string format, T arg) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, null, string.Format(format, arg));
    }
    
    public static void debug<T>(string category, string format, T arg) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, category, string.Format(format, arg));
    }
    
    public static void debug<T1, T2>(string format, T1 arg1, T2 arg2) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, null, string.Format(format, arg1, arg2));
    }
    
    public static void debug<T1, T2>(string category, string format, T1 arg1, T2 arg2) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, category, string.Format(format, arg1, arg2));
    }
    
    public static void debug<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, null, string.Format(format, arg1, arg2, arg3));
    }
    
    public static void debug<T1, T2, T3>(string category, string format, T1 arg1, T2 arg2, T3 arg3) {
        if (isDebugEnabled()) log(LogLevel.DEBUG, category, string.Format(format, arg1, arg2, arg3));
    }
    
    public static void info<T>(string format, T arg) {
        if (isInfoEnabled()) log(LogLevel.INFO, null, string.Format(format, arg));
    }
    
    public static void info<T>(string category, string format, T arg) {
        if (isInfoEnabled()) log(LogLevel.INFO, category, string.Format(format, arg));
    }
    
    public static void info<T1, T2>(string format, T1 arg1, T2 arg2) {
        if (isInfoEnabled()) log(LogLevel.INFO, null, string.Format(format, arg1, arg2));
    }
    
    public static void info<T1, T2>(string category, string format, T1 arg1, T2 arg2) {
        if (isInfoEnabled()) log(LogLevel.INFO, category, string.Format(format, arg1, arg2));
    }
    
    public static void info<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) {
        if (isInfoEnabled()) log(LogLevel.INFO, null, string.Format(format, arg1, arg2, arg3));
    }
    
    public static void info<T1, T2, T3>(string category, string format, T1 arg1, T2 arg2, T3 arg3) {
        if (isInfoEnabled()) log(LogLevel.INFO, category, string.Format(format, arg1, arg2, arg3));
    }
    
    public static void warn<T>(string format, T arg) {
        if (isWarnEnabled()) log(LogLevel.WARNING, null, string.Format(format, arg));
    }
    
    public static void warn<T>(string category, string format, T arg) {
        if (isWarnEnabled()) log(LogLevel.WARNING, category, string.Format(format, arg));
    }
    
    public static void warn<T1, T2>(string format, T1 arg1, T2 arg2) {
        if (isWarnEnabled()) log(LogLevel.WARNING, null, string.Format(format, arg1, arg2));
    }
    
    public static void warn<T1, T2>(string category, string format, T1 arg1, T2 arg2) {
        if (isWarnEnabled()) log(LogLevel.WARNING, category, string.Format(format, arg1, arg2));
    }
    
    public static void warn<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) {
        if (isWarnEnabled()) log(LogLevel.WARNING, null, string.Format(format, arg1, arg2, arg3));
    }
    
    public static void warn<T1, T2, T3>(string category, string format, T1 arg1, T2 arg2, T3 arg3) {
        if (isWarnEnabled()) log(LogLevel.WARNING, category, string.Format(format, arg1, arg2, arg3));
    }
    
    public static void error<T>(string format, T arg) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, string.Format(format, arg));
    }
    
    public static void error<T>(string category, string format, T arg) {
        if (isErrorEnabled()) log(LogLevel.ERROR, category, string.Format(format, arg));
    }
    
    public static void error<T1, T2>(string format, T1 arg1, T2 arg2) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, string.Format(format, arg1, arg2));
    }
    
    public static void error<T1, T2>(string category, string format, T1 arg1, T2 arg2) {
        if (isErrorEnabled()) log(LogLevel.ERROR, category,string.Format(format, arg1, arg2));
    }
    
    public static void error<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) {
        if (isErrorEnabled()) log(LogLevel.ERROR, null, string.Format(format, arg1, arg2, arg3));
    }
    
    public static void error<T1, T2, T3>(string category, string format, T1 arg1, T2 arg2, T3 arg3) {
        if (isErrorEnabled()) log(LogLevel.ERROR, category, string.Format(format, arg1, arg2, arg3));
    }
    
    // zero-alloc interpolated string logging
    public static void log(LogLevel level, string category, [InterpolatedStringHandlerArgument("level")] ref LogInterpolatedStringHandler handler) {
        log(level, category, handler.GetFormattedText());
    }
    
    public static void log(LogLevel level, [InterpolatedStringHandlerArgument("level")] ref LogInterpolatedStringHandler handler) {
        log(level, null, handler.GetFormattedText());
    }
    
    // core logging method
    public static void log(LogLevel level, string? category, string message) {
        if (!initialized) return;
        
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