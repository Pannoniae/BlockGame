using System.Globalization;
using BlockGame.logic;
using BlockGame.main;
using BlockGame.net;
using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.ui;
using BlockGame.util.log;
using BlockGame.util.stuff;
using BlockGame.world;
using BlockGame.world.entity;
using BlockGame.world.item.inventory;
using BlockGame.world.worldgen.generator;
using LiteNetLib;
using Molten.DoublePrecision;
using Registry = BlockGame.util.stuff.Registry;

namespace BlockGame.util.cmd;

public enum CmdSide : byte {
    /** render/UI stuff, never sent to the server */
    CLIENT,
    /** any world/entity/inventory state, always goes to the server both SP and MP */
    SERVER,
    /** no state, runs on where it's called */
    ANY
}

public readonly struct Command {
    public static readonly XUList<Command> commands = [];

    public readonly string name;
    public readonly string desc;
    public readonly Action<CommandSource, string[]> action;
    public readonly CmdSide side;
    public readonly bool cheat;

    public Command(string name, string desc, CmdSide side, Action<CommandSource, string[]> action, bool cheat = false) {
        if (cheat && side != CmdSide.SERVER) {
            throw new ArgumentException($"/{name}: cheat commands have to be server-side");
        }

        this.name = name;
        this.desc = desc;
        this.side = side;
        this.action = action;
        this.cheat = cheat;
    }

    public static Command? find(string name) {
        foreach (var command in commands) {
            if (command.name.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                return command;
            }
        }
        return null;
    }

    public static void execute(CommandSource src, string[] args) {
        if (args.Length == 0) {
            src.sendMessage("No command provided. Type /help for a list of commands.");
            return;
        }

        string cmdName = args[0];
        var cmd = find(cmdName);

        if (cmd == null) {
            src.sendMessage($"Unknown command: {cmdName}. Type /help for a list of commands.");
            return;
        }

        var c = cmd.Value;
        var server = src.getWorld().isServer;

        if (c.side == CmdSide.CLIENT && server) {
            src.sendMessage($"/{c.name} is client-side, use it in-game.");
            return;
        }

        if (c.side == CmdSide.SERVER && !server) {
            Log.error($"Server command /{c.name} executed on the client, plz fix");
            src.sendMessage($"/{c.name} has to run on the server.");
            return;
        }

        if (c.cheat && !src.isOp()) {
            src.sendMessage("You must be an operator to use this command.");
            return;
        }

        try {
            c.action(src, args[1..]);
        }
        catch (Exception e) {
            src.sendMessage($"Error executing command {cmdName}: {e.Message}");
            // dump into console
            Log.error($"Error executing command {cmdName}:");
            Log.error(e);
        }
    }

    public static void register() {
        var helpAction = (CommandSource source, string[] args) => {
            // the console can't run the client commands
            var server = source.getWorld().isServer && !source.isPlayer();
            source.sendMessage("Available commands:");
            foreach (var cmd in commands) {
                if (server && cmd.side == CmdSide.CLIENT) {
                    continue;
                }

                source.sendMessage($"/{cmd.name} - {cmd.desc}");
            }
        };

        commands.Add(new Command("help", "Lists all available commands", CmdSide.ANY, helpAction));
        commands.Add(new Command("?", "Lists all available commands", CmdSide.ANY, helpAction));

        // ============ CLIENT ============

        commands.Add(new Command("clear", "Clears the chat messages", CmdSide.CLIENT, (source, args) => {
            var chatMenu = Screen.GAME_SCREEN.CHAT;
            chatMenu.getMessages().Clear();
            chatMenu.addMessage("Cleared chat!");
        }));

        commands.Add(new Command("cb", "Toggles chunk borders", CmdSide.CLIENT, (source, args) => {
            Screen.GAME_SCREEN.chunkBorders = !Screen.GAME_SCREEN.chunkBorders;
            source.sendMessage("Chunk borders " + (Screen.GAME_SCREEN.chunkBorders ? "enabled" : "disabled"));
        }));

        commands.Add(new Command("fb", "Toggles fullbright mode", CmdSide.CLIENT, (source, args) => {
            Game.graphics.fullbright = !Game.graphics.fullbright;
            source.sendMessage("Fullbright " + (Game.graphics.fullbright ? "enabled" : "disabled"));

            // remesh everything to update lighting
            Game.instance.executeOnMainThread(() => { Screen.GAME_SCREEN.remeshWorld(0); });
        }));

        commands.Add(new Command("debug", "Various debug commands", CmdSide.CLIENT, (source, args) => {
            var subCmd = args.Length > 0 ? args[0] : "";

            switch (subCmd) {
                case "":
                    source.sendMessage("Debug commands: /debug lightmap, /debug atlas, /debug reg");
                    break;
                case "lightmap":
                    // dump lightmap to file
                    Game.textures.dumpLightmap();
                    source.sendMessage("Lightmap dumped to lightmap.png");
                    break;
                case "atlas":
                    // dump texture atlas to file
                    Game.textures.dumpAtlas();
                    source.sendMessage("Texture atlas dumped to atlas.png");
                    break;
                case "reg":
                case "registry":
                case "registries":
                    try {
                        var path = dumpRegistries();
                        source.sendMessage($"Dumped registries to {path}");
                    }
                    catch (Exception ex) {
                        source.sendMessage($"Failed to dump registries: {ex.Message}");
                    }
                    break;
                default:
                    source.sendMessage($"Unknown debug command: {subCmd}");
                    break;
            }
        }));

        commands.Add(new Command("cec", "Shows entity count stats (client's copy of the world)", CmdSide.CLIENT,
            (source, args) => entityStats(source, source.getWorld())));

        commands.Add(new Command("netstats", "Shows network statistics", CmdSide.CLIENT, (source, args) => {
            if (ClientConnection.instance is not { connected: true } conn) {
                source.sendMessage("Not connected to a server");
                return;
            }

            var m = Game.metrics;
            source.sendMessage("=== Netstats ===");
            source.sendMessage($"↑ {m.bytesSent / 1024.0:0.0}KB/s ({m.packetsSent} pkt/s)");
            source.sendMessage($"↓ {m.bytesReceived / 1024.0:0.0}KB/s ({m.packetsReceived} pkt/s)");
            source.sendMessage($"Ping: {conn.ping}ms");
        }));

        commands.Add(new Command("packetstats", "Shows packet statistics by type", CmdSide.CLIENT, (source, args) => {
            if (ClientConnection.instance is not { connected: true }) {
                source.sendMessage("Not connected to a server");
                return;
            }

            var stats = Game.metrics.packets;
            if (stats.Count == 0) {
                source.sendMessage("No packets received in the last second");
                return;
            }

            source.sendMessage("=== Packet Stats (last second) ===");
            foreach (var kvp in stats.OrderByDescending(kvp => kvp.Value)) {
                var typeName = kvp.Key.Name.Replace("Packet", "");
                source.sendMessage($"{typeName}: {kvp.Value}/s");
            }
            source.sendMessage($"Total: {Game.metrics.packetsReceived}/s");
        }));

        // ============ SERVER ============

        commands.Add(new Command("gamemode", "Changes the player's game mode", CmdSide.SERVER, (source, args) => {
            switch (args.Length) {
                case 0 when source is Player self:
                    source.sendMessage($"Current gamemode: {self.gameMode.name}. Usage: /gamemode [player] <creative|survival>");
                    return;
                case 1 when source is not Player:
                    source.sendMessage("You need to specify a player when changing gamemode from the console.");
                    return;
                case 1 or 2:
                    break;
                default:
                    source.sendMessage("Usage: /gamemode [player] <creative|survival>");
                    return;
            }

            var target = args.Length == 2 ? findPlayer(source, args[0]) : source as Player;
            if (target == null) {
                return;
            }

            var mode = parseMode(args[^1]);
            if (mode == null) {
                source.sendMessage("Invalid gamemode. Use: creative/c/1 or survival/s/0");
                return;
            }

            setGameMode(target, mode);
            source.sendMessage(target == source ? $"Set gamemode to {mode.name}" : $"Set gamemode of {target.name} to {mode.name}");
        }, true));

        commands.Add(new Command("tp", "Teleports a player to another player or to coordinates", CmdSide.SERVER, (source, args) => {
            Player? who;
            Vector3D to;

            switch (args.Length) {
                // /tp <player> - yourself to them
                case 1: {
                    who = self(source);
                    var target = who == null ? null : findPlayer(source, args[0]);
                    if (target == null) {
                        return;
                    }

                    to = target.position;
                    break;
                }
                // /tp <player1> <player2>
                case 2: {
                    who = findPlayer(source, args[0]);
                    var target = who == null ? null : findPlayer(source, args[1]);
                    if (target == null) {
                        return;
                    }

                    to = target.position;
                    break;
                }
                // /tp <x> <y> <z>
                case 3:
                    who = self(source);
                    if (who == null || !parsePos(source, args, 0, who.position, out to)) {
                        return;
                    }

                    break;
                // /tp <player> <x> <y> <z>
                case 4:
                    who = findPlayer(source, args[0]);
                    if (who == null || !parsePos(source, args, 1, who.position, out to)) {
                        return;
                    }

                    break;
                default:
                    source.sendMessage("Usage: /tp <player> [player] or /tp [player] <x> <y> <z>");
                    return;
            }

            who.teleport(to);
            source.sendMessage($"Teleported {who.name} to {fmt(to)}");
        }, true));

        commands.Add(new Command("fly", "Toggles noclip mode", CmdSide.SERVER, (source, args) => {
            if (self(source) is not { } player) {
                return;
            }

            // the owning client runs the physics, it gets this through the state sync
            player.noClip = !player.noClip;
            source.sendMessage("Noclip " + (player.noClip ? "enabled" : "disabled"));
        }, true));

        commands.Add(new Command("time", "Gets or sets the world time", CmdSide.SERVER, (source, args) => {
            var world = source.getWorld();

            if (args.Length == 0) {
                var currentTick = world.worldTick;
                var timeOfDay = (int)(world.getDayPercentage(currentTick) * World.TICKS_PER_DAY);
                source.sendMessage($"The time is {timeOfDay} (day {currentTick / World.TICKS_PER_DAY})");
            }
            else if (args is ["set", _] && int.TryParse(args[1], out int newTime)) {
                world.worldTick = newTime;

                // snap: the clients jump the sky colour instead of lerping half a day
                GameServer.instance.send(new TimeUpdatePacket { worldTick = newTime, snap = true }, DeliveryMethod.ReliableSequenced);
                source.sendMessage($"Set time to {newTime}");
            }
            else {
                source.sendMessage("Usage: /time or /time set <time>");
            }
        }, true));

        commands.Add(new Command("noise", "Sample noise values at player position", CmdSide.SERVER, (source, args) => {
            if (self(source) is not { } player) {
                return;
            }

            var pos = player.position.toBlockPos();
            var world = source.getWorld();

            string result;
            if (world.generator is NewWorldGenerator nwg) {
                result = nwg.sample(pos.X, pos.Y, pos.Z);
            }
            else if (world.generator is PerlinWorldGenerator pwg) {
                result = pwg.sample(pos.X, pos.Y, pos.Z);
            }
            else {
                source.sendMessage($"Generator '{world.generatorName}' does not support noise sampling");
                return;
            }

            foreach (var line in result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)) {
                source.sendMessage(line);
            }
        }));

        commands.Add(new Command("summon", "Summons an entity", CmdSide.SERVER, (source, args) => {
            if (args.Length is not (1 or 2 or 4 or 5)) {
                source.sendMessage("Usage: /summon <entity> [x y z] [count]");
                return;
            }

            var type = lookup(Registry.ENTITIES, args[0]);
            if (type < 0) {
                source.sendMessage($"Unknown entity: {args[0]}");
                return;
            }

            if (type == Entities.PLAYER || type == Entities.ITEM_ENTITY || type == Entities.FALLING_BLOCK) {
                source.sendMessage($"Cannot summon {args[0]}");
                return;
            }

            var pos = origin(source);
            if (args.Length >= 4 && !parsePos(source, args, 1, pos, out pos)) {
                return;
            }

            var count = 1;
            if (args.Length is 2 or 5 && (!int.TryParse(args[^1], out count) || count is < 1 or > 256)) {
                source.sendMessage("Count must be 1-256");
                return;
            }

            var world = source.getWorld();
            for (int i = 0; i < count; i++) {
                var e = Entities.create(world, type);
                if (e == null) {
                    source.sendMessage($"Failed to create entity: {args[0]}");
                    return;
                }

                e.teleport(pos);
                world.addEntity(e);
            }

            source.sendMessage($"Summoned {count}x {Registry.ENTITIES.getName(type)} at {fmt(pos)}");
        }, true));

        commands.Add(new Command("ec", "Shows entity count stats", CmdSide.SERVER,
            (source, args) => entityStats(source, source.getWorld())));

        commands.Add(new Command("killall", "Removes all entities of a type or all mobs", CmdSide.SERVER, (source, args) => {
            var world = source.getWorld();
            string targetType = args.Length > 0 ? args[0].ToLowerInvariant() : "mobs";

            int killed = 0;
            foreach (var e in world.entities) {
                // never kill players
                if (e is Player) {
                    continue;
                }

                bool shouldKill = targetType switch {
                    "all" => true,
                    "mobs" => e is Mob,
                    "passive" => e is Mob && Entities.spawnType[Entities.getID(e.type)] == SpawnType.PASSIVE,
                    "hostile" => e is Mob && Entities.spawnType[Entities.getID(e.type)] == SpawnType.HOSTILE,
                    _ => e.type.Equals(targetType, StringComparison.OrdinalIgnoreCase)
                };

                if (shouldKill) {
                    e.active = false;
                    killed++;
                }
            }

            source.sendMessage($"Killed {killed} entities ({targetType})");
        }, true));

        commands.Add(new Command("setblock", "Sets blocks in a region", CmdSide.SERVER, (source, args) => {
            if (args.Length != 7) {
                source.sendMessage("Usage: /setblock <x0> <y0> <z0> <x1> <y1> <z1> <block>");
                return;
            }

            var o = origin(source);
            if (!parsePos(source, args, 0, o, out var p0) || !parsePos(source, args, 3, o, out var p1)) {
                return;
            }

            var blockID = lookup(Registry.BLOCKS, args[6]);
            if (blockID < 0) {
                source.sendMessage($"Unknown block: {args[6]}");
                return;
            }

            int minX = (int)Math.Floor(Math.Min(p0.X, p1.X));
            int maxX = (int)Math.Floor(Math.Max(p0.X, p1.X));
            int minY = Math.Max(0, (int)Math.Floor(Math.Min(p0.Y, p1.Y)));
            int maxY = Math.Min(World.WORLDHEIGHT - 1, (int)Math.Floor(Math.Max(p0.Y, p1.Y)));
            int minZ = (int)Math.Floor(Math.Min(p0.Z, p1.Z));
            int maxZ = (int)Math.Floor(Math.Max(p0.Z, p1.Z));

            long volume = (long)(maxX - minX + 1) * (maxY - minY + 1) * (maxZ - minZ + 1);
            if (volume > MAX_FILL) {
                source.sendMessage($"Too many blocks ({volume}), the limit is {MAX_FILL}");
                return;
            }

            var world = source.getWorld();
            for (int x = minX; x <= maxX; x++) {
                for (int y = minY; y <= maxY; y++) {
                    for (int z = minZ; z <= maxZ; z++) {
                        world.setBlock(x, y, z, (ushort)blockID);
                    }
                }
            }

            source.sendMessage($"Set {volume} blocks to {Registry.BLOCKS.getName(blockID)}");
        }, true));

        commands.Add(new Command("give", "Gives an item to the player", CmdSide.SERVER, (source, args) => {
            if (args.Length is < 1 or > 3) {
                source.sendMessage("Usage: /give <item> [quantity] [metadata]");
                return;
            }

            if (self(source) is not { } player) {
                return;
            }

            var itemID = lookup(Registry.ITEMS, args[0]);
            if (itemID <= 0) {
                source.sendMessage($"Unknown item: {args[0]}");
                return;
            }

            int quantity = 1;
            if (args.Length >= 2 && (!int.TryParse(args[1], out quantity) || quantity <= 0)) {
                source.sendMessage("Invalid quantity");
                return;
            }

            int metadata = 0;
            if (args.Length >= 3 && !int.TryParse(args[2], out metadata)) {
                source.sendMessage("Invalid metadata");
                return;
            }

            // addItem shrinks the stack to what didn't fit
            var stack = new ItemStack(itemID, quantity, metadata);
            var given = player.inventory.addItem(stack) ? quantity : quantity - stack.quantity;
            (player as ServerPlayer)?.syncInventory();

            var name = Registry.ITEMS.getName(itemID);
            source.sendMessage(given == quantity ? $"Gave {quantity} of {name}" : $"Gave {given} of {name}, the rest didn't fit");
        }, true));

        commands.Add(new Command("ci", "Clears the player's inventory", CmdSide.SERVER, (source, args) => {
            if (self(source) is not { } player) {
                return;
            }

            player.inventory.clearAll();
            (player as ServerPlayer)?.syncInventory();
            source.sendMessage("Cleared inventory");
        }, true));

        commands.Add(new Command("chunkmuncher", "Deletes all blocks in the current chunk", CmdSide.SERVER, (source, args) => {
            if (self(source) is not { } player) {
                return;
            }

            var world = source.getWorld();
            var playerPos = player.position.toBlockPos();
            var chunkCoord = World.getChunkPos(playerPos.X, playerPos.Z);

            if (!world.getChunkMaybe(chunkCoord, out _)) {
                source.sendMessage("You're not in a valid chunk!");
                return;
            }

            // yeet all blocks in chunk
            int munched = 0;
            for (int cx = 0; cx < 16; cx++) {
                for (int cy = 0; cy < World.WORLDHEIGHT; cy++) {
                    for (int cz = 0; cz < 16; cz++) {
                        var wx = (chunkCoord.x << 4) + cx;
                        var wz = (chunkCoord.z << 4) + cz;
                        if (world.getBlock(wx, cy, wz) != 0) {
                            world.setBlockMetadata(wx, cy, wz, 0);
                            munched++;
                        }
                    }
                }
            }

            source.sendMessage($"Munched {munched} blocks from chunk ({chunkCoord.x}, {chunkCoord.z})");
        }, true));

        commands.Add(new Command("suicide", "Commits suicide", CmdSide.SERVER, (source, args) => {
            if (self(source) is not { } player) {
                return;
            }

            player.sethp(0);
            player.die();
            source.sendMessage("You have committed suicide.");
        }));

        var stopAction = (CommandSource source, string[] args) => {
            // integrated: the client would be left talking to a dead server. quit through the menu
            if (!GameServer.instance.isDedicated) {
                source.sendMessage("This is singleplayer, quit from the menu.");
                return;
            }

            source.sendMessage("Stopping server...");
            GameServer.instance.stop();
        };

        commands.Add(new Command("stop", "Stops the server", CmdSide.SERVER, stopAction, true));
        commands.Add(new Command("exit", "Stops the server", CmdSide.SERVER, stopAction, true));
        commands.Add(new Command("quit", "Stops the server", CmdSide.SERVER, stopAction, true));

        commands.Add(new Command("list", "Lists connected players", CmdSide.SERVER, (source, args) => {
            var conns = GameServer.instance.connections.Values;
            if (conns.Count == 0) {
                source.sendMessage("No players online");
                return;
            }

            source.sendMessage($"{conns.Count} player(s) online:");
            foreach (var conn in conns) {
                if (conn.authenticated) {
                    var m = conn.metrics;
                    source.sendMessage($"  {conn.username} (ping: {conn.ping}ms) ↑{m.bytesSent / 1024.0:0.0}KB/s ({m.packetsSent}/s) ↓{m.bytesReceived / 1024.0:0.0}KB/s ({m.packetsReceived}/s)");
                }
            }
        }));

        commands.Add(new Command("save", "Saves world and user data", CmdSide.SERVER, (source, args) => {
            var srv = GameServer.instance;
            source.sendMessage("Saving...");

            if (srv.isDedicated) {
                srv.saveUsers();
                srv.saveOps();
            }

            srv.world.worldIO.save(srv.world, srv.world.name, saveChunks: true);
            source.sendMessage("Save complete");
        }, true));

        commands.Add(new Command("op", "Grants operator permissions to a player", CmdSide.SERVER, (source, args) => {
            if (args.Length != 1) {
                source.sendMessage("Usage: /op <player>");
                return;
            }

            var srv = GameServer.instance;
            var username = args[0];
            if (!srv.ops.Add(username)) {
                source.sendMessage($"{username} is already an operator");
                return;
            }

            if (srv.isDedicated) {
                srv.saveOps();
            }

            source.sendMessage($"Made {username} an operator");
            notifyOnline(username, "&eYou are now an operator");
        }, true));

        commands.Add(new Command("deop", "Revokes operator permissions from a player", CmdSide.SERVER, (source, args) => {
            if (args.Length != 1) {
                source.sendMessage("Usage: /deop <player>");
                return;
            }

            var srv = GameServer.instance;
            var username = args[0];
            if (!srv.ops.Remove(username)) {
                source.sendMessage($"{username} is not an operator");
                return;
            }

            if (srv.isDedicated) {
                srv.saveOps();
            }

            source.sendMessage($"Removed operator status from {username}");
            notifyOnline(username, "&eYou are no longer an operator");
        }, true));

        commands.Add(new Command("setspawn", "Sets the world spawn point", CmdSide.SERVER, (source, args) => {
            var world = source.getWorld();
            Vector3D newSpawn;

            if (args.Length == 3) {
                if (!parsePos(source, args, 0, origin(source), out newSpawn)) {
                    return;
                }
            }
            else if (args.Length == 0 && source is Player player) {
                newSpawn = player.position;
            }
            else {
                source.sendMessage("Usage: /setspawn [x y z]");
                return;
            }

            world.spawn = newSpawn;
            source.sendMessage($"Set spawn to {fmt(newSpawn)}");
        }, true));

        // cheat because it speaks as [Server]. otherwise anyone can impersonate the server
        commands.Add(new Command("say", "Broadcasts a message to all players", CmdSide.SERVER, (source, args) => {
            if (args.Length < 1) {
                source.sendMessage("Usage: /say <message>");
                return;
            }

            var msg = string.Join(' ', args);
            GameServer.instance.send(new ChatMessagePacket { message = $"&h[Server]&r {msg}" }, DeliveryMethod.ReliableOrdered);

            // a player already got it through the broadcast
            if (source is not Player) {
                source.sendMessage($"&h[Server] {msg}");
            }

            GameServer.instance.discord?.sendMessage($"**[Server]** {TextColours.strip(msg)}");
        }, true));
    }

    private const int MAX_FILL = 262144;

    private static void setGameMode(Player player, GameMode mode) {
        var sp = player as ServerPlayer;
        var conn = sp?.conn;

        if (sp != null) {
            if (sp.currentInventoryID != Constants.INV_ID_PLAYER) {
                var id = sp.currentInventoryID;
                sp.currentCtx.removeViewer(conn!);
                sp.closeInventory();
                conn?.send(new InventoryClosePacket { invID = id }, DeliveryMethod.ReliableOrdered);
            }

            // the old context (and its 2x2 grid) gets yeeted so spill the items
            sp.pickupLooseItems();
            sp.inventoryCtx.removeViewer(conn!);
        }

        player.gameMode = mode;
        if (mode == GameMode.survival) {
            player.flyMode = false;
        }

        sp?.resetFall();

        player.inventoryCtx = mode == GameMode.creative
            ? new CreativeInventoryContext(player.inventory, 40)
            : new SurvivalInventoryContext(player.inventory);
        player.currentCtx = player.inventoryCtx;

        if (sp != null && conn != null) {
            player.inventoryCtx.addViewer(conn, Constants.INV_ID_PLAYER);
            conn.send(new GamemodePacket { gamemode = mode.id }, DeliveryMethod.ReliableOrdered);
            sp.syncInventory();
        }
    }

    private static GameMode? parseMode(string s) => s.ToLowerInvariant() switch {
        "creative" or "c" or "1" => GameMode.creative,
        "survival" or "s" or "0" => GameMode.survival,
        _ => null
    };

    // ============ ARG HELPERS ============
    // to reduce slopcoding in every single command....

    private static Player? self(CommandSource source) {
        if (source is Player p) {
            return p;
        }

        source.sendMessage("The console has to name a player.");
        return null;
    }

    private static Player? findPlayer(CommandSource source, string name) {
        foreach (var p in source.getWorld().players) {
            if (p.name.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                return p;
            }
        }

        source.sendMessage($"Player '{name}' not found");
        return null;
    }

    /** get origin, fallback is worldspawn */
    private static Vector3D origin(CommandSource source) => source is Player p ? p.position : source.getWorld().spawn;

    /** absolute, or ~ / ~5 / ~-3 relative to <paramref name="cur"/> */
    private static bool parseCoord(string s, double cur, out double r) {
        if (s.StartsWith('~')) {
            if (s.Length == 1) {
                r = cur;
                return true;
            }

            if (double.TryParse(s.AsSpan(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var off)) {
                r = cur + off;
                return true;
            }

            r = 0;
            return false;
        }

        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out r);
    }

    /** args[i..i+3] as a position */
    private static bool parsePos(CommandSource source, string[] args, int i, Vector3D cur, out Vector3D pos) {
        if (parseCoord(args[i], cur.X, out var x) && parseCoord(args[i + 1], cur.Y, out var y) && parseCoord(args[i + 2], cur.Z, out var z)) {
            pos = new Vector3D(x, y, z);
            return true;
        }

        source.sendMessage($"Invalid coordinates: {args[i]} {args[i + 1]} {args[i + 2]}");
        pos = default;
        return false;
    }

    /** registry ID by exact name, numeric ID, or case-insensitive name. -1 if none */
    private static int lookup<T>(Registry<T> reg, string name) {
        if (int.TryParse(name, out var id)) {
            return id >= 0 && id < reg.count() ? id : -1;
        }

        var exact = reg.getID(name);
        if (exact >= 0) {
            return exact;
        }

        foreach (var (k, v) in reg.nameToID) {
            if (k.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                return v;
            }
        }

        return -1;
    }

    private static string fmt(Vector3D p) => $"{p.X:0.#}, {p.Y:0.#}, {p.Z:0.#}";

    private static void notifyOnline(string username, string msg) {
        foreach (var conn in GameServer.instance.connections.Values) {
            if (conn.username.Equals(username, StringComparison.OrdinalIgnoreCase)) {
                conn.player?.sendMessage(msg);
                return;
            }
        }
    }

    private static void entityStats(CommandSource source, World world) {
        var entities = world.entities;

        var typeCounts = new Dictionary<string, int>();
        int totalMobs = 0;
        int passiveMobs = 0;
        int hostileMobs = 0;
        int other = 0;
        int notInWorld = 0;

        foreach (var e in entities) {
            typeCounts[e.type] = typeCounts.GetValueOrDefault(e.type, 0) + 1;

            if (!e.inWorld) {
                notInWorld++;
            }

            if (e is Mob) {
                totalMobs++;
                var spawnType = Entities.spawnType[Entities.getID(e.type)];
                if (spawnType == SpawnType.PASSIVE) {
                    passiveMobs++;
                }
                else if (spawnType.isHostile()) {
                    hostileMobs++;
                }
            }
            else {
                other++;
            }
        }

        source.sendMessage($"=== Entity Count: {entities.Count} ===");
        source.sendMessage($"Mobs: {totalMobs} (Passive: {passiveMobs}, Hostile: {hostileMobs}) ");
        source.sendMessage($"Other: {other}");
        source.sendMessage($"Not in world: {notInWorld}");
        source.sendMessage("");
        source.sendMessage("By Type:");
        foreach (var (type, count) in typeCounts.OrderByDescending(kv => kv.Value)) {
            source.sendMessage($"  {type}: {count}");
        }
    }

    private static string dumpRegistries() {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var path = $"registry_dump_{timestamp}.txt";

        using var writer = new StreamWriter(path);
        writer.WriteLine("=== BLOCKGAME REGISTRY DUMP ===");
        writer.WriteLine($"Generated: {DateTime.Now}");
        writer.WriteLine();

        var registryType = typeof(Registry);
        var fields = registryType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        foreach (var field in fields) {
            var value = field.GetValue(null);
            if (value == null) {
                continue;
            }

            // check if it's a Registry<T> or ObjectRegistry<T, TFactory>
            var fieldType = field.FieldType;
            var baseType = fieldType;
            while (baseType != null && !baseType.IsGenericType) {
                baseType = baseType.BaseType;
            }

            if (baseType == null || !baseType.GetGenericTypeDefinition().Name.Contains("Registry")) {
                continue;
            }

            writer.WriteLine($"=== {field.Name} ===");

            // get count
            var countMethod = fieldType.GetMethod("count");
            if (countMethod != null) {
                int count = (int)countMethod.Invoke(value, null)!;
                writer.WriteLine($"Total: {count} entries");
                writer.WriteLine();

                // dump all entries
                var getNameMethod = fieldType.GetMethod("getName");
                if (getNameMethod != null) {
                    for (int i = 0; i < count; i++) {
                        var name = (string?)getNameMethod.Invoke(value, [i]);
                        writer.WriteLine($"  [{i,4}] {name}");
                    }
                }
            }

            writer.WriteLine();
        }

        return path;
    }
}

public interface CommandSource {
    public void sendMessage(string msg);

    public World getWorld();

    public bool isPlayer() {
        return this is Player;
    }

    public bool isOp() {
        return this switch {
            ServerConsole => true,
            // the integrated host owns the world
            ServerPlayer sp => sp.conn is { isHost: true } || GameServer.instance.isOp(sp.name),
            _ => false
        };
    }
}
