namespace BlockGame;


/** https://stackoverflow.com/questions/550785/c-events-or-an-observer-interface-pros-cons */
public interface WorldListener {

    void onWorldLoad(World world);

    void onWorldUnload(World world);
    
    void onWorldTick(World world, float delta);
    
    void onWorldRender(World world, float delta);
    
    void onChunkLoad(World world, ChunkCoord coord);
    
    void onChunkUnload(World world, ChunkCoord coord);
    
}