namespace BlockGame.GUI;

public class Settings {
    public bool vSync = false;
    public int guiScale = 4;
    public bool AO = true;
    public bool smoothLighting = true;
    public int renderDistance = 8;
    public float FOV = 75;
    public int mipmapping = 4;
    //public int anisotropy = 8;
    public bool fxaa = true;

    public static Settings instance = new();
}