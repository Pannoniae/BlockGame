using BlockGame.logic;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.item.inventory;
using Molten.DoublePrecision;

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
                    source.sendMessage("Debug commands: /debug lightmap, /debug noise, /debug atlas");
                    break;
                case "lightmap":
                    // dump lightmap to file
                    Game.textures.dumpLightmap();
                    source.sendMessage("Lightmap dumped to lightmap.png");
                    break;
                case "noise":
                    // toggle noise debug display
                    Game.debugShowNoise = !Game.debugShowNoise;
                    source.sendMessage($"Noise debug display: {(Game.debugShowNoise ? "enabled" : "disabled")}");
                    break;
                case "atlas":
                    // dump texture atlas to file
                    Game.textures.dumpAtlas();
                    source.sendMessage("Texture atlas dumped to atlas.png");
                    break;
                default:
                    source.sendMessage($"Unknown debug command: {subCmd}");
                    break;
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

                if (entityField == null || !entityField.IsLiteral) {
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
                var blockField = typeof(Blocks).GetField(blockName);
                if (blockField == null || !blockField.IsLiteral) {
                    source.sendMessage($"Unknown block: {args[6]}");
                    return;
                }

                blockId = (ushort)blockField.GetValue(null)!;
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
}

public interface CommandSource {
    public void sendMessage(string msg);

    public World getWorld();
}