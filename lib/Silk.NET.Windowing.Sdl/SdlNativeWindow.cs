// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using SDL;
using Silk.NET.Core.Contexts;

namespace Silk.NET.SDL
{
    public struct SdlNativeWindow : INativeWindow
    {
        public unsafe SdlNativeWindow(SDL_Window* window) : this()
        {
            Kind = NativeWindowFlags.Sdl;
            Sdl = (nint) window;

            // SDL3: use window properties instead of SDL_SysWMInfo
            SDL_PropertiesID props = SDL3.SDL_GetWindowProperties(window);

            // detect video driver (needed for linux x11 vs wayland)
            string? driver = SDL3.SDL_GetCurrentVideoDriver();
            string? driverName = driver != null ? driver : null;

            // windows
            var hwnd = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_WIN32_HWND_POINTER, IntPtr.Zero);
            if (hwnd != IntPtr.Zero)
            {
                Kind |= NativeWindowFlags.Win32;
                var hdc = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_WIN32_HDC_POINTER, IntPtr.Zero);
                var hinstance = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_WIN32_INSTANCE_POINTER, IntPtr.Zero);
                Win32 = (hwnd, hdc, hinstance);
                DXHandle = hwnd;
            }
            // macos
            else if (SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_COCOA_WINDOW_POINTER, IntPtr.Zero) != IntPtr.Zero)
            {
                Kind |= NativeWindowFlags.Cocoa;
                Cocoa = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_COCOA_WINDOW_POINTER, IntPtr.Zero);
            }
            // linux x11
            else if (driverName == "x11")
            {
                var display = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_X11_DISPLAY_POINTER, IntPtr.Zero);
                var xwindow = SDL3.SDL_GetNumberProperty(props, SDL3.SDL_PROP_WINDOW_X11_WINDOW_NUMBER, 0);
                if (display != IntPtr.Zero && xwindow != 0)
                {
                    Kind |= NativeWindowFlags.X11;
                    X11 = (display, (nuint)xwindow);
                }
            }
            // linux wayland
            else if (driverName == "wayland")
            {
                var display = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_WAYLAND_DISPLAY_POINTER, IntPtr.Zero);
                var surface = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_WAYLAND_SURFACE_POINTER, IntPtr.Zero);
                if (display != IntPtr.Zero && surface != IntPtr.Zero)
                {
                    Kind |= NativeWindowFlags.Wayland;
                    Wayland = (display, surface);
                }
            }
            // ios
            else if (SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_UIKIT_WINDOW_POINTER, IntPtr.Zero) != IntPtr.Zero)
            {
                Kind |= NativeWindowFlags.UIKit;
                var uiwindow = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_UIKIT_WINDOW_POINTER, IntPtr.Zero);
                uint framebuffer = (uint)SDL3.SDL_GetNumberProperty(props, SDL3.SDL_PROP_WINDOW_UIKIT_OPENGL_FRAMEBUFFER_NUMBER, 0);
                uint colorbuffer = (uint)SDL3.SDL_GetNumberProperty(props, SDL3.SDL_PROP_WINDOW_UIKIT_OPENGL_RENDERBUFFER_NUMBER, 0);
                uint resolveFramebuffer = (uint)SDL3.SDL_GetNumberProperty(props, SDL3.SDL_PROP_WINDOW_UIKIT_OPENGL_RESOLVE_FRAMEBUFFER_NUMBER, 0);
                UIKit = (uiwindow, framebuffer, colorbuffer, resolveFramebuffer);
            }
            // android
            else if (SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_ANDROID_WINDOW_POINTER, IntPtr.Zero) != IntPtr.Zero)
            {
                Kind |= NativeWindowFlags.Android;
                var androidWindow = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_ANDROID_WINDOW_POINTER, IntPtr.Zero);
                var androidSurface = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_ANDROID_SURFACE_POINTER, IntPtr.Zero);
                Android = (androidWindow, androidSurface);
            }
            // vivante (removed in SDL3? keeping for compatibility)
            else if (SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_VIVANTE_DISPLAY_POINTER, IntPtr.Zero) != IntPtr.Zero)
            {
                Kind |= NativeWindowFlags.Vivante;
                var display = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_VIVANTE_DISPLAY_POINTER, IntPtr.Zero);
                var vivanteWindow = SDL3.SDL_GetPointerProperty(props, SDL3.SDL_PROP_WINDOW_VIVANTE_WINDOW_POINTER, IntPtr.Zero);
                Vivante = (display, vivanteWindow);
            }

            DXHandle ??= (nint)window;
        }

        public NativeWindowFlags Kind { get; }
        public (nint Display, nuint Window)? X11 { get; }
        public nint? Cocoa { get; }
        public (nint Display, nint Surface)? Wayland { get; }
        public nint? WinRT { get; }
        public (nint Window, uint Framebuffer, uint Colorbuffer, uint ResolveFramebuffer)? UIKit { get; }
        public (nint Hwnd, nint HDC, nint HInstance)? Win32 { get; }
        public (nint Display, nint Window)? Vivante { get; }
        public (nint Window, nint Surface)? Android { get; }
        public nint? Glfw { get; }
        public nint? Sdl { get; }
        public nint? DXHandle { get; }
        public (nint? Display, nint? Surface)? EGL { get; }
    }
}
