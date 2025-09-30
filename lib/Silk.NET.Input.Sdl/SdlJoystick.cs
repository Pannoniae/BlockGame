using System;
using System.Collections.Generic;
using SDL;
using Silk.NET.SDL;

namespace Silk.NET.Input.Sdl
{
    internal class SdlJoystick : IJoystick, ISdlDevice, ISdlJoystick, IDisposable
    {
        private readonly SdlInputContext _ctx;
        private readonly unsafe SDL_Joystick* _joystick;

        private Button[] _buttons;
        private Axis[] _axes;
        private Hat[] _hats;

        public unsafe SdlJoystick(SdlInputContext ctx, int currentIndex, SDL_JoystickID instanceId)
        {
            _ctx = ctx;
            InstanceId = instanceId;
            ActualIndex = currentIndex;
            _joystick = SDL3.SDL_OpenJoystick(instanceId);
            _buttons = new Button[0]; // arrays will be sized appropriately later
            _axes = new Axis[0];
            _hats = new Hat[0];
        }

        public unsafe string Name => SDL3.SDL_GetJoystickName(_joystick) ?? "Unknown Joystick";
        public int Index => ActualIndex;
        public int ActualIndex { get; set; }
        public SDL_JoystickID InstanceId { get; }

        public unsafe bool IsConnected => SDL3.SDL_JoystickConnected(_joystick) == true &&
                                          SDL3.SDL_IsGamepad(InstanceId) == false;

        public IReadOnlyList<Axis> Axes => _axes;
        public IReadOnlyList<Button> Buttons => _buttons;
        public IReadOnlyList<Hat> Hats => _hats;
        public Deadzone Deadzone { get; set; }
        public event Action<IJoystick, Button>? ButtonDown;
        public event Action<IJoystick, Button>? ButtonUp;
        public event Action<IJoystick, Axis>? AxisMoved;
        public event Action<IJoystick, Hat>? HatMoved;

        public void DoEvent(SDL_Event @event)
        {
            switch (@event.Type)
            {
                case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
                {
                    if (_axes.Length < @event.jaxis.axis + 1)
                    {
                        Array.Resize(ref _axes, @event.jaxis.axis + 1);
                    }

                    AxisMoved?.Invoke
                    (
                        this,
                        _axes[@event.jaxis.axis] = new Axis
                            (@event.jaxis.axis, (float) @event.jaxis.axis / short.MaxValue)
                    );
                    break;
                }
                case SDL_EventType.SDL_EVENT_JOYSTICK_BALL_MOTION:
                {
                    // todo investigate adding balls to the input spec later down the line
                    break;
                }
                case SDL_EventType.SDL_EVENT_JOYSTICK_HAT_MOTION:
                {
                    if (_hats.Length < @event.jhat.hat + 1)
                    {
                        Array.Resize(ref _hats, @event.jhat.hat + 1);
                    }

                    var val = @event.jhat.value;
                    HatMoved?.Invoke
                    (
                        this,
                        _hats[@event.jhat.hat] = new Hat
                        (
                            @event.jhat.hat, (Position2D) ((val & 0x01) * (int) Position2D.Up +
                                                           (val & 0x02) * (int) Position2D.Right +
                                                           (val & 0x04) * (int) Position2D.Down +
                                                           (val & 0x08) * (int) Position2D.Left)
                        )
                    );
                    break;
                }
                case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
                {
                    if (_buttons.Length < @event.jbutton.button + 1)
                    {
                        Array.Resize(ref _buttons, @event.jbutton.button + 1);
                    }

                    ButtonDown?.Invoke
                    (
                        this,
                        _buttons[@event.jbutton.button] = new Button
                            ((ButtonName) @event.jbutton.button, @event.jbutton.button, true)
                    );
                    break;
                }
                case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
                {
                    if (_buttons.Length < @event.jbutton.button + 1)
                    {
                        Array.Resize(ref _buttons, @event.jbutton.button + 1);
                    }

                    ButtonUp?.Invoke
                    (
                        this,
                        _buttons[@event.jbutton.button] = new Button
                            ((ButtonName) @event.jbutton.button, @event.jbutton.button, false)
                    );
                    break;
                }
                case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
                {
                    _ctx.ChangeConnection(this, true);
                    break;
                }
                case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
                {
                    _ctx.ChangeConnection(this, false);
                    break;
                }
            }
        }

        public unsafe void Dispose()
        {
            SDL3.SDL_CloseJoystick(_joystick);
        }
    }
}
