namespace BlockGame.logic;

public class SurvivalMode : GameMode {
    public SurvivalMode() {
        id = GameModeID.SURVIVAL;
        gameplay = true;
        flying = false;
        reach = 4f;
        name = "Survival";
    }
}