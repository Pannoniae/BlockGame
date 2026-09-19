using System.Diagnostics.CodeAnalysis;
using BlockGame.main;
using BlockGame.render;
using BlockGame.render.texpack;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using BlockGame.world.block;
using BlockGame.world.chunk;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class GraphicsSettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public GraphicsSettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeSettings();
    }

    private void initializeSettings() {
        var settings = Settings.instance;
        var settingElements = new List<GUIElement>();



        Func<float, string> getText = value => value == 0 ? "Mipmapping: Off" : $"Mipmapping: {value}x";
        var mipmapping = new Slider(this, "mipmapping", 0, 4, 1, settings.mipmapping, getText);
        mipmapping.setPosition(new Rectangle(0, 112, 128, 16));
        mipmapping.centreContents();
        mipmapping.applied += () => {
            settings.mipmapping = (int)mipmapping.value;
            Game.textures.reloadAtlases();
        };
        mipmapping.tooltip = "Mipmapping reduces the flickering of textures at a distance.";
        settingElements.Add(mipmapping);
        addElement(mipmapping);

        var anisotropy = new ToggleButton(this, "anisotropy", false,
            settings.anisotropy switch {
                0 => 0, 2 => 1, 4 => 2, 8 => 3, 16 => 4, 32 => 5, _ => 3
            },
            "Anisotropic Filtering: OFF", "Anisotropic Filtering: 2x", "Anisotropic Filtering: 4x",
            "Anisotropic Filtering: 8x", "Anisotropic Filtering: 16x", "Anisotropic Filtering: 32x");
        anisotropy.centreContents();
        anisotropy.clicked += _ => {
            settings.anisotropy = anisotropy.getIndex() switch {
                0 => 0, 1 => 2, 2 => 4, 3 => 8, 4 => 16, 5 => 32, _ => 8
            };
            Game.textures.reloadAtlases();
            Game.renderer?.updateAF();
        };
        anisotropy.tooltip =
            "Anisotropic filtering improves texture quality at oblique angles.\nHigher values provide better quality but may impact performance.\nAlso helps to reduce aliasing on transparent objects like foliage.\nValues above 16x are practically unnoticeable.";
        settingElements.Add(anisotropy);
        addElement(anisotropy);

        var clouds = new ToggleButton(this, "clouds", false, settings.cloudMode,
            "Clouds: OFF", "Clouds: Simple", "Clouds: Fancy", "Clouds: Serene", "Clouds: Hypercube");
        clouds.centreContents();
        clouds.clicked += _ => { settings.cloudMode = clouds.getIndex(); };
        clouds.tooltip =
            "Cloud rendering mode.\nOff: No clouds\nSimple: 2D clouds\nFancy: 3D clouds\nSerene: Tapered 3D clouds\nHypercube: 4D clouds (SLOW!)";
        settingElements.Add(clouds);
        addElement(clouds);

        var dayNightCycle = new ToggleButton(this, "dayNightCycle", false, settings.smoothDayNight ? 1 : 0,
            "Daylight Cycle: Classic", "Daylight Cycle: Dynamic");
        dayNightCycle.centreContents();
        dayNightCycle.clicked += _ => { settings.smoothDayNight = dayNightCycle.getIndex() == 1; };
        dayNightCycle.tooltip =
            "Controls how lighting changes throughout the day.\nClassic: sharp light level transitions like retro games.\nDynamic: smooth light level changes.";
        settingElements.Add(dayNightCycle);
        addElement(dayNightCycle);

        var reverseZ = new ToggleButton(this, "reverseZ", false, settings.reverseZ ? 1 : 0,
            "Reverse-Z: OFF", "Reverse-Z: ON");
        reverseZ.centreContents();
        reverseZ.clicked += _ => {
            settings.reverseZ = reverseZ.getIndex() == 1;
            Game.instance.updateFramebuffers();
            // adjust polygon offset to match new depth range
            Game.graphics.polyOffset(-2f, -3f);
            Game.graphics.setupDepthTesting();
            Game.gui.refreshMatrix(new Vector2I(Game.width, Game.height));
            // depth state will be updated on next frame
        };
        reverseZ.tooltip =
            "Reverse-Z depth buffer provides much better depth precision.\nReduces Z-fighting and allows infinite view distances.\nRequires graphics restart to take effect.";
        settingElements.Add(reverseZ);
        addElement(reverseZ);

        var fxaa = new ToggleButton(this, "fxaa", false, settings.fxaaEnabled ? 1 : 0,
            "FXAA: OFF", "FXAA: ON");
        fxaa.centreContents();
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

        foreach (var sample in Game.supportedMSAASamples) {
            msaaOptions.Add($"MSAA: {sample}x");
            msaaSampleValues.Add((int)sample);
        }

        var currentMsaaIndex = msaaSampleValues.IndexOf(settings.msaaSamples);
        if (currentMsaaIndex == -1) {
            currentMsaaIndex = 0; // fallback to OFF
        }

        var msaa = new ToggleButton(this, "msaa", false, currentMsaaIndex, msaaOptions.ToArray());
        msaa.centreContents();
        msaa.clicked += _ => {
            var index = msaa.getIndex();
            settings.msaaSamples = index < msaaSampleValues.Count ? msaaSampleValues[index] : 1;
            Game.instance.updateFramebuffers();
        };
        msaa.tooltip = "Multi-Sample Anti-Aliasing smooths the edges of geometry.\nDoes nothing for textures or foliage.\nThe options shown are hardware-validated for your GPU.";
        settingElements.Add(msaa);
        addElement(msaa);

        var ssaaOptions = new List<string> { "SSAA: OFF" };
        var ssaaSampleValues = new List<int> { 1 };

        foreach (var sample in Game.supportedMSAASamples) {
            if (sample > 8) {
                break;
            }
            ssaaOptions.Add($"SSAA: {sample}x");
            ssaaSampleValues.Add((int)sample);
        }

        var currentSsaaIndex = ssaaSampleValues.IndexOf(settings.ssaaSamples);
        if (currentSsaaIndex == -1) {
            currentSsaaIndex = 0;
        }

        var ssaa = new ToggleButton(this, "ssaa", false, currentSsaaIndex, ssaaOptions.ToArray());
        ssaa.centreContents();
        ssaa.clicked += _ => {
            var index = ssaa.getIndex();
            settings.ssaaSamples = index < ssaaSampleValues.Count ? ssaaSampleValues[index] : 1;
            Game.instance.updateFramebuffers();
        };
        ssaa.tooltip = "Super-Sample Anti-Aliasing renders the game at a higher resolution then downscales.\nProvides excellent quality but severely impacts performance.";
        settingElements.Add(ssaa);
        addElement(ssaa);



        var back = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.setPosition(new Vector2I(2, -18));
        back.clicked += _ => {
            deactivate();
            SettingsScreen.SETTINGS_MENU.pop();
        };
        addElement(back);

        layoutSettingsTwoCols(settingElements, new Vector2I(0, 16), mipmapping.GUIbounds.Width);
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
            SettingsScreen.SETTINGS_MENU.pop();
        }
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }
}
