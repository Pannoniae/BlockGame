using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
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
    public IInputContext input = null!;
    public ImGuiController imgui = null!;

    public Vector2D<float> centre => new(window.Size.X / 2f, window.Size.Y / 2f);

    public Camera camera;

    public IMouse mouse;
    public IKeyboard keyboard;

    private Vector2 lastMousePos;

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
        window.Resize += resize;
        window.Closing += close;
        window.Run();
    }

    private void init() {
        input = window.CreateInput();
        GL = window.CreateOpenGL();
        imgui = new ImGuiController(GL, window, input);
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

        stopwatch.Start();

        camera = new Camera(Vector3.UnitY * 17, Vector3.UnitZ * -1, Vector3.UnitY, (float)initialWidth / initialHeight);
        world = new World();
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
            unlockMouse();
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
    }

    private void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
    }

    private void resize(Vector2D<int> size) {
        GL.Viewport(size);
        width = size.X;
        height = size.Y;
        camera.aspectRatio = (float)width / height;
    }

    private void update(double dt) {
        /*var vec = new Vector2D<int>(0, 0);
        Console.Out.WriteLine(window.PointToClient(vec));
        Console.Out.WriteLine(window.PointToFramebuffer(vec));
        Console.Out.WriteLine(window.PointToScreen(vec));*/

        //Console.Out.WriteLine($"{camera.position}, {camera.forward}, {camera.zoom}");

        if (stopwatch.ElapsedMilliseconds > 1000) {
            int fps = (int)(1 / dt);
            window.Title = "BlockGame " + fps;
            stopwatch.Restart();
        }


        if (focused) {
            var moveSpeed = 4f * (float)dt;
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
                camera.position -= Vector3.Normalize(Vector3.Cross(camera.forward, camera.up)) * moveSpeed;
            }

            if (keyboard.IsKeyPressed(Key.D)) {
                //Move right
                camera.position += Vector3.Normalize(Vector3.Cross(camera.forward, camera.up)) * moveSpeed;
            }
        }
    }

    private void render(double dt) {
        GL.ClearColor(Color.DeepSkyBlue);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        //world.mesh();
        world.draw();

        imgui.Update((float)dt);
        ImGui.ShowDemoWindow();
        imgui.Render();
    }

    private void close() {
    }
}