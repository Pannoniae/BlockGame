using System.Numerics;
using BlockGame.GL.vertexformats;
using BlockGame.ui.element;
using BlockGame.util;
using Molten;

namespace BlockGame.ui;

public class StartupLoadingMenu : Menu {
    public double counter;
    public int dots = 2;

    private Text titleText;
    private Text statusText;
    private ProgressBar progressBar;
    
    private string currentStage = "";
    private float currentProgress;

    public StartupLoadingMenu() {
        titleText = Text.createText(this, "titleText", new Vector2I(0, 0), "BlockGame");
        titleText.centreContents();
        //titleText.thin = true;
        titleText.updateLayout();
        // Title text is bigger by default, no scale property needed
        addElement(titleText);

        statusText = Text.createText(this, "statusText", new Vector2I(0, 50), "Loading...");
        statusText.centreContents();
        //statusText.thin = true;
        statusText.updateLayout();
        addElement(statusText);

        progressBar = new ProgressBar(this, "progressBar", 200, 3);
        progressBar.setPosition(new Vector2(0, 25));
        progressBar.centreContents();
        addElement(progressBar);

        counter = 0;
    }

    public void updateProgress(float progress, string stage) {
        currentProgress = Math.Clamp(progress, 0f, 1f);
        currentStage = stage;
        
        // reset dots animation when stage changes
        dots = 0;
        counter = 0;
    }

    public override void render(double dt, double interp) {
        base.render(dt, interp);

        counter += dt;
        if (counter > 0.5) {
            dots = (dots + 1) % 3;
            counter = 0;
            statusText.text = currentStage + new string('.', dots + 1);
        }

        progressBar.setProgress(currentProgress);
    }

    public override void draw() {
        // draw gradient background
        Game.gui.drawGradientVertical(Game.gui.colourTexture, 
            new System.Drawing.RectangleF(0, 0, Game.width, Game.height),
            Color4b.CornflowerBlue,
            Color4b.MediumPurple);
        
        base.draw();
    }
}