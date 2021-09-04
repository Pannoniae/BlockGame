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
        private IWindow window;

        private const int width = 800;
        private const int height = 600;

        private IInputContext input;

        private ImGuiController imgui;

        private GL GL;

        private static Camera camera;
        
        //Used to track change in mouse movement to allow for moving of the Camera
        private static Vector2 lastMousePos;

        private List<IKeyboard> keyboards = new();

        private static BufferObject<float> vbo;
        private static BufferObject<uint> ebo;
        private static VertexArrayObject<float, uint> theCube;
        private static Shader shader;

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
            var windowOptions = WindowOptions.Default;
            windowOptions.VSync = false;
            windowOptions.Title = "BlockGame";
            windowOptions.Size = new Vector2D<int>(width, height);
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
                    Console.WriteLine(Marshal.PtrToStringUTF8(message, length)), 0);

            ebo = new BufferObject<uint>(GL, indices, BufferTargetARB.ElementArrayBuffer);
            vbo = new BufferObject<float>(GL, vertices, BufferTargetARB.ArrayBuffer);
            theCube = new VertexArrayObject<float, uint>(GL, vbo, ebo);

            theCube.vertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);

            shader = new Shader(GL, "shader.vert", "shader.frag");

            camera = new Camera(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, width / height);

            imgui = new ImGuiController(GL, window, input);
        }

        private void onClick(IMouse mouse, MouseButton button, Vector2 pos) {
        }

        private void onMove(IMouse mouse, Vector2 position) {
            const float lookSensitivity = 0.1f;
            if (lastMousePos == default) { lastMousePos = position; }
            else
            {
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
            GL.Enable(EnableCap.DepthTest);
            //Clear the color channel.
            GL.Clear((uint)ClearBufferMask.ColorBufferBit);
            theCube.bind();
            shader.use();
            shader.setUniform("uModel", Matrix4x4.Identity);
            shader.setUniform("uView", camera.getViewMatrix());
            shader.setUniform("uProjection", camera.getProjectionMatrix());
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

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