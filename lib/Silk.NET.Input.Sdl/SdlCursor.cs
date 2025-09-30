using System;
using System.Collections.Generic;
using Silk.NET.Core;
using SDL;

namespace Silk.NET.Input.Sdl
{
    internal class SdlCursor : ICursor
    {
        private readonly SdlMouse _mouse;
        private readonly SdlInputContext _ctx;

        private static readonly Dictionary<StandardCursor, SDL_SystemCursor> _cursorShapes =
            new Dictionary<StandardCursor, SDL_SystemCursor>
            {
                {StandardCursor.Arrow, SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT},
                {StandardCursor.IBeam, SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT},
                {StandardCursor.Crosshair, SDL_SystemCursor.SDL_SYSTEM_CURSOR_CROSSHAIR},
                {StandardCursor.Hand, SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER},
                {StandardCursor.HResize, SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE},
                {StandardCursor.VResize, SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE},
                {StandardCursor.NwseResize, SDL_SystemCursor.SDL_SYSTEM_CURSOR_NWSE_RESIZE},
                {StandardCursor.NeswResize, SDL_SystemCursor.SDL_SYSTEM_CURSOR_NESW_RESIZE},
                {StandardCursor.ResizeAll, SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE},
                {StandardCursor.NotAllowed, SDL_SystemCursor.SDL_SYSTEM_CURSOR_NOT_ALLOWED},
                {StandardCursor.Wait, SDL_SystemCursor.SDL_SYSTEM_CURSOR_WAIT},
                {StandardCursor.WaitArrow, SDL_SystemCursor.SDL_SYSTEM_CURSOR_PROGRESS}
            };

        private const int BytesPerCursorPixel = 4;

        private unsafe SDL_Cursor* _cursor = null;
        private unsafe SDL_Surface* _cursorSurface = null;
        private CursorType _cursorType = CursorType.Standard;
        private StandardCursor _standardCursor = StandardCursor.Default;
        private int _hotspotX = 0;
        private int _hotspotY = 0;
        private RawImage _image;

        internal unsafe SdlCursor(SdlMouse mouse, SdlInputContext ctx)
        {
            _mouse = mouse;
            _ctx = ctx;
        }

        /// <inheritdoc />
        public unsafe CursorType Type
        {
            get => _cursorType;
            set
            {
                if (_cursorType != value)
                {
                    _cursorType = value;

                    SDL_Surface* surface = null;
                    var c = _cursorType switch
                    {
                        CursorType.Standard => CreateStandardCursor(),
                        CursorType.Custom => CreateCustomCursor(out surface),
                        _ => throw new InvalidOperationException("SDL does not support the given cursor type.")
                    };

                    SDL3.SDL_SetCursor(c);
                    if (_cursor != null)
                    {
                        // destroy the old custom cursor
                        SDL3.SDL_DestroyCursor(_cursor);
                        _cursor = null;
                    }

                    if (_cursorSurface != null)
                    {
                        SDL3.SDL_DestroySurface(_cursorSurface);
                        _cursorSurface = null;
                    }

                    _cursor = c;
                    _cursorSurface = surface;
                }
            }
        }

        /// <inheritdoc />
        public StandardCursor StandardCursor
        {
            get => _standardCursor;
            set
            {
                if (_standardCursor != value)
                {
                    _standardCursor = value;
                    UpdateStandardCursor();
                }
            }
        }

        /// <inheritdoc />
        public unsafe CursorMode CursorMode
        {
            get => SDL3.SDL_GetWindowRelativeMouseMode((SDL_Window*) _ctx.Handle) ? CursorMode.Raw :
                SDL3.SDL_CursorVisible() ? CursorMode.Normal : CursorMode.Hidden;
            set
            {
                switch (value)
                {
                    case CursorMode.Normal:
                    {
                        _mouse.IsRaw = false;
                        SDL3.SDL_SetWindowRelativeMouseMode((SDL_Window*) _ctx.Handle, false);
                        SDL3.SDL_ShowCursor();
                        break;
                    }
                    case CursorMode.Hidden:
                    {
                        _mouse.IsRaw = false;
                        SDL3.SDL_SetWindowRelativeMouseMode((SDL_Window*) _ctx.Handle, false);
                        SDL3.SDL_HideCursor();
                        break;
                    }
                    case CursorMode.Disabled:
                    {
                        throw new PlatformNotSupportedException("CursorMode.Disabled is not supported by SDL.");
                    }
                    case CursorMode.Raw:
                    {
                        _mouse.IsRaw = true;
                        _mouse.AggregatePoint = default;
                        SDL3.SDL_SetWindowRelativeMouseMode((SDL_Window*) _ctx.Handle, true);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }

        public unsafe bool IsConfined
        {
            get => SDL3.SDL_GetWindowMouseGrab((SDL_Window*) _ctx.Handle);
            set => SDL3.SDL_SetWindowMouseGrab((SDL_Window*) _ctx.Handle, value);
        }

        /// <inheritdoc />
        public int HotspotX
        {
            get => _hotspotX;
            set
            {
                if (_hotspotX != value)
                {
                    _hotspotX = value;
                    UpdateCustomCursor();
                }
            }
        }

        /// <inheritdoc />
        public int HotspotY
        {
            get => _hotspotY;
            set
            {
                if (_hotspotY != value)
                {
                    _hotspotY = value;
                    UpdateCustomCursor();
                }
            }
        }

        /// <inheritdoc />
        public RawImage Image
        {
            get => _image;
            set
            {
                if (_image != value)
                {
                    _image = value;
                    UpdateCustomCursor();
                }
            }
        }

        /// <inheritdoc />
        public bool IsSupported(CursorMode mode)
        {
            return mode switch
            {
                CursorMode.Normal => true,
                CursorMode.Hidden => true,
                CursorMode.Disabled => false, // TODO maybe in the future we can implement this manually?
                CursorMode.Raw => true,
                _ => false
            };
        }

        /// <inheritdoc />
        public bool IsSupported(StandardCursor standardCursor)
        {
            return standardCursor switch
            {
                StandardCursor.Default => true,
                StandardCursor.Arrow => true,
                StandardCursor.IBeam => true,
                StandardCursor.Crosshair => true,
                StandardCursor.Hand => true,
                StandardCursor.HResize => true,
                StandardCursor.VResize => true,
                _ => false
            };
        }

        public unsafe void Dispose()
        {
            if (_cursor != null)
            {
                SDL3.SDL_DestroyCursor(_cursor);
            }

            if (_cursorSurface != null)
            {
                SDL3.SDL_DestroySurface(_cursorSurface);
            }

            _cursor = null;
            _cursorSurface = null;
            _standardCursor = StandardCursor.Default;
            _cursorType = CursorType.Standard;
            SDL3.SDL_SetCursor((SDL_Cursor*) 0);
        }

        private unsafe SDL_Cursor* CreateStandardCursor()
        {
            if (_standardCursor == StandardCursor.Default)
                return null;
            else
            {
                if (!_cursorShapes.ContainsKey(_standardCursor))
                    throw new InvalidOperationException("Sdl does not support the given standard cursor.");

                return SDL3.SDL_CreateSystemCursor(_cursorShapes[_standardCursor]);
            }
        }

        private unsafe SDL_Cursor* CreateCustomCursor(out SDL_Surface* surface)
        {
            surface = null;
            if (_image.Pixels.IsEmpty || _image.Width <= 0 || _image.Height <= 0)
                return null;

            if (_image.Pixels.Length % BytesPerCursorPixel != 0)
                throw new ArgumentOutOfRangeException
                    ($"Pixel data must provide a multiple of {BytesPerCursorPixel} bytes.");

            // the user might setup the values step-by-step, so use the
            // default cursor as long as the custom cursor state is not valid
            if (_image.Width * _image.Height * BytesPerCursorPixel != _image.Pixels.Length)
                return null;


            fixed (byte* p = _image.Pixels.Span) {
                surface = SDL3.SDL_CreateSurfaceFrom
                (
                    _image.Width, _image.Height, SDL_PixelFormat.SDL_PIXELFORMAT_ABGR8888,
                    (IntPtr)p, _image.Width * 4
                );

                return SDL3.SDL_CreateColorCursor(surface, 0, 0);
            }
        }

        private unsafe void UpdateStandardCursor()
        {
            var c = CreateStandardCursor();
            SDL3.SDL_SetCursor(c);
            if (_cursor != null)
            {
                SDL3.SDL_DestroyCursor(_cursor);
            }

            if (_cursorSurface != null)
            {
                SDL3.SDL_DestroySurface(_cursorSurface);
            }

            _cursor = c;
            _cursorSurface = null;
        }

        private unsafe void UpdateCustomCursor()
        {
            if (_cursorType == CursorType.Custom)
            {
                var c = CreateCustomCursor(out var surface);
                SDL3.SDL_SetCursor(c);
                if (_cursor != null)
                {
                    SDL3.SDL_DestroyCursor(_cursor);
                }

                if (_cursorSurface != null)
                {
                    SDL3.SDL_DestroySurface(_cursorSurface);
                }

                _cursor = c;
                _cursorSurface = surface;
            }
        }
    }
}
