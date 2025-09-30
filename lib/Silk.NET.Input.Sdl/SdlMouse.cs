using System;
using System.Collections.Generic;
using System.Numerics;
using SDL;
using Silk.NET.Input.Internals;
using Silk.NET.SDL;

namespace Silk.NET.Input.Sdl
{
    internal class SdlMouse : MouseImplementationBase, ISdlDevice
    {
        private readonly SdlInputContext _ctx;
        private readonly List<MouseButton> _downButtons = new List<MouseButton>();
        private readonly ScrollWheel[] _scrollWheels = new ScrollWheel[1];

        private bool _wheelChanged;

        /// <summary>
        /// Used by SdlCursor.
        /// </summary>
        internal bool IsRaw { get; set; }

        /// <summary>
        /// The current raw mouse position. This is done because when SDL is in raw mouse mode, it doesn't allow
        /// unlimited mouse movement like GLFW does so we have to simulate it.
        /// </summary>
        internal Vector2 AggregatePoint { get; set; }

        public SdlMouse(SdlInputContext ctx)
        {
            _ctx = ctx;
            Cursor = new SdlCursor(this, ctx);
        }

        public override string Name { get; } = "Silk.NET Mouse (via SDL)";
        public override int Index { get; } = 0;
        public override bool IsConnected { get; } = true;

        public override IReadOnlyList<MouseButton> SupportedButtons { get; } = new[]
            {MouseButton.Left, MouseButton.Middle, MouseButton.Right, MouseButton.Button4, MouseButton.Button5};

        public override IReadOnlyList<ScrollWheel> ScrollWheels => _scrollWheels;

        public override unsafe Vector2 Position
        {
            get
            {
                float x, y;
                SDL3.SDL_GetMouseState(&x, &y);
                return new Vector2(x, y);
            }
            set => SDL3.SDL_WarpMouseInWindow((SDL_Window*) _ctx.Handle, (int) value.X, (int) value.Y);
        }

        public override ICursor Cursor { get; }
        public override bool IsButtonPressed(MouseButton btn) => _downButtons.Contains(btn);
        public override event Action<IMouse, Vector2>? MouseMove;
        public override event Action<IMouse, ScrollWheel>? Scroll;

        public void Update()
        {
            if (!_wheelChanged && _scrollWheels[0] != default)
            {
                _scrollWheels[0] = default;
            }
            _wheelChanged = false;
        }

        public void DoEvent(SDL_Event @event)
        {
            switch (@event.Type)
            {
                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                {
                    if (IsRaw)
                    {
                        var aggr = AggregatePoint += new Vector2(@event.motion.xrel, @event.motion.yrel);
                        MouseMove?.Invoke(this, aggr);
                    }
                    else
                    {
                        MouseMove?.Invoke(this, new Vector2(@event.motion.x, @event.motion.y));
                    }

                    break;
                }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                {
                    _downButtons.Add(MapButton(@event.button.button));
                    HandleMouseDown(this, MapButton(@event.button.button));
                    break;
                }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                {
                    _downButtons.Remove(MapButton(@event.button.button));
                    HandleMouseUp(this, MapButton(@event.button.button));
                    break;
                }
                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                {
                    ref var wheelData = ref @event.wheel;
                    _wheelChanged = true;
                    _scrollWheels[0] = new ScrollWheel(wheelData.x, wheelData.y);
                    Scroll?.Invoke(this, _scrollWheels[0]);
                    break;
                }
            }
        }

        private static MouseButton MapButton(byte btn) => btn switch
        {
            1 => MouseButton.Left,
            2 => MouseButton.Middle,
            3 => MouseButton.Right,
            _ => (MouseButton) (btn - 1)
        };
    }
}
