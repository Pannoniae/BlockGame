namespace BlockGame.ui;

public class Settings {
    public bool vSync = false;
    public int guiScale = 4;
    public bool AO = true;
    public bool smoothLighting = true;
    public int renderDistance = 8;
    public float FOV = 75;
    public int mipmapping = 0;
    //public int anisotropy = 8;
    public int antiAliasing = 0; // 0=Off, 1=FXAA, 2=2xSSAA, 3=4xSSAA

    public static readonly Settings instance = new();

    /// <summary>
    /// Whether to use framebuffer effects.
    /// </summary>
    public bool framebufferEffects => antiAliasing > 0;
    
    /// <summary>
    /// Whether FXAA is enabled.
    /// </summary>
    public bool fxaa => antiAliasing == 1;
    
    /// <summary>
    /// SSAA multiplier (1, 2, or 4).
    /// </summary>
    public int ssaa => antiAliasing >= 2 ? (antiAliasing == 2 ? 2 : 4) : 1;

    public string getAAText() {
        return antiAliasing switch {
            0 => "Off",
            1 => "FXAA",
            2 => "2x SSAA",
            3 => "4x SSAA",
            _ => "Unknown"
        };
    }
}