using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SFML.Audio;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using TrippyGL;
using TrippyGL.ImageSharp;
using DebugSeverity = Silk.NET.OpenGL.DebugSeverity;
using DebugSource = Silk.NET.OpenGL.DebugSource;
using DebugType = Silk.NET.OpenGL.DebugType;
using DepthFunction = TrippyGL.DepthFunction;
using MouseButton = Silk.NET.Input.MouseButton;
using Sound = SFML.Audio.Sound;

namespace BlockGame;

public partial class Game {

    public static Game instance { get; private set; }

    public static int width;
    public static int height;

    public static IWindow window;
    public static GL GL = null!;
    public static GraphicsDevice GD = null!;
    public static IInputContext input = null!;
    public ImGuiController imgui = null!;

    public static Process proc;

    public static int centreX => width / 2;
    public static int centreY => height / 2;

    /// <summary>
    /// The current game screen which is shown.
    /// </summary>
    public FIFOStack<Screen> screenStack;

    public static GUI gui;

    public static IMouse mouse;
    public static IKeyboard keyboard;

    public Vector2 lastMousePos;
    public Vector3D<int>? targetedPos;
    public Vector3D<int>? previousPos;

    public int fps;
    public double ft;

    public static BlendState initialBlendState = BlendState.NonPremultiplied;

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
    /// True while clicking back into the game. Used to prevent the player instantly breaking a block when clicking on the screen to get back into the game world.
    /// </summary>
    public static bool lockingMouse;

    public Texture2D blockTexture;
    public Texture2D lightTexture;
    public Metrics metrics;

    public BlockingCollection<Action> mainThreadQueue = new();

    private SoundBuffer buffer;
    private Sound music;


    public Game() {
        instance = this;
        var windowOptions = WindowOptions.Default;
        //windowOptions.FramesPerSecond = 6000;
        //windowOptions.UpdatesPerSecond = 6000;
        windowOptions.VSync = false;
        windowOptions.Title = "BlockGame";
        windowOptions.Size = new Vector2D<int>(Constants.initialWidth, Constants.initialHeight);
        windowOptions.PreferredDepthBufferBits = 32;
        var api = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 4));
        ;
#if DEBUG
        api.Flags = ContextFlags.Debug;
#endif
        windowOptions.API = api;
        Window.PrioritizeGlfw();
        window = Window.Create(windowOptions);
        window.Load += init;
        //window.Update += update;
        window.Render += mainLoop;
        window.FramebufferResize += resize;
        window.Closing += close;

        window.Run();
    }

    private void GLDebug(GLEnum source, GLEnum type, int id, GLEnum severity, int length, IntPtr message, IntPtr userparam) {
        string msg = Marshal.PtrToStringAnsi(message, length)!;
        Console.Out.WriteLine($"{source} [{severity}] ({id}): {type}, {msg}");
    }

    private void init() {
        input = window.CreateInput();
        GL = window.CreateOpenGL();
#if DEBUG
        // initialise debug print
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.DebugMessageCallback(GLDebug, 0);
        GL.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, 0, true);
#endif


        imgui = new ImGuiController(GL, window, input);
        proc = Process.GetCurrentProcess();
        GD = new GraphicsDevice(GL);
        GD.BlendingEnabled = true;
        GD.BlendState = initialBlendState;
        GD.DepthTestingEnabled = true;
        GD.DepthState = DepthState.Default;
        GD.DepthState.DepthComparison = DepthFunction.LessOrEqual;
        GD.FaceCullingEnabled = true;
        GD.PolygonFrontFace = PolygonFace.CounterClockwise;
        GD.CullFaceMode = CullingMode.CullBack;
        foreach (var mouse in input.Mice) {
            mouse.MouseMove += onMouseMove;
            mouse.MouseDown += onMouseDown;
            mouse.MouseUp += onMouseUp;
            mouse.Scroll += onMouseScroll;
            mouse.Cursor.CursorMode = CursorMode.Normal;
        }

        mouse = input.Mice[0];

        foreach (var keyboard in input.Keyboards) {
            keyboard.KeyDown += onKeyDown;
            keyboard.KeyUp += onKeyUp;
        }

        keyboard = input.Keyboards[0];
        focused = true;

        width = window.FramebufferSize.X;
        height = window.FramebufferSize.Y;

        metrics = new Metrics();
        stopwatch.Start();
        permanentStopwatch.Start();

        blockTexture = Texture2DExtensions.FromFile(GD, "textures/blocks.png");
        lightTexture = Texture2DExtensions.FromFile(GD, "textures/lightmap.png");
        screenStack = [];
        //Screen.initScreens(gui);
        Screen.switchTo(Screen.LOADING);
        //world = new World();

        // SFML
        // don't use local variables, they go out of scope so nothing plays..... hold them statically
        var file = File.ReadAllBytes("snd/tests.flac");
        buffer = new SoundBuffer(file);
        music = new Sound(buffer);
        music.Loop = true;
        music.Play();
        Console.Out.WriteLine("played?");

        gui = new GUI();
        gui.loadFonts();

        RuntimeHelpers.PrepareMethod(typeof(ChunkSectionRenderer).GetMethod("constructVertices", BindingFlags.NonPublic | BindingFlags.Instance)!.MethodHandle);

        Console.Out.WriteLine("Loaded ASCII font.");
        Task.Run(() => {
            Console.Out.WriteLine("Loading unicode font...");
            gui.loadUnicodeFont();
        }).ContinueWith(_ => {
            Console.Out.WriteLine("Stitching unicode font...");
            executeOnMainThread(() => {
                gui.loadUnicodeFont2();
                Console.Out.WriteLine("Finished stitching unicode font.");
                Screen.switchTo(Screen.MAIN_MENU);
            });
        });
        resize(new Vector2D<int>(width, height));
        // GC after the whole font business - stitching takes hundreds of megs of heap, the game doesn't need that much
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
    }

    public partial class NV1 {
        [LibraryImport("nvapi.dll")]
        internal static partial int NvAPI_Initialize();

        [LibraryImport("nvapi.dll")]
        internal static partial int NvAPI_Initialize_32();
    }

    public static partial class NV2 {
        [LibraryImport("nvapi64.dll")]
        internal static partial int NvAPI_Initialize();

        [LibraryImport("nvapi64.dll")]
        internal static partial int NvAPI_Initialize_64();
    }
    public static void initDedicatedGraphics() {

        // fuck integrated GPUs, we want the dedicated card
        try {
            if (Environment.Is64BitProcess) {
                NativeLibrary.Load("nvapi64.dll");
                NV2.NvAPI_Initialize();
                NV2.NvAPI_Initialize_64();
            }
            else {
                NativeLibrary.Load("nvapi.dll");
                NV1.NvAPI_Initialize();
                NV1.NvAPI_Initialize_32();
            }
        }
        catch {
            // nothing!
            Console.Out.WriteLine("Well, apparently there is no nVidia");
        }
    }

    private void onMouseMove(IMouse m, Vector2 pos) {
        screenStack.peek().onMouseMove(m, pos);
    }

    private void onMouseDown(IMouse m, MouseButton button) {
        screenStack.peek().onMouseDown(m, button);
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
        screenStack.peek().click(m.Position);
    }

    private void onMouseScroll(IMouse m, ScrollWheel scroll) {
        screenStack.peek().scroll(m, scroll);
    }

    private void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        screenStack.peek().onKeyDown(keyboard, key, scancode);
    }

    private void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
    }

    public void resize(Vector2D<int> size) {
        GD.SetViewport(0, 0, (uint)size.X, (uint)size.Y);
        width = size.X;
        height = size.Y;
        screenStack.peek().resize(size);
        gui.resize(size);
    }

    public void executeOnMainThread(Action action) {
        mainThreadQueue.Add(action);
    }

    private void update(double dt) {
        //var before = permanentStopwatch.ElapsedMilliseconds;
        //dt = Math.Min(dt, 0.2);
        /*var vec = new Vector2D<int>(0, 0);
        Console.Out.WriteLine(window.PointToClient(vec));
        Console.Out.WriteLine(window.PointToFramebuffer(vec));
        Console.Out.WriteLine(window.PointToScreen(vec));*/
        screenStack.peek().update(dt);
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
            window.Title = $"BlockGame {fps} ({ft * 1000:0.##}ms)";
            stopwatch.Restart();
        }
        // handle imgui input
        /*var IO = ImGui.GetIO();
        if (IO.WantCaptureKeyboard || IO.WantCaptureMouse) {
            focused = false;
        }*/

        screenStack.peek().clear(GD, dt, interp);
        screenStack.peek().render(dt, interp);

        // before this, only GL, after this, only GD
        GD.ResetStates();

        // for GUI, no depth test
        GD.DepthTestingEnabled = false;
        gui.tb.Begin();
        screenStack.peek().draw();
        gui.tb.End();
        screenStack.peek().postDraw();
        //if (gui.debugScreen) {
        /*mgui.Update((float)dt);
        screen.imGuiDraw();
        imgui.Render();*/
        //}
    }

    public TimerAction setInterval(long interval, Action action) {
        var now = permanentStopwatch.ElapsedMilliseconds;
        var ta = new TimerAction(action, now, true, interval);
        timerQueue.Add(ta);
        return ta;
    }

    public TimerAction setTimeout(long timeout, Action action) {
        var now = permanentStopwatch.ElapsedMilliseconds;
        var ta = new TimerAction(action, now, false, timeout);
        timerQueue.Add(ta);
        return ta;
    }

    private void handleTimers() {
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
    }
}