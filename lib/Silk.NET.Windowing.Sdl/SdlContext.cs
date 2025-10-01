// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using SDL;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;
using Silk.NET.Maths;
using Silk.NET.Windowing.Sdl;

namespace Silk.NET.SDL
{
    public unsafe class SdlContext : IGLContext
    {
        private SDL_GLContextState* _ctx;
        private SDL_Window* _window;

        /// <summary>
        /// Creates a <see cref="SdlContext"/> from a native window using the given native interface.
        /// </summary>
        /// <param name="sdl">The native interface to use.</param>
        /// <param name="window">The native window to associate this context for.</param>
        /// <param name="source">The <see cref="IGLContextSource" /> to associate this context to, if any.</param>
        /// <param name="attributes">The attributes to eagerly pass to <see cref="Create"/>.</param>
        public SdlContext(
            SDL_Window* window,
            IGLContextSource? source = null,
            params (SDL_GLAttr Attribute, int Value)[] attributes)
        {
            Window = window;
            Source = source;
            if (attributes is not null && attributes.Length > 0)
            {
                Create(attributes);
            }
        }

        /// <summary>
        /// The native window to create a context for.
        /// </summary>
        public SDL_Window* Window
        {
            get => _window;
            set
            {
                AssertNotCreated();
                _window = value;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public Vector2D<int> FramebufferSize
        {
            get
            {
                AssertCreated();
                var ret = stackalloc int[2];
                SDL3.SDL_GetWindowSizeInPixels(Window, ret, &ret[1]);
                SdlExt.ThrowError();
                return *(Vector2D<int>*) ret;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public void Create(params (SDL_GLAttr Attribute, int Value)[] attributes)
        {
            foreach (var (attribute, value) in attributes)
            {
                if (!SDL3.SDL_GL_SetAttribute(attribute, value))
                {
                    SdlExt.ThrowError();
                }
            }

            _ctx = SDL3.SDL_GL_CreateContext(Window);
            if (_ctx == null)
            {
                SdlExt.ThrowError();
            }
        }

        private void AssertCreated()
        {
            if (_ctx == null)
            {
                throw new InvalidOperationException("Context not created.");
            }
        }

        private void AssertNotCreated()
        {
            if (_ctx != null)
            {
                throw new InvalidOperationException("Context created already.");
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public void Dispose()
        {
            if (_ctx != null)
            {
                SDL3.SDL_GL_DestroyContext(_ctx);
                _ctx = null;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public nint GetProcAddress(string proc, int? slot = default)
        {
            AssertCreated();
            SDL3.SDL_ClearError();
            var ret = SDL3.SDL_GL_GetProcAddress(proc);
            //Console.Out.WriteLine($"{proc} {ret}");
            SdlExt.ThrowError();
            if (ret == 0)
            {
                Throw(proc);
                return 0;
            }

            return ret;
            static void Throw(string proc) => throw new SymbolLoadingException(proc);
        }

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = default)
        {
            addr = 0;
            SDL3.SDL_ClearError();
            if (_ctx is null)
            {
                return false;
            }
            
            var ret = (nint) SDL3.SDL_GL_GetProcAddress(proc);
            if (!string.IsNullOrWhiteSpace(SDL3.SDL_GetError()))
            {
                SDL3.SDL_ClearError();
                return false;
            }

            return (addr = ret) != 0;
        }

        /// <inheritdoc cref="IGLContext" />
        public nint Handle
        {
            get
            {
                AssertCreated();
                return (nint) _ctx;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public IGLContextSource? Source { get; }

        /// <inheritdoc cref="IGLContext" />
        public bool IsCurrent
        {
            get
            {
                AssertCreated();
                return SDL3.SDL_GL_GetCurrentContext() == _ctx;
            }
        }

        /// <inheritdoc cref="IGLContext" />
        public void SwapInterval(int interval)
        {
            AssertCreated();
            SDL3.SDL_GL_SetSwapInterval(interval);
        }

        /// <inheritdoc cref="IGLContext" />
        public void SwapBuffers()
        {
            AssertCreated();
            SDL3.SDL_GL_SwapWindow(Window);
        }

        /// <inheritdoc cref="IGLContext" />
        public void MakeCurrent()
        {
            AssertCreated();
            SDL3.SDL_GL_MakeCurrent(Window, _ctx);
        }

        /// <inheritdoc cref="IGLContext" />
        public void Clear()
        {
            AssertCreated();
            if (IsCurrent)
            {
                SDL3.SDL_GL_MakeCurrent(Window, null);
            }
        }
    }
}
