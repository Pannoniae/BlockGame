using System.Runtime.CompilerServices;
using System.Text;
using Cysharp.Text;

namespace BlockGame.util;

/**
 * Zero-allocation interpolated string handler for logging.
 * Only builds the string if the log level is enabled.
 *
 * God I wish I had macros right now.
 */
[InterpolatedStringHandler]
public ref struct LogInterpolatedStringHandler : IDisposable {
    /**
     * This doesn't work with nullable?
     * Why? I DUNNO
     */
    private Utf16ValueStringBuilder builder;

    private readonly bool enabled;

    public LogInterpolatedStringHandler(int literalLength, int formattedCount, LogLevel level) {
        enabled = level switch {
            LogLevel.DEBUG => Log.isDebugEnabled(),
            LogLevel.INFO => Log.isInfoEnabled(),
            LogLevel.WARNING => Log.isWarnEnabled(),
            LogLevel.ERROR => Log.isErrorEnabled(),
            _ => false
        };
        if (enabled) {
            builder = ZString.CreateStringBuilder();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string? format) {
        if (enabled) {
            if (value is IFormattable formattable && format != null) {
                builder.Append(formattable.ToString(format, null));
            }
            else {
                builder.Append(value);
            }
        }
    }

    public void Dispose() {
        if (enabled) {
            builder.Dispose();
        }
    }

    internal readonly string GetFormattedText() {
        return enabled ? builder.ToString() : "";
    }
}

[InterpolatedStringHandler]
public ref struct DebugLogInterpolatedStringHandler : IDisposable {
    private Utf16ValueStringBuilder builder;

    private readonly bool enabled;

    public DebugLogInterpolatedStringHandler(int literalLength, int formattedCount) {
        enabled = Log.isDebugEnabled();
        if (enabled) {
            builder = ZString.CreateStringBuilder();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string? format) {
        if (enabled) {
            if (value is IFormattable formattable && format != null) {
                builder.Append(formattable.ToString(format, null));
            }
            else {
                builder.Append(value);
            }
        }
    }

    public void Dispose() {
        if (enabled) {
            builder.Dispose();
        }
    }

    internal readonly string GetFormattedText() {
        return enabled ? builder.ToString() : "";
    }
}

[InterpolatedStringHandler]
public ref struct InfoLogInterpolatedStringHandler : IDisposable {
    private Utf16ValueStringBuilder builder;

    private readonly bool enabled;

    public InfoLogInterpolatedStringHandler(int literalLength, int formattedCount) {
        enabled = Log.isInfoEnabled();
        if (enabled) {
            builder = ZString.CreateStringBuilder();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string? format) {
        if (enabled) {
            if (value is IFormattable formattable && format != null) {
                builder.Append(formattable.ToString(format, null));
            }
            else {
                builder.Append(value);
            }
        }
    }

    public void Dispose() {
        if (enabled) {
            builder.Dispose();
        }
    }

    internal readonly string GetFormattedText() {
        return enabled ? builder.ToString() : "";
    }
}

[InterpolatedStringHandler]
public ref struct WarnLogInterpolatedStringHandler : IDisposable {
    private Utf16ValueStringBuilder builder;

    private readonly bool enabled;

    public WarnLogInterpolatedStringHandler(int literalLength, int formattedCount) {
        enabled = Log.isWarnEnabled();
        if (enabled) {
            builder = ZString.CreateStringBuilder();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string? format) {
        if (enabled) {
            if (value is IFormattable formattable && format != null) {
                builder.Append(formattable.ToString(format, null));
            }
            else {
                builder.Append(value);
            }
        }
    }

    public void Dispose() {
        if (enabled) {
            builder.Dispose();
        }
    }

    internal readonly string GetFormattedText() {
        return enabled ? builder.ToString() : "";
    }
}

[InterpolatedStringHandler]
public ref struct ErrorLogInterpolatedStringHandler : IDisposable {
    private Utf16ValueStringBuilder builder;

    private readonly bool enabled;

    public ErrorLogInterpolatedStringHandler(int literalLength, int formattedCount) {
        enabled = Log.isErrorEnabled();
        if (enabled) {
            builder = ZString.CreateStringBuilder();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value) {
        if (enabled) {
            builder.Append(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, string? format) {
        if (enabled) {
            if (value is IFormattable formattable && format != null) {
                builder.Append(formattable.ToString(format, null));
            }
            else {
                builder.Append(value);
            }
        }
    }

    public void Dispose() {
        if (enabled) {
            builder.Dispose();
        }
    }

    internal readonly string GetFormattedText() {
        return enabled ? builder.ToString() : "";
    }
}
