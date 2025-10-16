using System.Runtime.CompilerServices;
using BlockGame.util;

namespace BlockGame.world.chunk;

[InlineArray(8)]
public struct ChunkCache {
    private Chunk? c;

    public Chunk? w { get => this[0]; set => this[0] = value; }
    public Chunk? e { get => this[1]; set => this[1] = value; }
    public Chunk? s { get => this[2]; set => this[2] = value; }
    public Chunk? n { get => this[3]; set => this[3] = value; }
    public Chunk? sw { get => this[4]; set => this[4] = value; }
    public Chunk? se { get => this[5]; set => this[5] = value; }
    public Chunk? nw { get => this[6]; set => this[6] = value; }
    public Chunk? ne { get => this[7]; set => this[7] = value; }

    public Chunk? this[RawDirectionExt i] { get => this[(int)i]; set => this[(int)i] = value; }

    public void clear() {
        for (int i = 0; i < 8; i++) {
            this[i] = null;
        }
    }
}