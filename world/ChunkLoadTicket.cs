namespace BlockGame;

public readonly record struct ChunkLoadTicket(ChunkCoord chunkCoord, ChunkStatus level) {
    public readonly ChunkCoord chunkCoord = chunkCoord;
    public readonly ChunkStatus level = level;
}