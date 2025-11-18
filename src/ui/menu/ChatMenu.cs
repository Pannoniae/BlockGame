using System.Drawing;
using System.Linq;
using System.Numerics;
using BlockGame.logic;
using BlockGame.main;
using BlockGame.net;
using BlockGame.net.packet;
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
            case Key.Escape:
                Game.instance.executeOnMainThread(closeChat);
                break;
            case Key.Enter:
                // if T is pressed but there's a message, don't return
                // wait a frame so the key doesn't immediately get pressed again
                history.PushFront(new ChatMessage(message, tick));
                doChat(message);
                historyIndex = -1;
                cursorPos = 0;
                Game.instance.executeOnMainThread(closeChat);
                break;
            case Key.Backspace when cursorPos > 0:
                message = message.Remove(cursorPos - 1, 1);
                cursorPos--;
                break;
            case Key.Delete when cursorPos < message.Length:
                message = message.Remove(cursorPos, 1);
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
            case Key.Left when cursorPos > 0:
                cursorPos--;
                break;
            case Key.Right when cursorPos < message.Length:
                cursorPos++;
                break;
            case Key.Home:
                cursorPos = 0;
                break;
            case Key.End:
                cursorPos = message.Length;
                break;
            case Key.C when keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight):
                if (!string.IsNullOrEmpty(message)) {
                    keyboard.ClipboardText = message;
                }
                break;
            case Key.V when keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight):
                var clipboardText = keyboard.ClipboardText;
                if (!string.IsNullOrEmpty(clipboardText)) {
                    // filter to printable characters only
                    var filtered = new string(clipboardText.Where(c => !char.IsControl(c)).ToArray());
                    if (!string.IsNullOrEmpty(filtered)) {
                        message = message.Insert(cursorPos, filtered);
                        cursorPos += filtered.Length;
                    }
                }
                break;
        }
    }

    private void doChat(string msg) {
        // if command, execute locally or send to server
        if (msg.StartsWith('/')) {
            var args = msg[1..].Split(' ');
            var cmdName = args[0];

            // find command to check if it's client-only
            var cmd = Command.find(cmdName);

            // unknown command or singleplayer - execute locally
            if (Net.mode == NetMode.SP) {
                Command.execute(Game.player, args);
            }
            // multiplayer client
            else if (Net.mode.isMPC()) {
                // client-only commands execute locally
                if (cmd != null && cmd.Value.side == NetMode.CL) {
                    Command.execute(Game.player, args);
                }
                // server commands go to server
                else {
                    ClientConnection.instance.send(
                        new CommandPacket { command = msg[1..] },
                        LiteNetLib.DeliveryMethod.ReliableOrdered
                    );
                }
            }
            else {
                // not connected - execute locally (will show errors if needed)
                Command.execute(Game.player, args);
            }
        }
        // if not command, send to server or display locally
        else {
            // multiplayer: send to server
            if (Net.mode.isMPC()) {
                ClientConnection.instance.send(new ChatMessagePacket { message = msg }, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            // singleplayer: just print with player name
            else {
                addMessage($"<{Game.player.name}> {msg}");
            }
        }
    }

    public override void onKeyRepeat(IKeyboard keyboard, Key key, int scancode) {
        switch (key) {
            case Key.Backspace when cursorPos > 0:
                message = message.Remove(cursorPos - 1, 1);
                cursorPos--;
                break;
            case Key.Delete when cursorPos < message.Length:
                message = message.Remove(cursorPos, 1);
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
            case Key.Left when cursorPos > 0:
                cursorPos--;
                break;
            case Key.Right when cursorPos < message.Length:
                cursorPos++;
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

        // calculate available width for text (subtract margins + prompt)
        const float leftMargin = 6;
        const float rightMargin = 6;
        var promptWidth = gui.measureStringUIThin("> ").X;
        var availableWidth = gui.uiWidth - leftMargin - rightMargin - promptWidth;

        // calculate text up to cursor for scroll offset
        var textBeforeCursor = message[..cursorPos];
        var cursorXOffset = gui.measureStringUIThin(textBeforeCursor).X;

        // calculate scroll offset to keep cursor visible
        float scrollOffset = 0;
        if (cursorXOffset > availableWidth) {
            scrollOffset = cursorXOffset - availableWidth + 4;
        }

        // build display string with cursor
        string msgWithCursor;
        if (cursorPos >= message.Length) {
            msgWithCursor = message + cursor;
        }
        else {
            msgWithCursor = message.Insert(cursorPos, cursor);
        }

        // draw background
        gui.drawUI(gui.colourTexture, RectangleF.FromLTRB(4, gui.uiHeight - 16, gui.uiWidth - 4, gui.uiHeight - 4),
            color: new Color(0, 0, 0, 128));

        // draw prompt
        gui.drawStringUIThin("> ", new Vector2(leftMargin, gui.uiHeight - 14));

        // draw text with clipping
        var textX = leftMargin + promptWidth - scrollOffset;
        var textY = gui.uiHeight - 14;

        // use scissor test to clip overflowing text
        var scissorX = (int)(leftMargin + promptWidth);
        var scissorY = (int)(gui.uiHeight - 16);
        var scissorW = (int)(gui.uiWidth - leftMargin - rightMargin - promptWidth);
        var scissorH = 12;
        Game.graphics.mainBatch.End();
        Game.graphics.mainBatch.Begin();

        Game.graphics.scissorUI(scissorX, scissorY, scissorW, scissorH);
        gui.drawStringUIThin(msgWithCursor, new Vector2(textX, textY));
        // break batch!
        Game.graphics.mainBatch.End();
        Game.graphics.mainBatch.Begin();
        Game.graphics.noScissor();
    }
}

/// <summary>
/// A chat message which contains the contents and when it was sent.
/// </summary>
public record struct ChatMessage(string message, int ticks) {
    public string message = message;
    public int ticks = ticks;
}