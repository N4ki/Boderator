using ArmaforcesMissionBot.DataClasses;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Handlers
{
    public class MessageHandler : IInstallable
    {
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private Config _config;
        static public Dictionary<ulong, Stack<Discord.IMessage>> _cachedDeletedMessages = new Dictionary<ulong, Stack<Discord.IMessage>>();
        static public Dictionary<ulong, Stack<Discord.IMessage>> _cachedEditedMessages = new Dictionary<ulong, Stack<Discord.IMessage>>();
        static public Dictionary<ulong, byte[]> _cachedImages = new Dictionary<ulong, byte[]>();

        public async Task Install(IServiceProvider map)
        {
            _client = map.GetService<DiscordSocketClient>();
            _config = map.GetService<Config>();
            _services = map;
            // Hook the MessageReceived event into our command handler 
            _client.MessageDeleted += MessageDeleted;
            _client.MessageUpdated += MessageUpdated;
            _client.MessageReceived += MessageReceived;
        }

        private async Task MessageDeleted(Discord.Cacheable<Discord.IMessage, ulong> before, ISocketMessageChannel channel)
        {
            if (before.Value == null)
                return;

            if (!_cachedDeletedMessages.ContainsKey(channel.Id))
                _cachedDeletedMessages.Add(channel.Id, new Stack<Discord.IMessage>());
            _cachedDeletedMessages[channel.Id].Push(before.Value);
        }

        private async Task MessageUpdated(Discord.Cacheable<Discord.IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            if (before.Value == null)
                return;

            if (!_cachedEditedMessages.ContainsKey(channel.Id))
                _cachedEditedMessages.Add(channel.Id, new Stack<Discord.IMessage>());
            _cachedEditedMessages[channel.Id].Push(before.Value);
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if(arg.Attachments.Any() && !arg.Author.IsBot)
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync(arg.Attachments.First().Url);
                _cachedImages[arg.Id] = await response.Content.ReadAsByteArrayAsync();
                /*var file = File.Create(arg.Attachments.First().Filename);
                file.Write(_cachedImages[arg.Id], 0, _cachedImages[arg.Id].Length);
                file.Close();*/
            }
        }
    }
}
