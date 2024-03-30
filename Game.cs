using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SoLoud;
using TrippyGL;
using DebugSeverity = Silk.NET.OpenGL.DebugSeverity;
using DebugSource = Silk.NET.OpenGL.DebugSource;
using DebugType = Silk.NET.OpenGL.DebugType;
using DepthFunction = TrippyGL.DepthFunction;
using MouseButton = Silk.NET.Input.MouseButton;

namespace BlockGame;

public class Game {

    public static Game instance { get; private set; }

    public int width;
    public int height;

    public IWindow window;
    public GL GL = null!;
    public GraphicsDevice GD = null!;
    public IInputContext input = null!;
    public ImGuiController imgui = null!;

    public Process proc;

    public int centreX => width / 2;
    public int centreY => height / 2;

    /// <summary>
    /// The current game screen which is shown.
    /// </summary>
    public Screen screen;
    public GUI gui;

    public IMouse mouse;
    public IKeyboard keyboard;

    public Vector2 lastMousePos;
    public Vector3D<int>? targetedPos;
    public Vector3D<int>? previousPos;

    public int fps;
    public double ft;

    public BlendState initialBlendState = BlendState.NonPremultiplied;

    public Stopwatch stopwatch = new();
    public double accumTime;
    public static readonly double fixeddt = 1 / 30d;
    public static readonly double maxTimestep = 1 / 5f;
    public double t;

    public bool focused;
    public bool firstFrame;

    /// <summary>
    /// True while clicking back into the game. Used to prevent the player instantly breaking a block when clicking on the screen to get back into the game world.
    /// </summary>
    public bool lockingMouse;

    public BTexture2D blockTexture;
    public Metrics metrics;

    public Game() {
        instance = this;
        var windowOptions = WindowOptions.Default;
        //windowOptions.FramesPerSecond = 6000;
        //windowOptions.UpdatesPerSecond = 6000;
        windowOptions.VSync = false;
        windowOptions.Title = "BlockGame";
        windowOptions.Size = new Vector2D<int>(Constants.initialWidth, Constants.initialHeight);
        windowOptions.PreferredDepthBufferBits = 32;
        var api = GraphicsAPI.Default;
#if DEBUG
        api.Flags = ContextFlags.Debug;
#endif
        windowOptions.API = api;
        Window.PrioritizeGlfw();
        window = Window.Create(windowOptions);
        window.Load += init;
        //window.Update += update;
        window.Render += render;
        window.FramebufferResize += resize;
        window.Closing += close;
        window.Run();
    }

    private void GLDebug(GLEnum source, GLEnum type, int id, GLEnum severity, int length, IntPtr message, IntPtr userparam) {
        string msg = Marshal.PtrToStringAuto(message, length)!;
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

        blockTexture = new BTexture2D("textures/blocks.png");
        //world = new World();
        gui = new GUI();
        Screen.initScreens(gui);
        screen = Screen.MAIN_MENU;
        resize(new Vector2D<int>(width, height));
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
    }

    private void onMouseMove(IMouse m, Vector2 position) {
        screen.onMouseMove(m, position);
    }

    private void onMouseDown(IMouse m, MouseButton button) {
        screen.onMouseDown(m, button);
    }

    public void lockMouse() {
        mouse.Cursor.CursorMode = CursorMode.Disabled;
        //mouse.Position = new Vector2(centre.X, centre.Y);
        focused = true;
        firstFrame = true;
        lockingMouse = true;
    }

    public void unlockMouse() {
        mouse.Cursor.CursorMode = CursorMode.Normal;
        focused = false;
    }

    private void onMouseUp(IMouse m, MouseButton button) {
        screen.click(m.Position);
    }

    private void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        screen.onKeyDown(keyboard, key, scancode);
    }

    private void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
    }

    public void resize(Vector2D<int> size) {
        GD.SetViewport(0, 0, (uint)size.X, (uint)size.Y);
        width = size.X;
        height = size.Y;
        screen.resize(size);
        gui.resize(size);
    }

    private void update(double dt) {
        //dt = Math.Min(dt, 0.2);
        /*var vec = new Vector2D<int>(0, 0);
        Console.Out.WriteLine(window.PointToClient(vec));
        Console.Out.WriteLine(window.PointToFramebuffer(vec));
        Console.Out.WriteLine(window.PointToScreen(vec));*/
        screen.update(dt);

    }

    /// <summary>
    /// Now the main loop which calls <see cref="update"/> too.
    /// </summary>
    /// <param name="dt">dt as fractions of a second. 1 = 1s</param>
    private void render(double dt) {
        dt = Math.Min(dt, maxTimestep);
        accumTime += dt;
        while (accumTime >= fixeddt) {
            update(fixeddt);
            t += fixeddt;
            accumTime -= fixeddt;
        }
        // get remaining time between 0 and 1
        var interp = accumTime / fixeddt;
        actualRender(dt, interp);

    }

    private void actualRender(double dt, double interp) {
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
        GD.ResetStates();
        GD.ClearColor = Color4b.DeepSkyBlue;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
        screen.render(dt, interp);

        // for GUI, no depth test
        GD.DepthTestingEnabled = false;
        screen.tb.Begin();
        screen.draw();
        screen.tb.End();
        if (screen.gui.debugScreen) {
            /*mgui.Update((float)dt);
            screen.imGuiDraw();
            imgui.Render();*/
        }
    }

    private void close() {
    }
}