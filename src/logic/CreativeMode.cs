namespace BlockGame.logic;

public class CreativeMode : GameMode {
    public CreativeMode() {
        id = GameModeID.CREATIVE;
        gameplay = false;
        flying = true;
        reach = 6f;
        name = "Creative";
    }
    
}