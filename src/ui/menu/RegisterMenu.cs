using BlockGame.main;
using BlockGame.net;
using BlockGame.net.packet;
using BlockGame.ui.element;
using BlockGame.util.log;
using LiteNetLib;
using Molten;
using Silk.NET.Input;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu;

public class RegisterMenu : Menu {
    private readonly TextBox passwordInput;
    private readonly TextBox confirmInput;
    private readonly Text errorText;
    private Text usernameDisplay;

    public string username = "";

    public RegisterMenu() {
        // title
        var title = Text.createText(this, "title", new Vector2I(0, 32), "Create Account");
        title.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        title.verticalAnchor = VerticalAnchor.TOP;
        addElement(title);

        // username display
        var usernameLabel = Text.createText(this, "usernameLabel", new Vector2I(0, 60), "Username:");
        usernameLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        usernameLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(usernameLabel);

        usernameDisplay = Text.createText(this, "usernameDisplay", new Vector2I(0, 72), "");
        usernameDisplay.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        usernameDisplay.verticalAnchor = VerticalAnchor.TOP;
        addElement(usernameDisplay);

        // password label
        var passwordLabel = Text.createText(this, "passwordLabel", new Vector2I(0, 96), "Choose Password:");
        passwordLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        passwordLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(passwordLabel);

        // password input
        passwordInput = new TextBox(this, "passwordInput") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.TOP,
            maxLength = 64,
            isPassword = true
        };
        passwordInput.setPosition(new Rectangle(0, 108, 128, 16));
        addElement(passwordInput);

        // confirm label
        var confirmLabel = Text.createText(this, "confirmLabel", new Vector2I(0, 132), "Confirm Password:");
        confirmLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        confirmLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(confirmLabel);

        // confirm input
        confirmInput = new TextBox(this, "confirmInput") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.TOP,
            maxLength = 64,
            isPassword = true
        };
        confirmInput.setPosition(new Rectangle(0, 144, 128, 16));
        addElement(confirmInput);

        // error text (hidden by default)
        errorText = Text.createText(this, "errorText", new Vector2I(0, 168), "");
        errorText.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        errorText.verticalAnchor = VerticalAnchor.TOP;
        errorText.active = false;
        addElement(errorText);

        // register button
        var registerButton = new Button(this, "register", false, "Register") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        registerButton.setPosition(new Vector2I(0, -24));
        registerButton.clicked += _ => register();
        addElement(registerButton);

        // cancel button
        var cancelButton = new Button(this, "cancel", false, "Cancel") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        cancelButton.setPosition(new Vector2I(24, -24));
        cancelButton.clicked += _ => cancel();
        addElement(cancelButton);
    }

    private void register() {
        var password = passwordInput.input.Trim();
        var confirm = confirmInput.input.Trim();

        if (string.IsNullOrEmpty(password)) {
            showError("Password cannot be empty");
            return;
        }

        if (password != confirm) {
            showError("Passwords do not match");
            return;
        }

        hideError();
        Log.info($"Registering account for {username}");

        // send auth packet
        if (ClientConnection.instance != null) {
            ClientConnection.instance.send(new AuthPacket {
                password = password
            }, DeliveryMethod.ReliableOrdered);
        }

        // return to loading screen
        Game.instance.switchTo(LOADING);
    }

    private void cancel() {
        // disconnect and return to multiplayer menu
        if (ClientConnection.instance != null) {
            ClientConnection.instance.disconnect();
        }
        Game.instance.switchTo(MULTIPLAYER_MENU);
    }

    public void showError(string message) {
        errorText.text = message;
        errorText.active = true;
    }

    public void hideError() {
        errorText.active = false;
    }

    public override void activate() {
        // clear fields and error on activation
        passwordInput.input = "";
        confirmInput.input = "";
        hideError();
        focusedElement = passwordInput;
        usernameDisplay.text = username;
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);
        switch (key) {
            case Key.Escape:
                cancel();
                break;
            case Key.Enter:
                register();
                break;
        }
    }
}