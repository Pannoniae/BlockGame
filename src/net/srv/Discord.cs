using BlockGame.net.packet;
using LiteNetLib;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;

namespace BlockGame.net.srv {
    public class Discord {
        private GatewayClient client;
        private Task? process = null;
        private CancellationTokenSource cts;

        private ulong channelId;

        public Discord(string token, ulong channelId) {
            client = new GatewayClient(new BotToken(token), new GatewayClientConfiguration {
                Logger = new ConsoleLogger(),
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
            process = Task.Run(() => client.StartAsync(cancellationToken: this.cts.Token));
        }

        public void stop() {
            // wait for server stop message to send before shutting down client
            client.Rest.SendMessageAsync(this.channelId, "**Server Stopped!**").GetAwaiter().GetResult();
            cts.Cancel();
        }

        public void sendMessage(string message) {
            client.Rest.SendMessageAsync(this.channelId, message);
        }

        public void updatePlayerCountStatus() {
            var players = GameServer.instance.connections.Count;
            var max = GameServer.instance.maxPlayers;

            client.UpdatePresenceAsync(
                new PresenceProperties(UserStatusType.DoNotDisturb)
                    .WithActivities([new UserActivityProperties($"{players}/{max} Players Online", UserActivityType.Watching)]));
        }
    }
}