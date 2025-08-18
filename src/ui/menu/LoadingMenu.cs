using System.Collections;
using System.Numerics;
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
        titleText = Text.createText(this, "titleText", new Vector2I(0, 0), "Loading world");
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
        var setupTimer = new WaitForMinimumTime(0.05);
        start("Setting up world");

        Game.renderer.setWorld(world);
        update(0.04f);
        yield return new WaitForNextFrame();
        yield return setupTimer;

        var initTimer = new WaitForMinimumTime(0.1);
        stage("Initializing world");
        Game.world.init(isLoading);
        update(0.08f);
        yield return initTimer;

        stage("Generating initial terrain");
        world.loadAroundPlayer(ChunkStatus.LIGHTED);
        update(0.10f);

        int totalChunks = world.chunkLoadQueue.Count;
        int processedChunks = 0;

        stage("Loading chunks");

        int loops = 0;
        while (world.chunkLoadQueue.Count > 0) {
            world.updateChunkloading(Game.permanentStopwatch.Elapsed.TotalMilliseconds, loading: true,
                ref processedChunks);

            int currentChunks = world.chunkLoadQueue.Count;
            processedChunks = totalChunks - currentChunks;
            float chunkProgress = totalChunks > 0 ? (float)processedChunks / totalChunks : 1f;
            // map chunk progress to 10%-50% of loading bar
            update(mapProgress(chunkProgress, 0.10f, 0.60f));

            loops++;
            // we update at ~20FPS, this shouldn't be too bad
            //if (loops % 3 == 0) {
            stage($"Loading chunks ({processedChunks}/{totalChunks})");
            yield return new WaitForNextFrame();
            //}
        }

        stage("Stabilising the world");
        // process all lighting updates

        var total = world.skyLightQueue.Count;
        int processed = 0;
        while (world.skyLightQueue.Count > 0) {
            world.processSkyLightQueueLoading(1000);
            processed++;
            if (processed % 10000 == 0) {
                update(mapProgress(processed / (float)total, 0.60f, 0.80f));
                stage($"Processing sky light ({processed}/{total})");
                yield return new WaitForNextFrame();
            }
            //yield return new WaitForNextFrame();
        }

        // mesh all chunks after lighting
        stage("Tesselating");
        yield return new WaitForNextFrame();

        world.loadAroundPlayer(ChunkStatus.MESHED);

        // mark all chunks dirty for remeshing since lighting was processed with noUpdate=true
        // idk why this is necessary tho, the following section SHOULD mesh everything but idk
        foreach (Chunk chunk in world.chunkList) {
            for (int i = 0; i < Chunk.CHUNKHEIGHT; i++) {
                world.dirtyChunk(new SubChunkCoord(chunk.coord.x, i, chunk.coord.z));

                if (i % 20 == 0) {
                    // update progress every 20 chunks
                    yield return new WaitForNextFrame();
                }
            }
        }

        totalChunks = world.chunkLoadQueue.Count;
        processedChunks = 0;
        while (world.chunkLoadQueue.Count > 0) {
            world.updateChunkloading(Game.permanentStopwatch.Elapsed.TotalMilliseconds, loading: true,
                ref processedChunks);
            yield return new WaitForNextFrame();
            int currentChunks = world.chunkLoadQueue.Count;
            processedChunks = totalChunks - currentChunks;
            float chunkProgress = totalChunks > 0 ? (float)processedChunks / totalChunks : 1f;
            // mesh generation: 80%-95%
            update(mapProgress(chunkProgress, 0.80f, 0.95f));

            if (processedChunks % 10 == 0) {
                stage($"Loading chunks ({processedChunks}/{totalChunks})");
                yield return new WaitForNextFrame();
            }
        }

        //update(0.95f);

        var readyTimer = new WaitForMinimumTime(0.05);
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

    /**
     * Maps progress (0-1) to a specific range of the overall loading bar.
     * @param progress Current progress from 0.0 to 1.0
     * @param startPercent Start of range (e.g. 0.15f for 15%)
     * @param endPercent End of range (e.g. 0.95f for 95%)
     * @return Mapped progress value
     */
    private float mapProgress(float progress, float startPercent, float endPercent) {
        return startPercent + (progress * (endPercent - startPercent));
    }
}