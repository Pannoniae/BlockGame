using System.Drawing;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace BlockGame {
    public class Game {
        private IWindow window;

        private IInputContext input;

        private ImGuiController imgui;

        private GL GL;
        public Game() {
            var windowOptions = WindowOptions.Default;
            windowOptions.VSync = false;
            windowOptions.Title = "BlockGame";
            windowOptions.Size = new Vector2D<int>(800, 600);
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
                // bind events
            }
            foreach (var keyboard in input.Keyboards) {
                keyboard.KeyDown += onKeyDown;
                keyboard.KeyUp += onKeyUp;
            }
            GL = GL.GetApi(window);
            GL.ClearColor(Color.Aqua);
            
            imgui = new ImGuiController(GL, window, input);
            
        }

        private void onResize(Vector2D<int> size) {
            GL.Viewport(size);
        }
        
        private void onUpdate(double dt) {
            
        }
        
        private void onRender(double dt) {
            //Clear the color channel.
            GL.Clear((uint) ClearBufferMask.ColorBufferBit);
            imgui.Update((float)dt);
            ImGui.ShowDemoWindow();
            imgui.Render();
        }
        
        private void onKeyUp(IKeyboard keyboard, Key key, int code) {
            throw new System.NotImplementedException();
        }

        private void onKeyDown(IKeyboard keyboard, Key key, int code) {
            
        }
        
        private void onClose() {
            
        }
    }
}