using System.Numerics;
using BlockGame.GL.vertexformats;
using Molten;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

public class Text : GUIElement {

    public bool shadowed = false;
    private string _text = "";
    public bool thin;

    public new string text {
        get => _text;
        set {
            _text = value;
            updateLayout();
        }
    }

    public Text(Menu menu, string name, string text) : base(menu, name) {
        _text = text;
        unscaledSize = true;
        updateLayout();
    }
    
    private void updateLayout() {
        if (!string.IsNullOrEmpty(_text)) {
            var textSize = Game.gui.measureString(_text, thin);
            guiPosition.Width = (int)textSize.X;
            guiPosition.Height = (int)textSize.Y;
        }
    }

    public static Text createText(Menu menu, string name, Vector2I pos, string text) {
        var bounds = Game.gui.measureString(text);
        var guitext = new Text(menu, name, text);
        guitext.setPosition(new Rectangle(pos.X, pos.Y, (int)(pos.X + bounds.X), (int)(pos.Y + bounds.Y)));
        return guitext;
    }


    public override void draw() {
        if (shadowed) {
            Game.gui.drawStringShadowed(_text, new Vector2(bounds.X, bounds.Y), thin);
        }
        else {
            Game.gui.drawString(_text, new Vector2(bounds.X, bounds.Y), thin);
        }
    }
}