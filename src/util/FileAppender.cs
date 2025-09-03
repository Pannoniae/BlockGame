using System.Text;
using K4os.Compression.LZ4.Streams;

namespace BlockGame.util;

/**
 * Writes logs to files with automatic rotation.
 * Creates latest.log and rotates to yyyy-MM-dd-N.log.lz4 when needed.
 */
public class FileAppender : IDisposable {
    private readonly string logDir;
    private readonly string latestLogPath;
    private StreamWriter? writer;
    private readonly Lock fileLock = new();
    private volatile bool disposed = false;
    
    public FileAppender(string logDir) {
        this.logDir = logDir;
        this.latestLogPath = Path.Combine(logDir, "latest.log");
        
        Directory.CreateDirectory(logDir);
        rotateIfNeeded();
        openLatestLog();
    }
    
    public void append(LogEvent logEvent) {
        if (disposed) return;
        
        lock (fileLock) {
            if (disposed || writer == null) return;
            
            try {
                // format: [12:34:56] [Thread-5/INFO] [WorldGen]: Generating chunk at 0,0
                var timeStr = logEvent.timestamp.ToString("HH:mm:ss");
                var levelStr = logEvent.level.getShortName();
                var threadStr = $"Thread-{logEvent.threadId}";
                
                if (logEvent.category != null) {
                    writer.WriteLine($"[{timeStr}] [{threadStr}/{levelStr}] [{logEvent.category}]: {logEvent.message}");
                } else {
                    writer.WriteLine($"[{timeStr}] [{threadStr}/{levelStr}]: {logEvent.message}");
                }
                
                writer.Flush(); // ensure it gets written immediately - can't lose logs on crash
            } catch {
                // swallow errors - logging shouldn't crash the game
            }
        }
    }
    
    public void shutdown() {
        Dispose();
    }
    
    public void Dispose() {
        if (disposed) return;
        
        lock (fileLock) {
            if (disposed) return;
            disposed = true;
            
            writer?.Flush(); // ensure final flush before closing
            writer?.Dispose();
            writer = null;
        }
    }
    
    private void rotateIfNeeded() {
        if (!File.Exists(latestLogPath)) return;
        
        // close current writer before rotation
        writer?.Dispose();
        writer = null;
        
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var index = 1;
        string rotatedPath;
        
        // find next available index: 2024-01-15-1.log.lz4, 2024-01-15-2.log.lz4, etc
        do {
            rotatedPath = Path.Combine(logDir, $"{today}-{index}.log.lz4");
            index++;
        } while (File.Exists(rotatedPath));
        
        try {
            // compress and move the old log using LZ4
            using var input = File.OpenRead(latestLogPath);
            using var output = File.Create(rotatedPath);
            using var lz4 = LZ4Stream.Encode(output, leaveOpen: false);
            input.CopyTo(lz4);
            
            // delete original after successful compression
            input.Close();
            File.Delete(latestLogPath);
        } catch {
            // if rotation fails, just overwrite - don't crash
        }
    }
    
    private void openLatestLog() {
        try {
            writer?.Dispose();
            writer = new StreamWriter(latestLogPath, append: true, Encoding.UTF8) {
                AutoFlush = false // we flush manually for perf
            };
        } catch {
            // if we can't open log file, disable file logging
            writer = null;
        }
    }
}