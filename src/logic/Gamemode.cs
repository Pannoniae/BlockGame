using BlockGame.util;

namespace BlockGame.logic;

public class GameMode {

    public GameModeID id;

    /**
     * Does this gamemode use blocks when building / uses breaking time for blocks?
     */
    public bool gameplay;

    public bool flying;
    public float reach;
    public string name;

    public static GameMode creative = new CreativeMode();
    public static GameMode survival = new SurvivalMode();

    public static GameMode[] modes = [creative, survival];

    public static GameMode fromID(GameModeID id) {
        return id switch {
            GameModeID.CREATIVE => creative,
            GameModeID.SURVIVAL => survival,
            _ => throw new InputException("Invalid gamemode ID: " + id)
        };
    }
}

public enum GameModeID : byte {
    CREATIVE = 0,
    SURVIVAL = 1
}