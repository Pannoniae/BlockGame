using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.ui.menu;
using Molten;

namespace BlockGame.ui.element;

public class Image : GUIElement {

    public BTexture2D texture;
    
    /** Scale in gui sizes. 4 = normal guiscale, 2 = small/half, 1 = 1px */
    public float scale => GUI.guiScale / 4f;
    
    // if guiscale is 4, 2/4
    // if guiscale is 2, 2/2
    // this is the divider to get from scale to real scale
    public float realScale => scale / GUI.guiScale;

    public Image(Menu menu, string name, string path) : base(menu, name) {
        texture = Game.textures.get(path);
        guiPosition.Width = (int)(texture.width * realScale);
        guiPosition.Height = (int)(texture.height * realScale);
    }

    public void setPosition(Vector2I pos) {
        setPosition(new Rectangle(pos.X, pos.Y, guiPosition.Width, guiPosition.Height));
        
    }
    
    public void setScale(float sc) {
        //scale = sc;
        guiPosition.Width = (int)(texture.width * realScale);
        guiPosition.Height = (int)(texture.height * realScale);
    }

    public override void draw() {
        Game.gui.draw(texture, new Vector2(bounds.X, bounds.Y), realScale);
    }
}