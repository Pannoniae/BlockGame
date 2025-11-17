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

public class LoginMenu : Menu {
    private readonly TextBox passwordInput;
    private readonly Text errorText;
    private Text usernameText;

    public string username = "";

    public LoginMenu() {
        // title
        var title = Text.createText(this, "title", new Vector2I(0, 32), "Login Required");
        title.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        title.verticalAnchor = VerticalAnchor.TOP;
        addElement(title);

        // username display
        var usernameLabel = Text.createText(this, "usernameLabel", new Vector2I(0, 60), "Username:");
        usernameLabel.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        usernameLabel.verticalAnchor = VerticalAnchor.TOP;
        addElement(usernameLabel);

        usernameText = Text.createText(this, "usernameText", new Vector2I(0, 72), "");
        usernameText.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        usernameText.verticalAnchor = VerticalAnchor.TOP;
        addElement(usernameText);

        // password label
        var passwordLabel = Text.createText(this, "passwordLabel", new Vector2I(0, 96), "Password:");
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

        // error text (hidden by default)
        errorText = Text.createText(this, "errorText", new Vector2I(0, 132), "");
        errorText.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        errorText.verticalAnchor = VerticalAnchor.TOP;
        errorText.active = false;
        addElement(errorText);

        // login button
        var loginButton = new Button(this, "login", false, "Login") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        loginButton.setPosition(new Vector2I(0, -24));
        loginButton.clicked += _ => login();
        addElement(loginButton);

        // cancel button
        var cancelButton = new Button(this, "cancel", false, "Cancel") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        cancelButton.setPosition(new Vector2I(24, -24));
        cancelButton.clicked += _ => cancel();
        addElement(cancelButton);
    }

    private void login() {
        var password = passwordInput.input.Trim();
        if (string.IsNullOrEmpty(password)) {
            showError("Password cannot be empty");
            return;
        }

        hideError();
        Log.info($"Logging in as {username}");

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
        // clear password and error on activation
        passwordInput.input = "";
        hideError();
        focusedElement = passwordInput;
        usernameText.text = username;
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
                login();
                break;
        }
    }
}