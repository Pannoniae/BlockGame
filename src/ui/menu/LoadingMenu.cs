using System.Collections;
using System.Numerics;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.chunk;
using Molten;

namespace BlockGame.ui.menu;

public class LoadingMenu : Menu, ProgressUpdater {
    public double counter;
    public int dots = 2;

    private Text titleText;
    private Text statusText;
    private Text tipText;
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

        tipText = Text.createText(this, "tipText", new Vector2I(0, 55), "Tip: " + Game.instance.getRandomTip());
        tipText.centreContents();
        tipText.thin = true;
        tipText.updateLayout();
        addElement(tipText);

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
        Game.gui.drawBG(Block.STONE, 16f);
        base.draw();

        // draw a random vertical line at x = 0
        // draw a rectangle (that's what we can draw)
        // Game.gui.draw(Game.gui.colourTexture, new RectangleF(Game.width / 2f, 0, 1, Game.height), color: Color.Red);
    }

    private IEnumerator loadWorldCoroutine(bool isLoading) {
        // Stage progress ranges (non-overlapping):
        // Setup: 0% - 5%
        // Init: 5% - 10%
        // Initial terrain: 10% - 15%
        // Initial chunk loading: 15% - 45%
        // Skylight processing: 45% - 65%
        // Marking dirty chunks: 65% - 70%
        // Meshing: 70% - 95%
        // Ready: 95% - 100%
        
        start("Setting up world");
        
        update(0.025f); // 2.5% progress
        yield return new WaitForNextFrame();
        update(0.05f); // Setup complete: 5%

        var initTimer = new WaitForMinimumTime(0.05);
        stage("Loading spawn chunks");
        Game.world.preInit(isLoading);
        yield return initTimer;
        update(0.10f); // Init complete: 10%
        
        yield return new WaitForNextFrame();
        world.init(isLoading);

        stage("Generating initial terrain");
        world.loadAroundPlayer(ChunkStatus.LIGHTED);
        update(0.15f); // Initial terrain complete: 15%

        // Initial chunk loading phase
        int total = world.chunkLoadQueue.Count;
        int c = 0;
        stage("Loading chunks");

        while (world.chunkLoadQueue.Count > 0) {
            world.updateChunkloading(Game.permanentStopwatch.Elapsed.TotalMilliseconds, loading: true,
                ref c);

            int currentChunks = world.chunkLoadQueue.Count;
            c = total - currentChunks;
            float chunkProgress = total > 0 ? (float)c / total : 1f;
            // Initial chunk loading: 15%-45%
            update(mapProgress(chunkProgress, 0.15f, 0.45f));

            stage($"Loading chunks ({c}/{total})");
            yield return new WaitForNextFrame();
        }

        // process all lighting updates
        stage("Lighting up");
        var totalSkyLight = world.skyLightQueue.Count;
        c = 0;
        
        while (world.skyLightQueue.Count > 0) {
            int before = world.skyLightQueue.Count;
            world.processSkyLightQueueLoading(1000);
            int after = world.skyLightQueue.Count;
            // it hits hard
            c += (before - after);
            
            if (c % 1000 == 0 || world.skyLightQueue.Count == 0) {
                float skyLightProgress = totalSkyLight > 0 ? (float)c / totalSkyLight : 1f;
                update(mapProgress(skyLightProgress, 0.45f, 0.65f));
                stage($"Processing skylight ({c}/{totalSkyLight})");
                yield return new WaitForNextFrame();
            }
        }

        // light all chunks after lighting
        stage("Spreading dirt");
        world.loadAroundPlayer(ChunkStatus.MESHED);
        
        // mark all chunks dirty for remeshing since lighting was processed with noUpdate=true
        // idk why this is necessary tho, the following section SHOULD mesh everything but idk
        int totalSubChunks = world.chunkList.Count * Chunk.CHUNKHEIGHT;
        c = 0;
        
        foreach (Chunk chunk in world.chunkList) {
            for (int i = 0; i < Chunk.CHUNKHEIGHT; i++) {
                world.dirtyChunk(new SubChunkCoord(chunk.coord.x, i, chunk.coord.z));
                c++;

                if (c % 50 == 0) {
                    float dirtyProgress = totalSubChunks > 0 ? (float)c / totalSubChunks : 1f;
                    update(mapProgress(dirtyProgress, 0.65f, 0.70f));
                    yield return new WaitForNextFrame();
                }
            }
        }
        update(0.70f); // Marking dirty complete: 70%

        // Meshing phase
        stage("Tesselating");
        total = world.chunkLoadQueue.Count;
        c = 0;
        
        while (world.chunkLoadQueue.Count > 0) {
            world.updateChunkloading(Game.permanentStopwatch.Elapsed.TotalMilliseconds, loading: true,
                ref c);
            
            int currentChunks = world.chunkLoadQueue.Count;
            c = total - currentChunks;
            //Log.info($"Meshed {currentChunks}/{total} chunks");
            float meshProgress = total > 0 ? (float)c / total : 1f;
            // Meshing: 70%-95%
            update(mapProgress(meshProgress, 0.70f, 0.95f));

            if (c % 5 == 0) {
                stage($"Tesselating ({c}/{total})");
            }
            yield return new WaitForNextFrame();
        }
        
        world.postInit(isLoading);
        
        var readyTimer = new WaitForMinimumTime(0.05);
        stage("Ready!");
        update(1.0f); // Complete: 100%
        yield return readyTimer;

        // we don't need it anymore
        world = null!;
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
     * Maps progress (0-1) to a specific range of the overall loading bar. (yes I know this exists in Meth, shut up)
     * @param progress Current progress from 0.0 to 1.0
     * @param startPercent Start of range (e.g. 0.15f for 15%)
     * @param endPercent End of range (e.g. 0.95f for 95%)
     * @return Mapped progress value
     */
    private static float mapProgress(float progress, float startPercent, float endPercent) {
        return startPercent + (progress * (endPercent - startPercent));
    }
}