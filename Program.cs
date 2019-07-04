using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBot.Handlers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ArmaforcesMissionBot
{
    class Program
    {
        public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private IServiceProvider    _services;
        private List<IInstallable>  _handlers;
        private Config              _config;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;

            _config = new Config();
            _config.Load();

            _client.GuildAvailable += Load;

            await _client.LoginAsync(TokenType.Bot, _config.DiscordToken);
            await _client.StartAsync();

            _services = BuildServiceProvider();

            _handlers = new List<IInstallable>();
            foreach (var handler in Assembly.GetEntryAssembly().DefinedTypes)
            {
                if (handler.ImplementedInterfaces.Contains(typeof(IInstallable)))
                {
                    _handlers.Add((IInstallable)Activator.CreateInstance(handler));
                    _handlers.Last().Install(_services);
                }
            }

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        public IServiceProvider BuildServiceProvider() => new ServiceCollection()
        .AddSingleton(_client)
        .AddSingleton<SignupsData>()
        .AddSingleton(_config)
        .BuildServiceProvider();

        private void LoadState()
        {

        }

        private async Task Load(SocketGuild guild)
        {
            if(guild.CategoryChannels.Any(x => x.Id == _config.SignupsCategory))
            {
                var signups = guild.CategoryChannels.Single(x => x.Id == _config.SignupsCategory).Channels.Where(x => x.Id != _config.SignupsArchive);
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
