using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SDL;
using Silk.NET.SDL;

namespace Silk.NET.Input.Sdl {
    internal partial class SdlKeyboard : IKeyboard, ISdlDevice {
        private readonly SdlInputContext _ctx;
        private List<SDL_Scancode> _scancodesDown = [];

        public SdlKeyboard(SdlInputContext ctx) {
            _ctx = ctx;
        }

        public string Name { get; } = "Silk.NET Keyboard (via SDL)";
        public int Index { get; } = 0;
        public bool IsConnected { get; } = true;

        public IReadOnlyList<Key> SupportedKeys { get; } =
            _keyMap.Values.Where(static x => x != Key.Unknown).Distinct().ToArray();

        public string ClipboardText {
            get => SDL3.SDL_GetClipboardText();
            set => SDL3.SDL_SetClipboardText(value);
        }

        public bool IsKeyPressed(Key key) {
            foreach (var scancode in _scancodesDown) {
                if (_keyMap.TryGetValue(scancode, out Key skey) && key == skey) {
                    return true;
                }
            }

            return false;
        }

        public bool IsScancodePressed(int scancode) => _scancodesDown.Contains((SDL_Scancode)scancode);

        public List<Key> GetPressedKeys() {
            var keys = new List<Key>();
            foreach (var sc in _scancodesDown) {
                if (_keyMap.TryGetValue(sc, out var key) && key != Key.Unknown) {
                    keys.Add(key);
                }
            }

            return keys;
        }

        public event Action<IKeyboard, Key, int>? KeyDown;
        public event Action<IKeyboard, Key, int>? KeyRepeat;
        public event Action<IKeyboard, Key, int>? KeyUp;
        public event Action<IKeyboard, char>? KeyChar;

        public void BeginInput() {
            unsafe {
                SDL3.SDL_StartTextInput((SDL_Window*)_ctx.Handle);
            }
        }

        public void EndInput() {
            unsafe {
                SDL3.SDL_StopTextInput((SDL_Window*)_ctx.Handle);
            }
        }

        public unsafe void DoEvent(SDL_Event @event) {
            switch (@event.Type) {
                case SDL_EventType.SDL_EVENT_KEY_DOWN: {
                    Key key;
                    if (_keyMap.TryGetValue(@event.key.scancode, out var mappedKey)) {
                        key = mappedKey;
                    }
                    else {
                        key = Key.Unknown;
                    }

                    if (!@event.key.repeat) {
                        _scancodesDown.Add(@event.key.scancode);
                        KeyDown?.Invoke(this, key, (int)@event.key.scancode);
                    }
                    else {
                        KeyRepeat?.Invoke(this, key, (int)@event.key.scancode);
                    }

                    break;
                }
                case SDL_EventType.SDL_EVENT_KEY_UP: {
                    if (!@event.key.repeat) {
                        Key keyUp;
                        if (_keyMap.TryGetValue(@event.key.scancode, out var key)) {
                            keyUp = key;
                        }
                        else {
                            keyUp = Key.Unknown;
                        }

                        _scancodesDown.Remove(@event.key.scancode);
                        KeyUp?.Invoke(this, keyUp, (int)@event.key.scancode);
                    }

                    break;
                }
                case SDL_EventType.SDL_EVENT_TEXT_EDITING: {
                    break;
                }
                case SDL_EventType.SDL_EVENT_TEXT_INPUT: {
                    if (KeyChar is not null) {
                        // SDL3 returns a zero-terminated UTF-8 string
                        var text = Marshal.PtrToStringUTF8((nint)(&@event.text.text[0]));
                        if (text != null) {
                            foreach (var c in text) {
                                KeyChar.Invoke(this, c);
                            }
                        }
                    }

                    break;
                }
                case SDL_EventType.SDL_EVENT_KEYMAP_CHANGED:
                    break;
            }
        }
    }
}