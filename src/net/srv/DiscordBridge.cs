using BlockGame.net.packet;
using LiteNetLib;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using LogLevel = BlockGame.util.log.LogLevel;
using Log = BlockGame.util.log.Log;

namespace BlockGame.net.srv;

public class DiscordBridge : IDisposable {
    private readonly GatewayClient client;
    private readonly CancellationTokenSource cts;

    private readonly ulong channelId;

    public DiscordBridge(string token, ulong channelId) {
        client = new GatewayClient(new BotToken(token), new GatewayClientConfiguration {
            Logger = new DiscordLogger(),
            Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent,
        });

        client.MessageCreate += async message => {
            if (message.Author.IsBot || message.ChannelId != channelId)
                return;

            var msg = $"&d[Discord] &r<{message.Author.Username}> {message.Content}";

            GameServer.instance.send(
                new ChatMessagePacket { message = msg },
                DeliveryMethod.ReliableOrdered
            );
        };

        client.Ready += async ready => {
            await client.Rest.SendMessageAsync(this.channelId, "**Server Started!**");
            await client.UpdatePresenceAsync(
                new PresenceProperties(UserStatusType.DoNotDisturb)
                    .WithActivities([
                        new UserActivityProperties("BlockGame", UserActivityType.Playing)
                    ])
            );
        };

        cts = new CancellationTokenSource();
        this.channelId = channelId;
    }

    public void start() {
        _ = Task.Run(() => client.StartAsync(cancellationToken: cts.Token));
    }

    public void stop() {
        // wait for server stop message to send before shutting down client
        client.Rest.SendMessageAsync(channelId, "**Server Stopped!**").GetAwaiter().GetResult();
        cts.Cancel();
    }

    public async Task sendMessage(string message) {
        await client.Rest.SendMessageAsync(this.channelId, message);
    }

    public async Task updatePlayerCountStatus() {
        var players = GameServer.instance.connections.Count;
        var max = GameServer.instance.maxPlayers;

        await client.UpdatePresenceAsync(
            new PresenceProperties(UserStatusType.DoNotDisturb)
                .WithActivities([new UserActivityProperties($"{players}/{max} Players Online", UserActivityType.Watching)]));
    }

    public void Dispose() {
        client.Dispose();
        cts.Dispose();
    }
}

public class DiscordLogger : IGatewayLogger {
    void IGatewayLogger.Log<TState>(NetCord.Logging.LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (!IsEnabled(logLevel))
            return;

        var msg = formatter(state, exception);
        Log.log(logLevel switch {
            NetCord.Logging.LogLevel.Critical or NetCord.Logging.LogLevel.Error => LogLevel.ERROR,
            NetCord.Logging.LogLevel.Warning => LogLevel.WARNING,
            NetCord.Logging.LogLevel.Information => LogLevel.INFO,
            NetCord.Logging.LogLevel.Debug or NetCord.Logging.LogLevel.Trace => LogLevel.DEBUG,
            _ => LogLevel.INFO,
        }, "Discord", msg);
    }

    public bool IsEnabled(NetCord.Logging.LogLevel logLevel) {
        var clevel = Log.minLevel switch {
            LogLevel.DEBUG => NetCord.Logging.LogLevel.Debug,
            LogLevel.INFO => NetCord.Logging.LogLevel.Information,
            LogLevel.WARNING => NetCord.Logging.LogLevel.Warning,
            LogLevel.ERROR => NetCord.Logging.LogLevel.Error,
            _ => NetCord.Logging.LogLevel.Information,
        };
        return logLevel >= clevel;
    }
}