using BlockGame.main;
using BlockGame.render;
using BlockGame.render.texpack;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using BlockGame.world.block;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class PerformanceSettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public PerformanceSettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeSettings();
    }

    private void initializeSettings() {
        var settings = Settings.instance;
        var elements = new List<GUIElement>();

        var frustumCulling = new ToggleButton(this, "frustumCulling", false, settings.frustumCulling ? 1 : 0,
            "Frustum Culling: OFF", "Frustum Culling: ON");
        frustumCulling.centreContents();
        frustumCulling.clicked += _ => { settings.frustumCulling = frustumCulling.getIndex() == 1; };
        frustumCulling.tooltip =
            "Frustum Culling skips rendering blocks outside the camera's view.\nThis usually improves performance. Consider turning it off if blocks are invisible.";
        elements.Add(frustumCulling);
        addElement(frustumCulling);

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
        rendererMode.centreContents();
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
        elements.Add(rendererMode);
        addElement(rendererMode);

        var fastWater = new ToggleButton(this, "fastWater", false, settings.fastWater ? 1 : 0,
            "Water: Nice", "Water: Fast");
        fastWater.centreContents();
        fastWater.clicked += _ => {
            settings.fastWater = fastWater.getIndex() == 1;
            // refresh the WHOLE renderer for this one
            Game.renderer?.reloadRenderer(settings.rendererMode, settings.rendererMode);
            remeshIfRequired(settings.renderDistance);
        };
        fastWater.tooltip =
            "Water rendering mode.\nFast improves performance on integrated GPUs by making water opaque.\nNice looks better.";
        elements.Add(fastWater);
        addElement(fastWater);

        var fastLeaves = new ToggleButton(this, "fastLeaves", false, settings.fastLeaves ? 1 : 0,
            "Leaves: Nice", "Leaves: Fast");
        fastLeaves.centreContents();
        fastLeaves.clicked += _ => {
            settings.fastLeaves = fastLeaves.getIndex() == 1;
            Block.updateLeafRenderMode();

            // rebuild block atlas with new alpha values
            TexturePackManager.reloadAtlases();
            // refresh the WHOLE renderer for this one
            Game.renderer?.reloadRenderer(settings.rendererMode, settings.rendererMode);
            remeshIfRequired(settings.renderDistance);
        };
        fastLeaves.tooltip =
            "Leaves rendering mode.\nFast culls interior faces and makes leaves opaque.\nNice shows all leaf faces with transparency.";
        elements.Add(fastLeaves);
        addElement(fastLeaves);

        var noAnimation = new ToggleButton(this, "noAnimation", false, settings.noAnimation ? 1 : 0,
            "Animated Textures: ON", "Animated Textures: OFF");
        noAnimation.centreContents();
        noAnimation.clicked += _ => {
            settings.noAnimation = noAnimation.getIndex() == 1;
            Game.textures.blockTexture.firstLoad = true;
            Game.textures.blockTexture.dtextures.Clear();
            TexturePackManager.reloadAtlases();
        };
        noAnimation.tooltip =
            "Toggle animated textures such as water, lava, and fire.\nTurning this off will improve performance.";
        elements.Add(noAnimation);
        addElement(noAnimation);

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

        layoutSettingsTwoCols(elements, new Vector2I(0, 16), frustumCulling.GUIbounds.Width);
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

    private void remeshIfRequired(int oldRenderDist) {
        // only remesh if we're in the game, NOT on the main menu
        if (Game.world != null && Game.renderer != null) {
            Screen.GAME_SCREEN.remeshWorld(oldRenderDist);
        }
    }
}