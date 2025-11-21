using SixLabors.ImageSharp;
using System.Numerics;
using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.util;
using BlockGame.util.font;
using FontStashSharp;
using Molten;

namespace BlockGame.ui.element;

/** Subtitle text for main menu */
public class Subtitle : GUIElement {

    private readonly string subtitleText;
    private readonly Color colour;

    private float textScale = 1.5f;

    public Subtitle(Menu menu, string name) : base(menu, name) {

        // randomly select subtitle
        subtitleText = Game.instance.getRandomTitle();

        // yellow colour like NOSTALGIA FR
        colour = new Color(255, 255, 0, 255);

        textScaledSize = true;
        var textSize = Game.gui.measureStringUI(subtitleText, thin: true) * GUI.TEXTSCALEV * textScale;
        guiPosition.Width = (int)textSize.X;
        guiPosition.Height = (int)textSize.Y;
    }

    public void setPosition(Vector2I pos) {
        setPosition(new Rectangle(pos.X, pos.Y, guiPosition.Width, guiPosition.Height));
    }

    public override void draw() {

        var time = Game.permanentStopwatch.ElapsedMilliseconds / 1000f;

        // pizzazz
        float pulse = 1f + MathF.Sin(time * 3f) * 0.025f;
        float rot = MathF.Sin(time * 2f) * 0.01f;

        var cx = bounds.X + bounds.Width / 2f;
        var cy = bounds.Y + bounds.Height / 2f;

        // (origin is in local text space before scaling)
        var size = Game.gui.guiFontThinl.MeasureString(subtitleText);
        var origin = size / 2f;

        Game.gui.guiFontThinl.DrawText(
            Game.fontLoader.rendererl,
            subtitleText,
            new Vector2(cx, cy),
            colour.toFS(),
            rot,
            origin,
            new Vector2(pulse * textScale),
            effect: FontSystemEffect.Shadow,
            effectAmount: 1
        );

        Game.gui.guiFontThinl.DrawText(
            Game.fontLoader.rendererl,
            subtitleText,
            new Vector2(cx, cy),
            colour.toFS(),
            rot,
            origin,
            new Vector2(pulse * textScale)
        );
    }
}