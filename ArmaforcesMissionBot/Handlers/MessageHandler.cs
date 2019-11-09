using ArmaforcesMissionBot.DataClasses;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Handlers
{
    public class MessageHandler : IInstallable
    {
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private Config _config;
        static public Dictionary<ulong, List<Discord.IMessage>> _cachedDeletedMessages = new Dictionary<ulong, List<Discord.IMessage>>();
        static public Dictionary<ulong, List<Discord.IMessage>> _cachedEditedMessages = new Dictionary<ulong, List<Discord.IMessage>>();

        public async Task Install(IServiceProvider map)
        {
            _client = map.GetService<DiscordSocketClient>();
            _config = map.GetService<Config>();
            _services = map;
            // Hook the MessageReceived event into our command handler
            _client.MessageDeleted += MessageDeleted;
            _client.MessageUpdated += MessageUpdated;
        }

        private async Task MessageDeleted(Discord.Cacheable<Discord.IMessage, ulong> before, ISocketMessageChannel channel)
        {
            if (before.Value == null)
                return;

            if (!_cachedDeletedMessages.ContainsKey(channel.Id))
                _cachedDeletedMessages.Add(channel.Id, new List<Discord.IMessage>());
            _cachedDeletedMessages[channel.Id].Add(before.Value);
        }

        private async Task MessageUpdated(Discord.Cacheable<Discord.IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            if (before.Value == null)
                return;

            if (!_cachedEditedMessages.ContainsKey(channel.Id))
                _cachedEditedMessages.Add(channel.Id, new List<Discord.IMessage>());
            _cachedEditedMessages[channel.Id].Add(before.Value);
        }
    }
}
