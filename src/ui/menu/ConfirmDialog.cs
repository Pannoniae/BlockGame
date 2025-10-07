using BlockGame.main;
using BlockGame.ui.element;
using Molten;
using Silk.NET.Input;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu;

public class ConfirmDialog : Menu {
    private Text messageText;
    private Action? onConfirm;
    private Action? onCancel;

    public const int offsetX = 64 + 16;

    public ConfirmDialog() {

        // message text
        messageText = Text.createText(this, "message", new Vector2I(0, -32), "");
        messageText.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        messageText.verticalAnchor = VerticalAnchor.CENTREDCONTENTS;
        addElement(messageText);

        // confirm button (yes/ok)
        var confirmButton = new Button(this, "confirm", false, "Yes") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.CENTREDCONTENTS
        };
        confirmButton.setPosition(new Vector2I(-offsetX, 16));
        confirmButton.clicked += _ => {
            onConfirm?.Invoke();
        };
        addElement(confirmButton);

        // cancel button (no/cancel)
        var cancelButton = new Button(this, "cancel", false, "No") {
            horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS,
            verticalAnchor = VerticalAnchor.CENTREDCONTENTS
        };
        cancelButton.setPosition(new Vector2I(offsetX, 16));
        cancelButton.clicked += _ => {
            onCancel?.Invoke();
        };
        addElement(cancelButton);
    }

    public void show(string message, Action onConfirm, Action? onCancel = null) {
        messageText.text = message;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
    }

    public override void draw() {
        // draw stone bg
        Game.gui.drawBG(16);
        base.draw();
    }

    public override void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyUp(keyboard, key, scancode);
        if (key == Key.Escape) {
            onCancel?.Invoke();
        }
    }
}