using ArmaforcesMissionBot.DataClasses;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot.Handlers
{
    public class InfoHandler : IInstallable
    {
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private Config _config;

        public async Task Install(IServiceProvider map)
        {
            _client = map.GetService<DiscordSocketClient>();
            _config = map.GetService<Config>();
            _services = map;
            // Hook the MessageReceived event into our command handler
            _client.GuildAvailable += GuildAvailable;
        }

        private async Task GuildAvailable(SocketGuild guild)
        {
            var channel = guild.GetTextChannel(_config.CreateMissionChannel);
            await channel.SendMessageAsync("Witam! Zalogowałem się.");
        }


    }
}
