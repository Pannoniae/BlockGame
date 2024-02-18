using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using TrippyGL;
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

    public float centreX => width / 2f;
    public float centreY => height / 2f;

    public GUI gui;

    public IMouse mouse;
    public IKeyboard keyboard;

    private Vector2 lastMousePos;
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

    public World world;
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

    private void init() {
        input = window.CreateInput();
        GL = window.CreateOpenGL();
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
            mouse.Cursor.CursorMode = CursorMode.Disabled;
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

        world = new World();
        gui = new GUI();
        GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        GL.DebugMessageCallback(GLDebug, 0);
    }

    private void GLDebug(GLEnum source, GLEnum type, int id, GLEnum severity, int length, IntPtr message,
        IntPtr userparam) {
        string msg = Marshal.PtrToStringAuto(message, length)!;
        Console.Out.WriteLine($"{source} [{severity}] ({id}): {type}, {msg}");
    }

    private void onMouseMove(IMouse m, Vector2 position) {
        if (!focused) {
            return;
        }

        if (firstFrame) {
            lastMousePos = position;
        }
        else {
            const float lookSensitivity = 0.1f;
            if (lastMousePos == default) {
                lastMousePos = position;
            }
            else {
                var xOffset = (position.X - lastMousePos.X) * lookSensitivity;
                var yOffset = (position.Y - lastMousePos.Y) * lookSensitivity;
                lastMousePos = position;

                world.player.camera.ModifyDirection(xOffset, yOffset);
            }
        }

        firstFrame = false;
    }

    private void onMouseDown(IMouse m, MouseButton button) {
        if (focused) {
            if (button == MouseButton.Left) {
                world.player.breakBlock();
            }
            else if (button == MouseButton.Right) {
                world.player.placeBlock();
            }
        }
        else {
            lockMouse();
        }
    }

    private void lockMouse() {
        mouse.Cursor.CursorMode = CursorMode.Disabled;
        //mouse.Position = new Vector2(centre.X, centre.Y);
        focused = true;
        firstFrame = true;
        lockingMouse = true;
    }

    private void unlockMouse() {
        mouse.Cursor.CursorMode = CursorMode.Normal;
        focused = false;
    }

    private void onMouseUp(IMouse m, MouseButton button) {
        // if no longer holding, the player isn't clicking into the window anymore
        if (focused && lockingMouse) {
            lockingMouse = false;
        }
    }

    private void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            unlockMouse();
        }

        if (key == Key.F3) {
            gui.debugScreen = !gui.debugScreen;
        }

        if (key == Key.F) {
            world.save("world");
        }

        if (key == Key.G) {
            world = World.load("world");
            resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
        }

        world.player.updatePickBlock(keyboard, key, scancode);
    }

    private void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
    }

    private void resize(Vector2D<int> size) {
        GL.Viewport(size);
        width = size.X;
        height = size.Y;
        world.player.camera.aspectRatio = (float)width / height;
        gui.resize(size);
    }

    private void update(double dt) {
        //dt = Math.Min(dt, 0.2);
        /*var vec = new Vector2D<int>(0, 0);
        Console.Out.WriteLine(window.PointToClient(vec));
        Console.Out.WriteLine(window.PointToFramebuffer(vec));
        Console.Out.WriteLine(window.PointToScreen(vec));*/

        world.player.pressedMovementKey = false;
        if (focused && !lockingMouse) {
            world.player.updateInput(dt);
        }
        world.update(dt);
        world.player.update(dt);

        targetedPos = world.naiveRaycastBlock(out previousPos);
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
            window.Title = "BlockGame " + fps;
            stopwatch.Restart();
        }
        // handle imgui input
        var IO = ImGui.GetIO();
        if (IO.WantCaptureKeyboard || IO.WantCaptureMouse) {
            //focused = false;
        }

        GD.ResetStates();
        GD.ClearColor = Color4b.DeepSkyBlue;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
        GD.DepthTestingEnabled = true;
        metrics.clear();

        //world.mesh();
        world.draw(interp);
        if (targetedPos.HasValue) {
            world.drawBlockOutline(interp);
        }

        // for GUI, no depth test
        GD.DepthTestingEnabled = false;
        gui.drawScreen();
        if (gui.debugScreen) {
            imgui.Update((float)dt);
            gui.imGuiDraw();
            imgui.Render();
        }
    }

    private void close() {
    }
}