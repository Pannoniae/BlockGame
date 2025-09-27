namespace BlockGame.logic;

public class GameMode {
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
}