// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using SDL;
using Silk.NET.SDL;
using Silk.NET.Windowing.Internals;
#if __IOS__
using Silk.NET.Windowing.Sdl.iOS;
#endif

// We can't import System because System has a type called nint on iOS and Mac Catalyst.
// As such, throughout this file System is fully qualified.

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace Silk.NET.Windowing.Sdl
{
    internal unsafe class SdlView : ViewImplementationBase
    {
        protected readonly SdlPlatform _platform;
        private const int WaitTimeout = 10;
        private IGLContext? _ctx;
        private SdlVkSurface? _vk;
        private int _continue;
        private SDL_WindowID? _id;
        private BreakneckLock _eventSync = BreakneckLock.Create();

        public SdlView(ViewOptions opts, SdlView? parent, SdlMonitor? monitor, SdlPlatform platform): base(opts)
        {
            _platform = platform;
            ParentView = parent;
            InitialMonitor = monitor;
        }

        public SdlView(void* nativeHandle, IGLContext? ctx, SdlPlatform platform) : base(default)
        {
            throw new NotSupportedException("Creating a Silk.NET window from a native handle is not supported with the SDL backend for now.");
            ParentView = null;
            InitialMonitor = null;
            IsInitialized = true;

            // TODO
            var props = SDL3.SDL_CreateProperties();
            SdlWindow = SDL3.SDL_CreateWindowWithProperties(props);
            _ctx = ctx;
            _platform = platform;
        }

        // Events
        public override event System.Action<Vector2D<int>>? Resize;
        public override event System.Action<Vector2D<int>>? FramebufferResize;
        public override event System.Action? Closing;
        public override event System.Action<bool>? FocusChanged;

        // Properties
        protected override IGLContext? CoreGLContext => API.API == ContextAPI.OpenGL || API.API == ContextAPI.OpenGLES
            ? _ctx ??= new SdlContext(SdlWindow, this)
            : null;
        protected override IVkSurface? CoreVkSurface => API.API == ContextAPI.Vulkan
            ? _vk ??= new SdlVkSurface(this)
            : null;
        protected override nint CoreHandle => (nint) SdlWindow;
        internal SDL_Window* SdlWindow { get; private set; }
        internal bool IsClosingVal { get; set; }
        protected override bool CoreIsClosing => IsClosingVal;
        public override bool IsEventDriven { get; set; }
        public List<SDL_Event> Events { get; } = [];
        protected SdlView? ParentView { get; }
        protected SdlMonitor? InitialMonitor { get; set; }

        public override Vector2D<int> FramebufferSize => (_ctx as SdlContext)?.FramebufferSize ?? CoreSize;

        public override VideoMode VideoMode
        {
            get
            {
                var mode = SDL3.SDL_GetWindowFullscreenMode(SdlWindow);
                return mode != null
                    ? new VideoMode(new Vector2D<int>(mode->w, mode->h), (int)mode->refresh_rate)
                    : default;
            }
        }

        protected override Vector2D<int> CoreSize
        {
            get
            {
                var ret = stackalloc int[2];
                SDL3.SDL_GetWindowSize(SdlWindow, ret, &ret[1]);
                return *(Vector2D<int>*) ret;
            }
        }

        // Methods
        public override void ContinueEvents() => Interlocked.Exchange(ref _continue, 1);
        protected override INativeWindow GetNativeWindow() => new SdlNativeWindow(SdlWindow);

        protected override void CoreInitialize(ViewOptions opts) => CoreInitialize
            (opts, null, null, null, null, null, null, null);

        protected void CoreInitialize
        (
            ViewOptions opts,
            SDL_WindowFlags? additionalFlags,
            int? x,
            int? y,
            int? w,
            int? h,
            string? title,
            IGLContext? sharedContext)
        {
            var flags = SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;
            if (additionalFlags is null)
            {
                flags |= _platform.IsViewOnly switch
                {
                    true => SDL_WindowFlags.SDL_WINDOW_BORDERLESS | SDL_WindowFlags.SDL_WINDOW_FULLSCREEN | SDL_WindowFlags.SDL_WINDOW_RESIZABLE,
                    false => SDL_WindowFlags.SDL_WINDOW_RESIZABLE
                };
            }
            else
            {
                flags |= additionalFlags.Value;
            }

            // Set window API.
            switch (opts.API.API)
            {
                case ContextAPI.None:
                {
                    break;
                }
                case ContextAPI.Vulkan:
                {
                    flags |= SDL_WindowFlags.SDL_WINDOW_VULKAN;
                    break;
                }
                case ContextAPI.OpenGLES:
                case ContextAPI.OpenGL:
                {
                    flags |= SDL_WindowFlags.SDL_WINDOW_OPENGL;
                    break;
                }
            }

            IsClosingVal = false;

            // Set window GL attributes
            if (opts.PreferredDepthBufferBits != -1)
            {
                SDL3.SDL_GL_SetAttribute
                (
                    SDL_GLAttr.SDL_GL_DEPTH_SIZE,
                    opts.PreferredDepthBufferBits ?? 24
                );
            }

            if (opts.PreferredStencilBufferBits != -1)
            {
                SDL3.SDL_GL_SetAttribute
                (
                    SDL_GLAttr.SDL_GL_STENCIL_SIZE,
                    opts.PreferredStencilBufferBits ?? 8
                );
            }

            if (opts.PreferredBitDepth?.X != -1)
            {
                SDL3.SDL_GL_SetAttribute
                (
                    SDL_GLAttr.SDL_GL_RED_SIZE,
                    opts.PreferredBitDepth?.X ?? 8
                );
            }

            if (opts.PreferredBitDepth?.Y != -1)
            {
                SDL3.SDL_GL_SetAttribute
                (
                    SDL_GLAttr.SDL_GL_GREEN_SIZE,
                    opts.PreferredBitDepth?.Y ?? 8
                );
            }

            if (opts.PreferredBitDepth?.Z != -1)
            {
                SDL3.SDL_GL_SetAttribute
                (
                    SDL_GLAttr.SDL_GL_BLUE_SIZE,
                    opts.PreferredBitDepth?.Z ?? 8
                );
            }

            if (opts.PreferredBitDepth?.W != -1)
            {
                SDL3.SDL_GL_SetAttribute
                (
                    SDL_GLAttr.SDL_GL_ALPHA_SIZE,
                    opts.PreferredBitDepth?.W ?? 8
                );
            }

            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_MULTISAMPLEBUFFERS, (opts.Samples == null || opts.Samples == -1) ? 0 : 1);
            SDL3.SDL_GL_SetAttribute(SDL_GLAttr.SDL_GL_MULTISAMPLESAMPLES, (opts.Samples == null || opts.Samples == -1) ? 0 : opts.Samples.Value);

            // Create window
            /*SdlWindow = SDL3.SDL_CreateWindow
            (
                title ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "Silk.NET Window",
                x ?? 50,
                y ?? 50,
                w ?? 1280,
                h ?? 720,
                (uint) flags
            );*/

            SDL_PropertiesID props = SDL3.SDL_CreateProperties();
            SDL3.SDL_SetStringProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_TITLE_STRING, title ?? Assembly.GetEntryAssembly()?.GetName().Name ?? "Silk.NET Window");
            SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_X_NUMBER, x ?? 50);
            SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_Y_NUMBER, y ?? 50);
            SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_WIDTH_NUMBER, w ?? 1280);
            SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_HEIGHT_NUMBER, h ?? 720);
            // For window flags you should use separate window creation properties,
            // but for easier migration from SDL2 you can use the following:
            SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_FLAGS_NUMBER, (long)flags);
            SdlWindow = SDL3.SDL_CreateWindowWithProperties(props);
            SDL3.SDL_DestroyProperties(props);

            if (SdlWindow == null)
            {
                SdlExt.ThrowError();
            }

            sharedContext?.MakeCurrent();
            (CoreGLContext as SdlContext)?.Create
            (
                (SDL_GLAttr.SDL_GL_CONTEXT_MAJOR_VERSION, opts.API.Version.MajorVersion),
                (SDL_GLAttr.SDL_GL_CONTEXT_MINOR_VERSION, opts.API.Version.MinorVersion),
                (
                    SDL_GLAttr.SDL_GL_CONTEXT_PROFILE_MASK,
                    (int) (opts.API.API == ContextAPI.OpenGLES
                        ? SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_ES
                        : opts.API.Profile switch
                        {
                            ContextProfile.Core => SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_CORE,
                            ContextProfile.Compatability => SDL_GLProfile.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY,
                            _ => throw new System.ArgumentOutOfRangeException(nameof(opts), "Bad ContextProfile")
                        })
                ),
                (SDL_GLAttr.SDL_GL_CONTEXT_FLAGS, (int) opts.API.Flags),
                (SDL_GLAttr.SDL_GL_SHARE_WITH_CURRENT_CONTEXT, sharedContext is null ? 0 : 1)
            );
            if (SdlWindow == null)
            {
                throw new PlatformException(SdlExt.GetErrorAsException());
            }

            var mode = SDL3.SDL_GetWindowFullscreenMode(SdlWindow);
            if (mode != null)
            {
                if (opts.VideoMode.RefreshRate.HasValue)
                {
                    mode->refresh_rate = opts.VideoMode.RefreshRate.Value;
                    SDL3.SDL_SetWindowFullscreenMode(SdlWindow, mode);
                }
            }
            else
            {
                SDL3.SDL_ClearError();
            }

            SdlExt.ThrowError();
        }

#if __IOS__
        private static bool _isRunning;
        public override void Run(Action onFrame)
        {
            if (_isRunning)
            {
                throw new NotSupportedException("A view is already running in this mobile application.");
            }

            if (!SilkMobile.IsRunning)
            {
                throw new InvalidOperationException
                (
                    "The view could not be created as the underlying mobile application is not running. On iOS, " +
                    "please wrap your main function in a call to SilkMobile.RunApp to ensure that application " +
                    "lifecycles can be managed properly."
                );
            }
            
            // This is not correct, we should be using SDL_iPhoneSetAnimationCallback and then letting
            // SDL_UIKitRunApp take care of the lifetime, but this would be a breaking change as Run in 2.X is expected
            // to only exit when the view does. We'll fix this properly in 3.0.
            _isRunning = true;
            base.Run(onFrame);
            _isRunning = false;
        }
#endif

        protected override void CoreReset()
        {
            if (SdlWindow == null)
            {
                return;
            }

            CoreGLContext?.Dispose();
            SDL3.SDL_DestroyWindow(SdlWindow);

            SdlWindow = null;
            _ctx = null;
            _vk = null;
        }

        public override void DoEvents()
        {
            do
            {
                _platform.DoEvents();
            } while (IsEventDriven && Events.Count == 0 && Interlocked.CompareExchange(ref _continue, 0, 1) == 0);

            ProcessEvents();
        }

        private void ClearEvents()
        { 
            // remove events in reverse order to prevent shuffling in the list
            for (var i = Events.Count - 1; i >= 0; i--)
            {
                RemoveEvent(i);
            }
        }
        
        internal void RemoveEvent(int index)
        {
            var @event = Events[index];
            if (@event.Type == SDL_EventType.SDL_EVENT_DROP_FILE)
            {
                SDL3.SDL_free(@event.drop.data);
            }

            Events.RemoveAt(index);
        }

        ~SdlView()
        {
            Reset();
        }

        public override void Focus()
        {
            SDL3.SDL_RaiseWindow(SdlWindow);
        }

        public override void Close()
        {
            Closing?.Invoke();
            IsClosingVal = true;
        }

        protected override void RegisterCallbacks()
        {
            _id = SDL3.SDL_GetWindowID(SdlWindow);
            _platform.EventReceived += OnEventReceived;
        }

        protected override void UnregisterCallbacks()
        {
            _id = null;
            _platform.EventReceived -= OnEventReceived;
        }


        public override Vector2D<int> PointToClient(Vector2D<int> point)
        {
            int x, y;
            SDL3.SDL_GetWindowPosition(SdlWindow, &x, &y);
            return new Vector2D<int>(point.X - x, point.Y - y);
        }

        public override Vector2D<int> PointToScreen(Vector2D<int> point)
        {
            int x, y;
            SDL3.SDL_GetWindowPosition(SdlWindow, &x, &y);
            return new Vector2D<int>(point.X + x, point.Y + y);
        }

        public void BeginEventProcessing(ref bool taken) => _eventSync.Enter(ref taken);
        public void EndEventProcessing(bool taken)
        {
            if (taken)
            {
                _eventSync.Exit();
            }
        }

        [SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
        public virtual void ProcessEvents()
        {
            var taken = false;
            BeginEventProcessing(ref taken);
            var count = Events.Count;
            var i = 0;
            for (var j = 0; j < count; j++)
            {
                var @event = Events[i];
                var skipped = false;
                switch (@event.Type)
                {
                    case SDL_EventType.SDL_EVENT_TERMINATING:
                    case SDL_EventType.SDL_EVENT_QUIT:
                    {
                        Close();
                        break;
                    }
                    //case EventType.AppTerminating:
                    //    break;
                    //case EventType.AppLowmemory:
                    //    break;
                    //case EventType.AppWillenterbackground: TODO Pausing event
                    //    break;
                    //case EventType.AppDidenterbackground:
                    //    break;
                    //case EventType.AppWillenterforeground: TODO Resuming event
                    //    break;
                    //case EventType.AppDidenterforeground:
                    //    break;
                            case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                            {
                                Resize?.Invoke(new Vector2D<int>(@event.window.data1, @event.window.data2));
                                FramebufferResize?.Invoke(FramebufferSize);
                                break;
                            }
                            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                            {
                                FocusChanged?.Invoke(true);
                                break;
                            }
                            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                            {
                                FocusChanged?.Invoke(false);
                                break;
                            }
                            case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                            {
                                Close();
                                break;
                            }
                    default:
                    {
                        i++;
                        skipped = true;
                        break;
                    }
                }

                if (!skipped)
                {
                    RemoveEvent(i);
                }
            }
            
            EndEventProcessing(taken);
        }

        private void OnEventReceived(IEnumerable<SDL_Event> events)
        {
            var taken = false;
            BeginEventProcessing(ref taken);
            foreach (var @event in events)
            {
                {
                    Events.Add(@event);
                }
            }
            
            EndEventProcessing(taken);
        }

        internal override void AfterProcessingEvents()
        {
            ClearEvents();
        }
    }
}
