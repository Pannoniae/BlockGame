using System.Drawing;
using System.Numerics;
using BlockGame.logic;
using BlockGame.main;
using BlockGame.ui.screen;
using BlockGame.util;
using BlockGame.world;
using CircularBuffer;
using Molten.DoublePrecision;
using Silk.NET.Input;

namespace BlockGame.ui.menu;

public class ChatMenu : Menu {
    /// <summary>
    /// max. 24 messages
    /// </summary>
    private readonly CircularBuffer<ChatMessage> messages = new(20);

    private readonly CircularBuffer<ChatMessage> history = new(20);

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
    
    public CircularBuffer<ChatMessage> getMessages() {
        return messages;
    }
    
    public void addMessage(string msg) {
        messages.PushFront(new ChatMessage(msg, tick));
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
                if (historyIndex < history.Size - 1) {
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
            switch (args[0]) {
                case "help":
                    addMessage("Commands: /help, /gamemode, /tp, /clear, /cb, /fb, /fly, /time, /debug");
                    break;
                case "gamemode":
                    if (args.Length == 2) {
                        switch (args[1].ToLower()) {
                            case "creative":
                            case "c":
                            case "1":
                                Game.gamemode = GameMode.creative;
                                // switch to creative inventory context
                                Game.player.inventoryCtx = new world.item.inventory.CreativeInventoryContext(40);
                                addMessage("Set gamemode to Creative");
                                break;
                            case "survival":
                            case "s":
                            case "0":
                                Game.gamemode = GameMode.survival;
                                // disable flying when switching to survival
                                Game.player.flyMode = false;
                                // switch to survival inventory context
                                Game.player.inventoryCtx = new world.item.inventory.SurvivalInventoryContext(Game.player.inventory);
                                addMessage("Set gamemode to Survival");
                                break;
                            default:
                                addMessage("Invalid gamemode. Use: creative/c/1 or survival/s/0");
                                break;
                        }
                    }
                    else {
                        var currentMode = Game.gamemode.name;
                        addMessage($"Current gamemode: {currentMode}. Usage: /gamemode <creative|survival>");
                    }
                    break;
                case "clear":
                    messages.Clear();
                    addMessage("Cleared chat!");
                    break;
                default:
                    addMessage("Unknown command: " + args[0]);
                    break;
                case "tp":
                    if (args.Length == 4) {
                        int x, y, z;

                        if (int.TryParse(args[1], out x) && int.TryParse(args[2], out y) &&
                            int.TryParse(args[3], out z)) {
                            Game.player.teleport(new Vector3D(x, y, z));
                            addMessage($"Teleported to {x}, {y}, {z}!");
                            break;
                        }

                        // relative coords?
                        if (args[1][0] == '~') {
                            x = (int)Game.player.position.X;
                        }
                        else {
                            bool success = int.TryParse(args[1], out x);
                            if (!success) {
                                addMessage("Invalid coordinates");
                                return;
                            }
                        }

                        if (args[2][0] == '~') {
                            y = (int)Game.player.position.Y;
                        }
                        else {
                            bool success = int.TryParse(args[2], out y);
                            if (!success) {
                                addMessage("Invalid coordinates");
                                return;
                            }
                        }

                        if (args[3][0] == '~') {
                            z = (int)Game.player.position.Z;
                        }
                        else {
                            bool success = int.TryParse(args[3], out z);
                            if (!success) {
                                addMessage("Invalid coordinates");
                                return;
                            }
                        }

                        Game.player.teleport(new Vector3D(x, y, z));
                        addMessage($"Teleported to {x}, {y}, {z}!");
                    }
                    else {
                        addMessage("Usage: /tp <x> <y> <z>");
                    }

                    break;
                case "cb":
                    if (Screen.GAME_SCREEN.chunkBorders) {
                        Screen.GAME_SCREEN.chunkBorders = false;
                        addMessage("Chunk borders disabled");
                    }
                    else {
                        Screen.GAME_SCREEN.chunkBorders = true;
                        addMessage("Chunk borders enabled");
                    }

                    break;
                case "fb":
                    // enable fullbright
                    if (Game.graphics.fullbright) {
                        Game.graphics.fullbright = false;
                        addMessage("Fullbright disabled");
                    }
                    else {
                        Game.graphics.fullbright = true;
                        addMessage("Fullbright enabled");
                    }

                    // remesh everything to update lighting
                    Game.instance.executeOnMainThread(() => { Screen.GAME_SCREEN.remeshWorld(0); });
                    break;
                case "fly":
                    Game.player.noClip = !Game.player.noClip;
                    addMessage("Noclip " + (Game.player.noClip ? "enabled" : "disabled"));
                    break;
                case "time":
                    if (args.Length == 1) {
                        // display current time
                        var currentTick = Game.world.worldTick;
                        var dayPercent = Game.world.getDayPercentage(currentTick);
                        var timeOfDay = (int)(dayPercent * World.TICKS_PER_DAY);
                        addMessage($"The time is {timeOfDay} (day {currentTick / World.TICKS_PER_DAY})");
                    }
                    else if (args.Length == 3 && args[1] == "set") {
                        // set time
                        if (int.TryParse(args[2], out int newTime)) {
                            Game.world.worldTick = newTime;
                            addMessage($"Set time to {newTime}");
                            
                            // remesh world
                            //Game.instance.executeOnMainThread(() => { Screen.GAME_SCREEN.remeshWorld(0); });
                        }
                        else {
                            addMessage("Usage: /time set <time>");
                        }
                    }
                    else {
                        addMessage("Usage: /time or /time set <time>");
                    }
                    break;
                case "debug":
                    // debug commands
                    
                    var subCmd = args.Length > 1 ? args[1] : "";
                    switch (subCmd) {
                        case "":
                            addMessage("Debug commands: /debug lightmap, /debug noise, /debug atlas");
                            break;
                        case "lightmap":
                            // dump lightmap to file
                            Game.textures.dumpLightmap();
                            addMessage("Lightmap dumped to lightmap.png");
                            break;
                        case "noise":
                            // toggle noise debug display
                            Game.debugShowNoise = !Game.debugShowNoise;
                            addMessage($"Noise debug display: {(Game.debugShowNoise ? "enabled" : "disabled")}");
                            break;
                        case "atlas":
                            // dump texture atlas to file
                            Game.textures.dumpAtlas();
                            addMessage("Texture atlas dumped to atlas.png");
                            break;
                        default:
                            addMessage($"Unknown debug command: {subCmd}");
                            break;
                    }
                    break;
                case "spawn":
                    /*int eID = args.Length > 1 ? args[1] : -1;
                    var cow = Game.world.addEntity(e);
                    if (cow != null) {
                        addMessage($"Spawned {cow.name} at {Game.player.position}");
                    }
                    else {
                        addMessage("Failed to spawn entity");
                    }
                    break;*/
                    break;
            }
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
                if (historyIndex < history.Size - 1) {
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