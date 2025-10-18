using System.Diagnostics.CodeAnalysis;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class VideoSettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public VideoSettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeSettings();
    }

    private void initializeSettings() {
        // load settings (later)
        var settings = Settings.instance;

        var settingElements = new List<GUIElement>();

        var vsync = new ToggleButton(this, "vsync", false, settings.vSync ? 1 : 0,
            "VSync: OFF", "VSync: ON");
        vsync.topCentre();
        vsync.clicked += _ => {
            settings.vSync = vsync.getIndex() == 1;
            Game.window.VSync = settings.vSync;
        };
        vsync.tooltip = "VSync locks your framerate to your monitor's refresh rate to prevent screen tearing.";
        settingElements.Add(vsync);
        addElement(vsync);

        var fullscreen = new ToggleButton(this, "fullscreen", false, settings.fullscreen ? 1 : 0,
            "Fullscreen: OFF", "Fullscreen: ON");
        fullscreen.topCentre();
        fullscreen.clicked += _ => {
            settings.fullscreen = fullscreen.getIndex() == 1;
            Game.instance.setFullscreen(settings.fullscreen);
        };
        fullscreen.tooltip = "Toggles fullscreen mode.";
        settingElements.Add(fullscreen);
        addElement(fullscreen);

        var guiScale = new ToggleButton(this, "guiScale", false, settings.guiScale == 4 ? 1 : 0,
            "GUI Scale: Small", "GUI Scale: Large");
        guiScale.topCentre();
        guiScale.clicked += _ => {
            settings.guiScale = guiScale.getIndex() == 1 ? 4 : 2;
            GUI.guiScale = settings.guiScale;
            Game.instance.resize();
        };
        settingElements.Add(guiScale);
        addElement(guiScale);

        var AO = new ToggleButton(this, "ao", false, settings.AO ? 1 : 0,
            "Ambient Occlusion: Disabled", "Ambient Occlusion: Enabled");
        AO.topCentre();
        AO.clicked += _ => {
            settings.AO = AO.getIndex() == 1;
            remeshIfRequired(settings.renderDistance);
        };
        AO.tooltip = "Ambient Occlusion makes block corners darker to simulate shadows.";
        settingElements.Add(AO);
        addElement(AO);

        var smoothLighting = new ToggleButton(this, "smoothLighting", false, settings.smoothLighting ? 1 : 0,
            "Smooth Lighting: Disabled", "Smooth Lighting: Enabled");
        smoothLighting.topCentre();
        smoothLighting.clicked += _ => {
            settings.smoothLighting = smoothLighting.getIndex() == 1;
            remeshIfRequired(settings.renderDistance);
        };
        smoothLighting.tooltip = "Smooth Lighting improves the game's look by smoothing the lighting between blocks.";
        settingElements.Add(smoothLighting);
        addElement(smoothLighting);

        var dayNightCycle = new ToggleButton(this, "dayNightCycle", false, settings.smoothDayNight ? 1 : 0,
            "Daylight Cycle: Classic", "Daylight Cycle: Dynamic");
        dayNightCycle.topCentre();
        dayNightCycle.clicked += _ => { settings.smoothDayNight = dayNightCycle.getIndex() == 1; };
        dayNightCycle.tooltip =
            "Controls how lighting changes throughout the day.\nClassic: sharp light level transitions like retro games.\nDynamic: smooth light level changes.";
        settingElements.Add(dayNightCycle);
        addElement(dayNightCycle);

        Func<float, string> getText = value => value == 0 ? "Mipmapping: Off" : $"Mipmapping: {value}x";
        var mipmapping = new Slider(this, "mipmapping", 0, 4, 1, settings.mipmapping, getText);
        mipmapping.setPosition(new Rectangle(0, 112, 128, 16));
        mipmapping.topCentre();
        mipmapping.applied += () => {
            settings.mipmapping = (int)mipmapping.value;
            Game.textures.blockTexture.reload();
        };
        mipmapping.tooltip = "Mipmapping reduces the flickering of textures at a distance.";
        settingElements.Add(mipmapping);
        addElement(mipmapping);

        var anisotropy = new ToggleButton(this, "anisotropy", false,
            settings.anisotropy switch {
                0 => 0, 1 => 1, 2 => 2, 4 => 3, 8 => 4, 16 => 5, 32 => 6, 64 => 7, 128 => 8, _ => 3
            },
            "Anisotropic Filtering: OFF", "Anisotropic Filtering: 1x", "Anisotropic Filtering: 2x",
            "Anisotropic Filtering: 4x",
            "Anisotropic Filtering: 8x", "Anisotropic Filtering: 16x", "Anisotropic Filtering: 32x",
            "Anisotropic Filtering: 64x");
        anisotropy.topCentre();
        anisotropy.clicked += _ => {
            settings.anisotropy = anisotropy.getIndex() switch {
                0 => 0, 1 => 1, 2 => 2, 3 => 4, 4 => 8, 5 => 16, 6 => 32, 7 => 64, 8 => 128, _ => 8
            };
            Game.textures.blockTexture.reload();
            Game.renderer?.updateAF();
        };
        anisotropy.tooltip =
            "Anisotropic filtering improves texture quality at oblique angles.\nHigher values provide better quality but may impact performance.\nAlso helps to reduce aliasing on transparent objects like foliage.\nValues above 16x are practically unnoticeable.";
        settingElements.Add(anisotropy);
        addElement(anisotropy);

        var fxaa = new ToggleButton(this, "fxaa", false, settings.fxaaEnabled ? 1 : 0,
            "FXAA: OFF", "FXAA: ON");
        fxaa.topCentre();
        fxaa.clicked += _ => {
            settings.fxaaEnabled = fxaa.getIndex() == 1;
            Game.instance.updateFramebuffers();
        };
        fxaa.tooltip = "Fast Approximate Anti-Aliasing smooths jagged edges with minimal performance impact.";
        settingElements.Add(fxaa);
        addElement(fxaa);

        // build MSAA options based on hardware support
        var msaaOptions = new List<string> { "MSAA: OFF" };
        var msaaSampleValues = new List<int> { 1 };

        foreach (var sample in Game.supportedMSAASamples) { // skip 1 (OFF)
            msaaOptions.Add($"MSAA: {sample}x");
            msaaSampleValues.Add((int)sample);
        }

        var currentMsaaIndex = msaaSampleValues.IndexOf(settings.msaaSamples);
        if (currentMsaaIndex == -1) {
            currentMsaaIndex = 0; // fallback to OFF
        }

        var msaa = new ToggleButton(this, "msaa", false, currentMsaaIndex, msaaOptions.ToArray());
        msaa.topCentre();
        msaa.clicked += _ => {
            var index = msaa.getIndex();
            settings.msaaSamples = index < msaaSampleValues.Count ? msaaSampleValues[index] : 1;
            Game.instance.updateFramebuffers();
        };
        msaa.tooltip = "Multi-Sample Anti-Aliasing uses hardware multisampling to reduce aliasing and jaggies.\nThe options shown are hardware-validated for your GPU.";
        settingElements.Add(msaa);
        addElement(msaa);

        var ssaa = new ToggleButton(this, "ssaa", false,
            settings.ssaaScale switch { 1 => 0, 2 => 1, 4 => 2, 8 => 3, _ => 0 },
            "SSAA: OFF", "SSAA: 2x", "SSAA: 4x", "SSAA: 8x");
        ssaa.topCentre();
        ssaa.clicked += _ => {
            settings.ssaaScale = ssaa.getIndex() switch {
                0 => 1, 1 => 2, 2 => 4, 3 => 8, _ => 1
            };
            Game.instance.updateFramebuffers();
        };
        ssaa.tooltip = "Super-Sample Anti-Aliasing renders the game at a higher resolution then downscales.\nProvides excellent quality but severely impacts performance.\nThis option stacked with MSAA is deadly.";
        settingElements.Add(ssaa);
        addElement(ssaa);

        var ssaaModeOptions = new List<string> { "SSAA Mode: Normal", "SSAA Mode: Weighted" };
        var ssaaModeTooltip =
            "SSAA sampling mode.\nNormal: uniform sampling\nWeighted: center-biased sampling for less blur";

        // Add per-sample option if supported
        if (Game.sampleShadingSupported) {
            ssaaModeOptions.Add("SSAA Mode: Per-Sample");
            ssaaModeTooltip += "\nPer-Sample: hardware-accelerated per-sample shading";
        }
        else {
            // clamp setting if per-sample was selected but not supported
            if (settings.ssaaMode >= 2) settings.ssaaMode = 0;
        }

        var ssaaMode = new ToggleButton(this, "ssaaMode", false, settings.ssaaMode, ssaaModeOptions.ToArray());
        ssaaMode.topCentre();
        ssaaMode.clicked += _ => {
            settings.ssaaMode = ssaaMode.getIndex();
            Game.instance.updateFramebuffers();
        };
        ssaaMode.tooltip = ssaaModeTooltip;
        settingElements.Add(ssaaMode);
        addElement(ssaaMode);

        var resScale = new ToggleButton(this, "resScale", false,
            settings.resolutionScale switch {
                0.25f => 0, 0.5f => 1, 0.75f => 2, _ => 3
            },
            "Resolution: 25%", "Resolution: 50%", "Resolution: 75%", "Resolution: 100%");
        resScale.topCentre();
        resScale.clicked += _ => {
            settings.resolutionScale = resScale.getIndex() switch {
                0 => 0.25f, 1 => 0.5f, 2 => 0.75f, _ => 1.0f
            };
            Game.instance.updateFramebuffers();
        };
        resScale.tooltip = "Renders the game at a lower internal resolution then upscales to window size.\nReduces GPU load for better performance on weaker PCs.";
        settingElements.Add(resScale);
        addElement(resScale);

        var resScaleFilter = new ToggleButton(this, "resScaleFilter", false, settings.resolutionScaleLinear ? 1 : 0,
            "Upscale Filter: Nearest", "Upscale Filter: Linear");
        resScaleFilter.topCentre();
        resScaleFilter.clicked += _ => {
            settings.resolutionScaleLinear = resScaleFilter.getIndex() == 1;
            Game.instance.updateFramebuffers();
        };
        resScaleFilter.tooltip = "Texture filtering for resolution scaling.\nNearest: pixelated/sharp upscaling\nLinear: smooth upscaling";
        settingElements.Add(resScaleFilter);
        addElement(resScaleFilter);


        getText = value => "Render Distance: " + value;
        var renderDistance = new Slider(this, "renderDistance", 2, 96, 1, settings.renderDistance, getText);
        renderDistance.setPosition(new Rectangle(0, 112, 128, 16));
        renderDistance.topCentre();
        renderDistance.tooltip =
            "The maximum distance at which blocks are rendered.\nHigher values may reduce performance.";
        renderDistance.applied += () => {
            var old = settings.renderDistance;
            settings.renderDistance = (int)renderDistance.value;
            remeshIfRequired(old);
        };
        settingElements.Add(renderDistance);
        addElement(renderDistance);


        getText = [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")](value) => {
            return value switch {
                75 => "Field of View: Normal",
                50 => "Field of View: Fish Eye",
                120 => "Field of View: Quake Pro",
                150 => "Field of View: Tunnel Vision",
                _ => "Field of View: " + value
            };
        };
        var FOV = new FOVSlider(this, "FOV", 50, 150, 1, (int)settings.FOV, getText);
        FOV.setPosition(new Rectangle(0, 112, 128, 16));
        FOV.topCentre();
        FOV.applied += () => { settings.FOV = (int)FOV.value; };
        settingElements.Add(FOV);
        addElement(FOV);

        var frustumCulling = new ToggleButton(this, "frustumCulling", false, settings.frustumCulling ? 1 : 0,
            "Frustum Culling: OFF", "Frustum Culling: ON");
        frustumCulling.topCentre();
        frustumCulling.clicked += _ => { settings.frustumCulling = frustumCulling.getIndex() == 1; };
        frustumCulling.tooltip =
            "Frustum Culling skips rendering blocks outside the camera's view.\nThis usually improves performance. Consider turning it off if blocks are invisible.";
        settingElements.Add(frustumCulling);
        addElement(frustumCulling);

        var crtEffect = new ToggleButton(this, "crtEffect", false, settings.crtEffect ? 1 : 0,
            "CRT Effect: OFF", "CRT Effect: ON");
        crtEffect.topCentre();
        crtEffect.clicked += _ => {
            settings.crtEffect = crtEffect.getIndex() == 1;
            Game.instance.updateFramebuffers();
        };
        crtEffect.tooltip =
            "CRT Effect adds a retro CRT monitor effect with scanlines.\nProvides an authentic vintage computing experience or something.";
        settingElements.Add(crtEffect);
        addElement(crtEffect);

        var reverseZ = new ToggleButton(this, "reverseZ", false, settings.reverseZ ? 1 : 0,
            "Reverse-Z: OFF", "Reverse-Z: ON");
        reverseZ.topCentre();
        reverseZ.clicked += _ => {
            settings.reverseZ = reverseZ.getIndex() == 1;
            Game.instance.updateFramebuffers();
            // adjust polygon offset to match new depth range
            Game.graphics.polyOffset(-2f, -3f);
            Game.graphics.setupDepthTesting();
            Game.gui.refreshMatrix(new Vector2I(Game.width, Game.height));
            // Depth state will be updated on next frame
        };
        reverseZ.tooltip =
            "Reverse-Z depth buffer provides much better depth precision.\nReduces Z-fighting and allows infinite view distances.\nRequires graphics restart to take effect.";
        settingElements.Add(reverseZ);
        addElement(reverseZ);

        // fuck it it's too liminal
        var affineMapping = new ToggleButton(this, "affineMapping", false, settings.affineMapping ? 1 : 0,
            "Affine Mapping: OFF", "Affine Mapping: ON");
        affineMapping.topCentre();
        affineMapping.clicked += _ => { settings.affineMapping = affineMapping.getIndex() == 1; };
        affineMapping.tooltip =
            "PS1-style affine texture mapping (no perspective correction).\nVERY LIMINAL.";
        settingElements.Add(affineMapping);
        addElement(affineMapping);

        var vertexJitter = new ToggleButton(this, "vertexJitter", false, settings.vertexJitter ? 1 : 0,
            "Vertex Jitter: OFF", "Vertex Jitter: ON");
        vertexJitter.topCentre();
        vertexJitter.clicked += _ => { settings.vertexJitter = vertexJitter.getIndex() == 1; };
        vertexJitter.tooltip =
            "PS1-style vertex snapping/wobble effect.\nMUCH NOSTALGIA.";
        settingElements.Add(vertexJitter);
        addElement(vertexJitter);

        var clouds = new ToggleButton(this, "clouds", false, settings.cloudMode,
            "Clouds: OFF", "Clouds: Simple", "Clouds: Fancy", "Clouds: Hypercube");
        clouds.topCentre();
        clouds.clicked += _ => { settings.cloudMode = clouds.getIndex(); };
        clouds.tooltip =
            "Cloud rendering mode.\nOff: No clouds\nSimple: Flat clouds\nFancy: 3D clouds\nHypercube: 4D clouds (SLOW!)";
        settingElements.Add(clouds);
        addElement(clouds);

        // build renderer options based on hardware support
        var rendererOptions = new List<string> { "Renderer: Auto", "Renderer: Plain" };
        var rendererModeValues = new List<RendererMode> { RendererMode.Auto, RendererMode.Plain };

        if (Game.hasInstancedUBO) {
            rendererOptions.Add("Renderer: Instanced");
            rendererModeValues.Add(RendererMode.Instanced);
        }
        if (Game.hasBindlessMDI) {
            rendererOptions.Add("Renderer: Bindless MDI");
            rendererModeValues.Add(RendererMode.BindlessMDI);
        }
        if (Game.hasCMDL) {
            rendererOptions.Add("Renderer: Command List");
            rendererModeValues.Add(RendererMode.CommandList);
        }

        var currentRendererIndex = rendererModeValues.IndexOf(settings.rendererMode);
        if (currentRendererIndex == -1) {
            currentRendererIndex = 0; // fallback to Auto
        }

        var rendererMode = new ToggleButton(this, "rendererMode", false, currentRendererIndex, rendererOptions.ToArray());
        rendererMode.topCentre();
        rendererMode.clicked += _ => {
            var old = settings.rendererMode;
            var index = rendererMode.getIndex();
            var newm = index < rendererModeValues.Count ? rendererModeValues[index] : RendererMode.Auto;
            Game.renderer?.reloadRenderer(old, newm);
            Game.instance.updateFramebuffers();
            // REMESH THE ENTIRE WORLD
            if (Game.world != null) {
                Screen.GAME_SCREEN.remeshWorld(Settings.instance.renderDistance);
            }
        };
        rendererMode.tooltip = "World rendering backend:\n" +
            "Auto: The best one available for your hardware\n" +
            "Command List: NVIDIA NV_command_list (fastest on RTX)\n" +
            "Bindless MDI: NVIDIA Multi-draw indirect\n" +
            "Instanced: Instanced rendering /w uniform buffers\n" +
            "Plain: Straightforward renderer (maximum compatibility)";
        settingElements.Add(rendererMode);
        addElement(rendererMode);

        // Windows Defender exclusion (Windows only)
        if (OperatingSystem.IsWindows()) {
            var defenderExclusion = new Button(this, "defenderExclusion", false, "Add Defender Exclusion");
            defenderExclusion.topCentre();
            defenderExclusion.clicked += _ => {
                Game.addDefenderExclusion();
                // refresh button text after adding
                defenderExclusion.text = "Defender Exclusion: Added";
            };
            defenderExclusion.tooltip =
                "Adds this folder to Windows Defender exclusions to improve file I/O performance.\nRequires administrator privileges (UAC prompt will appear).";
            settingElements.Add(defenderExclusion);
            addElement(defenderExclusion);
        }


        var back = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.setPosition(new Vector2I(2, -18));
        back.clicked += _ => {
            deactivate();
            parentScreen.returnToPrevScreen();
        };
        addElement(back);

        layoutSettingsTwoCols(settingElements, new Vector2I(0, 16), vsync.GUIbounds.Width);
    }

    private void remeshIfRequired(int oldRenderDist) {
        // only remesh if we're in the game, NOT on the main menu
        // we have a dedicated screen now so can't just check the screen
        // quick hack!
        if (Game.world != null && Game.renderer != null) {
            Screen.GAME_SCREEN.remeshWorld(oldRenderDist);
        }
    }

    public override void deactivate() {
        base.deactivate();
        Settings.instance.save();
    }

    public override void clear(double dt, double interp) {
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            parentScreen.returnToPrevScreen();
        }
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }
}