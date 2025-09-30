using System;
using System.Collections.Generic;
using SDL;
using Silk.NET.SDL;

namespace Silk.NET.Input.Sdl
{
    internal class SdlGamepad : IGamepad, ISdlDevice, ISdlJoystick, IDisposable
    {
        private readonly SdlInputContext _ctx;
        private readonly unsafe SDL_Gamepad* _controller;

        private readonly Button[] _buttons =
        {
            new Button(ButtonName.A, 0x0, false),
            new Button(ButtonName.B, 0x1, false),
            new Button(ButtonName.X, 0x2, false),
            new Button(ButtonName.Y, 0x3, false),
            new Button(ButtonName.Back, 0x4, false),
            new Button(ButtonName.Home, 0x5, false),
            new Button(ButtonName.Start, 0x6, false),
            new Button(ButtonName.LeftStick, 0x7, false),
            new Button(ButtonName.RightStick, 0x8, false),
            new Button(ButtonName.LeftBumper, 0x9, false),
            new Button(ButtonName.RightBumper, 0xA, false),
            new Button(ButtonName.DPadUp, 0xB, false),
            new Button(ButtonName.DPadDown, 0xC, false),
            new Button(ButtonName.DPadLeft, 0xD, false),
            new Button(ButtonName.DPadRight, 0xE, false)
        };

        private readonly bool[] _thumbsticksChanged = new bool[2];

        private readonly Thumbstick[] _thumbsticks =
        {
            new Thumbstick(0, 0, 0),
            new Thumbstick(1, 0, 0)
        };

        private readonly Trigger[] _triggers =
        {
            new Trigger(0, 0),
            new Trigger(1, 0)
        };

        public unsafe SdlGamepad(SdlInputContext ctx, int currentIndex, SDL_JoystickID instanceId)
        {
            _ctx = ctx;
            InstanceId = instanceId;
            ActualIndex = currentIndex;
            VibrationMotors = new IMotor[] {new SdlMotor(0, this), new SdlMotor(1, this)};
            _controller = SDL3.SDL_OpenGamepad((SDL_JoystickID)currentIndex);
        }

        public unsafe string Name =>
            IsConnected ? SDL3.SDL_GetGamepadName(_controller) ?? "Silk.NET Gamepad (via SDL)" : "Silk.NET Gamepad (via SDL)";

        public int Index => ActualIndex;
        public bool IsConnected => SDL3.SDL_IsGamepad((SDL_JoystickID)Index) == true;
        public IReadOnlyList<Button> Buttons => _buttons;
        public IReadOnlyList<Thumbstick> Thumbsticks => _thumbsticks;
        public IReadOnlyList<Trigger> Triggers => _triggers;
        public IReadOnlyList<IMotor> VibrationMotors { get; }
        public Deadzone Deadzone { get; set; }
        public event Action<IGamepad, Button>? ButtonDown;
        public event Action<IGamepad, Button>? ButtonUp;
        public event Action<IGamepad, Thumbstick>? ThumbstickMoved;
        public event Action<IGamepad, Trigger>? TriggerMoved;

        public void DoEvent(SDL_Event @event)
        {
            switch (@event.Type)
            {
                case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                {
                    switch (@event.gaxis.Axis)
                    {
                        case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX:
                        {
                            var thumbstick0 = new Thumbstick(0,
                                Deadzone.Apply((float) @event.gaxis.value / short.MaxValue),
                                Deadzone.Apply(_thumbsticks[0].Y));

                            if (thumbstick0.X != _thumbsticks[0].X)
                            {
                                _thumbsticksChanged[0] = true;
                            }
                            _thumbsticks[0] = thumbstick0;
                            break;
                        }
                        case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY:
                        {
                            var thumbstick0 = new Thumbstick(0,
                                Deadzone.Apply(_thumbsticks[0].X),
                                Deadzone.Apply((float) @event.gaxis.value / short.MaxValue));

                            if (thumbstick0.Y != _thumbsticks[0].Y)
                            {
                                _thumbsticksChanged[0] = true;
                            }
                            _thumbsticks[0] = thumbstick0;
                            break;
                        }
                        case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX:
                        {
                            var thumbstick1 = new Thumbstick(1,
                                Deadzone.Apply((float) @event.gaxis.value / short.MaxValue),
                                Deadzone.Apply(_thumbsticks[1].Y));

                            if (thumbstick1.X != _thumbsticks[1].X)
                            {
                                _thumbsticksChanged[1] = true;
                            }
                            _thumbsticks[1] = thumbstick1;
                            break;
                        }
                        case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY:
                        {
                            var thumbstick1 = new Thumbstick(1,
                                Deadzone.Apply(_thumbsticks[1].X),
                                Deadzone.Apply((float) @event.gaxis.value / short.MaxValue));

                            if (thumbstick1.Y != _thumbsticks[1].Y)
                            {
                                _thumbsticksChanged[1] = true;
                            }
                            _thumbsticks[1] = thumbstick1;
                            break;
                        }
                        case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER:
                        {
                            TriggerMoved?.Invoke
                                (this, _triggers[0] = new Trigger(0, (float) @event.gaxis.value / short.MaxValue));
                            break;
                        }
                        case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER:
                        {
                            TriggerMoved?.Invoke
                                (this, _triggers[1] = new Trigger(1, (float) @event.gaxis.value / short.MaxValue));
                            break;
                        }
                    }

                    break;
                }
                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                {
                    var ogBtn = _buttons[@event.gbutton.button];
                    ButtonDown?.Invoke
                        (this, _buttons[@event.gbutton.button] = new Button(ogBtn.Name, ogBtn.Index, true));
                    break;
                }
                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                {
                    var ogBtn = _buttons[@event.gbutton.button];
                    ButtonUp?.Invoke
                        (this, _buttons[@event.gbutton.button] = new Button(ogBtn.Name, ogBtn.Index, false));
                    break;
                }
                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                {
                    _ctx.ChangeConnection(this, true);
                    break;
                }
                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                {
                    _ctx.ChangeConnection(this, false);
                    break;
                }
            }

            for (var i = 0; i < _thumbsticksChanged.Length; i++)
            {
                if (_thumbsticksChanged[i])
                {
                    ThumbstickMoved?.Invoke(this, _thumbsticks[i]);
                    _thumbsticksChanged[i] = false;
                }
            }
        }

        public unsafe void Update()
        {
            if (VibrationChanged)
            {
                SDL3.SDL_RumbleGamepad
                (
                    _controller, (ushort) (0xffff / VibrationMotors[0].Speed),
                    (ushort) (0xffff / VibrationMotors[1].Speed), 0xffff
                );

                VibrationChanged = false;
            }
        }

        public int ActualIndex { get; set; }
        public SDL_JoystickID InstanceId { get; }
        public bool VibrationChanged { get; set; }

        public unsafe void Dispose()
        {
            SDL3.SDL_CloseGamepad(_controller);
        }
    }
}
