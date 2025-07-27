using System.Collections;
using System.Drawing;
using System.Numerics;
using BlockGame.GL.vertexformats;
using BlockGame.id;
using BlockGame.ui.element;
using BlockGame.util;
using Molten;

namespace BlockGame.ui;

public class LoadingMenu : Menu, ProgressUpdater {
    public double counter;
    public int dots = 2;

    private Text titleText;
    private Text statusText;
    private ProgressBar progressBar;
    private World world;

    private Coroutine loadingCoroutine;
    private string currentStage = "Initializing";
    private float currentProgress;

    public LoadingMenu() {
        titleText = Text.createText(this, "titleText", new Vector2I(0, 0), "Loading world...");
        //titleText.updateLayout();
        titleText.centreContents();
        //titleText.thin = true;
        addElement(titleText);

        statusText = Text.createText(this, "statusText", new Vector2I(0, 25), "Initializing...");
        //statusText.updateLayout();
        statusText.centreContents();
        //statusText.thin = true;
        addElement(statusText);

        progressBar = new ProgressBar(this, "progressBar", 200, 3);
        progressBar.setPosition(new Vector2(0, 35));
        progressBar.centreContents();
        addElement(progressBar);

        counter = 0;
    }

    public void load(World world, bool isLoading) {
        this.world = world;
        loadingCoroutine = Game.startCoroutine(loadWorldCoroutine(isLoading));
    }


    public override void update(double dt) {
        base.update(dt);
        if (loadingCoroutine.isCompleted) {
            Game.instance.switchToScreen(Screen.GAME_SCREEN);
            Game.instance.lockMouse();
        }
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
        
        // we draw BG first! elements after
        Game.gui.drawBG(Block.get(Blocks.STONE), 16f);
        base.draw();
        
        // draw a random vertical line at x = 0
        // draw a rectangle (that's what we can draw)
        // Game.gui.draw(Game.gui.colourTexture, new RectangleF(Game.width / 2f, 0, 1, Game.height), color: Color4b.Red);
    }

    private IEnumerator loadWorldCoroutine(bool isLoading) {
        var setupTimer = new WaitForMinimumTime(0.1);
        start("Setting up world");

        Game.renderer.setWorld(world);
        update(0.04f);
        yield return new WaitForNextFrame();
        yield return setupTimer;

        var initTimer = new WaitForMinimumTime(0.2);
        stage("Initializing world");
        Game.world.init(isLoading);
        update(0.08f);
        yield return initTimer;

        stage("Generating initial terrain");
        world.loadAroundPlayer();
        update(0.15f);

        int totalChunks = world.chunkLoadQueue.Count;
        int processedChunks = 0;

        stage("Loading chunks");
        while (world.chunkLoadQueue.Count > 0) {
            world.updateChunkloading(Game.permanentStopwatch.Elapsed.TotalMilliseconds, loading: true,
                ref processedChunks);
            yield return new WaitForNextFrame();

            int currentChunks = world.chunkLoadQueue.Count;
            processedChunks = totalChunks - currentChunks;
            float chunkProgress = totalChunks > 0 ? (float)processedChunks / totalChunks : 1f;
            // we want to start at 0.15 and end at 0.95, so we scale the chunk progress
            update(0.15f + (chunkProgress * 0.8f));

            if (processedChunks % 10 == 0) {
                stage($"Loading chunks ({processedChunks}/{totalChunks})");
            }
            
            // pause 0.1 seconds every 100 chunks to avoid freezing the UI
            //if (processedChunks % 10 == 0) {
                // yield return new WaitForSeconds(0.1);
            //}
        }

        var finalizeTimer = new WaitForMinimumTime(0.3);
        stage("Finalizing world");
        update(0.95f);
        yield return finalizeTimer;

        var readyTimer = new WaitForMinimumTime(0.2);
        stage("Ready!");
        update(1.0f);
        yield return readyTimer;
    }

    public void sleep() {
        Task.Delay(2000).Wait();
    }

    public void start(string stage) {
        currentStage = stage;
        currentProgress = 0f;

        // reset the dots for the status text
        dots = 0;
        statusText.text = currentStage + new string('.', dots + 1);
        counter = 0;
    }

    public void stage(string stage) {
        currentStage = stage;
    }

    public void update(float progress) {
        currentProgress = Math.Clamp(progress, 0f, 1f);
    }
}