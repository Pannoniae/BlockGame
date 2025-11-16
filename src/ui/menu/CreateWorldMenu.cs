using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.worldgen.generator;
using Molten;
using Silk.NET.Input;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu;

public class CreateWorldMenu : Menu {
    private readonly TextBox nameInput;
    private readonly TextBox seedInput;
    private readonly ToggleButton generatorButton;

    public const int spacing = 24;
    public const int labelspacing = 12;

    public CreateWorldMenu() {
        // title
        var title = Text.createText(this, "title", new Vector2I(0, 32), "Create New World");
        title.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        title.verticalAnchor = VerticalAnchor.TOP;
        addElement(title);

        // world name label
        var nameLabel = Text.createText(this, "nameLabel", new Vector2I(0, 50), "World Name:");
        nameLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        nameLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(nameLabel);

        // world name input
        nameInput = new TextBox(this, "nameInput") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.TOP
        };
        nameInput.setPosition(new Rectangle(0, 50 + labelspacing, 128, 16));
        addElement(nameInput);

        // seed label (optional)
        var seedLabel = Text.createText(this, "seedLabel", new Vector2I(0, 50 + labelspacing + spacing), "Seed (optional):");
        seedLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        seedLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(seedLabel);

        // seed input
        seedInput = new TextBox(this, "seedInput") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.TOP
        };
        seedInput.setPosition(new Rectangle(0, 50 + labelspacing + spacing + labelspacing, 128, 16));
        addElement(seedInput);

        // generator label
        var genLabel = Text.createText(this, "genLabel", new Vector2I(0, 50 + labelspacing + spacing + labelspacing + spacing), "Generator:");
        genLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        genLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(genLabel);

        // generator toggle button
        generatorButton = new ToggleButton(this, "generator", false, 0, WorldGenerators.all.Select(n => "generator." + n).ToArray()) {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.TOP
        };
        generatorButton.setPosition(new Vector2I(0, 50 + labelspacing + spacing + labelspacing + spacing + labelspacing));
        addElement(generatorButton);

        // create button
        var createButton = new Button(this, "create", false, "Create") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        createButton.setPosition(new Vector2I(0, -24));
        createButton.clicked += _ => createWorld();
        addElement(createButton);

        // cancel button (returns to level select)
        var cancelButton = new Button(this, "cancel", false, "Cancel") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        cancelButton.setPosition(new Vector2I(24, -24));
        cancelButton.clicked += _ => Game.instance.switchTo(LEVEL_SELECT);
        addElement(cancelButton);
    }

    private void createWorld() {
        var worldName = nameInput.input.Trim();
        //Console.Out.WriteLine("world name: " + worldName);
        if (string.IsNullOrEmpty(worldName)) {
            worldName = "New World";
            nameInput.input = worldName;
        }

        // generate folder name (replace spaces with underscores)
        var folderName = worldName.Replace(" ", "_");

        // ensure folder name is unique
        var originalFolderName = folderName;
        int suffix = 1;
        while (WorldIO.worldExists(folderName)) {
            folderName = $"{originalFolderName}_{suffix}";
            suffix++;
        }

        // parse seed (or generate random)
        int seed;
        if (string.IsNullOrEmpty(seedInput.input) || !int.TryParse(seedInput.input, out seed)) {
            // if seed is string, hash to int
            seed = !string.IsNullOrEmpty(seedInput.input) ? seedInput.input.GetHashCode() : Game.random.Next();
        }

        // get generator name (strip "generator." prefix)
        var generatorName = generatorButton.getState().Replace("generator.", "");

        // create world
        var world = new World(folderName, seed, worldName, generatorName);
        Net.mode = NetMode.SP;
        Game.setWorld(world);
        Game.instance.switchTo(LOADING);
        LOADING.load(world, false);
    }

    public override void activate() {
        // reset inputs
        nameInput.input = "New World";
        seedInput.input = "";
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);
        if (key == Key.Escape) {
            Game.instance.switchTo(LEVEL_SELECT);
        }
    }
}