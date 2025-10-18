using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.util;
using Molten;


namespace BlockGame.ui.element;

public class ProgressBar : GUIElement {
    private float progress = 0f;
    private Color backgroundColor = new(32, 32, 32, 255);
    private Color foregroundColor = new(0, 255, 0, 255);
    
    public ProgressBar(Menu menu, string name, int width, int height) : base(menu, name) {
        guiPosition.Width = width;
        guiPosition.Height = height;
    }
    
    public void setProgress(float value) {
        progress = Math.Clamp(value, 0f, 1f);
    }
    
    public override void draw() {
        var bg = bounds;
        
        Game.gui.draw(Game.gui.colourTexture, new RectangleF(bg.X, bg.Y, bg.Width, bg.Height), default, backgroundColor);
        
        if (progress > 0f) {
            var progressWidth = (int)(bg.Width * progress);
            Game.gui.draw(Game.gui.colourTexture, new RectangleF(bg.X, bg.Y, progressWidth, bg.Height), default, foregroundColor);
        }
    }
}