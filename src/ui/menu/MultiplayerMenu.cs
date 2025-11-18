using BlockGame.main;
using BlockGame.net;
using BlockGame.ui.element;
using BlockGame.util.log;
using Molten;
using Silk.NET.Input;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu;

public class MultiplayerMenu : Menu {
    private readonly TextBox addressInput;
    private readonly TextBox usernameInput;

    public const int spacing = 24;
    public const int labelspacing = 12;

    public MultiplayerMenu() {
        // title
        var title = Text.createText(this, "title", new Vector2I(0, 32), "Multiplayer");
        title.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        title.verticalAnchor = VerticalAnchor.TOP;
        addElement(title);

        // username label
        var usernameLabel = Text.createText(this, "usernameLabel", new Vector2I(0, 50), "Username:");
        usernameLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        usernameLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(usernameLabel);

        // username input
        usernameInput = new TextBox(this, "usernameInput") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.TOP,
            maxLength = 32
        };
        usernameInput.tooltip = "What's your name?";
        usernameInput.setPosition(new Rectangle(0, 50 + labelspacing, 128, 16));
        addElement(usernameInput);

        // password label
        var passwordLabel = Text.createText(this, "passwordLabel", new Vector2I(0, 50 + labelspacing + spacing), "Password:");
        passwordLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        passwordLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(passwordLabel);

        // password input
        var passwordInput = new TextBox(this, "passwordInput") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.TOP,
            maxLength = 32,
            isPassword = true
        };
        passwordInput.tooltip = "Server password (leave blank if none)";
        passwordInput.setPosition(new Rectangle(0, 50 + labelspacing + spacing + labelspacing, 128, 16));
        addElement(passwordInput);

        // server address label
        var addressLabel = Text.createText(this, "addressLabel", new Vector2I(0, 50 + labelspacing + spacing + labelspacing + spacing), "Server Address:");
        addressLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        addressLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(addressLabel);

        // server address input
        addressInput = new TextBox(this, "addressInput") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.TOP,
            maxLength = 64
        };
        addressInput.setPosition(new Rectangle(0, 50 + labelspacing + spacing + labelspacing + spacing + labelspacing, 128, 16));
        addElement(addressInput);

        // connect button
        var connectButton = new Button(this, "connect", false, "Connect") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        connectButton.setPosition(new Vector2I(0, -24));
        connectButton.clicked += _ => connect();
        addElement(connectButton);

        // cancel button
        var cancelButton = new Button(this, "cancel", false, "Cancel") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        cancelButton.setPosition(new Vector2I(24, -24));
        cancelButton.clicked += _ => Game.instance.switchTo(MAIN_MENU);
        addElement(cancelButton);
    }

    private void connect() {

        Net.mode = NetMode.MPC;
        var username = usernameInput.getInput().Trim();
        if (string.IsNullOrEmpty(username)) {
            username = "Player";
        }

        // save to settings!
        Settings.instance.playerName = username;
        Settings.instance.save();

        var address = addressInput.getInput().Trim();
        if (string.IsNullOrEmpty(address)) {
            Log.warn("No server address specified");
            return;
        }

        // parse address:port
        string host;
        int port = 31337; // default
        if (address.Contains(':')) {
            var parts = address.Split(':');
            host = parts[0];
            if (parts.Length > 1 && int.TryParse(parts[1], out var p)) {
                port = p;
            }
        } else {
            host = address;
        }

        Log.info($"Connecting to {host}:{port} as {username}...");

        // create client and attempt connection
        Game.client ??= new ClientConnection();
        Game.client.connect(host, port, username);

        // TODO: show connecting screen instead of going back to main menu
        Game.instance.switchTo(LOADING);
        LOADING.connect();
    }

    public override void activate() {
        // set defaults
        if (string.IsNullOrEmpty(usernameInput.getInput())) {
            usernameInput.setInput(Settings.instance.playerName);
        }
        if (string.IsNullOrEmpty(addressInput.getInput())) {
            addressInput.setInput("localhost:31337");
        }
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);
        switch (key) {
            case Key.Escape:
                Game.instance.switchTo(MAIN_MENU);
                break;
            case Key.Enter:
                connect();
                break;
        }
    }
}