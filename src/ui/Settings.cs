using BlockGame.main;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.util.xNBT;

namespace BlockGame.ui;

public enum RendererMode {
    Auto = 0,
    Plain = 1,
    Instanced = 2,
    BindlessMDI = 3,
    CommandList = 4
}

public static class RendererModeExt {
    public static string yes(this RendererMode mode) {
        return mode switch {
            RendererMode.Auto => "A",
            RendererMode.Plain => "GL",
            RendererMode.Instanced => "IUBO",
            RendererMode.BindlessMDI => "BMDI",
            RendererMode.CommandList => "CMDL",
            _ => "???"
        };
    }
}

public class Settings {
    public bool vSync = false;
    public int guiScale = 4;
    public bool AO = true;
    public bool smoothLighting = false;
    public int renderDistance = 8;
    public float FOV = 75;
    public int mipmapping = 0;
    public int anisotropy = 0; // 0, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024
    public bool fxaaEnabled = false;
    public int msaaSamples = 1; // 1, 2, 4, 8, 16, 32
    public int ssaaScale = 1; // 1, 2 (2x2), 4 (4x4), 8 (8x8)
    public int ssaaMode = 0; // 0=Normal, 1=Weighted, 2=Per-sample
    public bool fullscreen = false;
    public bool smoothDayNight = false; // false = classic/stepped, true = dynamic/smooth
    public bool frustumCulling = true;
    public bool crtEffect = false;
    public bool reverseZ = true; // reverse-Z depth buffer for improved precision
    /**
     * Don't use this! Use getActualRendererMode() instead.
     */
    internal RendererMode rendererMode = RendererMode.Auto;

    public static readonly Settings instance = new();

    /// <summary>
    /// Whether to use framebuffer effects.
    /// </summary>
    public bool framebufferEffects => fxaaEnabled || msaaSamples > 1 || ssaaScale > 1 || getActualRendererMode() == RendererMode.CommandList || crtEffect || reverseZ;
    
    /// <summary>
    /// Whether FXAA is enabled.
    /// </summary>
    public bool fxaa => fxaaEnabled;
    
    /// <summary>
    /// SSAA multiplier (1, 2, or 4).
    /// </summary>
    public int ssaa {
        get {
            // per-sample mode doesn't use traditional SSAA scaling
            if (ssaaMode == 2) return 1;
            
            return ssaaScale;
        }
    }

    /// <summary>
    /// MSAA sample count (1, 2, 4, 8, 16, 32).
    /// </summary>
    public int msaa {
        get {
            // per-sample mode requires MSAA, force it on if not already enabled
            if (ssaaMode == 2) {
                return ssaaScale;
            }
            
            // validate against hardware support
            if (Game.supportedMSAASamples.Contains(msaaSamples)) {
                return msaaSamples;
            }
            
            // fallback1
            for (int i = Game.supportedMSAASamples.Length - 1; i >= 0; i--) {
                if (Game.supportedMSAASamples[i] <= msaaSamples) {
                    return (int)Game.supportedMSAASamples[i];
                }
            }
            // fallback2
            return 1; // fallback to no MSAA
        }
    }

    /// <summary>
    /// Effective framebuffer scale factor for rendering (1 for per-sample mode, ssaa for others)
    /// </summary>
    public int effectiveScale => ssaaMode == 2 ? 1 : ssaa;

    public string getAAText() {
        var parts = new List<string>();
        
        if (fxaaEnabled) parts.Add("FXAA");
        if (msaaSamples > 1) parts.Add($"{msaaSamples}x MSAA");
        if (ssaaScale > 1) parts.Add($"{ssaaScale}x SSAA");
        
        return parts.Count > 0 ? string.Join(" + ", parts) : "Off";
    }

    /**
     * Gets the actual renderer mode, falling back to supported alternatives if needed.
     */
    public RendererMode getActualRendererMode() {
        return rendererMode switch {
            RendererMode.Auto => Game.hasCMDL ? RendererMode.CommandList :
                                Game.hasBindlessMDI ? RendererMode.BindlessMDI :
                                Game.hasInstancedUBO ? RendererMode.Instanced :
                                RendererMode.Plain,
            RendererMode.CommandList => Game.hasCMDL ? RendererMode.CommandList :
                                       Game.hasBindlessMDI ? RendererMode.BindlessMDI :
                                       Game.hasInstancedUBO ? RendererMode.Instanced :
                                       RendererMode.Plain,
            RendererMode.BindlessMDI => Game.hasBindlessMDI ? RendererMode.BindlessMDI :
                                       Game.hasInstancedUBO ? RendererMode.Instanced :
                                       RendererMode.Plain,
            RendererMode.Instanced => Game.hasInstancedUBO ? RendererMode.Instanced : RendererMode.Plain,
            RendererMode.Plain => RendererMode.Plain,
            _ => RendererMode.Plain
        };
    }

    public void save() {
        var tag = new NBTCompound("");
        tag.addByte("vSync", (byte)(vSync ? 1 : 0));
        tag.addInt("guiScale", guiScale);
        tag.addByte("AO", (byte)(AO ? 1 : 0));
        tag.addByte("smoothLighting", (byte)(smoothLighting ? 1 : 0));
        tag.addInt("renderDistance", renderDistance);
        tag.addFloat("FOV", FOV);
        tag.addInt("mipmapping", mipmapping);
        tag.addInt("anisotropy", anisotropy);
        tag.addByte("fxaaEnabled", (byte)(fxaaEnabled ? 1 : 0));
        tag.addInt("msaaSamples", msaaSamples);
        tag.addInt("ssaaScale", ssaaScale);
        tag.addInt("ssaaMode", ssaaMode);
        tag.addByte("fullscreen", (byte)(fullscreen ? 1 : 0));
        tag.addByte("smoothDayNight", (byte)(smoothDayNight ? 1 : 0));
        tag.addByte("frustumCulling", (byte)(frustumCulling ? 1 : 0));
        tag.addByte("crtEffect", (byte)(crtEffect ? 1 : 0));
        tag.addByte("reverseZ", (byte)(reverseZ ? 1 : 0));
        tag.addInt("rendererMode", (int)rendererMode);
        
        SNBT.writeToFile(tag, "settings.snbt", true);
    }

    public void load() {
        try {
            if (!File.Exists("settings.snbt")) {
                return;
            }

            var tag = (NBTCompound)SNBT.readFromFile("settings.snbt");
            vSync = tag.getByte("vSync") != 0;
            guiScale = tag.getInt("guiScale");
            AO = tag.getByte("AO") != 0;
            smoothLighting = tag.getByte("smoothLighting") != 0;
            renderDistance = tag.getInt("renderDistance");
            FOV = tag.getFloat("FOV");
            mipmapping = tag.getInt("mipmapping");
            anisotropy = tag.getInt("anisotropy");
            fxaaEnabled = tag.getByte("fxaaEnabled") != 0;
            msaaSamples = tag.getInt("msaaSamples");
            ssaaScale = tag.getInt("ssaaScale");
            ssaaMode = tag.getInt("ssaaMode");
            fullscreen = tag.getByte("fullscreen") != 0;
            smoothDayNight = tag.getByte("smoothDayNight") != 0;
            frustumCulling = tag.getByte("frustumCulling") != 0;
            crtEffect = tag.getByte("crtEffect") != 0;
            reverseZ = tag.getByte("reverseZ") != 0;
            
            if (tag.has("rendererMode")) {
                rendererMode = (RendererMode)tag.getInt("rendererMode");
            }
        } catch (Exception e) {
            Log.warn("Failed to load settings", e);
            
            // todo load default settings if loading fails
        }
    }
}