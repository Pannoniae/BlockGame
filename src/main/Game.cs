using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.logic;
using BlockGame.render;
using BlockGame.snd;
using BlockGame.ui;
using BlockGame.ui.menu;
using BlockGame.ui.screen;
using BlockGame.util;
using BlockGame.util.font;
using BlockGame.util.log;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.item;
using Molten;
using SDL;
using Silk.NET.Core;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.ARB;
using Silk.NET.OpenGL.Legacy.Extensions.NV;
using Silk.NET.WGL;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using BatcherBeginMode = BlockGame.GL.BatcherBeginMode;
using DebugSeverity = Silk.NET.OpenGL.Legacy.DebugSeverity;
using DebugSource = Silk.NET.OpenGL.Legacy.DebugSource;
using DebugType = Silk.NET.OpenGL.Legacy.DebugType;
using Image = SixLabors.ImageSharp.Image;
using IWindow = Silk.NET.Windowing.IWindow;
using Monitor = Silk.NET.Windowing.Monitor;
using MouseButton = Silk.NET.Input.MouseButton;
using PrimitiveType = Silk.NET.OpenGL.Legacy.PrimitiveType;
using VideoMode = Silk.NET.Windowing.VideoMode;

namespace BlockGame.main;

public partial class Game {
    public static Game instance;

    public static int width;
    public static int height;

    public static IWindow window;
    public static Silk.NET.OpenGL.Legacy.GL GL = null!;
    private static IInputContext input = null!;
    
    
    private static WGL wgl;
    public static Glfw glfw;

    private readonly bool sdl;

    private static readonly bool windows = OperatingSystem.IsWindows();
    
    public static Process proc;

    public static Spy spy;

    private static int timerID;

    /// <summary>
    /// Stop logspam
    /// </summary>
    public static bool shutUp = false;

    /// <summary>
    /// The current game screen which is shown.
    /// </summary>
    public Screen currentScreen = null!;

    public static bool devMode;

    public static InputTracker inputs;

    public static Graphics graphics;
    public static GUI gui;
    public static BlockRenderer blockRenderer;

    public static Textures textures;
    public static Metrics metrics;
    public static Profiler profiler;

    public static FontLoader fontLoader;
    public static SoundEngine snd;

    public static World world;
    public static ClientPlayer player;
    public static WorldRenderer renderer = null!;

    private static Coroutines cs;

    public static IMouse mouse;
    public static Vector2 mousePos;
    public static IKeyboard keyboard;

    public Vector2 lastMousePos;
    public Vector3I? targetedPos;
    public Vector3I? previousPos;

    public static RayCollision raycast;

    public static GameMode gamemode;

    public int fps;
    public double ft;

    public static XRandom random = new XRandom();
    public static XRandom clientRandom = new XRandom();

    public static Stopwatch stopwatch = new();

    /// <summary>
    /// Stopwatch but keeps running
    /// </summary>
    public static Stopwatch permanentStopwatch = new();

    public static int globalTick = 0;

    public double accumTime;
    public static readonly double fixeddt = 1 / 60d;
    public static readonly double maxTimestep = 1 / 5f;
    public double t;

    /// <summary>
    /// List of things to do later.
    /// </summary>
    public static List<TimerAction> timerQueue = new();
    
    public static WorldThread worldThread;

    public static bool focused;
    public static bool firstFrame;

    public static bool mouseDisabled;

    /// <summary>
    /// True while clicking back into the game. Used to prevent the player instantly breaking a block when clicking on the menu to get back into the game world.
    /// </summary>
    public static bool lockingMouse;

    public BlockingCollection<Action> mainThreadQueue = new();

    private Vector2D<int> preFullscreenSize;
    private Vector2D<int> preFullscreenPosition;
    private WindowState preFullscreenState;

    private readonly string[] titles;
    private readonly string title;


    private static IntPtr hdc;
    public static bool noUpdate;
    
    /** debug flag for showing noise information in F3 screen */
    public static bool debugShowNoise = false;


    #if DEBUG
    public static string VERSION = "BlockGame v0.0.2 DEBUG";
    #else
    public static string VERSION = "BlockGame v0.0.2";
    #endif

    public static int centreX => width / 2;
    public static int centreY => height / 2;


    public Game(bool devMode) {
        Game.devMode = devMode;
        instance = this;

        SuperluminalPerf.Initialize(@"D:\programs\slp\API\dll\x64\PerformanceAPI.dll");

        #if DEBUG
        Log.init(minLevel: LogLevel.DEBUG);
        #else
        Log.init(minLevel: LogLevel.INFO);
        #endif
        Log.log(LogLevel.INFO, "Game", "Starting game!");

        regNativeLib();
        sigHandler();

        // load titles
        titles = File.ReadAllLines("assets/titles.txt");

        var windowOptions = WindowOptions.Default;
        //windowOptions.FramesPerSecond = 6000;
        //windowOptions.UpdatesPerSecond = 6000;
        windowOptions.VSync = false;
        title = getRandomSplash();


        IMonitor mainMonitor = Monitor.GetMainMonitor(null);
        Vector2D<int> windowSize = GetNewWindowSize(mainMonitor);

        windowOptions.Size = new Vector2D<int>(Constants.initialWidth, Constants.initialHeight);
        windowOptions.VideoMode = new VideoMode(windowSize);
        windowOptions.ShouldSwapAutomatically = false;
        windowOptions.IsVisible = true;
        windowOptions.WindowState = WindowState.Maximized;
        windowOptions.PreferredDepthBufferBits = 32;
        windowOptions.PreferredStencilBufferBits = 0;
        var api = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Compatability,
            ContextFlags.Default | ContextFlags.Debug,
            new APIVersion(4, 6));
        #if DEBUG
        api.Flags = ContextFlags.Debug;
        // if we are in debug mode, force x11 because stupid wayland doesn't work with renderdoc for debugging
        // GLFW_PLATFORM = 0x00050003
        // #define GLFW_PLATFORM_WAYLAND   0x00060003
        // #define GLFW_PLATFORM_X11   0x00060004

        #endif
        // force x11 in release too because wayland breaks everything
        // it even breaks the fucking mouse position
        if (false && RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            GlfwProvider.UninitializedGLFW.Value.InitHint((InitHint)0x00050003, 0x00060004);
        }

        //windowOptions.Samples = 4;
        windowOptions.API = api;

        sdl = true;
        if (sdl) {
            Window.PrioritizeSdl();
            // set hints
            SDL3.SDL_SetHint(SDL3.SDL_HINT_APP_NAME, "BlockGame");
            //SDL3.SDL_SetHint(SDL3.SDL_HINT_WINDOWS_GAMEINPUT, "1");

            // print SDL version!
            var a = SDL3.SDL_GetVersion();
            Log.info($"SDL version: { SDL3.SDL_VERSIONNUM_MAJOR(a)}.{SDL3.SDL_VERSIONNUM_MINOR(a)}.{SDL3.SDL_VERSIONNUM_MICRO(a)}");

        }
        else {
            Window.PrioritizeGlfw();
            #if DEBUG
        //GlfwProvider.GLFW.Value.WindowHint(WindowHintRobustness.ContextRobustness, Robustness.LoseContextOnReset);
            #endif
        }



        // no errors if in release mode, fuck glGetError() ;)
        // ALSO WHAT THE FUCK SILK.NET
        // here, Glfw.GetApi() does not work, you have to use THIS so the context flags get picked up
        // which moron thought it shouldn't work?
        #if !DEBUG
        //GlfwProvider.GLFW.Value.WindowHint(WindowHintBool.ContextNoError, true);
        #endif
        window = Window.Create(windowOptions);

        setTitle("BlockGame", title, "");
        window.Load += init;
        window.FocusChanged += focus;
        //window.Update += update;
        window.Render += mainLoop;
        window.FramebufferResize += resize;
        window.Closing += close;
        
        window.Initialize();

        // print API used
        Log.info($"Initialised {window.GetType()} window!");


        if (!sdl) {
            glfw = Glfw.GetApi();
            unsafe {
                glfw.SetWindowSizeLimits((WindowHandle*)window.Native!.Glfw,
                    Constants.minWidth, Constants.minHeight, Glfw.DontCare, Glfw.DontCare);
            }
        }

        if (windows) {
            hdc = window.Native!.Win32!.Value!.HDC;
        }

        if (!sdl) {
            // GLFW get version
            //Log.info($"GLFW version: {glfw.GetVersionString()}");
        }

        window.Run(runCallback);

        window.DoEvents();
        window.Reset();
    }

    /// <summary>
    /// Calculates the size to use for a new window as two thirds the size of the main monitor.
    /// </summary>
    /// <param name="monitor">The monitor in which the window will be located.</param>
    private static Vector2D<int> GetNewWindowSize(IMonitor monitor) {
        return monitor.VideoMode.Resolution ?? monitor.Bounds.Origin;
    }

    private static void runCallback() {
        profiler.startFrame();
        
        profiler.section(ProfileSectionName.Events);
        window.DoEvents();
        
        if (!window.IsClosing) {
            window.DoUpdate();
        }

        if (!window.IsClosing) {
            window.DoRender();
            
            //GlfwWindow
            //GL.Finish();
            //GL.Flush();
            
            profiler.section(ProfileSectionName.Swap);
            window.SwapBuffers();
        }
        
        // Store profiling data for this frame after swap is complete
        var profileData = profiler.endFrame();
        
        // Only update if we're in the game screen to avoid profiling menus
        if (instance.currentScreen == Screen.GAME_SCREEN) {
            ((GameScreen)instance.currentScreen).INGAME_MENU.UpdateProfileHistory(profileData);
        }
    }


    private string getRandomSplash() {
        return titles[clientRandom.Next(titles.Length)];
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

    private static void GLDebug(GLEnum _source, GLEnum _type, int id, GLEnum _severity, int length, IntPtr message,
        IntPtr userparam) {
        
        // convert types into something usable (probably a noop)
        var source = (DebugSource)_source;
        var type = (DebugType)_type;
        var severity = (DebugSeverity)_severity;
        
        var logLevel = severity switch {
            DebugSeverity.DebugSeverityHigh => LogLevel.ERROR,
            DebugSeverity.DebugSeverityMedium => LogLevel.WARNING,
            DebugSeverity.DebugSeverityLow => LogLevel.INFO,
            DebugSeverity.DebugSeverityNotification => LogLevel.DEBUG,
            _ => LogLevel.DEBUG
        };
        
        string msg = Marshal.PtrToStringAnsi(message, length)!;
        Log.log(logLevel, $"{source} [{type}] [{severity}] ({id}): {msg}");
        // Dump stacktrace
        //Console.Out.WriteLine(Environment.StackTrace);
        
        // sort by severity
        if (type is DebugType.DebugTypeError or DebugType.DebugTypeOther or DebugType.DebugTypePortability && 
            severity is DebugSeverity.DebugSeverityHigh or DebugSeverity.DebugSeverityMedium) {
            Log.log(LogLevel.INFO, Environment.StackTrace);
        }
    }

    /// <summary>
    /// Renders a single frame during startup to show loading progress
    /// </summary>
    private void jankyFrame() {
        window.DoEvents();
        
        // update the menu with a small dt to trigger animations
        currentScreen.update(0.016);
        
        // minimal rendering for startup screen
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, (uint)width, (uint)height);
        
        // bind default texture for GUI rendering
        graphics.tex(0, gui.colourTexture);
        
        // clear and render current screen
        currentScreen.clear(0.016, 0);
        
        GL.Disable(EnableCap.DepthTest);
        
        // render GUI
        graphics.mainBatch.Begin();
        graphics.immediateBatch.Begin(BatcherBeginMode.Immediate);
        
        currentScreen.draw();
        if (GUI.WIREFRAME) {
            foreach (var element in currentScreen.currentMenu.elements.Values) {
                gui.drawWireframe(element.bounds, Color4b.Red);
            }
        }
        currentScreen.postDraw();
        
        graphics.mainBatch.End();
        graphics.immediateBatch.End();
        
        GL.Enable(EnableCap.DepthTest);
        
        // swap buffers to display
        window.SwapBuffers();
    }

    private void init() {
        GL = window.CreateLegacyOpenGL();

        if (sdl) {
            unsafe {
                // todo why do I have to call SDL_Init here?? The thing is, if I do it BEFORE the window is created, we get a 1.1 context....
                // the input starting is needed to get unicode text input
                // this WILL break on mobile platforms and IME
                // we don't care about mobile but we will need to fix IME at some point;)
                SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO | SDL_InitFlags.SDL_INIT_VIDEO |
                              SDL_InitFlags.SDL_INIT_EVENTS |
                              SDL_InitFlags.SDL_INIT_GAMEPAD | SDL_InitFlags.SDL_INIT_JOYSTICK);
                SDL3.SDL_StartTextInput((SDL_Window*)window.Handle);
            }
        }

        // check for sample shading support (OpenGL 4.0+ or ARB_sample_shading extension)
        var version = GL.GetStringS(StringName.Version);
        Log.info("Running on OpenGL " + version);
        sampleShadingSupported = version.StartsWith("4.") || GL.TryGetExtension(out ArbSampleShading arbSampleShading);
        
        // check if this is an NVIDIA card
        var vendor = GL.GetStringS(StringName.Vendor);
        var rendererS = GL.GetStringS(StringName.Renderer);
        isNVCard = vendor.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase);
        
        Log.info($"GPU vendor: {vendor}");
        
        Log.info($"(NVIDIA: {isNVCard})");
        //isNVCard = false;
        
        // check if this is an AMD card
        isAMDCard = vendor.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                    vendor.Contains("ATI");
        
        Log.info($"(AMD: {isAMDCard})");
        
        //isAMDCard = true;
        
        // check for AMD integrated graphics
        isAMDIntegratedCard = isAMDCard && (vendor.Contains("Radeon Vega", StringComparison.OrdinalIgnoreCase) ||
                                           vendor.Contains("Radeon Graphics", StringComparison.OrdinalIgnoreCase) ||
                                           vendor.Contains("Radeon(TM) Graphics", StringComparison.OrdinalIgnoreCase)
                                           || rendererS.Contains("Radeon Vega", StringComparison.OrdinalIgnoreCase) ||
                                             rendererS.Contains("Radeon Graphics", StringComparison.OrdinalIgnoreCase) ||
                                           rendererS.Contains("Radeon(TM) Graphics", StringComparison.OrdinalIgnoreCase) ||
                                           (rendererS.Contains("Radeon", StringComparison.OrdinalIgnoreCase) && rendererS.Contains("Graphics", StringComparison.OrdinalIgnoreCase)));
        
        Log.info($"(AMD integrated: {isAMDIntegratedCard})");

        //isAMDIntegratedCard = true;
        
        // check if this is an Intel iGPU
        isIntelIntegratedCard = vendor.Contains("Intel", StringComparison.OrdinalIgnoreCase) &&
                                 (vendor.Contains("UHD", StringComparison.OrdinalIgnoreCase) ||
                                  vendor.Contains("Iris", StringComparison.OrdinalIgnoreCase) ||
                                  vendor.Contains("HD Graphics", StringComparison.OrdinalIgnoreCase));
        
        Log.info($"(Intel integrated: {isIntelIntegratedCard})");
        
        // check for NV shader buffer load support
        hasSBL = GL.TryGetExtension(out NVShaderBufferLoad nvShaderBufferLoad);
        sbl = nvShaderBufferLoad;
        Log.info($"NV_shader_buffer_load supported: {hasSBL}");
        //hasSBL = false;
        
        // check for NV vertex buffer unified memory support
        hasVBUM = GL.TryGetExtension(out NVVertexBufferUnifiedMemory nvVertexBufferUnifiedMemory);
        vbum = nvVertexBufferUnifiedMemory;
        Log.info($"NV_vertex_buffer_unified_memory supported: {hasVBUM}");
        //hasVBUM = false;
        
        // check for NV uniform buffer unified memory support
        hasUBUM = GL.IsExtensionPresent("NV_uniform_buffer_unified_memory");
        Log.info($"NV_uniform_buffer_unified_memory supported: {hasUBUM}");
        //hasUBUM = false;
        
        // check for gl_BaseInstance UBO rendering support (OpenGL 4.6)
        var ver = GL.GetStringS(StringName.Version);
        var majorVersion = int.Parse(ver.Split('.')[0]);
        var minorVersion = int.Parse(ver.Split('.')[1].Split(' ')[0]);
        hasInstancedUBO = (majorVersion > 4) || (majorVersion == 4 && minorVersion >= 6);
        Log.info($"gl_BaseInstance UBO rendering supported: {hasInstancedUBO}");
        //hasInstancedUBO = false;
        
        // if AMD integrated, disable it too because it doesn't work
        if (isIntelIntegratedCard) {
            Log.info("Disabling gl_BaseInstance UBO rendering on integrated GPUs!");
            hasInstancedUBO = false;
        }
        
        GL.TryGetExtension(out extbu);
        
        // check for NV_command_list support
        hasCMDL = GL.TryGetExtension(out NVCommandList nvCommandList);
        cmdl = nvCommandList;
        Log.info($"NV_command_list supported: {hasCMDL}");
        //hasCMDL = false;
        
        // check for NV_bindless_multi_draw_indirect support
        hasBindlessMDI = GL.TryGetExtension(out NVBindlessMultiDrawIndirect nvBindlessMultiDrawIndirect);
        bmdi = nvBindlessMultiDrawIndirect;
        Log.info($"NV_bindless_multi_draw_indirect supported: {hasBindlessMDI}");
        //hasBindlessMDI = false;
        
        // check for ARB_shading_language_include support  
        hasShadingLanguageInclude = GL.TryGetExtension(out ArbShadingLanguageInclude arbShadingLanguageInclude);
        arbInclude = arbShadingLanguageInclude;
        Log.info($"ARB_shading_language_include supported: {hasShadingLanguageInclude}");
        hasShadingLanguageInclude = false;
        
        // check for ARB_shader_draw_parameters support (OpenGL 4.6)
        hasShaderDrawParameters = (majorVersion > 4) || (majorVersion == 4 && minorVersion >= 6) ||
                                  GL.IsExtensionPresent("ARB_shader_draw_parameters");
        Log.info($"ARB_shader_draw_parameters supported: {hasShaderDrawParameters}");
        //hasShaderDrawParameters = false;
        
        // print all valid anti-aliasing modes
        printAntiAliasingModes();
        
        if (hasShadingLanguageInclude) {
            BlockGame.GL.Shader.initializeIncludeFiles();
        }

        //#if DEBUG
        // initialise debug print
        unsafe {
            GL.Enable(EnableCap.DebugOutput);
            //GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageCallback(GLDebug, 0);
            #if DEBUG
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, (uint*)0, true);
            GL.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DebugSeverityNotification, 0, (uint*)0, false);
            #else
            // stop buffer source spam
            GL.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, (uint*)0, true);
            GL.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DebugSeverityNotification, 0,
                (uint*)0, false);
            #endif
            //#endif

            // send a test debug message
            #if DEBUG
            var str = "Debug message test";
            GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeOther, 0,
                 DebugSeverity.DebugSeverityHigh, (uint)str.Length, str);
            #endif

            GL.GetInteger(GetPName.ContextFlags, out int noErrors);
            Log.info($"GL no error: {(noErrors & (int)GLEnum.ContextFlagNoErrorBit) != 0}");

            GL.GetInteger(GetPName.ContextFlags, out int robust);
            Log.info($"GL robust: {robust} {(robust & (int)GLEnum.ContextFlagRobustAccessBit) != 0}");
        }

        Configuration.Default.PreferContiguousImageBuffers = true;
        proc = Process.GetCurrentProcess();



        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.CullFace(GLEnum.Back);

        GL.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

        // init memoryutils!
        // the good thing is that we can yeet this off to another thread
        _ = Task.Run(MemoryUtils.init);
        
        // we load the settings FIRST so our graphics settings get picked up when initialising stuff
        Settings.instance.load();

        graphics = new Graphics();
        graphics.init();
        
        // setup depth testing with reverse-Z support
        graphics.setupDepthTesting();
        graphics.setupBlend();

        // BE CAREFUL! Reverse-Z fucks this if you don't invert
        // this used to be 1 and 2 but even then it z-fights on my card so increase
        // todo if someone complains, increase again
        graphics.polyOffset(-3f, -3f);

        camera = new Camera(Constants.initialWidth, Constants.initialHeight);
        
        // initialize viewport and projection for GUI rendering
        width = window.FramebufferSize.X;
        height = window.FramebufferSize.Y;
        graphics.resize(new Vector2D<int>(width, height));

        // Initialize FXAA shader uniforms
        g_fxaa_texelStepLocation = graphics.fxaaShader.getUniformLocation("u_texelStep");
        g_fxaa_showEdgesLocation = graphics.fxaaShader.getUniformLocation("u_showEdges");
        g_fxaa_lumaThresholdLocation = graphics.fxaaShader.getUniformLocation("u_lumaThreshold");
        g_fxaa_mulReduceLocation = graphics.fxaaShader.getUniformLocation("u_mulReduce");
        g_fxaa_minReduceLocation = graphics.fxaaShader.getUniformLocation("u_minReduce");
        g_fxaa_maxSpanLocation = graphics.fxaaShader.getUniformLocation("u_maxSpan");

        graphics.fxaaShader.setUniform(g_fxaa_showEdgesLocation, 0);
        graphics.fxaaShader.setUniform(g_fxaa_lumaThresholdLocation, g_lumaThreshold);
        graphics.fxaaShader.setUniform(g_fxaa_mulReduceLocation, 1.0f / g_mulReduceReciprocal);
        graphics.fxaaShader.setUniform(g_fxaa_minReduceLocation, 1.0f / g_minReduceReciprocal);
        graphics.fxaaShader.setUniform(g_fxaa_maxSpanLocation, g_maxSpan);
        
        // Initialize SSAA shader uniforms
        g_ssaa_texelStepLocation = graphics.ssaaShader.getUniformLocation("u_texelStep");
        g_ssaa_factorLocation = graphics.ssaaShader.getUniformLocation("u_ssaaFactor");
        g_ssaa_modeLocation = graphics.ssaaShader.getUniformLocation("u_ssaaMode");
        
        // Initialize CRT shader uniforms
        //g_crt_maskTypeLocation = graphics.crtShader.getUniformLocation("u_maskType");
        g_crt_curveLocation = graphics.crtShader.getUniformLocation("u_curve");
        g_crt_sharpnessLocation = graphics.crtShader.getUniformLocation("u_sharpness");
        g_crt_colorOffsetLocation = graphics.crtShader.getUniformLocation("u_colorOffset");
        g_crt_brightnessLocation = graphics.crtShader.getUniformLocation("u_brightness");
        g_crt_aspectLocation = graphics.crtShader.getUniformLocation("u_aspect");
        g_crt_minScanlineThicknessLocation = graphics.crtShader.getUniformLocation("u_minScanlineThickness");
        g_crt_wobbleStrengthLocation = graphics.crtShader.getUniformLocation("u_wobbleStrength");
        g_crt_timeLocation = graphics.crtShader.getUniformLocation("u_time");
        g_crt_scanlineResLocation = graphics.crtShader.getUniformLocation("u_scanlineRes");


        // needed for stupid laptop GPUs
        #if !DEBUG && LAPTOP_SUPPORT
            initDirectX();
        #endif
        
        focused = true;

        metrics = new Metrics();
        profiler = new Profiler();
        stopwatch.Start();
        permanentStopwatch.Start();
        globalTick = 0;

        snd = new SoundEngine();

        cs = new Coroutines();

        var music = snd.playMusic("snd/tests.ogg");
        music.loop = true;

        snd.muteMusic();

        // initialize the GUI first
        fontLoader = new FontLoader("fonts/BmPlus_IBM_VGA_9x16.otb", "fonts/BmPlus_IBM_VGA_9x16.otb");
        gui = new GUI();
        gui.loadFont(16);

        // NOW we can show the startup loading screen
        currentScreen = new MainMenuScreen();
        Menu.STARTUP_LOADING = new StartupLoadingMenu();
        setMenu(Menu.STARTUP_LOADING);
        //jankyFrame();
        
        //  set icon
        Menu.STARTUP_LOADING.updateProgress(0.1f, "Loading joy");
        setIconToBlock();
        input = window.CreateInput();
        
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


        var a = InputTracker.DUMMY;
        inputs = new InputTracker();
        
        Menu.STARTUP_LOADING.updateProgress(0.3f, "Loading textures");
        textures = new Textures(GL);
        Screen.init();
        Menu.init();
        
        Menu.STARTUP_LOADING.updateProgress(0.4f, "Loading fonts");
        jankyFrame();

        Menu.STARTUP_LOADING.updateProgress(0.5f, "Setting up renderer");
        //Thread.Sleep(1000);
        jankyFrame();
        renderer = new WorldRenderer();
        
        Menu.STARTUP_LOADING.updateProgress(0.7f, "Initializing block renderer");
        jankyFrame();
        blockRenderer = new BlockRenderer();

        Menu.STARTUP_LOADING.updateProgress(0.8f, "Loading blocks");
        jankyFrame();
        Block.preLoad();
        Item.preLoad();

        //RuntimeHelpers.PrepareMethod(typeof(ChunkSectionRenderer).GetMethod("constructVertices", BindingFlags.NonPublic | BindingFlags.Instance)!.MethodHandle);
        
        jankyFrame();
        Menu.STARTUP_LOADING.updateProgress(1.0f, "loaded!");
        jankyFrame();
        switchTo(Menu.MAIN_MENU);

        Block.postLoad();
        resize(new Vector2D<int>(width, height));

        Spy.init();

        // initialize game mode (default to creative for now)
        gamemode = new CreativeMode();

        // apply fullscreen setting
        setFullscreen(Settings.instance.fullscreen);

        // request focus so first click works?
        // todo do we really need this? it works inconsistently... need testing. my SDL layer might be fucked, sorry
        if (sdl) {
            unsafe {
                SDL3.SDL_RaiseWindow((SDL_Window*)window.Handle);
            }
        }

        // GC after the whole font business - stitching takes hundreds of megs of heap, the game doesn't need that much
        MemoryUtils.cleanGC();
    }

    private void setIconToBlock() {
        using var logo = Image.Load<Rgba32>("logo.png");
        var success = logo.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> imgData);
        if (!success) {
            Log.warn("Couldn't set window logo!");
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
    
    /**
     * Sets up a new world.
     * Also ensures the dependencies aren't messed up.
     *
     * Optionally, unloads everything if you pass null.
     */
    public static void setWorld(World? world) {
        
        // dispose of everything before
        
        Game.world?.Dispose();
        renderer.setWorld(null);
        blockRenderer.setWorld(null);
        
        // clear up the chunk cache!
        ArrayBlockData.blockPool.clear();
        ArrayBlockData.lightPool.clear();

        if (world != null) {
            Game.world = world;

            // setup auxiliary
            renderer.setWorld(world);
            blockRenderer.setWorld(world);
        }
    }

    public static Coroutine startCoroutine(IEnumerator coroutine) {
        return cs.start(coroutine);
    }

    public static Coroutine<T> startCoroutine<T>(IEnumerator<T> coroutine) {
        return cs.start(coroutine);
    }

    /** Start a typed coroutine, R is the type of the result. */
    public static TypedCoroutine<R> startCoroutine<R>(IEnumerator coroutine) {
        return cs.start<R>(coroutine);
    }

    public static Coroutine startCoroutineNextFrame(IEnumerator coroutine) {
        return cs.startNextFrame(coroutine);
    }

    public static Coroutine<T> startCoroutineNextFrame<T>(IEnumerator<T> coroutine) {
        return cs.startNextFrame(coroutine);
    }

    /** Start a typed coroutine, R is the type of the result. */
    public static TypedCoroutine<R> startCoroutineNextFrame<R>(IEnumerator coroutine) {
        return cs.startNextFrame<R>(coroutine);
    }

    private void onMouseMove(IMouse m, Vector2 pos) {
        currentScreen.onMouseMove(m, pos);
    }

    private void onMouseDown(IMouse m, MouseButton button) {
        currentScreen.onMouseDown(m, button);
    }

    public void lockMouse() {
        mouse.Cursor.CursorMode = CursorMode.Raw;
        //mouse.Position = new Vector2(centre.X, centre.Y);
        focused = true;
        firstFrame = true;
    }

    public void unlockMouse() {
        mouse.Cursor.CursorMode = CursorMode.Normal;
        focused = false;
    }

    private void onMouseUp(IMouse m, MouseButton button) {
        currentScreen.onMouseUp(mousePos, button);
    }

    private void onMouseScroll(IMouse m, ScrollWheel scroll) {
        currentScreen.scroll(m, scroll);
    }

    private void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.F7 && keyboard.IsKeyPressed(Key.F6)) {
            Log.error("Crashing game!");
            executeOnMainThread(() =>
                throw new InputException("Manual crash!")
            );
        }

        if (key == Key.F11) {
            Settings.instance.fullscreen = !Settings.instance.fullscreen;
            setFullscreen(Settings.instance.fullscreen);
        }
        
        // gui wireframe code needs to be here because we want it to apply on all screens


        // gui bounds debug - ctrl+numpad9
        if (key == Key.Keypad9 && keyboard.IsKeyPressed(Key.ControlLeft)) {
            GUI.SHOW_GUI_BOUNDS = !GUI.SHOW_GUI_BOUNDS;
            Log.info("Enabled GUI bounds: " + GUI.SHOW_GUI_BOUNDS);
        }

        else {
            if (key == Key.Keypad9) {
                GUI.WIREFRAME = !GUI.WIREFRAME;
                Log.info("Enabled GUI wireframe: " + GUI.WIREFRAME);
            }
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

    private static void recalcGUIScale() {
        var guiScaleTarget = 2;
        // status update! we target 1920x1080 as the minimum for guiScale 4 to look good.
        // so the "virtual" GUI size is 480x270.
        // but we only have 360 because 4:3 and shit
        // status update, it should be slightly smaller because title bar and shit so we cut 30 from it
        while (guiScaleTarget < Settings.instance.guiScale && width / ((double)guiScaleTarget + 1) >= 360 &&
               height / ((double)guiScaleTarget + 1) >= 240) {
            guiScaleTarget++;
        }
        
        // if resized, debug print
        if (guiScaleTarget != GUI.guiScale) {
            Log.info($"GUI scale changed from {GUI.guiScale} to {guiScaleTarget}");
        }

        GUI.guiScale = guiScaleTarget;
    }

    public void resize() {
        resize(new Vector2D<int>(width, height));
    }

    public void executeOnMainThread(Action action) {
        mainThreadQueue.Add(action);
    }

    private void update(double dt) {
        globalTick++;
        mousePos = mouse.Position;
        
        // reset events
        inputs.reset();
        
        textures.update(dt);
        currentScreen.update(dt);
        gui.update(dt);
    }

    /// <summary>
    /// Now the main loop which calls <see cref="update"/> too.
    /// </summary>
    /// <param name="dt">dt as fractions of a second. 1 = 1s</param> 
    private void mainLoop(double dt) {
        dt = Math.Min(dt, maxTimestep);
        accumTime += dt;
        //var i = 0;
        if (!noUpdate) {
            profiler.section(ProfileSectionName.Logic);
            while (accumTime >= fixeddt) {
                update(fixeddt);
                t += fixeddt;
                accumTime -= fixeddt;
                //i++;
            }
        }
        else {
            while (accumTime >= fixeddt) {
                t += fixeddt;
                accumTime -= fixeddt;
            }
        }

        //Console.Out.WriteLine($"{i} updates called");
        // get remaining time between 0 and 1
        var interp = accumTime / fixeddt;
        // when paused, use interp=1.0 so rendering uses current state without interpolation
        if (world?.paused == true) {
            interp = 1.0;
        }
        actualRender(dt, interp);
    }

    public static void yes() {
        Console.Out.WriteLine("yes");
    }

    private void actualRender(double dt, double interp) {
        /*if (dt > 0.016) {
            Console.Out.WriteLine("Missed a frame!");
        }*/
        
        profiler.section(ProfileSectionName.Other);
        // consume main thread actions
        while (mainThreadQueue.TryTake(out var action)) {
            action();
        }

        handleTimers();
        cs.updateFrame(dt);
        snd.update();

        if (stopwatch.ElapsedMilliseconds > 1000) {
            ft = dt;
            fps = (int)(1 / ft);
            setTitle("BlockGame", title, $"{fps} ({ft * 1000:0.##}ms)");
            stopwatch.Restart();
        }
        else {
            // Still update frametime for graph even if we don't update the title
            ft = dt;
        }
        
        //currentScreen.clear(dt, interp);

        if (Settings.instance.framebufferEffects) {
            // clear 0 as well
            // todo this fucks up with msaa and has blue fringing? need to investigate..
            // not anymore??
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            currentScreen.clear(dt, interp);
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Settings.instance.framebufferEffects ? fbo : 0);
        // clear AFTER binding the framebuffer
        // otherwise it won't clean shit
        currentScreen.clear(dt, interp);

        // Set viewport for SSAA/MSAA rendering
        if (Settings.instance.framebufferEffects) {
            var ssaaWidth = width * Settings.instance.effectiveScale;
            var ssaaHeight = height * Settings.instance.effectiveScale;
            GL.Viewport(0, 0, (uint)ssaaWidth, (uint)ssaaHeight);
        }
        
        GL.Enable(EnableCap.DepthTest);
        
        profiler.section(ProfileSectionName.World3D);
        if (currentScreen == Screen.GAME_SCREEN) {
            fontLoader.renderer3D.begin();
        }
        
        currentScreen.render(dt, interp);
        currentScreen.postRender(dt, interp);

        if (currentScreen == Screen.GAME_SCREEN) {
            fontLoader.renderer3D.end();
        }

        profiler.section(ProfileSectionName.PostFX);
        if (Settings.instance.framebufferEffects) {
            var ssaaWidth = width * Settings.instance.effectiveScale;
            var ssaaHeight = height * Settings.instance.effectiveScale;

            // Handle MSAA resolve if needed
            if (Settings.instance.msaa > 1) {
                // Resolve MSAA framebuffer to regular texture
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fbo);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, resolveFbo);
                GL.BlitFramebuffer(0, 0, ssaaWidth, ssaaHeight, 0, 0, ssaaWidth, ssaaHeight,
                    ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

                // Use resolve texture for post-processing
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                graphics.tex(0, resolveTex);
            }
            else {
                // Regular framebuffer path
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                graphics.tex(0, FBOtex);
            }

            // Restore viewport for final screen rendering

            GL.Viewport(0, 0, (uint)width, (uint)height);
        }

        if (Settings.instance.framebufferEffects) {
            // Select the appropriate post-processing shader
            if (Settings.instance.crtEffect) {
                graphics.crtShader.use();
                
                // Set CRT shader uniforms
                //graphics.crtShader.setUniform(g_crt_maskTypeLocation, 3); // Wide Grille by default
                graphics.crtShader.setUniform(g_crt_curveLocation, 0.1f); // Reduced from 0.5f for normal curvature
                graphics.crtShader.setUniform(g_crt_sharpnessLocation, 0.6666666f);
                graphics.crtShader.setUniform(g_crt_colorOffsetLocation, 0.0f);
                graphics.crtShader.setUniform(g_crt_brightnessLocation, 1.0f);
                graphics.crtShader.setUniform(g_crt_aspectLocation, (float)height / width);
                graphics.crtShader.setUniform(g_crt_minScanlineThicknessLocation, 0.5f);
                graphics.crtShader.setUniform(g_crt_wobbleStrengthLocation, 0.0f);
                graphics.crtShader.setUniform(g_crt_timeLocation, (float)(permanentStopwatch.ElapsedMilliseconds / 1000.0));
                graphics.crtShader.setUniform(g_crt_scanlineResLocation, height <= 1080 ? 240.0f : 360.0f); // 240 lines for 1080p, 360 for 1440p and stuff
            } else if (Settings.instance.fxaa) {
                graphics.fxaaShader.use();
            } else if (Settings.instance.ssaa > 1) {
                graphics.ssaaShader.use();
            } else {
                graphics.simplePostShader.use();
            }

            GL.BindVertexArray(throwawayVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        // clear depth for GUI!!
        GL.Clear(ClearBufferMask.DepthBufferBit);
        graphics.setDepthFunc();

        GL.Disable(EnableCap.DepthTest);
        //GL.Disable(EnableCap.CullFace);

        // for GUI, no depth test
        //GD.BlendingEnabled = true;
        
        profiler.section(ProfileSectionName.GUI);
        graphics.mainBatch.Begin();
        graphics.immediateBatch.Begin(BatcherBeginMode.Immediate);

        currentScreen.draw();
        currentScreen.postDraw();
        
        
        // if wireframe, draw bbox
        if (GUI.WIREFRAME) {
            foreach (var element in currentScreen.currentMenu.elements.Values) {
                gui.drawWireframe(element.bounds, Color4b.Red);
            }
        }
        
        graphics.mainBatch.End();
        graphics.immediateBatch.End();
        //Console.Out.WriteLine(((InstantShader)graphics.mainBatch.shader).MVP);
        //GD.BlendingEnabled = false;
        
        if (GUI.WIREFRAME) {
            graphics.mainBatch.Begin();
            foreach (var element in currentScreen.currentMenu.elements.Values) {
                gui.drawWireframe(element.bounds, Color4b.Red);
            }
            graphics.mainBatch.End();
        }

        // gui bounds debug overlay
        if (GUI.SHOW_GUI_BOUNDS) {
            graphics.mainBatch.Begin();
            gui.drawGUIBounds();
            graphics.mainBatch.End();
        }
        
        GL.Enable(EnableCap.DepthTest);

        //GL.Finish();
        //GL.Flush();
    }

    public static TimerAction setInterval(long interval, Action action) {
        var now = permanentStopwatch.ElapsedMilliseconds;
        var ta = new TimerAction(timerID++, action, now, true, interval);
        timerQueue.Add(ta);
        return ta;
    }

    public static TimerAction setTimeout(long timeout, Action action) {
        var now = permanentStopwatch.ElapsedMilliseconds;
        var ta = new TimerAction(timerID++, action, now, false, timeout);
        timerQueue.Add(ta);
        return ta;
    }

    public static void clearInterval(TimerAction action) {
        timerQueue.Remove(action);
    }
    
    public static void clearInterval(int id) {
        timerQueue.RemoveAll(ta => ta.id == id);
    }

    private static void handleTimers() {
        for (var i = 0; i < timerQueue.Count; i++) {
            var timerAction = timerQueue[i];
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
        Log.shutdown();
        ///dev?.Dispose();
        //buffer?.Dispose();
    }

    public static void mm() {
        mouseDisabled = !mouseDisabled;
        
        if (mouseDisabled) {
            mouse.MouseMove -= instance.onMouseMove;
        }
        else {
            mouse.MouseMove += instance.onMouseMove;
        }
    }
}