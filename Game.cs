using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace BlockGame {
    public class Game {

        public static Game instance { get; private set; }

        private IWindow window;

        private const int width = 800;
        private const int height = 600;

        private IInputContext input;

        private ImGuiController imgui;

        public GL GL;

        public Camera camera;

        private World world;

        //Used to track change in mouse movement to allow for moving of the Camera
        private static Vector2 lastMousePos;

        private DateTime now;

        private List<IKeyboard> keyboards = new();

        private BufferObject<float> vbo;
        private BufferObject<uint> ebo;
        private VertexArrayObject<float, uint> theCube;
        

        private static readonly float[] vertices = {
            //X    Y      Z     
            -0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, 0.5f, -0.5f,
            0.5f, 0.5f, -0.5f,
            -0.5f, 0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,

            -0.5f, -0.5f, 0.5f,
            0.5f, -0.5f, 0.5f,
            0.5f, 0.5f, 0.5f,
            0.5f, 0.5f, 0.5f,
            -0.5f, 0.5f, 0.5f,
            -0.5f, -0.5f, 0.5f,

            -0.5f, 0.5f, 0.5f,
            -0.5f, 0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, 0.5f,
            -0.5f, 0.5f, 0.5f,

            0.5f, 0.5f, 0.5f,
            0.5f, 0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, 0.5f,
            0.5f, 0.5f, 0.5f,

            -0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, 0.5f,
            0.5f, -0.5f, 0.5f,
            -0.5f, -0.5f, 0.5f,
            -0.5f, -0.5f, -0.5f,

            -0.5f, 0.5f, -0.5f,
            0.5f, 0.5f, -0.5f,
            0.5f, 0.5f, 0.5f,
            0.5f, 0.5f, 0.5f,
            -0.5f, 0.5f, 0.5f,
            -0.5f, 0.5f, -0.5f,
        };

        private static readonly uint[] indices = {
            0, 1, 3,
            1, 2, 3
        };

        public Game() {
            instance = this;
            var windowOptions = WindowOptions.Default;
            windowOptions.VSync = false;
            windowOptions.Title = "BlockGame";
            windowOptions.Size = new Vector2D<int>(width, height);
            var api = GraphicsAPI.Default;
            api.Flags = ContextFlags.Debug;
            windowOptions.API = api;
            window = Window.Create(windowOptions);

            window.Render += onRender;
            window.Update += onUpdate;
            window.Load += onLoad;
            window.FramebufferResize += onResize;
            window.Closing += onClose;
            window.Run();
        }

        private void onLoad() {
            input = window.CreateInput();
            foreach (var mouse in input.Mice) {
                mouse.Click += onClick;
                mouse.MouseMove += onMove;
                mouse.Scroll += onScroll;
                mouse.Cursor.CursorMode = CursorMode.Raw;
            }

            foreach (var keyboard in input.Keyboards) {
                keyboard.KeyDown += onKeyDown;
                keyboard.KeyUp += onKeyUp;

                keyboards.Add(keyboard);
            }

            GL = GL.GetApi(window);
            GL.ClearColor(Color.Aqua);
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback(
                (source, type, id, severity, length, message, param) =>
                    Console.WriteLine(Marshal.PtrToStringAnsi(message, length)), 0);

            ebo = new BufferObject<uint>(GL, indices, BufferTargetARB.ElementArrayBuffer);
            vbo = new BufferObject<float>(GL, vertices, BufferTargetARB.ArrayBuffer);
            theCube = new VertexArrayObject<float, uint>(GL, vbo, ebo);
            theCube.vertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
            camera = new Camera(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, width / height);
            world = new World();

            imgui = new ImGuiController(GL, window, input);

            now = DateTime.Now;
        }

        private void onClick(IMouse mouse, MouseButton button, Vector2 pos) {
        }

        private void onMove(IMouse mouse, Vector2 position) {
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

        private void onScroll(IMouse mouse, ScrollWheel scrollWheel) {
            camera.ModifyZoom(scrollWheel.Y);
        }

        private void onResize(Vector2D<int> size) {
            GL.Viewport(size);
        }

        private void onUpdate(double dt) {


            if ((DateTime.Now - now).TotalMilliseconds > 1000) {
                var gcinfo = GC.GetGCMemoryInfo();
                var current = GC.GetTotalMemory(false);
                var max2 = gcinfo.TotalCommittedBytes;
                Console.Out.WriteLine($"{Utils.formatMB(current)} / {Utils.formatMB(max2)} ");
                now = DateTime.Now;
            }
            
            var moveSpeed = 2.5f * (float)dt;

            if (keyboards.Any(kb => kb.IsKeyPressed(Key.W))) {
                //Move forwards
                camera.position += moveSpeed * camera.forward;
            }

            if (keyboards.Any(kb => kb.IsKeyPressed(Key.S))) {
                //Move backwards
                camera.position -= moveSpeed * camera.forward;
            }

            if (keyboards.Any(kb => kb.IsKeyPressed(Key.A))) {
                //Move left
                camera.position -= Vector3.Normalize(Vector3.Cross(camera.forward, camera.up)) * moveSpeed;
            }

            if (keyboards.Any(kb => kb.IsKeyPressed(Key.D))) {
                //Move right
                camera.position += Vector3.Normalize(Vector3.Cross(camera.forward, camera.up)) * moveSpeed;
            }
        }

        private void onRender(double dt) {
            //Clear the color channel.
            GL.Clear((uint)ClearBufferMask.ColorBufferBit);
            world.meshTheWorld();
            world.draw();

            //const string str = "HELLO!!!!!";
            //GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeError, 0,
            //    DebugSeverity.DebugSeverityHigh, (uint)str.Length, str);
            // UI
            imgui.Update((float)dt);
            ImGui.ShowDemoWindow();
            imgui.Render();
        }

        private void onKeyUp(IKeyboard keyboard, Key key, int code) {
            if (key == Key.Escape) {
                window.Close();
            }
        }

        private void onKeyDown(IKeyboard keyboard, Key key, int code) {
        }

        private void onClose() {
        }
    }
}