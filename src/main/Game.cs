using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.snd;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.font;
using Molten;
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
using Monitor = Silk.NET.Windowing.Monitor;
using MouseButton = Silk.NET.Input.MouseButton;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;
using VideoMode = Silk.NET.Windowing.VideoMode;

namespace BlockGame;

public partial class Game {
    public static Game instance;

    public static int width;
    public static int height;

    public static IWindow window;
    public static Silk.NET.OpenGL.GL GL = null!;
    public static IInputContext input = null!;

    public static Process proc;

    /// <summary>
    /// Stop logspam
    /// </summary>
    public static bool shutUp = false;

    /// <summary>
    /// The current game screen which is shown.
    /// </summary>
    public Screen currentScreen;

    public static bool devMode;

    public static Graphics graphics;
    public static GUI gui;

    public static TextureManager textureManager;
    public static Metrics metrics;

    public static FontLoader fontLoader;
    public static SoundEngine snd;

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

    public static XRandom random = new XRandom();
    public static XRandom clientRandom = new XRandom();

    public static Stopwatch stopwatch = new();

    /// <summary>
    /// Stopwatch but keeps running
    /// </summary>
    public static Stopwatch permanentStopwatch = new();

    public double accumTime;
    public static readonly double fixeddt = 1 / 60d;
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

    public BlockingCollection<Action> mainThreadQueue = new();

    private Vector2D<int> preFullscreenSize;
    private Vector2D<int> preFullscreenPosition;
    private WindowState preFullscreenState;

    private readonly string[] splashes;
    private readonly string splash;

    private uint fbo;
    private uint FBOtex;
    private uint throwawayVAO;
    private uint depthBuffer;
    
    // MSAA resolve framebuffer (for MSAA -> regular texture)
    private uint resolveFbo;
    private uint resolveTex;

    private int g_texelStepLocation;
    private int g_showEdgesLocation;
    private int g_fxaaOnLocation;
    private int g_ssaaFactorLocation;
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

    public static int centreX => width / 2;
    public static int centreY => height / 2;


    public Game(bool devMode) {
        Game.devMode = devMode;
        instance = this;

        regNativeLib();
        sigHandler();

        // load splashes
        splashes = File.ReadAllLines("assets/splashes.txt");

        var windowOptions = WindowOptions.Default;
        //windowOptions.FramesPerSecond = 6000;
        //windowOptions.UpdatesPerSecond = 6000;
        windowOptions.VSync = false;
        splash = getRandomSplash();
        
        
        IMonitor mainMonitor = Monitor.GetMainMonitor(null);
        Vector2D<int> windowSize = GetNewWindowSize(mainMonitor);
        
        windowOptions.Size = new Vector2D<int>(Constants.initialWidth, Constants.initialHeight);
        windowOptions.VideoMode = new VideoMode(windowSize);
        windowOptions.ShouldSwapAutomatically = true;
        windowOptions.IsVisible = true;
        windowOptions.PreferredDepthBufferBits = 32;
        windowOptions.PreferredStencilBufferBits = 0;
        var api = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Compatability, ContextFlags.Default | ContextFlags.Debug,
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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            GlfwProvider.UninitializedGLFW.Value.InitHint((InitHint)0x00050003, 0x00060004);
        }

        //windowOptions.Samples = 4;
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
        
        window.Initialize();
        window.Run(runCallback);
        
        window.DoEvents();
        window.Reset();
    }
    
    /// <summary>
    /// Calculates the size to use for a new window as two thirds the size of the main monitor.
    /// </summary>
    /// <param name="monitor">The monitor in which the window will be located.</param>
    private static Vector2D<int> GetNewWindowSize(IMonitor monitor)
    {
        return (monitor.VideoMode.Resolution ?? monitor.Bounds.Origin);
    }

    private static void runCallback() {
        window.DoEvents();
            if (!window.IsClosing)
            {
                window.DoUpdate();
            }
            if (!window.IsClosing)
            {
                window.DoRender();
                //window.SwapBuffers();
                //GL.Finish();
                //GL.Flush();
            }
    }

    private static PosixSignalRegistration reg;
    private static PosixSignalRegistration reg2;

    private static sigaction_t sa;
    private static sigaction_t sa2;

    private static void sigSegvHandler(PosixSignalContext psc) {
        //psc.Cancel = true;
        Console.WriteLine("SIGSEGV in managed thread");
        Console.Out.Flush();
        //Environment.Exit(139);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct sigaction_t {
        public IntPtr sa_handler;
        public ulong sa_mask;
        public int sa_flags;
        public IntPtr sa_restorer;
    }

    [LibraryImport("libc", SetLastError = true)]
    private static partial int sigaction(int signum, ref sigaction_t act, IntPtr oldact);

    [UnmanagedCallersOnly(CallConvs = new[] {typeof(CallConvCdecl)})]
    static void sigSegvHandler(int signal) {
        // can't reliably use Console.WriteLine here, use write() syscall
        byte[] msg = "SIGSEGV caught\n"u8.ToArray();
        write(2, in msg[0], (nuint)msg.Length); // stderr
        fsync(2); // flush stderr 
        //_exit(139);
    }
    
    [LibraryImport("libc")]
    private static partial int fsync(int fd);

    [LibraryImport("libc")]
    private static partial nint write(int fd, in byte buf, nuint count);

    [LibraryImport("libc")]
    private static partial void _exit(int status);
    

    private unsafe void sigHandler() {
        if (OperatingSystem.IsLinux()) {
            /*
            // todo this shit doesn't work..... why?
            //reg = PosixSignalRegistration.Create((PosixSignal)11, sigSegvHandler);
            //reg2 = PosixSignalRegistration.Create((PosixSignal)6, sigSegvHandler);
            
            sa = new sigaction_t {
                sa_handler = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&sigSegvHandler,
                sa_flags = 0x40000000 // SA_RESTART
            };
            sigaction(11, ref sa, IntPtr.Zero);
            sa2 = new sigaction_t {
                sa_handler = (IntPtr)(delegate* unmanaged[Cdecl]<int, void>)&sigSegvHandler,
                sa_flags = 0x40000000 // SA_RESTART
            };
            sigaction(6, ref sa2, IntPtr.Zero);
            */
        }
    }


    private static void regNativeLib() {
        //NativeLibrary.SetDllImportResolver(typeof(Game).Assembly, nativeLibPath);
        //NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly, nativeLibPath);
    }
    
    private static IntPtr nativeLibPath(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
        string arch = RuntimeInformation.OSArchitecture == Architecture.X64 ? "x64" : "x86";

        // Create path to libs
        string libsPath = Path.Combine(AppContext.BaseDirectory, "libs", arch);
        string libraryPath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            libraryPath = Path.Combine(libsPath, $"{libraryName}.dll");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            // macOS has *one* path regardless of architecture
            libsPath = Path.Combine(AppContext.BaseDirectory, "libs", "osx");
            libraryPath = Path.Combine(libsPath, $"lib{libraryName}.dylib");
        }
        // Linux
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            libraryPath = Path.Combine(libsPath, $"lib{libraryName}.so");
        }
        else {
            throw new PlatformNotSupportedException($"Platform {RuntimeInformation.OSDescription} is not supported.");
        }

        // Try to load the library if it exists
        if (File.Exists(libraryPath)) {
            if (!shutUp) {
                Console.WriteLine($"Loading native library: {libraryName} {libraryPath}");
            }
            shutUp = true;
            return NativeLibrary.Load(libraryPath);
        }

        // Fallback to default resolution
        Console.WriteLine($"Couldn't find native library {libraryName}, falling back to default resolution");
        return IntPtr.Zero;
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
        //Console.Out.WriteLine(Environment.StackTrace);
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
        g_ssaaFactorLocation = graphics.fxaaShader.getUniformLocation("u_ssaaFactor");

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

        snd = new SoundEngine();

        var music = snd.playMusic("snd/tests.flac");
        snd.setLoop(music, true);

        // Keep the console application running until playback finishes
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
        
        // apply fullscreen setting
        setFullscreen(Settings.instance.fullscreen);
        
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
        currentScreen.onMouseUp(mousePos, button);
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

        if (key == Key.F11) {
            Settings.instance.fullscreen = !Settings.instance.fullscreen;
            setFullscreen(Settings.instance.fullscreen);
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
        GL.DeleteFramebuffer(resolveFbo);
        
        var ssaaWidth = width * Settings.instance.ssaa;
        var ssaaHeight = height * Settings.instance.ssaa;
        var samples = Settings.instance.msaa;
        
        GL.Viewport(0, 0, (uint)ssaaWidth, (uint)ssaaHeight);

        if (samples > 1) {
            // Create MSAA framebuffer
            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            // Create multisampled color texture
            GL.DeleteTexture(FBOtex);
            FBOtex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DMultisample, FBOtex);
            GL.TexImage2DMultisample(TextureTarget.Texture2DMultisample, (uint)samples, InternalFormat.Rgba8, (uint)ssaaWidth, (uint)ssaaHeight, true);

            // Create multisampled depth buffer
            GL.DeleteRenderbuffer(depthBuffer);
            depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, (uint)samples, InternalFormat.DepthComponent, (uint)ssaaWidth, (uint)ssaaHeight);

            // Attach to MSAA framebuffer
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, FBOtex, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new Exception("MSAA Framebuffer is not complete");
            }

            // Create resolve framebuffer for post-processing
            resolveFbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, resolveFbo);

            GL.DeleteTexture(resolveTex);
            resolveTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, resolveTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)ssaaWidth, (uint)ssaaHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, resolveTex, 0);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new Exception("Resolve Framebuffer is not complete");
            }
        } else {
            // Regular framebuffer (no MSAA)
            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            GL.DeleteTexture(FBOtex);
            FBOtex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, FBOtex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)ssaaWidth, (uint)ssaaHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            GL.DeleteRenderbuffer(depthBuffer);
            depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, (uint)ssaaWidth, (uint)ssaaHeight);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, FBOtex, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete) {
                throw new Exception("Framebuffer is not complete");
            }

            resolveFbo = 0;
            resolveTex = 0;
        }

        graphics.fxaaShader.use();
        GL.Uniform2(g_texelStepLocation, 1.0f / ssaaWidth, 1.0f / ssaaHeight);

        throwawayVAO = GL.CreateVertexArray();
    }

    private void deleteFramebuffer() {
        GL.DeleteFramebuffer(fbo);
        GL.DeleteTexture(FBOtex);
        GL.DeleteRenderbuffer(depthBuffer);
        GL.DeleteFramebuffer(resolveFbo);
        GL.DeleteTexture(resolveTex);
        GL.DeleteVertexArray(throwawayVAO);
    }

    public void executeOnMainThread(Action action) {
        mainThreadQueue.Add(action);
    }

    public void setFullscreen(bool fullscreen) {
        
        if (fullscreen == (window.WindowState == WindowState.Fullscreen)) {
            Console.Out.WriteLine("Already in desired state, returning");
            return;
        }

        var windowMonitor = window.Monitor;
        if (windowMonitor == null) {
            Console.Out.WriteLine("Failed to switch to fullscreen: window isn't on any monitor!");
            return;
        }

        // temporarily remove resize handler to prevent issues during switch
        window.FramebufferResize -= resize;

        if (fullscreen) {
            var screenSize = windowMonitor.VideoMode.Resolution ?? windowMonitor.Bounds.Size;
            preFullscreenSize = window.Size;
            preFullscreenPosition = window.Position;
            preFullscreenState = window.WindowState;
            
            // Force Normal state first, then Fullscreen
            if (window.WindowState != WindowState.Normal) {
                window.WindowState = WindowState.Normal;
            }
            window.WindowState = WindowState.Fullscreen;
            window.Size = screenSize;
        } else {
            Console.Out.WriteLine("Exiting fullscreen");
            if (preFullscreenSize.X < 10 || preFullscreenSize.Y < 10 || preFullscreenState == WindowState.Fullscreen) {
                preFullscreenSize = windowMonitor.Bounds.Size * 2 / 3;
                preFullscreenPosition = windowMonitor.Bounds.Origin + new Vector2D<int>(50);
                preFullscreenState = WindowState.Normal;
                Console.Out.WriteLine("Using fallback window settings");
            }

            // Always go to Normal first, then to the desired state
            window.WindowState = WindowState.Normal;
            window.Size = preFullscreenSize;
            window.Position = preFullscreenPosition;
            if (preFullscreenState != WindowState.Normal) {
                window.WindowState = preFullscreenState;
            }
        }

        // restore resize handler and trigger resize
        window.FramebufferResize += resize;
        resize(window.FramebufferSize);
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
        gui.update(dt);
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

    public static void yes() {
        Console.Out.WriteLine("yes");
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
        snd.update();

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

        // Set viewport for SSAA rendering
        if (Settings.instance.framebufferEffects) {
            var ssaaWidth = width * Settings.instance.ssaa;
            var ssaaHeight = height * Settings.instance.ssaa;
            GL.Viewport(0, 0, (uint)ssaaWidth, (uint)ssaaHeight);
        }

        graphics.mainBatch.Begin();
        fontLoader.renderer3D.begin();
        graphics.immediateBatch.Begin(BatcherBeginMode.Immediate);

        GL.Enable(EnableCap.DepthTest);
        currentScreen.clear(dt, interp);
        currentScreen.render(dt, interp);
        currentScreen.postRender(dt, interp);
        fontLoader.renderer3D.end();

        if (Settings.instance.framebufferEffects) {
            var ssaaWidth = width * Settings.instance.ssaa;
            var ssaaHeight = height * Settings.instance.ssaa;
            
            // Handle MSAA resolve if needed
            if (Settings.instance.msaa > 1) {
                // Resolve MSAA framebuffer to regular texture
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fbo);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, resolveFbo);
                GL.BlitFramebuffer(0, 0, ssaaWidth, ssaaHeight, 0, 0, ssaaWidth, ssaaHeight, 
                    ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
                
                // Use resolve texture for post-processing
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, resolveTex);
            } else {
                // Regular framebuffer path
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, FBOtex);
            }
            
            // Restore viewport for final screen rendering
            
            GL.Viewport(0, 0, (uint)width, (uint)height);
        }

        if (Settings.instance.framebufferEffects) {
            graphics.fxaaShader.use();
            graphics.fxaaShader.setUniform(g_fxaaOnLocation, Settings.instance.fxaa);
            graphics.fxaaShader.setUniform(g_ssaaFactorLocation, Settings.instance.ssaa);

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
        
        GL.Finish();
        GL.Flush();
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
        ///dev?.Dispose();
        //buffer?.Dispose();
    }
}
