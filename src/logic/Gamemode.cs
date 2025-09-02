namespace BlockGame.src.logic;

public class Gamemode {
    
    /**
     * Does this gamemode use blocks when building / uses breaking time for blocks?
     */
    public bool gameplay;
    public bool flying;
    public float reach;
    
    public static Gamemode[] modes = [];
}