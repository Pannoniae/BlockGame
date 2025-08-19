using Molten;

namespace BlockGame;


/** https://stackoverflow.com/questions/550785/c-events-or-an-observer-interface-pros-cons */
public interface WorldListener {

    void onWorldLoad();

    void onWorldUnload();
    
    void onWorldTick(float delta);
    
    void onWorldRender(float delta);
    
    void onChunkLoad(ChunkCoord coord);
    
    void onChunkUnload(ChunkCoord coord);

    void onDirtyChunk(SubChunkCoord coord);
    
    void onDirtyChunksBatch(ReadOnlySpan<SubChunkCoord> coords);
    
    void onDirtyArea(Vector3I min, Vector3I max);

}