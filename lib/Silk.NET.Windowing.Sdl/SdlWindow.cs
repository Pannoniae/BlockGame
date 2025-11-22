// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SDL;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using RawImage = Silk.NET.Core.RawImage;

namespace Silk.NET.Windowing.Sdl {
    internal unsafe class SdlWindow : SdlView, IWindow {
        private WindowOptions _extendedOptionsCache;
        private List<string> _droppedFiles = new();

        public SdlWindow(WindowOptions opts, SdlView? parent, SdlMonitor? monitor, SdlPlatform platform)
            : base(new ViewOptions(opts), parent, monitor, platform) {
            _extendedOptionsCache = opts;
            WindowClass = opts.WindowClass ?? Window.DefaultWindowClass;
        }

        public SdlWindow(void* nativeHandle, IGLContext? ctx, SdlPlatform platform) :
            base(nativeHandle, ctx, platform) {
        }

        public bool IsVisible {
            get => !IsInitialized
                ? _extendedOptionsCache.IsVisible
                : _extendedOptionsCache.IsVisible =
                    (SDL3.SDL_GetWindowFlags(SdlWindow) & SDL_WindowFlags.SDL_WINDOW_HIDDEN) == 0;
            set {
                _extendedOptionsCache.IsVisible = value;
                if (!IsInitialized) {
                    return;
                }

                if (value) {
                    SDL3.SDL_ShowWindow(SdlWindow);
                }
                else {
                    SDL3.SDL_HideWindow(SdlWindow);
                }
            }
        }

        public Vector2D<int> Position {
            get {
                if (IsInitialized) {
                    var ret = stackalloc int[2];
                    SDL3.SDL_GetWindowPosition(SdlWindow, ret, &ret[1]);
                    return _extendedOptionsCache.Position = *(Vector2D<int>*)ret;
                }

                return _extendedOptionsCache.Position;
            }
            set {
                _extendedOptionsCache.Position = value;
                if (!IsInitialized) {
                    return;
                }

                SDL3.SDL_SetWindowPosition(SdlWindow, value.X, value.Y);
            }
        }

        public new Vector2D<int> Size {
            get => IsInitialized ? _extendedOptionsCache.Size = base.Size : _extendedOptionsCache.Size;
            set {
                _extendedOptionsCache.Size = value;
                if (!IsInitialized) {
                    return;
                }

                SDL3.SDL_SetWindowSize(SdlWindow, value.X, value.Y);
            }
        }

        public string Title {
            get => IsInitialized
                ? _extendedOptionsCache.Title = SDL3.SDL_GetWindowTitle(SdlWindow)!
                : _extendedOptionsCache.Title;
            set {
                _extendedOptionsCache.Title = value;
                if (!IsInitialized) {
                    return;
                }

                SDL3.SDL_SetWindowTitle(SdlWindow, value);
            }
        }

        public WindowState WindowState {
            get => IsInitialized
                ? _extendedOptionsCache.WindowState = ToWindowState(SDL3.SDL_GetWindowFlags(SdlWindow))
                : _extendedOptionsCache.WindowState;
            set {
                _swapIntervalChanged = true;
                _extendedOptionsCache.WindowState = value;
                if (!IsInitialized) {
                    return;
                }

                switch (value) {
                    case WindowState.Normal: {
                        SDL3.SDL_SetWindowFullscreen(SdlWindow, false);
                        SDL3.SDL_RestoreWindow(SdlWindow);
                        break;
                    }
                    case WindowState.Minimized: {
                        SDL3.SDL_MinimizeWindow(SdlWindow);
                        break;
                    }
                    case WindowState.Maximized: {
                        SDL3.SDL_MaximizeWindow(SdlWindow);
                        break;
                    }
                    case WindowState.Fullscreen: {
                        SDL3.SDL_SetWindowFullscreen(SdlWindow, true);
                        break;
                    }
                    default: {
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                    }
                }
            }
        }

        public WindowBorder WindowBorder {
            get => IsInitialized
                ? _extendedOptionsCache.WindowBorder = ToWindowBorder(SDL3.SDL_GetWindowFlags(SdlWindow))
                : _extendedOptionsCache.WindowBorder;
            set {
                _extendedOptionsCache.WindowBorder = value;
                if (!IsInitialized) {
                    return;
                }

                switch (value) {
                    case WindowBorder.Resizable: {
                        SDL3.SDL_SetWindowBordered(SdlWindow, true);
                        SDL3.SDL_SetWindowResizable(SdlWindow, true);
                        break;
                    }
                    case WindowBorder.Fixed: {
                        SDL3.SDL_SetWindowBordered(SdlWindow, true);
                        SDL3.SDL_SetWindowResizable(SdlWindow, false);
                        break;
                    }
                    case WindowBorder.Hidden: {
                        SDL3.SDL_SetWindowBordered(SdlWindow, false);
                        SDL3.SDL_SetWindowResizable(SdlWindow, false);
                        break;
                    }
                    default: {
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                    }
                }
            }
        }

        public string? WindowClass { get; }

        public unsafe Rectangle<int> BorderSize {
            get {
                int l = 0, t = 0, r = 0, b = 0;
                SDL3.SDL_GetWindowBordersSize(SdlWindow, &t, &l, &b, &r);
                return new Rectangle<int>(new(l, t), new(r - l, b - t));
            }
        }

        public bool TransparentFramebuffer => false; // doesn't look like SDL doesn't support this

        public bool TopMost {
            get => _extendedOptionsCache.TopMost;
            set {
                SDL3.SDL_SetWindowAlwaysOnTop(SdlWindow, value);
                _extendedOptionsCache.TopMost = value;
            }
        }

        public IGLContext? SharedContext => _extendedOptionsCache.SharedContext;

        public IWindow CreateWindow(WindowOptions opts) => new SdlWindow(opts, this, null, _platform);

        public IWindowHost? Parent => (IWindowHost?)ParentView ?? Monitor;

        public IMonitor? Monitor {
            get {
                if (!IsInitialized) {
                    return InitialMonitor;
                }

                var monitor = SDL3.SDL_GetDisplayForWindow(SdlWindow);
                if (monitor == 0) {
                    if (WindowState != WindowState.Fullscreen) {
                        var monitors = SDL3.SDL_GetDisplays();
                        if (monitors.Count < 0) {
                            throw new PlatformException(SdlExt.GetErrorAsException());
                        }

                        // Determine which monitor this window is on. [6 marks]
                        for (var i = 0; i < monitors.Count; i++) {
                            var pos = Position;
                            var size = Size;
                            Rectangle<int> bounds;
                            SDL_Rect bounds2;
                            SDL3.SDL_GetDisplayUsableBounds(monitors[i], &bounds2);
                            bounds = new Rectangle<int>(new Vector2D<int>(bounds2.x, bounds2.y),
                                new Vector2D<int>(bounds2.w, bounds2.h));
                            if (bounds.Contains(new Vector2D<int>(pos.X + size.X / 2, pos.Y + size.Y / 2))) {
                                return new SdlMonitor(_platform, monitors[i]);
                            }
                        }
                    }
                }

                return monitor == 0
                    ? null
                    : new SdlMonitor(_platform, monitor);
            }
            set {
                _swapIntervalChanged = true;
                if (!IsInitialized) {
                    throw new InvalidOperationException("Window is not initialized.");
                }

                if (value is null) {
                    throw new ArgumentNullException(nameof(value));
                }

                Position = value.Bounds.Origin;
            }
        }

        public new bool IsClosing {
            get => base.IsClosing;
            set => IsClosingVal = value;
        }

        public event Action<Vector2D<int>>? Move;
        public event Action<WindowState>? StateChanged;
        public event Action<string[]>? FileDrop;

        public void SetWindowIcon(ReadOnlySpan<RawImage> icons) {
            var icon = icons[0];

            fixed (byte* ptr = icon.Pixels.Span) {
                var surface = SDL3.SDL_CreateSurfaceFrom
                (
                    icon.Width, icon.Height,
                    // inverted because little-endian!
                    SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888,
                    (IntPtr)ptr, icon.Width * 4
                );

                SDL3.SDL_SetWindowIcon(SdlWindow, surface);
                SDL3.SDL_DestroySurface(surface);
            }
        }

        private static WindowState ToWindowState(SDL_WindowFlags flags) {
            if ((flags & (SDL_WindowFlags.SDL_WINDOW_FULLSCREEN)) != 0) {
                return WindowState.Fullscreen;
            }

            if ((flags & SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0) {
                return WindowState.Maximized;
            }

            if ((flags & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0) {
                return WindowState.Minimized;
            }

            return WindowState.Normal;
        }

        private static WindowBorder ToWindowBorder(SDL_WindowFlags flags) {
            if ((flags & SDL_WindowFlags.SDL_WINDOW_RESIZABLE) != 0) {
                return WindowBorder.Resizable;
            }

            return (flags & SDL_WindowFlags.SDL_WINDOW_BORDERLESS) != 0
                ? WindowBorder.Hidden
                : WindowBorder.Fixed;
        }

        [SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
        public override void ProcessEvents() {
            base.ProcessEvents();
            var i = 0;
            var c = Events.Count;
            for (var j = 0; j < c; j++) {
                var @event = Events[i];
                var skipped = false;
                switch (@event.Type) {
                    //case WindowEventID.WindoweventNone:
                    //    break;
                    //case WindowEventID.WindoweventShown:
                    //    break;
                    //case WindowEventID.WindoweventHidden:
                    //    break;
                    //case WindowEventID.WindoweventExposed:
                    //    break;
                    case SDL_EventType.SDL_EVENT_WINDOW_MOVED: {
                        Move?.Invoke(new Vector2D<int>(@event.window.data1, @event.window.data2));
                        break;
                    }
                    case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                        break;
                    case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED: {
                        StateChanged?.Invoke(WindowState.Minimized);
                        break;
                    }
                    case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED: {
                        StateChanged?.Invoke(WindowState.Maximized);
                        break;
                    }
                    case SDL_EventType.SDL_EVENT_WINDOW_RESTORED: {
                        StateChanged?.Invoke(WindowState.Normal);
                        break;
                    }
                    //case WindowEventID.WindoweventEnter:
                    //    break;
                    //case WindowEventID.WindoweventLeave:
                    //    break;
                    //case WindowEventID.WindoweventHitTest:
                    //    break;
                    case SDL_EventType.SDL_EVENT_DROP_FILE: {
                        string path = SilkMarshal.PtrToString((nint)@event.drop.data, NativeStringEncoding.UTF8) ?? "";
                        _droppedFiles.Add(path);
                        break;
                    }
                    default: {
                        i++;
                        skipped = true;
                        break;
                    }
                }

                if (!skipped) {
                    RemoveEvent(i);
                }
            }

            if (_droppedFiles.Count > 0) {
                FileDrop?.Invoke(_droppedFiles.ToArray());
                _droppedFiles.Clear();
            }
        }

        protected override void CoreInitialize(ViewOptions opts) {
            _swapIntervalChanged = true;
            SDL3.SDL_setenv_unsafe("SDL_VIDEO_X11_WMCLASS", WindowClass, 1);

            SDL_WindowFlags flags = 0;
            if (!IsVisible) {
                flags |= SDL_WindowFlags.SDL_WINDOW_HIDDEN;
            }

            flags |= WindowBorder switch {
                WindowBorder.Resizable => SDL_WindowFlags.SDL_WINDOW_RESIZABLE,
                WindowBorder.Fixed => 0,
                WindowBorder.Hidden => SDL_WindowFlags.SDL_WINDOW_BORDERLESS,
                _ => 0
            };
            flags |= WindowState switch {
                WindowState.Normal => 0,
                WindowState.Minimized => SDL_WindowFlags.SDL_WINDOW_MINIMIZED,
                WindowState.Maximized => SDL_WindowFlags.SDL_WINDOW_MAXIMIZED,
                WindowState.Fullscreen => SDL_WindowFlags.SDL_WINDOW_FULLSCREEN,
                _ => 0
            };
            CoreInitialize
            (
                opts, flags, (InitialMonitor?.Bounds.Origin.X ?? 0) + _extendedOptionsCache.Position.X,
                (InitialMonitor?.Bounds.Origin.Y ?? 0) + _extendedOptionsCache.Position.Y, Size.X, Size.Y, Title,
                SharedContext
            );
        }
    }
}