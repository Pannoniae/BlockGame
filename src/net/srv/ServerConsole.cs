using System.Text;
using BlockGame.util.cmd;
using BlockGame.util.log;
using BlockGame.world;

namespace BlockGame.net.srv;

/** handles console commands for the server */
public class ServerConsole : CommandSource {
    private readonly GameServer server;
    private readonly Thread consoleThread;
    private bool running = true;
    private readonly StringBuilder inputBuffer = new();
    private readonly Lock inputLock = new();

    public ServerConsole(GameServer server) {
        this.server = server;
        consoleThread = new Thread(readConsole) {
            Name = "Console",
            IsBackground = true
        };
    }

    public void start() {
        // register input preservation callbacks with logger
        ConsoleAppender.registerInputHandler(getInputBuffer, restoreInputLine);
        consoleThread.Start();
    }

    public void stop() {
        running = false;
    }

    private void readConsole() {
        while (running) {
            try {
                // check for input without blocking
                if (!Console.KeyAvailable) {
                    Thread.Sleep(10);
                    continue;
                }

                var key = Console.ReadKey(intercept: true);

                lock (inputLock) {
                    if (key.Key == ConsoleKey.Enter) {
                        var input = inputBuffer.ToString();
                        inputBuffer.Clear();
                        Console.WriteLine(); // newline after command

                        if (!string.IsNullOrWhiteSpace(input)) {
                            processCommand(input.Trim());
                        }
                    }
                    else if (key.Key == ConsoleKey.Backspace && inputBuffer.Length > 0) {
                        inputBuffer.Length--;
                        Console.Write("\b \b"); // erase character visually
                    }
                    else if (!char.IsControl(key.KeyChar)) {
                        inputBuffer.Append(key.KeyChar);
                        Console.Write(key.KeyChar); // echo character
                    }
                }
            }
            catch (Exception e) {
                Log.error($"Error reading console: {e.Message}");
            }
        }
    }

    /** callback: get current input buffer for preservation */
    private string getInputBuffer() {
        lock (inputLock) {
            return inputBuffer.ToString();
        }
    }

    /** callback: restore input line after log output */
    private void restoreInputLine(string buffer) {
        lock (inputLock) {
            Console.Write("> " + buffer);
        }
    }

    private void processCommand(string input) {
        var args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // strip leading / if present
        if (args.Length > 0 && args[0].StartsWith('/')) {
            args[0] = args[0][1..];
        }

        Command.execute(this, args);
    }

    // CommandSource impl
    public void sendMessage(string msg) {
        Log.info(msg);
    }

    public World getWorld() {
        return server.world;
    }
}