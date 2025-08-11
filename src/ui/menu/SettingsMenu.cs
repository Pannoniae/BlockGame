using System.Diagnostics.CodeAnalysis;
using BlockGame.GL.vertexformats;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL;

using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

public class SettingsMenu : Menu {

    public Menu prevMenu;

    public SettingsMenu() {
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
        vsync.tooltip = "Turns on vertical synchronisation.";
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
        smoothLighting.tooltip = "Smooth Lighting improves the game's look by smoothing the lighting between Block.";
        settingElements.Add(smoothLighting);
        addElement(smoothLighting);

        var dayNightCycle = new ToggleButton(this, "dayNightCycle", false, settings.smoothDayNight ? 1 : 0,
            "Daylight Cycle: Classic", "Daylight Cycle: Dynamic");
        dayNightCycle.topCentre();
        dayNightCycle.clicked += _ => {
            settings.smoothDayNight = dayNightCycle.getIndex() == 1;
        };
        dayNightCycle.tooltip = "Controls how lighting changes throughout the day.\nClassic: sharp light level transitions like retro games.\nDynamic: smooth light level changes.";
        settingElements.Add(dayNightCycle);
        addElement(dayNightCycle);

        var mipmapping = new Slider(this, "mipmapping", 0, 4, 1, settings.mipmapping);
        mipmapping.setPosition(new Rectangle(0, 112, 128, 16));
        mipmapping.topCentre();
        mipmapping.applied += () => {
            settings.mipmapping = (int)mipmapping.value;
            Game.textureManager.blockTexture.bind();
            Game.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, settings.mipmapping != 0 ? (int)GLEnum.NearestMipmapLinear : (int)GLEnum.Nearest);
            Game.textureManager.blockTexture.reload();
        };
        mipmapping.getText = value => value == 0 ? "Mipmapping: Off" : $"Mipmapping: {value}x";
        mipmapping.tooltip = "Mipmapping reduces the flickering of textures at a distance.";
        settingElements.Add(mipmapping);
        addElement(mipmapping);

        var anisotropy = new ToggleButton(this, "anisotropy", false, 
            settings.anisotropy switch { 0 => 0, 1 => 1, 2 => 2, 4 => 3, 8 => 4, 16 => 5, 32 => 6, 64 => 7, 128 => 8, _ => 3 },
            "Anisotropic Filtering: OFF", "Anisotropic Filtering: 1x", "Anisotropic Filtering: 2x", "Anisotropic Filtering: 4x", 
            "Anisotropic Filtering: 8x", "Anisotropic Filtering: 16x", "Anisotropic Filtering: 32x", "Anisotropic Filtering: 64x");
        anisotropy.topCentre();
        anisotropy.clicked += _ => {
            settings.anisotropy = anisotropy.getIndex() switch { 0 => 0, 1 => 1, 2 => 2, 3 => 4, 4 => 8, 5 => 16, 6 => 32, 7 => 64, 8 => 128, _ => 8 };
            Game.textureManager.blockTexture.reload();
            Game.renderer?.updateAF();
        };
        anisotropy.tooltip = "Anisotropic filtering improves texture quality at oblique angles.\nHigher values provide better quality but may impact performance.\nValues above 16x are practically unnoticeable.";
        settingElements.Add(anisotropy);
        addElement(anisotropy);
        
        var antiAliasing = new ToggleButton(this, "antiAliasing", false, settings.antiAliasing,
            "Anti-Aliasing: Off", "Anti-Aliasing: FXAA", "Anti-Aliasing: 2x MSAA", "Anti-Aliasing: 4x MSAA", 
            "Anti-Aliasing: 2x SSAA", "Anti-Aliasing: 4x SSAA", "Anti-Aliasing: 2x MSAA + 2x SSAA", 
            "Anti-Aliasing: 4x MSAA + 2x SSAA", "Anti-Aliasing: 4x MSAA + 4x SSAA");
        antiAliasing.topCentre();
        antiAliasing.clicked += _ => {
            var index = antiAliasing.getIndex();
            
            settings.antiAliasing = index;
            Game.instance.updateFramebuffers();
        };
        antiAliasing.tooltip = "Anti-Aliasing techniques smooth jagged edges.\nFXAA is fast, MSAA provides good quality with moderate performance impact,\nSSAA provides best quality but impacts performance significantly.\nIt will kill your RTX 5090, I warned you!";
        settingElements.Add(antiAliasing);
        addElement(antiAliasing);

        var ssaaModeOptions = new List<string> { "SSAA Mode: Normal", "SSAA Mode: Weighted" };
        var ssaaModeTooltip = "SSAA sampling mode.\nNormal: uniform sampling\nWeighted: center-biased sampling for less blur";
        
        // Add per-sample option if supported
        if (Game.sampleShadingSupported) {
            ssaaModeOptions.Add("SSAA Mode: Per-Sample");
            ssaaModeTooltip += "\nPer-Sample: hardware-accelerated per-sample shading";
        } else {
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
        renderDistance.tooltip = "The maximum distance at which blocks are rendered.\nHigher values may reduce performance.";
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
        frustumCulling.clicked += _ => {
            settings.frustumCulling = frustumCulling.getIndex() == 1;
        };
        frustumCulling.tooltip = "Frustum Culling skips rendering blocks outside the camera's view.\nThis can improve performance in large worlds.";
        settingElements.Add(frustumCulling);
        addElement(frustumCulling);
        

        var back = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.setPosition(new Vector2I(2, -18));
        back.clicked += returnToPrevMenu;
        addElement(back);

        layoutSettingsTwoCols(settingElements, new Vector2I(0, 16), vsync.GUIbounds.Width);
    }

    private void remeshIfRequired(int oldRenderDist) {
        if (Game.instance.currentScreen == Screen.GAME_SCREEN) {
            Screen.GAME_SCREEN.remeshWorld(oldRenderDist);
        }
    }

    public void layoutSettingsTwoCols(List<GUIElement> elements, Vector2I startPos, int buttonWidth) {
        // to the left/right
        var offset = buttonWidth / 2 + 8;
        var pos = startPos;
        for (int i = 0; i < elements.Count; i++) {
            var element = elements[i];
            int o;
            if (i % 2 == 0) {
                o = -offset;
            }
            else {
                o = offset;
            }
            element.setPosition(new Rectangle(pos.X + o, pos.Y, element.GUIbounds.Width, element.GUIbounds.Height));
            if (i % 2 == 1) {
                pos.Y += 18;
            }
        }
    }

    public void layoutSettings(List<GUIElement> elements, Vector2I startPos) {
        var pos = startPos;
        foreach (var element in elements) {
            element.setPosition(new Rectangle(pos.X, pos.Y, element.GUIbounds.Width, element.GUIbounds.Height));
            pos.Y += 18;
        }
    }

    public override void deactivate() {
        base.deactivate();
        // save settings too
    }

    public override void clear(double dt, double interp) {
        Game.graphics.clearColor(Color4b.SlateGray);
        Game.GL.ClearDepth(1f);
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            returnToPrevMenu(null);
        }
    }

    public void returnToPrevMenu(GUIElement guiElement) {
        Game.instance.executeOnMainThread(() => Game.instance.switchTo(prevMenu));
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }
}