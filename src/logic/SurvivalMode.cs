namespace BlockGame.logic;

public class SurvivalMode : GameMode {
    public SurvivalMode() {
        gameplay = true;
        flying = false;
        reach = 4f;
        name = "Survival";
    }
}