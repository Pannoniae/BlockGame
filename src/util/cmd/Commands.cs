using BlockGame.logic;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util.log;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.entity;
using BlockGame.world.item.inventory;
using BlockGame.world.worldgen.generator;
using Molten.DoublePrecision;
using Registry = BlockGame.util.stuff.Registry;

namespace BlockGame.util.cmd;

public readonly struct Command {
    public static XUList<Command> commands = [];

    public readonly string name;
    public readonly string desc;
    public readonly Action<CommandSource, string[]> action;
    public readonly NetMode side;

    public Command(string name, string desc, NetMode side, Action<CommandSource, string[]> action) {
        this.name = name;
        this.desc = desc;
        this.action = action;
    }

    public static void execute(CommandSource src, string[] args) {
        if (args.Length == 0) {
            src.sendMessage("No command provided. Type /help for a list of commands.");
            return;
        }

        string cmdName = args[0];
        Command? cmd = null;
        foreach (var command in commands) {
            if (command.name.Equals(cmdName, StringComparison.OrdinalIgnoreCase)) {
                cmd = command;
                break;
            }
        }

        if (cmd == null) {
            src.sendMessage($"Unknown command: {cmdName}. Type /help for a list of commands.");
            return;
        }

        // check side
        // NOTE; not functional yet, we don't have netcode!
        if ((cmd.Value.side != NetMode.BOTH && Net.mode != cmd.Value.side) && false) {
            src.sendMessage($"Command {cmdName} cannot be used in the current mode.");
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
        commands.Add(new Command("help", "Lists all available commands", NetMode.BOTH, (source, args) => {
            source.sendMessage("Available commands:");
            foreach (var cmd in commands) {
                source.sendMessage($"/{cmd.name} - {cmd.desc}");
            }
        }));

        commands.Add(new Command("gamemode", "Changes the player's game mode", NetMode.CL, (source, args) => {
            if (args.Length == 1) {
                switch (args[0].ToLower()) {
                    case "creative":
                    case "c":
                    case "1":
                        Game.gamemode = GameMode.creative;
                        // switch to creative inventory context
                        Game.player.inventoryCtx = new CreativeInventoryContext(40);
                        source.sendMessage("Set gamemode to Creative");
                        break;
                    case "survival":
                    case "s":
                    case "0":
                        Game.gamemode = GameMode.survival;
                        // disable flying when switching to survival
                        Game.player.flyMode = false;
                        // switch to survival inventory context
                        Game.player.inventoryCtx = new SurvivalInventoryContext(Game.player.inventory);
                        source.sendMessage("Set gamemode to Survival");
                        break;
                    default:
                        source.sendMessage("Invalid gamemode. Use: creative/c/1 or survival/s/0");
                        break;
                }
            }
            else {
                var currentMode = Game.gamemode.name;
                source.sendMessage($"Current gamemode: {currentMode}. Usage: /gamemode <creative|survival>");
            }
        }));

        commands.Add(new Command("tp", "Teleports the player to specified coordinates", NetMode.CL, (source, args) => {
            if (args.Length == 3) {
                if (!parseCoord(args[0], Game.player.position.X, out int x) ||
                    !parseCoord(args[1], Game.player.position.Y, out int y) ||
                    !parseCoord(args[2], Game.player.position.Z, out int z)) {
                    source.sendMessage("Invalid coordinates");
                    return;
                }

                Game.player.teleport(new Vector3D(x, y, z));
                source.sendMessage($"Teleported to {x}, {y}, {z}!");
            }
            else {
                source.sendMessage("Usage: /tp <x> <y> <z>");
            }
        }));

        commands.Add(new Command("clear", "Clears the chat messages", NetMode.CL, (source, args) => {
            var chatMenu = Screen.GAME_SCREEN.CHAT;
            chatMenu.getMessages().Clear();
            chatMenu.addMessage("Cleared chat!");
        }));

        commands.Add(new Command("cb", "Toggles chunk borders", NetMode.CL, (source, args) => {
            if (Net.mode != NetMode.MPS) {
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

        commands.Add(new Command("fb", "Toggles fullbright mode", NetMode.CL, (source, args) => {
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

        commands.Add(new Command("fly", "Toggles noclip mode", NetMode.CL, (source, args) => {
            Game.player.noClip = !Game.player.noClip;
            source.sendMessage("Noclip " + (Game.player.noClip ? "enabled" : "disabled"));
        }));

        commands.Add(new Command("time", "Gets or sets the world time", NetMode.CL, (source, args) => {
            if (args.Length == 0) {
                // display current time
                var currentTick = Game.world.worldTick;
                var dayPercent = Game.world.getDayPercentage(currentTick);
                var timeOfDay = (int)(dayPercent * World.TICKS_PER_DAY);
                source.sendMessage($"The time is {timeOfDay} (day {currentTick / World.TICKS_PER_DAY})");
            }
            else if (args is ["set", _]) {
                // set time
                if (int.TryParse(args[1], out int newTime)) {
                    Game.world.worldTick = newTime;
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
        }));

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

                // parse position (default to player position)
                Vector3D spawnPos = Game.player.position;
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

                entity.position = spawnPos;
                entity.prevPosition = spawnPos;
                source.getWorld().addEntity(entity);
                source.sendMessage($"Summoned {args[0]} at {(int)spawnPos.X}, {(int)spawnPos.Y}, {(int)spawnPos.Z}");
            }));

        commands.Add(new Command("ec", "Shows entity count stats", NetMode.BOTH, (source, args) => {
            var world = source.getWorld();
            var entities = world.entities;

            // count by type
            var typeCounts = new Dictionary<string, int>();
            int totalMobs = 0;
            int passiveMobs = 0;
            int hostileMobs = 0;
            int other = 0;

            foreach (var e in entities) {
                var typeName = e.type;
                typeCounts[typeName] = typeCounts.GetValueOrDefault(typeName, 0) + 1;

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
            source.sendMessage("");
            source.sendMessage("By Type:");
            foreach (var (type, count) in typeCounts.OrderByDescending(kv => kv.Value)) {
                source.sendMessage($"  {type}: {count}");
            }
        }));

        commands.Add(new Command("setblock", "Sets blocks in a specified region", NetMode.CL, (source, args) => {

            if (args.Length < 7) {
                source.sendMessage("Usage: /setblock <x0> <y0> <z0> <x1> <y1> <z1> <block>");
                return;
            }

            if (!parseCoord(args[0], Game.player.position.X, out int x0) ||
                !parseCoord(args[1], Game.player.position.Y, out int y0) ||
                !parseCoord(args[2], Game.player.position.Z, out int z0) ||
                !parseCoord(args[3], Game.player.position.X, out int x1) ||
                !parseCoord(args[4], Game.player.position.Y, out int y1) ||
                !parseCoord(args[5], Game.player.position.Z, out int z1)) {
                source.sendMessage("Invalid coordinates");
                return;
            }

            // parse block
            ushort blockId;
            if (ushort.TryParse(args[6], out blockId)) {
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
        }));

        commands.Add(new Command("give", "Gives an item to the player", NetMode.CL, (source, args) => {
            if (args.Length < 1) {
                source.sendMessage("Usage: /give <item> [quantity] [metadata]");
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
            if (Game.player.inventory.addItem(itemStack)) {
                source.sendMessage($"Gave {quantity} of {args[0]}");
            }
            else {
                source.sendMessage("Not enough space in inventory");
            }
        }));

        commands.Add(new Command("ci", "Clears the player's inventory", NetMode.CL, (source, args) => {
            Game.player.inventory.clearAll();
            source.sendMessage("Cleared inventory");
        }));

        commands.Add(new Command("chunkmuncher", "Deletes all blocks in the current chunk", NetMode.CL,
            (source, args) => {

                var playerPos = Game.player.position.toBlockPos();
                var chunkCoord = World.getChunkPos(playerPos.X, playerPos.Z);

                if (!Game.world.getChunkMaybe(chunkCoord, out var playerChunk)) {
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
                            if (Game.world.getBlock(wx, cy, wz) != 0) {
                                Game.world.setBlockMetadata(wx, cy, wz, 0);
                                munched++;
                            }
                        }
                    }
                }

                source.sendMessage($"Munched {munched} blocks from chunk ({chunkCoord.x}, {chunkCoord.z})");
            }));

        commands.Add(new Command("suicide", "Commits suicide", NetMode.CL, (source, args) => {
            Game.player.hp = 0;
            source.sendMessage("You have committed suicide.");
        }));
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
}