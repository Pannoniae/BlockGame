using System.Numerics;
using BlockGame.GL;
using Molten;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

public class Image : GUIElement {

    BTexture2D texture;

    public Image(Menu menu, string name, string path) : base(menu, name) {
        texture = Game.textureManager.get(path);
        guiPosition.Width = (int)texture.width;
        guiPosition.Height = (int)texture.height;
    }

    public void setPosition(Vector2I pos) {
        setPosition(new Rectangle(pos.X, pos.Y, guiPosition.Width, guiPosition.Height));
    }

    public override void draw() {
        Rectangle tex;
        Game.gui.draw(texture, new Vector2(bounds.X, bounds.Y));
        var centre = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
    }
}