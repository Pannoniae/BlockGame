using BlockGame.main;
using BlockGame.world;
using BlockGame.world.entity;
using BlockGame.world.worldgen;
using BlockGame.util.log;
using BlockGame.world.chunk;
using DiscordRPC;
using DiscordRPC.Logging;

namespace BlockGame.util;

/**
 * Discord Rich Presence integration
 *
 * Shows current game state in Discord:
 * - Menu vs playing
 * - Singleplayer/Multiplayer
 * - Current biome
 * - Player count in multiplayer
 */
public class DiscordPresence : IDisposable {
    private const string APP_ID = "1440954871858728981";

    private readonly DiscordRpcClient client;
    private BiomeType lastBiome = BiomeType.Plains;
    private bool inWorld = false;
    private long sessionStart;

    public DiscordPresence() {
        sessionStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        client = new DiscordRpcClient(APP_ID);

        client.Logger = new ConsoleLogger { Level = DiscordRPC.Logging.LogLevel.Warning };

        // events
        client.OnReady += (sender, e) => {
            Log.info($"Discord RPC ready: {e.User.Username}");
            // set initial state after client is ready
            updateMenu();
        };

        client.OnError += (sender, e) => {
            Log.error($"Discord RPC error: {e.Message}");
        };

        client.Initialize();
    }

    /** update presence for main menu / not in world */
    public void updateMenu() {
        inWorld = false;

        client.SetPresence(new RichPresence {
            Details = "In Menu",
            State = "Playing BlockGame",
            Timestamps = new Timestamps {
                Start = DateTime.UtcNow.AddSeconds(-(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - sessionStart))
            },
            Buttons = [
                new Button { Label = "Discord", Url = "https://discord.gg/tdbsvWpADe" }
            ],
            Assets = new DiscordRPC.Assets {
                LargeImageKey = "logo",
                LargeImageText = "BlockGame",
                SmallImageKey = "logo",
                SmallImageText = "In Menu"
            }
        });
    }

    /** update presence for being in-world */
    public void updateWorld(World? world, BiomeType biome) {
        if (world == null) {
            updateMenu();
            return;
        }

        inWorld = true;
        lastBiome = biome;

        // determine if SP or MP
        string state;
        if (Net.mode.isMPC()) {
            var playerCount = Game.client?.playerList.Count ?? 0;
            state = $"Multiplayer ({playerCount} players)";
        }
        else {
            state = "Singleplayer";
        }

        client.SetPresence(new RichPresence {
            Details = $"In {biome}",
            State = state,
            Timestamps = new Timestamps {
                Start = DateTime.UtcNow.AddSeconds(-(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - sessionStart))
            },
            Buttons = [
                new Button { Label = "Discord", Url = "https://discord.gg/tdbsvWpADe" }
            ],
            Assets = new DiscordRPC.Assets {
                LargeImageKey = "logo",
                LargeImageText = "BlockGame",
                SmallImageKey = getBiomeLogo(biome),
                SmallImageText = biome.ToString()
            }
        });
    }

    private static string getBiomeLogo(BiomeType biome) {
        return biome switch {
            BiomeType.Desert => "biomedesert",
            BiomeType.Forest => "biomeforest",
            BiomeType.Taiga => "biomesnow",
            BiomeType.Jungle => "biomejungle",
            BiomeType.Ocean => "biomeocean",
            _ => "biomeforest"
        };
    }

    /** get current biome at the player's position */
    public static BiomeType getBiomeAtPlayer(Player player) {
        try {
            var pos = player.position;
            int x = (int)pos.X;
            int y = (int)pos.Y;
            int z = (int)pos.Z;

            int cx = x >> 4;
            int cz = z >> 4;

            var chunk = player.world.getChunk(new ChunkCoord(cx, cz));
            if (chunk?.biomeData == null) {
                return BiomeType.Plains;
            }

            // get local coords
            int lx = x & 15;
            int lz = z & 15;

            var temp = chunk.biomeData.getTemp(lx, y, lz);
            var hum = chunk.biomeData.getHum(lx, y, lz);

            return Biomes.getType(temp, hum, y);
        }
        catch (Exception e) {
            Log.error(e);
            return BiomeType.Plains;
        }
    }

    public void checkBiomeUpdate(Player? player) {
        if (!inWorld || player?.world == null) {
            return;
        }

        var currentBiome = getBiomeAtPlayer(player);
        if (currentBiome != lastBiome) {
            updateWorld(player.world, currentBiome);
        }
    }

    public void Dispose() {
        client?.Dispose();
    }
}
