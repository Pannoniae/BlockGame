using System.Drawing;
using System.Numerics;
using BlockGame.logic;
using BlockGame.main;
using BlockGame.ui.screen;
using BlockGame.util;
using BlockGame.util.cmd;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.item.inventory;
using Molten.DoublePrecision;
using Silk.NET.Input;

namespace BlockGame.ui.menu;

public class ChatMenu : Menu {
    /// <summary>
    /// max. 24 messages
    /// </summary>
    private readonly XRingBuffer<ChatMessage> messages = new(20);

    private readonly XRingBuffer<ChatMessage> history = new(20);

    public int cursorPos = 0;

    /// <summary>
    /// How far in the history we are
    /// </summary>
    private int historyIndex = -1;

    /// <summary>
    /// Current message to be typed
    /// </summary>
    public string message = "";

    public int tick;

    public override bool isModal() {
        return false;
    }

    public override bool isBlockingInput() {
        return true;
    }

    public override bool pausesWorld() {
        return false;
    }
    
    public XRingBuffer<ChatMessage> getMessages() {
        return messages;
    }
    
    public void addMessage(string msg) {
        messages.PushFront(new ChatMessage(msg, tick));
    }

    // parse coordinate with relative support (~, ~5, ~-3)
    private static bool parseCoord(string arg, double playerPos, out int result) {
        if (arg[0] == '~') {
            if (arg.Length == 1) {
                result = (int)playerPos;
                return true;
            }
            if (int.TryParse(arg[1..], out int offset)) {
                result = (int)playerPos + offset;
                return true;
            }
            result = 0;
            return false;
        }
        return int.TryParse(arg, out result);
    }

    public override void onKeyChar(IKeyboard keyboard, char ch) {
        if (!char.IsControl(ch)) {
            
            // if the message is empty, only one thing to do
            if (message.Length == 0) {
                cursorPos = 0;
            }

            message = message.Insert(cursorPos, ch.ToString());
            cursorPos++;
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
                cursorPos = 0;
                Game.instance.executeOnMainThread(closeChat);
                break;
            case Key.Backspace when message.Length > 0:
                message = message.Remove(cursorPos - 1, 1);
                cursorPos--;
                break;
            case Key.Up:
                if (historyIndex < history.Count - 1) {
                    historyIndex++;
                    message = history[historyIndex].message;
                    cursorPos = message.Length;
                }
                break;
            case Key.Down:
                if (historyIndex > 0) {
                    historyIndex--;
                    message = history[historyIndex].message;
                    cursorPos = message.Length;
                }
                break;
            case Key.Left:
                if (cursorPos > 0) {
                    cursorPos--;
                }

                break;
            case Key.Right:
                if (cursorPos < message.Length) {
                    cursorPos++;
                }

                break;
        }
    }

    private void doChat(string msg) {
        // if command, execute
        if (msg.StartsWith('/')) {
            var args = msg[1..].Split(' ');

            Command.execute(Game.player, args);
        }
        // if not command, just print with player name
        else {
            addMessage($"<Player> {msg}");
        }
    }

    public override void onKeyRepeat(IKeyboard keyboard, Key key, int scancode) {
        switch (key) {
            case Key.Backspace when message.Length > 0:
                message = message.Remove(cursorPos - 1, 1);
                cursorPos--;
                break;
            case Key.Up:
                if (historyIndex < history.Count - 1) {
                    historyIndex++;
                    message = history[historyIndex].message;
                    cursorPos = message.Length;
                }
                break;
            case Key.Down:
                if (historyIndex > 0) {
                    historyIndex--;
                    message = history[historyIndex].message;
                    cursorPos = message.Length;
                }
                break;
            case Key.Left:
                if (cursorPos > 0) {
                    cursorPos--;
                }
                break;
            case Key.Right:
                if (cursorPos < message.Length) {
                    cursorPos++;
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
        var cursor = Game.permanentStopwatch.ElapsedMilliseconds % 1000 < 500 ? "|" : " ";


        string msgWithCursor;
        if (cursorPos >= message.Length) {
            msgWithCursor = message + cursor;
        }
        else {
            msgWithCursor = message.Insert(cursorPos, cursor);
        }

        gui.drawUI(gui.colourTexture, RectangleF.FromLTRB(4, gui.uiHeight - 16, gui.uiWidth - 4, gui.uiHeight - 4),
            color: new Color(0, 0, 0, 128));
        gui.drawStringUIThin("> " + msgWithCursor, new Vector2(6, Game.gui.uiHeight - 14));
    }
}

/// <summary>
/// A chat message which contains the contents and when it was sent.
/// </summary>
public record struct ChatMessage(string message, int ticks) {
}