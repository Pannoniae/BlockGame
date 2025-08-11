namespace BlockGame.ui;

public class Settings {
    public bool vSync = false;
    public int guiScale = 4;
    public bool AO = true;
    public bool smoothLighting = true;
    public int renderDistance = 8;
    public float FOV = 75;
    public int mipmapping = 0;
    public int anisotropy = 0; // 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024
    public int antiAliasing = 0; // 0=Off, 1=FXAA, 2=2xMSAA, 3=4xMSAA, 4=2xSSAA, 5=4xSSAA, 6=2xMSAA+2xSSAA, 7=4xMSAA+2xSSAA, 8=4xMSAA+4xSSAA
    public int ssaaMode = 0; // 0=Normal, 1=Weighted, 2=Per-sample
    public bool fullscreen = false;
    public bool smoothDayNight = false; // false = classic/stepped, true = dynamic/smooth

    public static readonly Settings instance = new();

    /// <summary>
    /// Whether to use framebuffer effects.
    /// </summary>
    public bool framebufferEffects => antiAliasing > 0 || Game.hasCMDL;
    
    /// <summary>
    /// Whether FXAA is enabled.
    /// </summary>
    public bool fxaa => antiAliasing == 1;
    
    /// <summary>
    /// SSAA multiplier (1, 2, or 4).
    /// </summary>
    public int ssaa {
        get {
            var factor = antiAliasing switch {
                4 or 6 or 7 => 2, // 2xSSAA, 2xMSAA+2xSSAA, 4xMSAA+2xSSAA
                5 or 8 => 4, // 4xSSAA, 4xMSAA+4xSSAA
                _ => 1
            };
            
            // per-sample mode doesn't use traditional SSAA scaling
            if (ssaaMode == 2) return 1;
            
            return factor;
        }
    }

    /// <summary>
    /// MSAA sample count (1, 2, 4, or 8).
    /// </summary>
    public int msaa {
        get {
            var factor = antiAliasing switch {
                2 or 6 => 2, // 2xMSAA, 2xMSAA+2xSSAA
                3 or 7 or 8 => 4, // 4xMSAA, 4xMSAA+2xSSAA, 4xMSAA+4xSSAA
                _ => 1
            };
            
            // per-sample mode requires MSAA, force it on if not already enabled
            if (ssaaMode == 2 && factor == 1) {
                factor = 4; // default to 4x MSAA for per-sample mode
            }
            
            return factor;
        }
    }

    /// <summary>
    /// Effective framebuffer scale factor for rendering (1 for per-sample mode, ssaa for others)
    /// </summary>
    public int effectiveScale => ssaaMode == 2 ? 1 : ssaa;

    public string getAAText() {
        return antiAliasing switch {
            0 => "Off",
            1 => "FXAA",
            2 => "2x MSAA",
            3 => "4x MSAA",
            4 => "2x SSAA",
            5 => "4x SSAA",
            6 => "2x MSAA + 2x SSAA",
            7 => "4x MSAA + 2x SSAA",
            8 => "4x MSAA + 4x SSAA",
            _ => "Unknown AA"
        };
    }
}