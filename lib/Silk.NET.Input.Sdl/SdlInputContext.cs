// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using SDL;
using Silk.NET.Input.Internals;
using Silk.NET.Windowing.Sdl;

namespace Silk.NET.Input.Sdl {
    internal class SdlInputContext : InputContextImplementationBase {
        private readonly SdlView _sdlView; // to circumvent CS0122
        private nint _lastHandle;

        public SdlInputContext(SdlView view) : base(view) {
            _sdlView = view;
            SdlGamepads = new Dictionary<SDL_JoystickID, SdlGamepad>();
            SdlJoysticks = new Dictionary<SDL_JoystickID, SdlJoystick>();
            Gamepads = new IsConnectedWrapper<IGamepad>
            (
                new CastReadOnlyList<SdlGamepad, IGamepad>
                    (new ReadOnlyCollectionListAdapter<SdlGamepad>(SdlGamepads.Values))
            );
            Joysticks = new IsConnectedWrapper<IJoystick>
            (
                new CastReadOnlyList<SdlJoystick, IJoystick>
                    (new ReadOnlyCollectionListAdapter<SdlJoystick>(SdlJoysticks.Values))
            );
            Keyboards = new IKeyboard[] { new SdlKeyboard(this) };
            Mice = new IMouse[] { new SdlMouse(this) };
        }

        // Public properties
        public override IReadOnlyList<IGamepad> Gamepads { get; }
        public override IReadOnlyList<IJoystick> Joysticks { get; }
        public override IReadOnlyList<IKeyboard> Keyboards { get; }
        public override IReadOnlyList<IMouse> Mice { get; }
        public override IReadOnlyList<IInputDevice> OtherDevices { get; } = Array.Empty<IInputDevice>();

        // Implementation-specific properties
        public Dictionary<SDL_JoystickID, SdlGamepad> SdlGamepads { get; }
        public Dictionary<SDL_JoystickID, SdlJoystick> SdlJoysticks { get; }

        public override nint Handle => Window.Handle;
        public override event Action<IInputDevice, bool>? ConnectionChanged;

        public override void ProcessEvents() {
            if (Window.Handle == 0) {
                throw new InvalidOperationException("Input update event fired without an underlying window.");
            }

            if (_lastHandle != Window.Handle) {
                RefreshJoysticksAndGamepads();
                _lastHandle = Window.Handle;
            }

            var i = 0;
            var c = _sdlView.Events.Count;
            for (var j = 0; j < c; j++) {
                var @event = _sdlView.Events[i];
                var skipped = false;
                var r = 0;
                switch ((SDL_EventType)@event.Type) {
                    case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    case SDL_EventType.SDL_EVENT_KEY_UP:
                    case SDL_EventType.SDL_EVENT_TEXT_EDITING:
                    case SDL_EventType.SDL_EVENT_TEXT_INPUT: {
                        ((SdlKeyboard)Keyboards[0]).DoEvent(@event);
                        break;
                    }
                    case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    case SDL_EventType.SDL_EVENT_MOUSE_WHEEL: {
                        ((SdlMouse)Mice[0]).DoEvent(@event);
                        break;
                    }
                    case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION: {
                        if (SdlJoysticks.TryGetValue(@event.gaxis.which, out var joy)) {
                            joy.DoEvent(@event);
                            break;
                        }

                        if (r > 0) {
                            // discard this event
                            skipped = true;
                            break;
                        }

                        RefreshJoysticksAndGamepads();
                        r++;
                        goto case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION;
                    }
                    case SDL_EventType.SDL_EVENT_JOYSTICK_BALL_MOTION: {
                        if (SdlJoysticks.TryGetValue(@event.jball.which, out var joy)) {
                            joy.DoEvent(@event);
                            break;
                        }

                        if (r > 0) {
                            // discard this event, this joystick does not exist
                            skipped = true;
                            break;
                        }

                        RefreshJoysticksAndGamepads();
                        r++;
                        goto case SDL_EventType.SDL_EVENT_JOYSTICK_BALL_MOTION;
                    }
                    case SDL_EventType.SDL_EVENT_JOYSTICK_HAT_MOTION: {
                        if (SdlJoysticks.TryGetValue(@event.jhat.which, out var joy)) {
                            joy.DoEvent(@event);
                            break;
                        }

                        if (r > 0) {
                            // discard this event, this joystick does not exist
                            skipped = true;
                            break;
                        }

                        RefreshJoysticksAndGamepads();
                        r++;
                        goto case SDL_EventType.SDL_EVENT_JOYSTICK_HAT_MOTION;
                    }
                    case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
                    case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP: {
                        if (SdlJoysticks.TryGetValue(@event.jbutton.which, out var joy)) {
                            joy.DoEvent(@event);
                            break;
                        }

                        if (r > 0) {
                            // discard this event, this joystick does not exist
                            skipped = true;
                            break;
                        }

                        RefreshJoysticksAndGamepads();
                        r++;
                        goto case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP;
                    }
                    case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
                    case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED: {
                        RefreshJoysticksAndGamepads();
                        if (SdlJoysticks.TryGetValue(@event.jbutton.which, out var joy)) {
                            ConnectionChanged?.Invoke(joy, @event.Type != SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED);
                        }

                        break;
                    }
                    case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION: {
                        if (SdlGamepads.TryGetValue(@event.gaxis.which, out var gp)) {
                            gp.DoEvent(@event);
                            break;
                        }

                        if (r > 0) {
                            // discard this event, this gamepad does not exist
                            skipped = true;
                            break;
                        }

                        RefreshJoysticksAndGamepads();
                        r++;
                        goto case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION;
                    }
                    case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                    case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP: {
                        if (SdlGamepads.TryGetValue(@event.gbutton.which, out var gp)) {
                            gp.DoEvent(@event);
                            break;
                        }

                        if (r > 0) {
                            // discard this event, this gamepad does not exist
                            skipped = true;
                            break;
                        }

                        RefreshJoysticksAndGamepads();
                        r++;
                        goto case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP;
                    }
                    case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                    case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                    case SDL_EventType.SDL_EVENT_GAMEPAD_REMAPPED: {
                        if (SdlGamepads.TryGetValue(@event.gdevice.which, out var gp)) {
                            gp.DoEvent(@event);
                            break;
                        }

                        if (r > 0) {
                            // discard this event, this gamepad does not exist
                            skipped = true;
                            break;
                        }

                        RefreshJoysticksAndGamepads();
                        r++;
                        goto case SDL_EventType.SDL_EVENT_GAMEPAD_REMAPPED;
                    }
                    case SDL_EventType.SDL_EVENT_FINGER_DOWN:
                    case SDL_EventType.SDL_EVENT_FINGER_UP:
                    case SDL_EventType.SDL_EVENT_FINGER_MOTION:
                        //case SDL_EventType.Dollargesture:
                        //case SDL_EventType.Dollarrecord:
                        //case SDL_EventType.Multigesture:
                    {
                        // TODO touch input
                        break;
                    }
                    default: {
                        skipped = true;
                        break;
                    }
                }

                if (!skipped) {
                    _sdlView.RemoveEvent(i);
                }
                else {
                    i++;
                }
            }

            ((SdlMouse)Mice[0]).Update();
            foreach (var gp in SdlGamepads.Values) {
                gp.Update();
            }

            // There's actually nowhere here that will raise an SDL error that we cause.
            // Sdl.ThrowError();
        }

        private void RefreshJoysticksAndGamepads() {
            var joysticks = SDL3.SDL_GetJoysticks();

            if (joysticks == null) {
                return;
            }

            for (var i = 0; i < joysticks.Count; i++) {
                var instanceId = joysticks[i];
                if (SDL3.SDL_IsGamepad(instanceId) == true) {
                    if (!SdlGamepads.TryGetValue(instanceId, out var gp)) {
                        SdlGamepads.Add(instanceId, new SdlGamepad(this, i, instanceId));
                    }
                    else {
                        gp.ActualIndex = i;
                    }
                }
                else {
                    if (!SdlJoysticks.TryGetValue(instanceId, out var joy)) {
                        SdlJoysticks.Add(instanceId, new SdlJoystick(this, i, instanceId));
                    }
                    else {
                        joy.ActualIndex = i;
                    }
                }
            }

            joysticks.Dispose();
        }

        public override void CoreDispose() {
            foreach (var gp in SdlGamepads.Values) {
                gp.Dispose();
            }

            foreach (var joy in SdlJoysticks.Values) {
                joy.Dispose();
            }
        }

        public void ChangeConnection(IInputDevice device, bool connected)
            => ConnectionChanged?.Invoke(device, connected);
    }
}