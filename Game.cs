using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
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

    public const int initialWidth = 1200;
    public const int initialHeight = 800;

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

    public Camera camera;

    public GUI gui;

    public IMouse mouse;
    public IKeyboard keyboard;

    private Vector2 lastMousePos;
    public Vector3D<int>? targetedPos;
    public Vector3D<int>? previousPos;

    public int fps;
    public double frametime;

    public Stopwatch stopwatch = new();

    public bool focused;
    public bool firstFrame;

    public World world;

    public Game() {
        instance = this;
        var windowOptions = WindowOptions.Default;
        windowOptions.VSync = false;
        windowOptions.Title = "BlockGame";
        windowOptions.Size = new Vector2D<int>(initialWidth, initialHeight);
        windowOptions.PreferredDepthBufferBits = 32;
        var api = GraphicsAPI.Default;
#if DEBUG
        api.Flags = ContextFlags.Debug;
#endif
        windowOptions.API = api;
        Window.PrioritizeGlfw();
        window = Window.Create(windowOptions);
        window.Load += init;
        window.Update += update;
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
        GD.BlendState = BlendState.NonPremultiplied;
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

        stopwatch.Start();

        camera = new Camera(Vector3.UnitY * 17, Vector3.UnitZ * 1, Vector3.UnitY, (float)initialWidth / initialHeight);
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

                camera.ModifyDirection(xOffset, yOffset);
            }
        }

        firstFrame = false;
    }

    private void onMouseDown(IMouse m, MouseButton button) {
        if (focused) {
            if (button == MouseButton.Left) {
                if (targetedPos.HasValue) {
                    var pos = targetedPos.Value;
                    world.setBlock(pos.X, pos.Y, pos.Z, 0);
                }
            }
            else if (button == MouseButton.Right) {
                if (previousPos.HasValue) {
                    var pos = previousPos.Value;
                    world.setBlock(pos.X, pos.Y, pos.Z, 1);
                }
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
    }

    private void unlockMouse() {
        mouse.Cursor.CursorMode = CursorMode.Normal;
        focused = false;
    }

    private void onMouseUp(IMouse m, MouseButton button) {
    }

    private void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            unlockMouse();
        }

        if (key == Key.F3) {
            gui.debugScreen = !gui.debugScreen;
        }
    }

    private void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
    }

    private void resize(Vector2D<int> size) {
        GL.Viewport(size);
        width = size.X;
        height = size.Y;
        camera.aspectRatio = (float)width / height;
        gui.resize(size);
    }

    private void update(double dt) {
        /*var vec = new Vector2D<int>(0, 0);
        Console.Out.WriteLine(window.PointToClient(vec));
        Console.Out.WriteLine(window.PointToFramebuffer(vec));
        Console.Out.WriteLine(window.PointToScreen(vec));*/

        //Console.Out.WriteLine($"{camera.position}, {camera.forward}, {camera.zoom}");

        if (stopwatch.ElapsedMilliseconds > 1000) {
            frametime = dt;
            fps = (int)(1 / frametime);
            stopwatch.Restart();
        }

        targetedPos = world.naiveRaycastBlock(out previousPos);


        if (focused) {
            var moveSpeed = 4f * (float)dt;

            if (keyboard.IsKeyPressed(Key.ShiftLeft)) {
                moveSpeed *= 5;
            }

            if (keyboard.IsKeyPressed(Key.W)) {
                //Move forwards
                camera.position += moveSpeed * camera.forward;
            }

            if (keyboard.IsKeyPressed(Key.S)) {
                //Move backwards
                camera.position -= moveSpeed * camera.forward;
            }

            if (keyboard.IsKeyPressed(Key.A)) {
                //Move left
                camera.position -= Vector3.Normalize(Vector3.Cross(camera.up, camera.forward)) * moveSpeed;
            }

            if (keyboard.IsKeyPressed(Key.D)) {
                //Move right
                camera.position += Vector3.Normalize(Vector3.Cross(camera.up, camera.forward)) * moveSpeed;
            }
        }
    }

    private void render(double dt) {
        GD.ResetStates();
        GD.ClearColor = Color4b.DeepSkyBlue;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
        GD.DepthTestingEnabled = true;

        //world.mesh();
        world.draw();
        if (targetedPos.HasValue) {
            world.drawBlockOutline();
        }

        // for GUI, no depth test
        GD.DepthTestingEnabled = false;
        gui.draw();
        if (gui.debugScreen) {
            imgui.Update((float)dt);
            gui.imGuiDraw();
            imgui.Render();
        }
    }

    private void close() {
    }
}