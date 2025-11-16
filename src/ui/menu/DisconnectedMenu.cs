using System.Numerics;
using BlockGame.main;
using BlockGame.ui.element;
using Molten;

namespace BlockGame.ui.menu;

public class DisconnectedMenu : Menu {
    private Text reasonText;
    private Text titleText;

    public DisconnectedMenu() {
        // title
        titleText = Text.createText(this, "title", new Vector2I(0, -48), "Disconnected");
        titleText.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        titleText.verticalAnchor = VerticalAnchor.CENTREDCONTENTS;

        // reason text
        reasonText = Text.createText(this, "reason", new Vector2I(0, -24), "");
        reasonText.horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        reasonText.verticalAnchor = VerticalAnchor.CENTREDCONTENTS;

        // back to main menu button
        var mainMenu = new Button(this, "mainMenu", false, "Back to Main Menu");
        mainMenu.setPosition(new Vector2I(0, 8));
        mainMenu.centreContents();
        mainMenu.clicked += _ => {
            Game.instance.executeOnMainThread(() => {
                Game.instance.switchTo(Menu.MAIN_MENU);
            });
        };

        addElement(titleText);
        addElement(reasonText);
        addElement(mainMenu);
    }

    /** show menu with custom reason */
    public void show(string reason, bool kicked = false) {
        reasonText.text = reason;
        titleText.text = kicked ? "Kicked from server" : "Disconnected";
    }

    public override void draw() {
        // draw stone bg like ConfirmDialog
        Game.gui.drawBG(16);
        base.draw();
    }
}