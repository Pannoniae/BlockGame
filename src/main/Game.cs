using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.font;
using Molten;
using SFML.Audio;
using Silk.NET.Core;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using BatcherBeginMode = BlockGame.GL.BatcherBeginMode;
using DebugSeverity = Silk.NET.OpenGL.DebugSeverity;
using DebugSource = Silk.NET.OpenGL.DebugSource;
using DebugType = Silk.NET.OpenGL.DebugType;
using Image = SixLabors.ImageSharp.Image;
using IWindow = Silk.NET.Windowing.IWindow;
using MouseButton = Silk.NET.Input.MouseButton;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;
using Shader = BlockGame.GL.Shader;
using Sound = SFML.Audio.Sound;

namespace BlockGame;

public partial class Game {
    public static Game instance { get; private set; }

    public static int width;
    public static int height;

    public static IWindow window;
    public static Silk.NET.OpenGL.GL GL = null!;
    public static IInputContext input = null!;

    public static Process proc;

    public static int centreX => width / 2;
    public static int centreY => height / 2;

    /// <summary>
    /// The current game screen which is shown.
    /// </summary>
    public Screen currentScreen;

    public static bool devMode;

    public static Graphics graphics;
    public static GUI gui;
    
    public static World? world;
    public static Player? player;
    public static WorldRenderer? renderer;

    public static IMouse mouse;
    public static Vector2 mousePos;
    public static IKeyboard keyboard;

    public Vector2 lastMousePos;
    public Vector3I? targetedPos;
    public Vector3I? previousPos;

    public int fps;
    public double ft;

    public static Random random = new Random(1337 * 1337);
    public static Random clientRandom = new Random();

    public static Stopwatch stopwatch = new();

    /// <summary>
    /// Stopwatch but keeps running
    /// </summary>
    public static Stopwatch permanentStopwatch = new();

    public double accumTime;
    public static readonly double fixeddt = 1 / 30d;
    public static readonly double maxTimestep = 1 / 5f;
    public double t;

    /// <summary>
    /// List of things to do later.
    /// </summary>
    public static List<TimerAction> timerQueue = new();

    public static bool focused;
    public static bool firstFrame;

    /// <summary>
    /// True while clicking back into the game. Used to prevent the player instantly breaking a block when clicking on the menu to get back into the game world.
    /// </summary>
    public static bool lockingMouse;

    public static TextureManager textureManager;
    public static Metrics metrics;
    
    public static FontLoader fontLoader;

    public BlockingCollection<Action> mainThreadQueue = new();

    private SoundBuffer buffer;
    private Sound music;

    private readonly string[] splashes;
    private readonly string splash;

    private uint fbo;
    private uint FBOtex;
    private uint throwawayVAO;
    private uint depthBuffer;

    private int g_texelStepLocation;
    private int g_showEdgesLocation;
    private int g_fxaaOnLocation;
    private int g_lumaThresholdLocation;
    private int g_mulReduceLocation;
    private int g_minReduceLocation;
    private int g_maxSpanLocation;

    #if DEBUG
    public static string VERSION = "BlockGame v0.0.2 DEBUG";
    #else
    public static string VERSION = "BlockGame v0.0.2";
    #endif

    private static readonly float g_lumaThreshold = 0.5f;
    private static readonly float g_mulReduceReciprocal = 8.0f;
    private static readonly float g_minReduceReciprocal = 128.0f;
    private static readonly float g_maxSpan = 8.0f;


    public Game(bool devMode) {
        Game.devMode = devMode;
        instance = this;

        // load splashes
        splashes = File.ReadAllLines("splashes.txt");

        var windowOptions = WindowOptions.Default;
        //windowOptions.FramesPerSecond = 6000;
        //windowOptions.UpdatesPerSecond = 6000;
        windowOptions.VSync = false;
        splash = getRandomSplash();
        windowOptions.Size = new Vector2D<int>(Constants.initialWidth, Constants.initialHeight);
        windowOptions.PreferredDepthBufferBits = 32;
        windowOptions.PreferredStencilBufferBits = 0;
        var api = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible,
            new APIVersion(4, 6));
        ;
        #if DEBUG
        api.Flags = ContextFlags.Debug;
        // if we are in debug mode, force x11 because stupid wayland doesn't work with renderdoc for debugging
        // GLFW_PLATFORM = 0x00050003
        // #define GLFW_PLATFORM_WAYLAND   0x00060003
        // #define GLFW_PLATFORM_X11   0x00060004

        #endif
        // force x11 in release too because wayland breaks everything
        // it even breaks the fucking mouse position
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            GlfwProvider.UninitializedGLFW.Value.InitHint((InitHint)0x00050003, 0x00060004);
        }

        windowOptions.API = api;
        Window.PrioritizeGlfw();

        #if DEBUG
        GlfwProvider.GLFW.Value.WindowHint(WindowHintRobustness.ContextRobustness, Robustness.LoseContextOnReset);
        #endif

        // no errors if in release mode, fuck glGetError() ;)
        // ALSO WHAT THE FUCK SILK.NET
        // here, Glfw.GetApi() does not work, you have to use THIS so the context flags get picked up
        // which moron thought it shouldn't work?
        #if !DEBUG
        //GlfwProvider.GLFW.Value.WindowHint(WindowHintBool.ContextNoError, true);
        #endif
        window = Window.Create(windowOptions);
        setTitle("BlockGame", splash, "");
        window.Load += init;
        window.FocusChanged += focus;
        //window.Update += update;
        window.Render += mainLoop;
        window.FramebufferResize += resize;
        window.Closing += close;

        window.Run();
    }

    private string getRandomSplash() {
        return splashes[clientRandom.Next(splashes.Length)];
    }

    public static void setTitle(string baseTitle, string splash, string addition) {
        window.Title = $"{baseTitle} - {splash} {addition}";
    }

    private void focus(bool given) {
        if (currentScreen != Screen.GAME_SCREEN) {
            return;
        }

        if (!given && !world.inMenu) {
            Screen.GAME_SCREEN.pause();
        }
        else {
            //Screen.GAME_SCREEN.backToGame();
        }
    }

    private void GLDebug(GLEnum source, GLEnum type, int id, GLEnum severity, int length, IntPtr message,
        IntPtr userparam) {
        string msg = Marshal.PtrToStringAnsi(message, length)!;
        Console.Out.WriteLine($"{source} [{severity}] ({id}): {type}, {msg}");
        // Dump stacktrace
        Console.Out.WriteLine(Environment.StackTrace);
    }

    private void init() {
        //  set icon
        setIconToBlock();
        input = window.CreateInput();
        GL = window.CreateOpenGL();
        //#if DEBUG
        // initialise debug print
        unsafe {
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageCallback(GLDebug, 0);
            #if DEBUG
            GL.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, (uint*)0, true);
            GL.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DebugSeverityNotification, 0, (uint*)0, false);
            #else
            // stop buffer source spam
            GL.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, (uint*)0, true);
            GL.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DebugSeverityNotification, 0, (uint*)0, false);
            #endif
            //#endif

            // send a test debug message
            #if DEBUG
            var str = "Debug message test";
            GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeOther, 0,
                 DebugSeverity.DebugSeverityHigh, (uint)str.Length, str);
            #endif

            GL.GetInteger(GetPName.ContextFlags, out int noErrors);
            Console.Out.WriteLine($"GL no error: {(noErrors & (int)GLEnum.ContextFlagNoErrorBit) != 0}");

            GL.GetInteger(GetPName.ContextFlags, out int robust);
            Console.Out.WriteLine($"GL robust: {robust} {(robust & (int)GLEnum.ContextFlagRobustAccessBit) != 0}");
        }

        Configuration.Default.PreferContiguousImageBuffers = true;
        proc = Process.GetCurrentProcess();
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationModeEXT.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);

        //GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.CullFace(GLEnum.Back);

        GL.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

        
        graphics = new Graphics();
        
        textureManager = new TextureManager(GL);
        
        g_texelStepLocation = graphics.fxaaShader.getUniformLocation("u_texelStep");
        g_showEdgesLocation = graphics.fxaaShader.getUniformLocation("u_showEdges");
        g_fxaaOnLocation = graphics.fxaaShader.getUniformLocation("u_fxaaOn");

        g_lumaThresholdLocation = graphics.fxaaShader.getUniformLocation("u_lumaThreshold");
        g_mulReduceLocation = graphics.fxaaShader.getUniformLocation("u_mulReduce");
        g_minReduceLocation = graphics.fxaaShader.getUniformLocation("u_minReduce");
        g_maxSpanLocation = graphics.fxaaShader.getUniformLocation("u_maxSpan");

        graphics.fxaaShader.setUniform(g_showEdgesLocation, 0);
        graphics.fxaaShader.setUniform(g_lumaThresholdLocation, g_lumaThreshold);
        graphics.fxaaShader.setUniform(g_mulReduceLocation, 1.0f / g_mulReduceReciprocal);
        graphics.fxaaShader.setUniform(g_minReduceLocation, 1.0f / g_minReduceReciprocal);
        graphics.fxaaShader.setUniform(g_maxSpanLocation, g_maxSpan);


        // needed for stupid laptop GPUs
        #if !DEBUG && LAPTOP_SUPPORT
            initDirectX();
        #endif

        mouse = input.Mice[0];
        mouse.MouseMove += onMouseMove;
        mouse.MouseDown += onMouseDown;
        mouse.MouseUp += onMouseUp;
        mouse.Scroll += onMouseScroll;
        mouse.Cursor.CursorMode = CursorMode.Normal;

        keyboard = input.Keyboards[0];
        keyboard.KeyDown += onKeyDown;
        keyboard.KeyRepeat += onKeyRepeat;
        keyboard.KeyUp += onKeyUp;
        keyboard.KeyChar += onKeyChar;
        focused = true;

        width = window.FramebufferSize.X;
        height = window.FramebufferSize.Y;

        metrics = new Metrics();
        stopwatch.Start();
        permanentStopwatch.Start();

        // SFML
        // don't use local variables, they go out of scope so nothing plays..... hold them statically
        //var file = File.ReadAllBytes("snd/tests.flac");
        //buffer = new SoundBuffer(file);
        //music = new Sound(buffer);
        //music.Loop = true;
        //music.Play();
        Console.Out.WriteLine("played?");
        
        gui = new GUI();
        renderer = new WorldRenderer();

        currentScreen = new MainMenuScreen();
        setMenu(Menu.LOADING);
        fontLoader = new FontLoader("fonts/8x13.bdf", "fonts/6x13.bdf");
        gui.loadFont(13);

        Block.preLoad();

        //RuntimeHelpers.PrepareMethod(typeof(ChunkSectionRenderer).GetMethod("constructVertices", BindingFlags.NonPublic | BindingFlags.Instance)!.MethodHandle);

        Console.Out.WriteLine("Loaded ASCII font.");
        switchTo(Menu.MAIN_MENU);
        Block.postLoad();
        resize(new Vector2D<int>(width, height));
        // GC after the whole font business - stitching takes hundreds of megs of heap, the game doesn't need that much
        MemoryUtils.cleanGC();
    }

    private void setIconToBlock() {
        using var logo = Image.Load<Rgba32>("logo.png");
        var success = logo.DangerousTryGetSinglePixelMemory(out var imgData);
        if (!success) {
            Console.Out.WriteLine("Couldn't set window logo!");
        }

        // yes, this is a piece of shit code copying the pixels all over the place
        // but we only do it once to set the logo so idc
        var img = new RawImage(logo.Width, logo.Height,
            new Memory<byte>(MemoryMarshal.Cast<Rgba32, byte>(imgData.Span).ToArray()));
        window.SetWindowIcon(ref img);
    }


    // don't actually use unless you are an idiot
    private void setMenu(Menu menu) {
        currentScreen.currentMenu = menu;
        menu.size = new Vector2I(width, height);
        menu.centre = menu.size / 2;
        menu.activate();
        menu.resize(new Vector2I(width, height));
    }

    /// <summary>
    /// Clears the entire screenstack and pushes the menu.
    /// </summary>
    public void switchTo(Menu menu) {
        currentScreen.currentMenu.deactivate();
        setMenu(menu);
    }

    /// <summary>
    /// Clears the entire screenstack and pushes the menu.
    /// </summary>
    public void switchToScreen(Screen screen) {
        currentScreen.deactivate();
        currentScreen = screen;
        screen.size = new Vector2I(width, height);
        screen.centre = screen.size / 2;
        screen.activate();
        screen.resize(new Vector2I(width, height));
    }

    public partial class NV1 {
        [LibraryImport("nvapi.dll", EntryPoint = "nvapi_QueryInterface")]
        internal static partial int NvAPI_Initialize();
    }

    public static partial class NV2 {
        [LibraryImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface")]
        internal static partial int NvAPI_Initialize();
    }

    public static void initDedicatedGraphics() {
        // fuck integrated GPUs, we want the dedicated card
        try {
            if (Environment.Is64BitProcess) {
                NativeLibrary.Load("nvapi64.dll");
                NV2.NvAPI_Initialize();
            }
            else {
                NativeLibrary.Load("nvapi.dll");
                NV1.NvAPI_Initialize();
            }
        }
        catch (Exception e) {
            // nothing!
            Console.Out.WriteLine("Well, apparently there is no nVidia");
        }
    }

    #if LAPTOP_SUPPORT
    public static void initDirectX() {
        unsafe {
            try {
                const bool forceDxvk = false;

                DXGI dxgi = null!;
                D3D11 d3d11 = null!;

                ComPtr<ID3D11Device> device = default;
                ComPtr<ID3D11DeviceContext> deviceContext = default;

                dxgi = DXGI.GetApi(window, forceDxvk);
                d3d11 = D3D11.GetApi(window, forceDxvk);

                // Create our D3D11 logical device.
                SilkMarshal.ThrowHResult
                (
                    d3d11.CreateDevice
                    (
                        default(ComPtr<IDXGIAdapter>),
                        D3DDriverType.Hardware,
                        Software: default,
                        (uint)CreateDeviceFlag.None,
                        null,
                        0,
                        D3D11.SdkVersion,
                        ref device,
                        null,
                        ref deviceContext
                    )
                );
                Console.Out.WriteLine("Successfully setup DirectX!");
            }
            catch (Exception e) {
                Console.Out.WriteLine("Couldn't setup DirectX!");
            }
        }
    }
    #endif

    private void onMouseMove(IMouse m, Vector2 pos) {
        currentScreen.onMouseMove(m, pos);
    }

    private void onMouseDown(IMouse m, MouseButton button) {
        currentScreen.onMouseDown(m, button);
    }

    public void lockMouse() {
        mouse.Cursor.CursorMode = CursorMode.Disabled;
        //mouse.Position = new Vector2(centre.X, centre.Y);
        focused = true;
        firstFrame = true;
    }

    public void unlockMouse() {
        mouse.Cursor.CursorMode = CursorMode.Normal;
        focused = false;
    }

    private void onMouseUp(IMouse m, MouseButton button) {
        currentScreen.onMouseUp(mousePos);
    }

    private void onMouseScroll(IMouse m, ScrollWheel scroll) {
        currentScreen.scroll(m, scroll);
    }

    private void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.F7 && keyboard.IsKeyPressed(Key.F6)) {
            Console.Out.WriteLine("Crashing game!");
            executeOnMainThread(() =>
                throw new Exception("Manual crash!")
            );
        }

        currentScreen.onKeyDown(keyboard, key, scancode);
    }

    private void onKeyRepeat(IKeyboard keyboard, Key key, int scancode) {
        currentScreen.onKeyRepeat(keyboard, key, scancode);
    }

    private void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
        currentScreen.onKeyUp(keyboard, key, scancode);
    }

    private void onKeyChar(IKeyboard keyboard, char c) {
        currentScreen.onKeyChar(keyboard, c);
    }

    public void resize(Vector2D<int> size) {
        var s = new Vector2I(size.X, size.Y);
        graphics.resize(size);
        fontLoader.renderer3D.OnViewportChanged(s);
        width = size.X;
        height = size.Y;

        // don't allow the screen to be too small
        recalcGUIScale();
        gui.resize(s);
        currentScreen.resize(s);

        if (Settings.instance.framebufferEffects) {
            genFramebuffer();
        }
    }

    private void recalcGUIScale() {
        var guiScaleTarget = 2;
        while (guiScaleTarget < Settings.instance.guiScale && width / ((double)guiScaleTarget + 1) >= 300 &&
               height / ((double)guiScaleTarget + 1) >= 200) {
            guiScaleTarget++;
        }

        GUI.guiScale = guiScaleTarget;
    }

    public void resize() {
        resize(new Vector2D<int>(width, height));
    }

    public void updateFramebuffers() {
        if (Settings.instance.framebufferEffects) {
            genFramebuffer();
        }
        else {
            deleteFramebuffer();
        }
    }

    private unsafe void genFramebuffer() {
        GL.DeleteFramebuffer(fbo);
        fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        GL.Viewport(0, 0, (uint)width, (uint)height);

        GL.DeleteTexture(FBOtex);
        FBOtex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, FBOtex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, null);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        GL.DeleteRenderbuffer(depthBuffer);
        depthBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, (uint)width,
            (uint)height);

        // Attach the color buffer ...
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, FBOtex, 0);

        // ... and the depth buffer,
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, depthBuffer);

        // Check if the framebuffer is complete
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
            throw new Exception("Framebuffer is not complete");
        }

        graphics.fxaaShader.use();

        GL.Uniform2(g_texelStepLocation, 1.0f / width, 1.0f / width);

        throwawayVAO = GL.CreateVertexArray();
    }

    private void deleteFramebuffer() {
        GL.DeleteFramebuffer(fbo);
        GL.DeleteTexture(FBOtex);
        GL.DeleteRenderbuffer(depthBuffer);
        GL.DeleteVertexArray(throwawayVAO);
    }

    public void executeOnMainThread(Action action) {
        mainThreadQueue.Add(action);
    }

    private void update(double dt) {
        //var before = permanentStopwatch.ElapsedMilliseconds;
        //dt = Math.Min(dt, 0.2);
        /*var vec = new Vector2I(0, 0);
        Console.Out.WriteLine(window.PointToClient(vec));
        Console.Out.WriteLine(window.PointToFramebuffer(vec));
        Console.Out.WriteLine(window.PointToScreen(vec));*/
        mousePos = mouse.Position;
        textureManager.blockTexture.update(dt);
        currentScreen.update(dt);
        //var after = permanentStopwatch.ElapsedMilliseconds;
        //Console.Out.WriteLine(after - before);
    }

    /// <summary>
    /// Now the main loop which calls <see cref="update"/> too.
    /// </summary>
    /// <param name="dt">dt as fractions of a second. 1 = 1s</param>
    private void mainLoop(double dt) {
        dt = Math.Min(dt, maxTimestep);
        accumTime += dt;
        //var i = 0;
        while (accumTime >= fixeddt) {
            update(fixeddt);
            t += fixeddt;
            accumTime -= fixeddt;
            //i++;
        }

        //Console.Out.WriteLine($"{i} updates called");
        // get remaining time between 0 and 1
        var interp = accumTime / fixeddt;
        actualRender(dt, interp);
    }

    private void actualRender(double dt, double interp) {
        /*if (dt > 0.016) {
            Console.Out.WriteLine("Missed a frame!");
        }*/
        // consume main thread actions
        while (mainThreadQueue.TryTake(out var action)) {
            action();
        }

        handleTimers();

        if (stopwatch.ElapsedMilliseconds > 1000) {
            ft = dt;
            fps = (int)(1 / ft);
            setTitle("BlockGame", splash, $"{fps} ({ft * 1000:0.##}ms)");
            stopwatch.Restart();
        }
        else {
            // Still update frametime for graph even if we don't update the title
            ft = dt;
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Settings.instance.framebufferEffects ? fbo : 0);

        graphics.mainBatch.Begin();
        fontLoader.renderer3D.begin();
        graphics.immediateBatch.Begin(BatcherBeginMode.Immediate);

        GL.Enable(EnableCap.DepthTest);
        currentScreen.clear(dt, interp);
        currentScreen.render(dt, interp);
        currentScreen.postRender(dt, interp);
        fontLoader.renderer3D.end();

        if (Settings.instance.framebufferEffects) {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, FBOtex);
        }

        if (Settings.instance.framebufferEffects) {
            graphics.fxaaShader.use();
            graphics.fxaaShader.setUniform(g_fxaaOnLocation, Settings.instance.fxaa);

            GL.BindVertexArray(throwawayVAO);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        GL.Disable(EnableCap.DepthTest);
        //GL.Disable(EnableCap.CullFace);

        // for GUI, no depth test
        //GD.BlendingEnabled = true;

        currentScreen.draw();
        currentScreen.postDraw();
        graphics.mainBatch.End();
        graphics.immediateBatch.End();
        //Console.Out.WriteLine(((InstantShader)graphics.mainBatch.shader).MVP);
        //GD.BlendingEnabled = false;
        GL.Enable(EnableCap.DepthTest);
    }

    public static TimerAction setInterval(long interval, Action action) {
        var now = permanentStopwatch.ElapsedMilliseconds;
        var ta = new TimerAction(action, now, true, interval);
        timerQueue.Add(ta);
        return ta;
    }

    public static TimerAction setTimeout(long timeout, Action action) {
        var now = permanentStopwatch.ElapsedMilliseconds;
        var ta = new TimerAction(action, now, false, timeout);
        timerQueue.Add(ta);
        return ta;
    }

    public static void clearInterval(TimerAction action) {
        timerQueue.Remove(action);
    }

    private static void handleTimers() {
        foreach (var timerAction in timerQueue) {
            var now = permanentStopwatch.ElapsedMilliseconds;
            if (timerAction.enabled && now - timerAction.lastCalled > timerAction.interval) {
                timerAction.action();
                if (!timerAction.repeating) {
                    // mark for removal
                    timerAction.enabled = false;
                }
                // schedule it again
                else {
                    timerAction.lastCalled = now;
                }
            }
        }

        // remove inactive timers
        timerQueue.RemoveAll(ta => !ta.enabled);
    }

    private void close() {
        buffer?.Dispose();
        music?.Dispose();
    }
}