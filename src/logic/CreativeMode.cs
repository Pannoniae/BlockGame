namespace BlockGame.logic;

public class CreativeMode : GameMode {
    public CreativeMode() {
        gameplay = false;
        flying = true;
        reach = 6f;
        name = "Creative";
    }
    
}