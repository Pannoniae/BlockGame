using System.Diagnostics.CodeAnalysis;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

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

        var mipmapping = new Slider(this, "mipmapping", 0, 4, 1, settings.mipmapping);
        mipmapping.setPosition(new Rectangle(0, 112, 128, 16));
        mipmapping.topCentre();
        mipmapping.applied += () => {
            settings.mipmapping = (int)mipmapping.value;
            Game.textures.blockTexture.reload();
        };
        mipmapping.getText = value => value == 0 ? "Mipmapping: Off" : $"Mipmapping: {value}x";
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

        var renderDistance = new Slider(this, "renderDistance", 2, 96, 1, settings.renderDistance);
        renderDistance.setPosition(new Rectangle(0, 112, 128, 16));
        renderDistance.topCentre();
        renderDistance.tooltip =
            "The maximum distance at which blocks are rendered.\nHigher values may reduce performance.";
        renderDistance.applied += () => {
            var old = settings.renderDistance;
            settings.renderDistance = (int)renderDistance.value;
            remeshIfRequired(old);
        };
        renderDistance.getText = value => "Render Distance: " + value;
        settingElements.Add(renderDistance);
        addElement(renderDistance);

        var FOV = new FOVSlider(this, "FOV", 50, 150, 1, (int)settings.FOV);
        FOV.setPosition(new Rectangle(0, 112, 128, 16));
        FOV.topCentre();
        FOV.applied += () => { settings.FOV = (int)FOV.value; };
        FOV.getText = [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")](value) => {
            if (value == 75)
                return "Field of View: Normal";
            if (value == 50)
                return "Field of View: Fish Eye";
            if (value == 120)
                return "Field of View: Quake Pro";
            if (value == 150)
                return "Field of View: Tunnel Vision";
            return "Field of View: " + value;
        };
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
            // Depth state will be updated on next frame
        };
        reverseZ.tooltip =
            "Reverse-Z depth buffer provides much better depth precision.\nReduces Z-fighting and allows infinite view distances.\nRequires graphics restart to take effect.";
        settingElements.Add(reverseZ);
        addElement(reverseZ);

        var rendererMode = new ToggleButton(this, "rendererMode", false, (int)settings.rendererMode,
            "Renderer: Auto", "Renderer: Plain", "Renderer: Instanced", "Renderer: Bindless MDI", "Renderer: Command List");
        rendererMode.topCentre();
        rendererMode.clicked += _ => {
            var old = settings.rendererMode;
            var newm = (RendererMode)rendererMode.getIndex();
            Game.renderer?.reloadRenderer(old, newm);
            Game.instance.updateFramebuffers();
            // REMESH THE ENTIRE WORLD
            Screen.GAME_SCREEN.remeshWorld(Settings.instance.renderDistance);
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
            parentScreen.returnToPrevScreen();
        };
        addElement(back);

        layoutSettingsTwoCols(settingElements, new Vector2I(0, 16), vsync.GUIbounds.Width);
    }

    private void remeshIfRequired(int oldRenderDist) {
        if (Game.instance.currentScreen == Screen.GAME_SCREEN) {
            Screen.GAME_SCREEN.remeshWorld(oldRenderDist);
        }
    }

    public override void deactivate() {
        base.deactivate();
        Settings.instance.save();
    }

    public override void clear(double dt, double interp) {
        Game.graphics.clearColor(Color4b.SlateGray);
        Game.graphics.clearDepth();
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