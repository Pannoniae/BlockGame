using BlockGame.logic;
using BlockGame.main;
using BlockGame.net;
using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.ui;
using BlockGame.util.log;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.entity;
using BlockGame.world.item.inventory;
using BlockGame.world.worldgen.generator;
using LiteNetLib;
using Molten.DoublePrecision;
using Registry = BlockGame.util.stuff.Registry;

namespace BlockGame.util.cmd;

public readonly struct Command {
    public static readonly XUList<Command> commands = [];

    public readonly string name;
    public readonly string desc;
    public readonly Action<CommandSource, string[]> action;
    public readonly NetMode side;
    public readonly bool cheat;

    public Command(string name, string desc, NetMode side, Action<CommandSource, string[]> action, bool cheat = false) {
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

        // check side
        bool match = (Net.mode & cmd.Value.side) == 0;
        if (cmd.Value.side != NetMode.BOTH && match) {
            src.sendMessage($"Command {cmdName} cannot be used in the current mode.");
            return;
        }

        // check permissions
        if (cmd.Value.cheat && !src.isOp()) {
            src.sendMessage("You must be an operator to use this command.");
            return;
        }

        try {
            cmd.Value.action(src, args[1..]);
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
            source.sendMessage("Available commands:");
            foreach (var cmd in commands) {
                source.sendMessage($"/{cmd.name} - {cmd.desc}");
            }
        };

        commands.Add(new Command("help", "Lists all available commands", NetMode.BOTH, helpAction));
        commands.Add(new Command("?", "Lists all available commands", NetMode.BOTH, helpAction));

        commands.Add(new Command("gamemode", "Changes the player's game mode", NetMode.BOTH, (source, args) => {
            // 1-arg version
            if (args.Length == 1) {

                // bail on server
                if (!source.isPlayer()) {
                    source.sendMessage("You need to specify a player when changing gamemode on a server.");
                    return;
                }

                switch (args[0].ToLower()) {
                    case "creative":
                    case "c":
                    case "1":
                        var player = source as Player;
                        player!.gameMode = GameMode.creative;
                        // switch to creative inventory context
                        player.inventoryCtx = new CreativeInventoryContext(40);

                        // sync gamemode
                        if (Net.mode.isDed()) {
                            var srvPlayer = player as ServerPlayer;
                            srvPlayer.conn.send(new GamemodePacket {
                                gamemode = GameMode.creative.id
                            }, DeliveryMethod.ReliableOrdered);
                        }


                        source.sendMessage("Set gamemode to Creative");
                        break;
                    case "survival":
                    case "s":
                    case "0":
                        player = source as Player;
                        player!.gameMode = GameMode.survival;
                        // disable flying when switching to survival
                        player.flyMode = false;
                        // switch to survival inventory context
                        player.inventoryCtx = new SurvivalInventoryContext(player.inventory);

                        // sync gamemode
                        if (Net.mode.isDed()) {
                            var srvPlayer = player as ServerPlayer;
                            srvPlayer.conn.send(new GamemodePacket {
                                gamemode = GameMode.survival.id
                            }, DeliveryMethod.ReliableOrdered);
                        }

                        source.sendMessage("Set gamemode to Survival");
                        break;
                    default:
                        source.sendMessage("Invalid gamemode. Use: creative/c/1 or survival/s/0");
                        break;
                }
            }
            else if (args.Length == 0 && source.isPlayer()) {
                var currentMode = (source as Player)?.gameMode.name;
                source.sendMessage($"Current gamemode: {currentMode}. Usage: /gamemode <creative|survival>");
            }

            else if (args.Length == 2) {
                var name = args[0];
                foreach (var player in source.getWorld().players) {
                    if (player.name.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                        switch (args[1].ToLower()) {
                            case "creative":
                            case "c":
                            case "1":
                                player.gameMode = GameMode.creative;
                                // switch to creative inventory context
                                player.inventoryCtx = new CreativeInventoryContext(40);

                                if (Net.mode.isDed()) {
                                    var srvPlayer = player as ServerPlayer;
                                    srvPlayer.conn.send(new GamemodePacket {
                                        gamemode = GameMode.creative.id
                                    }, DeliveryMethod.ReliableOrdered);
                                }

                                source.sendMessage($"Set gamemode of {name} to Creative");
                                break;
                            case "survival":
                            case "s":
                            case "0":
                                player.gameMode = GameMode.survival;
                                // disable flying when switching to survival
                                player.flyMode = false;
                                // switch to survival inventory context
                                player.inventoryCtx = new SurvivalInventoryContext(player.inventory);

                                if (Net.mode.isDed()) {
                                    var srvPlayer = player as ServerPlayer;
                                    srvPlayer.conn.send(new GamemodePacket {
                                        gamemode = GameMode.survival.id
                                    }, DeliveryMethod.ReliableOrdered);
                                }

                                source.sendMessage($"Set gamemode of {name} to Survival");
                                break;
                            default:
                                source.sendMessage("Invalid gamemode. Use: creative/c/1 or survival/s/0");
                                break;
                        }

                        return;
                    }
                }
                source.sendMessage($"Player '{name}' not found.");
            }
            else {
                source.sendMessage("Usage: /gamemode <creative|survival>");
            }
        }, true));

        commands.Add(new Command("tp", "Teleports the player to specified coordinates", NetMode.BOTH, (source, args) => {
            // 1-arg: /tp <player> - teleport yourself to player
            if (args.Length == 1) {
                if (source is not Player player) {
                    source.sendMessage("Console cannot teleport itself");
                    return;
                }

                var targetName = args[0];
                Player? target = null;

                foreach (var p in source.getWorld().players) {
                    if (p.name.Equals(targetName, StringComparison.OrdinalIgnoreCase)) {
                        target = p;
                        break;
                    }
                }

                if (target == null) {
                    source.sendMessage($"Player '{targetName}' not found");
                    return;
                }

                player.teleport(target.position);
                source.sendMessage($"Teleported to {targetName}");
            }
            // 2-arg: /tp <player1> <player2> - teleport player1 to player2
            else if (args.Length == 2) {
                var player1Name = args[0];
                var player2Name = args[1];
                Player? player1 = null;
                Player? player2 = null;

                foreach (var p in source.getWorld().players) {
                    if (p.name.Equals(player1Name, StringComparison.OrdinalIgnoreCase)) {
                        player1 = p;
                    }
                    if (p.name.Equals(player2Name, StringComparison.OrdinalIgnoreCase)) {
                        player2 = p;
                    }
                }

                if (player1 == null) {
                    source.sendMessage($"Player '{player1Name}' not found");
                    return;
                }
                if (player2 == null) {
                    source.sendMessage($"Player '{player2Name}' not found");
                    return;
                }

                player1.teleport(player2.position);
                source.sendMessage($"Teleported {player1Name} to {player2Name}");
            }
            // 3-arg: /tp <x> <y> <z> - teleport yourself to coords
            else if (args.Length == 3) {
                if (source is not Player player) {
                    source.sendMessage("Console must specify player: /tp <player> <x> <y> <z>");
                    return;
                }

                if (!parseCoord(args[0], player.position.X, out int x) ||
                    !parseCoord(args[1], player.position.Y, out int y) ||
                    !parseCoord(args[2], player.position.Z, out int z)) {
                    source.sendMessage("Invalid coordinates");
                    return;
                }

                player.teleport(new Vector3D(x, y, z));
                source.sendMessage($"Teleported to {x}, {y}, {z}!");
            }
            // 4-arg: /tp <player> <x> <y> <z> - teleport player to coords
            else if (args.Length == 4) {
                var targetName = args[0];
                Player? target = null;

                foreach (var p in source.getWorld().players) {
                    if (p.name.Equals(targetName, StringComparison.OrdinalIgnoreCase)) {
                        target = p;
                        break;
                    }
                }

                if (target == null) {
                    source.sendMessage($"Player '{targetName}' not found");
                    return;
                }

                if (!parseCoord(args[1], target.position.X, out int x) ||
                    !parseCoord(args[2], target.position.Y, out int y) ||
                    !parseCoord(args[3], target.position.Z, out int z)) {
                    source.sendMessage("Invalid coordinates");
                    return;
                }

                target.teleport(new Vector3D(x, y, z));
                source.sendMessage($"Teleported {targetName} to {x}, {y}, {z}!");
            }
            else {
                source.sendMessage("Usage: /tp <player|x> [player|y] [z] or /tp <player> <x> <y> <z>");
            }
        }, true));

        commands.Add(new Command("clear", "Clears the chat messages", NetMode.CL, (source, args) => {
            var chatMenu = Screen.GAME_SCREEN.CHAT;
            chatMenu.getMessages().Clear();
            chatMenu.addMessage("Cleared chat!");
        }));

        commands.Add(new Command("cb", "Toggles chunk borders", NetMode.CL, (source, args) => {
            if (source.isPlayer()) {
                if (Screen.GAME_SCREEN.chunkBorders) {
                    Screen.GAME_SCREEN.chunkBorders = false;
                    source.sendMessage("Chunk borders disabled");
                }
                else {
                    Screen.GAME_SCREEN.chunkBorders = true;
                    source.sendMessage("Chunk borders enabled");
                }
            }
            else {
                source.sendMessage("This command can only be used from the chat menu.");
            }
        }));

        commands.Add(new Command("fb", "Toggles fullbright mode", NetMode.SP, (source, args) => {
            if (Game.graphics.fullbright) {
                Game.graphics.fullbright = false;
                source.sendMessage("Fullbright disabled");
            }
            else {
                Game.graphics.fullbright = true;
                source.sendMessage("Fullbright enabled");
            }

            // remesh everything to update lighting
            Game.instance.executeOnMainThread(() => { Screen.GAME_SCREEN.remeshWorld(0); });
        }));

        commands.Add(new Command("fly", "Toggles noclip mode", NetMode.BOTH, (source, args) => {
            if (!source.isPlayer()) {
                source.sendMessage("This command can only be used by a player.");
                return;
            }

            var player = source as Player;
            player.noClip = !player.noClip;
            source.sendMessage("Noclip " + (player.noClip ? "enabled" : "disabled"));
        }, true));

        commands.Add(new Command("time", "Gets or sets the world time", NetMode.BOTH, (source, args) => {
            if (args.Length == 0) {
                // display current time
                var currentTick = source.getWorld().worldTick;
                var dayPercent = source.getWorld().getDayPercentage(currentTick);
                var timeOfDay = (int)(dayPercent * World.TICKS_PER_DAY);
                source.sendMessage($"The time is {timeOfDay} (day {currentTick / World.TICKS_PER_DAY})");
            }
            else if (args is ["set", _]) {
                // set time
                if (int.TryParse(args[1], out int newTime)) {
                    source.getWorld().worldTick = newTime;
                    source.sendMessage($"Set time to {newTime}");

                    // remesh world
                    //Game.instance.executeOnMainThread(() => { Screen.GAME_SCREEN.remeshWorld(0); });
                }
                else {
                    source.sendMessage("Usage: /time set <time>");
                }
            }
            else {
                source.sendMessage("Usage: /time or /time set <time>");
            }
        }, true));

        commands.Add(new Command("debug", "Various debug commands", NetMode.CL, (source, args) => {
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

        commands.Add(new Command("noise", "Sample noise values at player position", NetMode.CL, (source, args) => {
            var pos = Game.player.position.toBlockPos();

            var gen = Game.world.generator;
            string result;

            if (gen is NewWorldGenerator nwg) {
                result = nwg.sample(pos.X, pos.Y, pos.Z);
            }
            else if (gen is PerlinWorldGenerator pwg) {
                result = pwg.sample(pos.X, pos.Y, pos.Z);
            }
            else {
                source.sendMessage($"Generator '{Game.world.generatorName}' does not support noise sampling");
                return;
            }

            // send each line separately
            foreach (var line in result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)) {
                source.sendMessage(line);
            }
        }));

        commands.Add(new Command("summon", "Summons an entity at the specified location", NetMode.CL,
            (source, args) => {
                if (args.Length < 1) {
                    source.sendMessage("Usage: /summon <entity> [x] [y] [z]");
                    return;
                }

                var entityName = args[0].ToUpper();
                var entityField = typeof(Entities).GetField(entityName);

                if (entityField == null || !entityField.IsStatic || entityField.FieldType != typeof(int)) {
                    source.sendMessage($"Unknown entity: {args[0]}");
                    return;
                }

                int entityType = (int)entityField.GetValue(null)!;
                if (entityType == Entities.PLAYER || entityType == Entities.ITEM_ENTITY) {
                    source.sendMessage($"Cannot summon entity: {args[0]}");
                    return;
                }

                var entity = Entities.create(source.getWorld(), entityType);
                if (entity == null) {
                    source.sendMessage($"Failed to create entity: {args[0]}");
                    return;
                }

                var player = source as Player;

                // parse position (default to player position)
                Vector3D spawnPos;
                if (args.Length >= 4) {
                    if (double.TryParse(args[1], out double sx) &&
                        double.TryParse(args[2], out double sy) &&
                        double.TryParse(args[3], out double sz)) {
                        spawnPos = new Vector3D(sx, sy, sz);
                    }
                    else {
                        source.sendMessage("Invalid coords");
                        return;
                    }
                }
                else {
                    if (player == null) {
                        source.sendMessage("You must specify coordinates when summoning from console.");
                        return;
                    }
                    spawnPos = player.position;
                }

                entity.position = spawnPos;
                entity.prevPosition = spawnPos;
                source.getWorld().addEntity(entity);
                source.sendMessage($"Summoned {args[0]} at {(int)spawnPos.X}, {(int)spawnPos.Y}, {(int)spawnPos.Z}");
            }, true));

        commands.Add(new Command("ec", "Shows entity count stats", NetMode.BOTH, (source, args) => {
            var world = source.getWorld();
            var entities = world.entities;

            // count by type
            var typeCounts = new Dictionary<string, int>();
            int totalMobs = 0;
            int passiveMobs = 0;
            int hostileMobs = 0;
            int other = 0;
            int notInWorld = 0;

            foreach (var e in entities) {
                var typeName = e.type;
                typeCounts[typeName] = typeCounts.GetValueOrDefault(typeName, 0) + 1;

                if (!e.inWorld) notInWorld++;

                if (e is Mob) {
                    totalMobs++;
                    var spawnType = Entities.spawnType[Entities.getID(e.type)];
                    if (spawnType == SpawnType.PASSIVE) passiveMobs++;
                    else if (spawnType == SpawnType.HOSTILE) hostileMobs++;
                } else {
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
        }));

        commands.Add(new Command("killall", "Kills all entities of a type or all mobs", NetMode.BOTH, (source, args) => {
            var world = source.getWorld();
            var entities = world.entities;

            int killed = 0;
            string targetType = args.Length > 0 ? args[0].ToLower() : "mobs";

            // collect entities to kill (can't modify list while iterating)
            var toKill = new List<Entity>();
            foreach (var e in entities) {
                // never kill players
                if (e is Player) continue;

                bool shouldKill = targetType switch {
                    "all" => true,
                    "mobs" => e is Mob,
                    "passive" => e is Mob && Entities.spawnType[Entities.getID(e.type)] == SpawnType.PASSIVE,
                    "hostile" => e is Mob && Entities.spawnType[Entities.getID(e.type)] == SpawnType.HOSTILE,
                    _ => e.type.Equals(targetType, StringComparison.OrdinalIgnoreCase)
                };

                if (shouldKill) toKill.Add(e);
            }

            // kill them
            foreach (var e in toKill) {
                e.active = false;
                killed++;
            }

            source.sendMessage($"Killed {killed} entities ({targetType})");
        }, true));

        commands.Add(new Command("setblock", "Sets blocks in a specified region", NetMode.CL, (source, args) => {

            if (args.Length < 7) {
                source.sendMessage("Usage: /setblock <x0> <y0> <z0> <x1> <y1> <z1> <block>");
                return;
            }

            if (!source.isPlayer()) {
                source.sendMessage("This command can currently only be used by a player. Yell at the developer!");
                return;
            }

            var player = (source as Player)!;

            if (!parseCoord(args[0], player.position.X, out int x0) ||
                !parseCoord(args[1], player.position.Y, out int y0) ||
                !parseCoord(args[2], player.position.Z, out int z0) ||
                !parseCoord(args[3], player.position.X, out int x1) ||
                !parseCoord(args[4], player.position.Y, out int y1) ||
                !parseCoord(args[5], player.position.Z, out int z1)) {
                source.sendMessage("Invalid coordinates");
                return;
            }

            // parse block
            if (ushort.TryParse(args[6], out var blockId)) {
                // numeric ID
            }
            else {
                // block name
                var blockName = args[6].ToUpper();
                var blockField = typeof(Block).GetField(blockName);
                if (blockField == null || blockField.GetValue(null) is not Block) {
                    source.sendMessage($"Unknown block: {args[6]}");
                    return;
                }

                blockId = ((Block)blockField.GetValue(null)!).id;
            }

            // clamp and sort coords
            int minX = Math.Min(x0, x1);
            int maxX = Math.Max(x0, x1);
            int minY = Math.Max(0, Math.Min(y0, y1));
            int maxY = Math.Min(World.WORLDHEIGHT - 1, Math.Max(y0, y1));
            int minZ = Math.Min(z0, z1);
            int maxZ = Math.Max(z0, z1);

            // fill region
            int count = 0;
            for (int x = minX; x <= maxX; x++) {
                for (int y = minY; y <= maxY; y++) {
                    for (int z = minZ; z <= maxZ; z++) {
                        source.getWorld().setBlock(x, y, z, blockId);
                        count++;
                    }
                }
            }

            source.sendMessage($"Set {count} blocks to {args[6]}");
        }, true));

        commands.Add(new Command("give", "Gives an item to the player", NetMode.CL, (source, args) => {
            if (args.Length < 1) {
                source.sendMessage("Usage: /give <item> [quantity] [metadata]");
                return;
            }

            if (source is not Player player) {
                source.sendMessage("This command can currently only be used by a player. Yell at the developer!");
                return;
            }

            // parse item
            if (int.TryParse(args[0], out var itemID)) {
                // numeric ID
            }
            else {
                // item name
                var itemName = args[0];

                itemID = Registry.ITEMS.getID(itemName);

                if (itemID == -1) {
                    // todo parse player to give to
                    source.sendMessage($"Unknown item: {args[0]}");
                    return;
                }
            }

            // parse quantity
            int quantity = 1;
            if (args.Length >= 2 && !int.TryParse(args[1], out quantity)) {
                source.sendMessage("Invalid quantity");
                return;
            }

            int metadata = 0;
            if (args.Length >= 3 && !int.TryParse(args[2], out metadata)) {
                source.sendMessage("Invalid metadata");
                return;
            }

            // give item
            var itemStack = new ItemStack(itemID, quantity, metadata);
            if (player.inventory.addItem(itemStack)) {
                source.sendMessage($"Gave {quantity} of {args[0]}");
            }
            else {
                source.sendMessage("Not enough space in inventory");
            }
        }, true));

        commands.Add(new Command("ci", "Clears the player's inventory", NetMode.BOTH, (source, args) => {
            if (source is not Player player) {
                source.sendMessage("This command can currently only be used by a player. Yell at the developer!");
                return;
            }
            player.inventory.clearAll();

            // sync all slots!
            for (int i = 0; i < Game.player.inventory.size(); i++) {
                player.inventoryCtx.notifyAllSlotsChanged();
            }
            source.sendMessage("Cleared inventory");
        }, true));

        commands.Add(new Command("chunkmuncher", "Deletes all blocks in the current chunk", NetMode.CL,
            (source, args) => {
                if (source is not Player player) {
                    source.sendMessage("This command can currently only be used by a player. Yell at the developer!");
                    return;
                }

                var playerPos = player.position.toBlockPos();
                var chunkCoord = World.getChunkPos(playerPos.X, playerPos.Z);

                if (!source.getWorld().getChunkMaybe(chunkCoord, out var playerChunk)) {
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
                            if (source.getWorld().getBlock(wx, cy, wz) != 0) {
                                source.getWorld().setBlockMetadata(wx, cy, wz, 0);
                                munched++;
                            }
                        }
                    }
                }

                source.sendMessage($"Munched {munched} blocks from chunk ({chunkCoord.x}, {chunkCoord.z})");
            }, true));

        commands.Add(new Command("suicide", "Commits suicide", NetMode.BOTH, (source, args) => {
            if (source is not Player player) {
                source.sendMessage("Only players can commit suicide.");
                return;
            }

            player.sethp(0);
            source.sendMessage("You have committed suicide.");
        }));

        commands.Add(new Command("netstats", "Shows network statistics", NetMode.BOTH, (source, args) => {
            if (!source.isPlayer()) {
                // server side - show all connected players' stats
                if (GameServer.instance.connections.Count == 0) {
                    source.sendMessage("No players connected");
                    return;
                }

                source.sendMessage("=== Netstats ===");
                foreach (var conn in GameServer.instance.connections.Values) {
                    if (conn.authenticated) {
                        var m = conn.metrics;
                        source.sendMessage($"[{conn.username}] ↑{m.bytesSent / 1024.0:0.0}KB/s ({m.packetsSent}/s) ↓{m.bytesReceived / 1024.0:0.0}KB/s ({m.packetsReceived}/s) ping:{conn.ping}ms");
                    }
                }
            }
            else if (Net.mode == NetMode.MPC) {
                // client side - show client connection stats
                if (ClientConnection.instance == null || !ClientConnection.instance.connected) {
                    source.sendMessage("Not connected to a server");
                    return;
                }

                var m = Game.metrics;
                source.sendMessage("=== Netstats ===");
                source.sendMessage($"↑ {m.bytesSent / 1024.0:0.0}KB/s ({m.packetsSent} pkt/s)");
                source.sendMessage($"↓ {m.bytesReceived / 1024.0:0.0}KB/s ({m.packetsReceived} pkt/s)");
                source.sendMessage($"Ping: {ClientConnection.instance.ping}ms");
            }
            else {
                source.sendMessage("Network statistics only available in multiplayer");
            }
        }));

        var stopAction = (CommandSource source, string[] args) => {
            source.sendMessage("Stopping server...");
            GameServer.instance.stop();
        };

        commands.Add(new Command("stop", "Stops the server", NetMode.DED, stopAction, true));
        commands.Add(new Command("exit", "Stops the server", NetMode.DED, stopAction, true));
        commands.Add(new Command("quit", "Stops the server", NetMode.DED, stopAction, true));

        commands.Add(new Command("list", "Lists connected players", NetMode.DED, (source, args) => {
            if (GameServer.instance.connections.Count == 0) {
                source.sendMessage("No players online");
            }
            else {
                source.sendMessage($"{GameServer.instance.connections.Count} player(s) online:");
                foreach (var conn in GameServer.instance.connections.Values) {
                    if (conn.authenticated) {
                        source.sendMessage($"  {conn.username} (ping: {conn.ping}ms)");
                    }
                }
            }
        }));

        commands.Add(new Command("save", "Saves world and user data", NetMode.DED, (source, args) => {
            source.sendMessage("Saving...");
            GameServer.instance.saveUsers();
            GameServer.instance.saveOps();
            GameServer.instance.world.worldIO.save(GameServer.instance.world, GameServer.instance.world.name, saveChunks: true);
            source.sendMessage("Save complete");
        }, true));

        commands.Add(new Command("op", "Grants operator permissions to a player", NetMode.DED, (source, args) => {
            if (args.Length < 1) {
                source.sendMessage("Usage: /op <player>");
                return;
            }

            var username = args[0];
            if (GameServer.instance.ops.Contains(username)) {
                source.sendMessage($"{username} is already an operator");
                return;
            }

            GameServer.instance.ops.Add(username);
            GameServer.instance.saveOps();
            source.sendMessage($"Made {username} an operator");

            // notify the player if online
            foreach (var conn in GameServer.instance.connections.Values) {
                if (conn.username.Equals(username, StringComparison.OrdinalIgnoreCase)) {
                    conn.player?.sendMessage("&eYou are now an operator");
                    break;
                }
            }
        }, true));

        commands.Add(new Command("deop", "Revokes operator permissions from a player", NetMode.DED, (source, args) => {
            if (args.Length < 1) {
                source.sendMessage("Usage: /deop <player>");
                return;
            }

            var username = args[0];
            if (!GameServer.instance.ops.Remove(username)) {
                source.sendMessage($"{username} is not an operator");
                return;
            }

            GameServer.instance.saveOps();
            source.sendMessage($"Removed operator status from {username}");

            // notify the player if online
            foreach (var conn in GameServer.instance.connections.Values) {
                if (conn.username.Equals(username, StringComparison.OrdinalIgnoreCase)) {
                    conn.player?.sendMessage("&eYou are no longer an operator");
                    break;
                }
            }
        }, true));
    }

    private static bool parseCoord(string input, double current, out int result) {
        if (input.StartsWith('~')) {
            if (input.Length == 1) {
                result = (int)current;
                return true;
            }

            if (int.TryParse(input[1..], out int offset)) {
                result = (int)(current + offset);
                return true;
            }

            result = 0;
            return false;
        }

        return int.TryParse(input, out result);
    }

    private static string dumpRegistries() {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path = $"registry_dump_{timestamp}.txt";

        using var writer = new StreamWriter(path);
        writer.WriteLine("=== BLOCKGAME REGISTRY DUMP ===");
        writer.WriteLine($"Generated: {DateTime.Now}");
        writer.WriteLine();
        
        var registryType = typeof(util.stuff.Registry);
        var fields = registryType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        foreach (var field in fields) {
            var value = field.GetValue(null);
            if (value == null) continue;

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

    /** check if this command source has operator permissions */
    public bool isOp() {
        // console is always OP
        if (Net.mode == NetMode.DED) {
            var sp = this as ServerPlayer;
            return this is ServerConsole || (sp != null && GameServer.instance.isOp(sp.name));
        }
        // in singleplayer/client, player is always OP
        if (Net.mode == NetMode.SP) {
            return true;
        }
        // in multiplayer client, check if player is OP (this won't work, server handles it)
        if (this is ServerPlayer p) {
            return GameServer.instance.isOp(p.name);
        }
        return false;
    }
}