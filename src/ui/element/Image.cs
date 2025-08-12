using System.Numerics;
using BlockGame.GL;
using Molten;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

public class Image : GUIElement {

    public BTexture2D texture;
    
    /** Scale in gui sizes. 4 = normal guiscale, 2 = small/half, 1 = 1px */
    public float scale = 4f;
    
    public float realScale => scale / GUI.guiScale;

    public Image(Menu menu, string name, string path) : base(menu, name) {
        texture = Game.textureManager.get(path);
        guiPosition.Width = (int)texture.width;
        guiPosition.Height = (int)texture.height;
    }

    public void setPosition(Vector2I pos) {
        setPosition(new Rectangle(pos.X, pos.Y, guiPosition.Width, guiPosition.Height));
    }
    
    public void setScale(float sc) {
        scale = sc;
        guiPosition.Width = (int)(texture.width * realScale);
        guiPosition.Height = (int)(texture.height * realScale);
    }

    public override void draw() {
        Rectangle tex;
        Game.gui.draw(texture, new Vector2(bounds.X, bounds.Y), realScale);
        var centre = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
    }
}