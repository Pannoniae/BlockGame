using System.Buffers;
using System.Collections.Concurrent;

namespace BlockGame.net;

/**
 * In-process transport for the integrated server.
 */
public sealed class MemPipe {
    public readonly record struct Frame(byte[] buf, int len);

    public readonly ConcurrentQueue<Frame> toServer = new();
    public readonly ConcurrentQueue<Frame> toClient = new();

    /** cleared on disconnect from either end */
    public volatile bool open = true;

    public static void write(ConcurrentQueue<Frame> q, ReadOnlySpan<byte> bytes) {
        var buf = ArrayPool<byte>.Shared.Rent(bytes.Length);
        bytes.CopyTo(buf);
        q.Enqueue(new Frame(buf, bytes.Length));
    }

    public void send(ReadOnlySpan<byte> bytes) {
        if (!open) {
            return;
        }
        write(toServer, bytes);
    }

    public void receive(ReadOnlySpan<byte> bytes) {
        if (!open) {
            return;
        }
        write(toClient, bytes);
    }

    public void close() {
        open = false;
        drain(toServer);
        drain(toClient);
    }

    private static void drain(ConcurrentQueue<Frame> q) {
        while (q.TryDequeue(out var f)) {
            ArrayPool<byte>.Shared.Return(f.buf);
        }
    }
}
