using System.Runtime.CompilerServices;
using System.Text;

namespace BlockGame.util;

/**
 * Zero-allocation interpolated string handler for logging.
 * Only builds the string if the log level is enabled.
 */
[InterpolatedStringHandler]
public ref struct LogInterpolatedStringHandler {
    private readonly StringBuilder? builder;

    private readonly bool enabled;
    
    public LogInterpolatedStringHandler(int literalLength, int formattedCount, LogLevel level) {
        enabled = level switch {
            LogLevel.DEBUG => Log.isDebugEnabled(),
            LogLevel.INFO => Log.isInfoEnabled(), 
            LogLevel.WARNING => Log.isWarnEnabled(),
            LogLevel.ERROR => Log.isErrorEnabled(),
            _ => false
        };
        builder = enabled ? new StringBuilder(literalLength) : null;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string value) {
        builder?.Append(value);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value) {
        builder?.Append(value);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string? format) {
        if (builder != null) {
            if (value is IFormattable formattable && format != null) {
                builder.Append(formattable.ToString(format, null));
            } else {
                builder.Append(value);
            }
        }
    }
    
    internal readonly string GetFormattedText() {
        return builder?.ToString() ?? string.Empty;
    }
}