using System.Drawing;
using System.Numerics;
using CircularBuffer;
using Silk.NET.Input;
using Molten.DoublePrecision;
using TrippyGL;

namespace BlockGame.ui;

public class ChatMenu : Menu {

    /// <summary>
    /// max. 24 messages
    /// </summary>
    public readonly CircularBuffer<ChatMessage> messages = new(20);
    public readonly CircularBuffer<ChatMessage> history = new(20);

    /// <summary>
    /// How far in the history we are
    /// </summary>
    private int historyIndex = -1;

    /// <summary>
    /// Current message to be typed
    /// </summary>
    public string message = "Test";

    public int tick;

    public override bool isModal() {
        return false;
    }

    public override bool isBlockingInput() {
        return true;
    }

    public override void onKeyChar(IKeyboard keyboard, char ch) {
        if ((char.IsLetterOrDigit(ch) || char.IsPunctuation(ch) || char.IsWhiteSpace(ch)) &&
            !char.IsControl(ch)) {
            message += ch;
        }
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        switch (key) {
            case Key.Enter:
                // if T is pressed but there's a message, don't return
                // wait a frame so the key doesn't immediately get pressed again
                history.PushFront(new ChatMessage(message, tick));
                doChat(message);
                historyIndex = -1;
                Game.instance.executeOnMainThread(closeChat);
                break;
            case Key.Backspace when message.Length > 0:
                message = message[..^1];
                break;
            case Key.Up:
                if (historyIndex < history.Size - 1) {
                    historyIndex++;
                    message = history[historyIndex].message;
                }
                break;
            case Key.Down:
                if (historyIndex > 0) {
                    historyIndex--;
                    message = history[historyIndex].message;
                }
                break;
        }
    }

    private void doChat(string msg) {
        // if command, execute
        if (msg.StartsWith('/')) {
            var args = msg.Split(' ');
            switch (args[0]) {
                case "/help":
                    messages.PushFront(new ChatMessage("Commands: /help", tick));
                    break;
                case "/clear":
                    messages.Clear();
                    messages.PushFront(new ChatMessage("Cleared chat!", tick));
                    break;
                default:
                    messages.PushFront(new ChatMessage("Unknown command: " + args[0], tick));
                    break;
                case "/tp":
                    if (args.Length == 4) {
                        if (int.TryParse(args[1], out int x) && int.TryParse(args[2], out int y) &&
                            int.TryParse(args[3], out int z)) {
                            ((GameScreen)screen).world.player.teleport(new Vector3D(x, y, z));
                            messages.PushFront(new ChatMessage($"Teleported to {x}, {y}, {z}!", tick));
                        }
                        else {
                            messages.PushFront(new ChatMessage("Invalid coordinates", tick));
                        }
                    }
                    else {
                        messages.PushFront(new ChatMessage("Usage: /tp <x> <y> <z>", tick));
                    }
                    break;
            }
        }
        // if not command, just print with player name
        else {
            messages.PushFront(new ChatMessage($"<Player> {msg}", tick));
        }
    }

    public override void onKeyRepeat(IKeyboard keyboard, Key key, int scancode) {
        switch (key) {
            case Key.Backspace when message.Length > 0:
                message = message[..^1];
                break;
            case Key.Up:
                if (historyIndex < history.Size - 1) {
                    historyIndex++;
                    message = history[historyIndex].message;
                }
                break;
            case Key.Down:
                if (historyIndex > 0) {
                    historyIndex--;
                    message = history[historyIndex].message;
                }
                break;
        }
    }

    public void closeChat() {
        message = "";
        Game.instance.lockMouse();
        screen.switchToMenu(((GameScreen)screen).INGAME_MENU);
    }

    public override void draw() {
        base.draw();
        var gui = Game.gui;
        var cursor = Game.permanentStopwatch.ElapsedMilliseconds % 1000 < 500 ? "|" : "";
        gui.drawUI(gui.colourTexture, RectangleF.FromLTRB(4, gui.uiHeight - 16, gui.uiWidth - 4, gui.uiHeight - 4), color: new Color4b(0, 0, 0, 128));
        gui.drawStringUIThin("> " + message + cursor, new Vector2(6, Game.gui.uiHeight - 13));
    }
}

/// <summary>
/// A chat message which contains the contents and when it was sent.
/// </summary>
public record struct ChatMessage(string message, int ticks) {
}