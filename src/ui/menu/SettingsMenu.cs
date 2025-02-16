using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using TrippyGL;
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

        var mipmapping = new Slider(this, "mipmapping", 0, 4, 1, settings.mipmapping);
        mipmapping.setPosition(new Rectangle(0, 112, 128, 16));
        mipmapping.topCentre();
        mipmapping.applied += () => {
            settings.mipmapping = (int)mipmapping.value;
            Game.textureManager.blockTexture.bind();
            Game.GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, settings.mipmapping != 0 ? (int)GLEnum.NearestMipmapLinear : (int)GLEnum.Nearest);
            Game.textureManager.blockTexture.reload();
        };
        mipmapping.getText = value => value == 0 ? "Mipmapping: Off" : $"Mipmapping: {value}x";
        mipmapping.tooltip = "Mipmapping reduces the flickering of textures at a distance.";
        settingElements.Add(mipmapping);
        addElement(mipmapping);

        var fxaa = new ToggleButton(this, "fxaa", false, settings.fxaa ? 1 : 0,
            "FXAA: Disabled", "FXAA: Enabled");
        fxaa.topCentre();
        fxaa.clicked += _ => {
            settings.fxaa = fxaa.getIndex() == 1;
            Game.instance.updateFramebuffers();
        };
        fxaa.tooltip = "FXAA is a fast anti-aliasing technique that smooths the jagged edges of blocks.";
        settingElements.Add(fxaa);
        addElement(fxaa);

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
        FOV.getText = value => {
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

    public override void clear(GraphicsDevice GD, double dt, double interp) {
        GD.ClearColor = Color4b.SlateGray;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
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