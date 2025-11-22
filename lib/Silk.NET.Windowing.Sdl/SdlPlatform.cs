// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using SDL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;

[assembly: WindowPlatform(typeof(SdlPlatform))]

namespace Silk.NET.Windowing.Sdl
{
    internal class SdlPlatform : IWindowPlatform
    {
        private SdlView? _view;
        private List<SDL_Event> _eventBuffer = [];
        private BreakneckLock _lock = BreakneckLock.Create();
        public static SdlPlatform GetOrRegister()
        {
            var val = Window.GetOrDefault<SdlPlatform>();
            if (val is null)
            {
                Window.Add(val = new SdlPlatform());
            }

            return val;
        }

        private Lazy<bool> _isApplicable = new Lazy<bool>
        (
            () =>
            {
                return true;
            }
        );

        public unsafe IWindow CreateWindow(WindowOptions opts)
        {
            if (!IsApplicable)
            {
                ThrowUnsupported();
                return null!;
            }

            if (IsViewOnly)
            {
                throw new PlatformNotSupportedException("Platform is view-only.");
            }

            return (SdlWindow)(_view = new SdlWindow(opts, null, null, this));
        }

        string Name => nameof(SdlPlatform);

        public unsafe bool IsViewOnly
        {
            get
            {
                if (!IsApplicable) return false;
                var platformPtr = SDL3.SDL_GetPlatform();
                var platform = platformPtr ?? "";
                return platform switch
                {
                    "Windows" => false,
                    "Mac OS X" => false,
                    "Linux" => false,
                    _ => true
                };
            }
        }

        public bool IsApplicable => _isApplicable.Value;
        public event Action<List<SDL_Event>>? EventReceived;

        public IView GetView(ViewOptions? opts = null)
        {
            if (!IsApplicable)
            {
                ThrowUnsupported();
                return null!;
            }

            return opts switch
            {
                null when _view is null => throw new InvalidOperationException
                (
                    "No view has been created prior to this call, and couldn't " +
                    "create one due to no view options being provided."
                ),
                null => _view!,
                _ => IsViewOnly
                    ? _view ??= new SdlView(opts!.Value, null, null, this)
                    : _view = (SdlView) CreateWindow(new WindowOptions(opts!.Value))
            };
        }

        public unsafe void ClearContexts()
        {
            if (!IsApplicable)
            {
                ThrowUnsupported();
                return;
            }

            var currentWindow = SDL3.SDL_GL_GetCurrentWindow();
            SDL3.SDL_GL_MakeCurrent(currentWindow, null);
        }

        public unsafe IEnumerable<IMonitor> GetMonitors()
        {
            if (!IsApplicable)
            {
                ThrowUnsupported();
                yield break;
            }

            var displays = SDL3.SDL_GetDisplays();
            for (var i = 0; i < displays.Count; i++)
            {
                yield return new SdlMonitor(this, displays[i]);
            }
            displays.Dispose();
        }

        public IMonitor GetMainMonitor()
        {
            if (!IsApplicable)
            {
                ThrowUnsupported();
                return null!;
            }
            
            return new SdlMonitor(this, 0);
        }

        private void ThrowUnsupported()
            => throw new PlatformNotSupportedException("SDL not supported on this platform");

        public bool IsSourceOfView(IView view) => view is SdlView;

        public unsafe SdlView From(void* handle, IGLContext? ctx)
            => IsViewOnly ? new SdlView(handle, ctx, this) : new SdlWindow(handle, ctx, this);

        public unsafe void DoEvents()
        {
            var taken = false;
            _lock.TryEnter(ref taken);
            if (!taken)
            {
                return;
            }

            SDL_Event @event;
            while (SDL3.SDL_PollEvent(&@event))
            {
                _eventBuffer.Add(@event);
            }

            EventReceived?.Invoke(_eventBuffer);
            _eventBuffer.Clear();
            if (taken)
            {
                _lock.Exit();
            }
        }
    }
}
