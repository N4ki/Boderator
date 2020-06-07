using ArmaforcesMissionBot.DataClasses;
using ArmaforcesMissionBot.Handlers;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using ArmaforcesMissionBot.Database;
using ArmaforcesMissionBot.DataClasses.SQL;
using LinqToDB.Data;

namespace ArmaforcesMissionBot
{
	class Program
    {
        public static void Main(string[] args)
        => new Program().MainAsync(args).GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private IServiceProvider    _services;
        private List<IInstallable>  _handlers;
        private Config              _config;
        private Timer               _timer;
        private int                 _statusCounter;
        private static Program _instance;

        public static RuntimeData GetMissions() => _instance._services.GetService<RuntimeData>();
        public static MissionsArchiveData GetArchiveMissions() => _instance._services.GetService<MissionsArchiveData>();
        public static OpenedDialogs GetDialogs() => _instance._services.GetService<OpenedDialogs>();
        public static IReadOnlyCollection<GuildEmote> GetEmotes() => _instance._client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("AF_AFGuild"))).Emotes;
        public static IReadOnlyCollection<SocketGuildUser> GetUsers() => _instance._client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("AF_AFGuild"))).Users;
        public static SocketTextChannel GetChannel(ulong channelID) => _instance._client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("AF_AFGuild"))).GetTextChannel(channelID);
        public static SocketGuildUser GetGuildUser(ulong userID) => _instance._client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("AF_AFGuild"))).GetUser(userID);
        public static DiscordSocketClient GetClient() => _instance._client;
        public static Config GetConfig() => _instance._config;

        public async Task MainAsync(string[] args)
        {
            _instance = this;

            var config = new DiscordSocketConfig();
            config.MessageCacheSize = 100000;
            //config.LogLevel = LogSeverity.Verbose;
            _client = new DiscordSocketClient(config: config);

            _client.Log += Log;

            _config = new Config();
            _config.Load();

            DataConnection.DefaultSettings = new DbBoderatorSettings(_config);

            _client.GuildAvailable += Load;

            _services = BuildServiceProvider();

            _handlers = new List<IInstallable>();
            foreach (var handler in Assembly.GetEntryAssembly().DefinedTypes)
            {
                if (handler.ImplementedInterfaces.Contains(typeof(IInstallable)))
                {
                    _handlers.Add((IInstallable)Activator.CreateInstance(handler));
                    _ = _handlers.Last().Install(_services);
                }
            }

            await _client.LoginAsync(TokenType.Bot, _config.DiscordToken);
            await _client.StartAsync();

            _timer = new Timer();
            _timer.Interval = 5000;

            _timer.Elapsed += UpdateStatus;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://*:5555")
                .UseStartup<Startup>()
                .Build()
                .Start();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private void UpdateStatus(object sender, ElapsedEventArgs e)
        {
            var runtimeData = _services.GetService<RuntimeData>();
            Game status;
            using (var db = new DbBoderator())
            {
	            if (_statusCounter < runtimeData.OpenedMissions.Count)
	            {
		            var missionId = runtimeData.OpenedMissions.ElementAt(_statusCounter);
		            var mission = db.Missions.Single(x => x.SignupChannel == missionId);
                    status = new Game($"Miejsc: {Helpers.MiscHelper.CountFreeSlots(missionId)}/{Helpers.MiscHelper.CountAllSlots(missionId)} - {mission.Title}");
                }
	            else
	            {
		            status = new Game($"Prowadzone zapisy: {db.Missions.Count(x => x.CloseDate > DateTime.Now)}");
	            }
            }

            if (_statusCounter >= runtimeData.OpenedMissions.Count)
	            _statusCounter = 0;
            else
	            _statusCounter++;

            _client.SetActivityAsync(status);
        }

        public IServiceProvider BuildServiceProvider() => new ServiceCollection()
        .AddSingleton(_client)
        .AddSingleton(_config)
        .AddSingleton<RuntimeData>()
        .AddSingleton<OpenedDialogs>()
        .AddSingleton<MissionsArchiveData>()
        .BuildServiceProvider();

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
