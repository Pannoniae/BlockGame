using BlockGame.main;
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
    public float resolutionScale = 1.0f; // 0.25, 0.5, 0.75, 1.0
    public bool resolutionScaleLinear = true; // true = linear filtering, false = nearest
    public bool fullscreen = false;
    public bool smoothDayNight = false; // false = classic/stepped, true = dynamic/smooth
    public bool frustumCulling = true;
    public bool crtEffect = false;
    public bool reverseZ = true; // reverse-Z depth buffer for improved precision
    public bool affineMapping = false; // SO LIMINAL UWU
    public bool vertexJitter = false; // SO LIMINAL UWU
    public int cloudMode = 1; // 0=off, 1=simple, 2=fancy, 3=smooth fancy, 4=hypercube
    public bool opaqueWater = false;
    /**
     * Don't use this! Use getActualRendererMode() instead.
     */
    internal RendererMode rendererMode = RendererMode.Auto;

    public bool viewBobbing = true;
    public int mouseInv = 1; // 1 = normal, -1 = inverted

    public float sfxVolume = 1.0f; // 0.0 to 1.0
    public float musicVolume = 1.0f; // 0.0 to 1.0

    public string playerName;
    public string skinPath = "character.png";
    public string texturePack = "vanilla";

    public static readonly Settings instance = new();

    /// <summary>
    /// Whether to use framebuffer effects.
    /// </summary>
    public bool framebufferEffects => fxaaEnabled || msaaSamples > 1 || ssaaScale > 1 || resolutionScale != 1.0f || getActualRendererMode() == RendererMode.CommandList || crtEffect || reverseZ;
    
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

    public Settings() {
        playerName = "TestSubject" + Game.clientRandom.Next(0, 9999);
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
        tag.addFloat("resolutionScale", resolutionScale);
        tag.addByte("resolutionScaleLinear", (byte)(resolutionScaleLinear ? 1 : 0));
        tag.addByte("fullscreen", (byte)(fullscreen ? 1 : 0));
        tag.addByte("smoothDayNight", (byte)(smoothDayNight ? 1 : 0));
        tag.addByte("frustumCulling", (byte)(frustumCulling ? 1 : 0));
        tag.addByte("crtEffect", (byte)(crtEffect ? 1 : 0));
        tag.addByte("reverseZ", (byte)(reverseZ ? 1 : 0));
        tag.addByte("affineMapping", (byte)(affineMapping ? 1 : 0));
        tag.addByte("vertexJitter", (byte)(vertexJitter ? 1 : 0));
        tag.addInt("cloudMode", cloudMode);
        tag.addByte("viewBobbing", (byte)(viewBobbing ? 1 : 0));
        tag.addByte("opaqueWater", (byte)(opaqueWater ? 1 : 0));

        tag.addInt("rendererMode", (int)rendererMode);
        tag.addInt("mouseInv", mouseInv);
        tag.addFloat("sfxVolume", sfxVolume);
        tag.addFloat("musicVolume", musicVolume);
        tag.addString("playerName", playerName);
        tag.addString("skinPath", skinPath);
        tag.addString("texturePack", texturePack);

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
            if (tag.has("resolutionScale")) {
                resolutionScale = tag.getFloat("resolutionScale");
            }
            if (tag.has("resolutionScaleLinear")) {
                resolutionScaleLinear = tag.getByte("resolutionScaleLinear") != 0;
            }
            fullscreen = tag.getByte("fullscreen") != 0;
            smoothDayNight = tag.getByte("smoothDayNight") != 0;
            frustumCulling = tag.getByte("frustumCulling") != 0;
            crtEffect = tag.getByte("crtEffect") != 0;
            reverseZ = tag.getByte("reverseZ") != 0;
            if (tag.has("affineMapping")) {
                affineMapping = tag.getByte("affineMapping") != 0;
            }
            if (tag.has("vertexJitter")) {
                vertexJitter = tag.getByte("vertexJitter") != 0;
            }
            if (tag.has("cloudMode")) {
                cloudMode = tag.getInt("cloudMode");
            }
            if (tag.has("viewBobbing")) {
                viewBobbing = tag.getByte("viewBobbing") != 0;
            }

            // if first load and igpu, true
            opaqueWater = tag.getByte("opaqueWater", 0) != 0 || (!tag.has("opaqueWater") && Game.isIntegratedCard);

            if (tag.has("rendererMode")) {
                rendererMode = (RendererMode)tag.getInt("rendererMode");
            }
            if (tag.has("mouseInv")) {
                mouseInv = tag.getInt("mouseInv");
            }
            if (tag.has("sfxVolume")) {
                sfxVolume = tag.getFloat("sfxVolume");
            }
            if (tag.has("musicVolume")) {
                musicVolume = tag.getFloat("musicVolume");
            }
            if (tag.has("playerName")) {
                playerName = tag.getString("playerName");
            }
            if (tag.has("skinPath")) {
                skinPath = tag.getString("skinPath");
            }
            if (tag.has("texturePack")) {
                texturePack = tag.getString("texturePack");
            }
        } catch (Exception e) {
            Log.warn("Failed to load settings", e);

            // todo load default settings if loading fails
        }
    }
}