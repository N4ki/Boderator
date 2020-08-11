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

        public static SignupsData GetMissions() => _instance._services.GetService<SignupsData>();
        public static MissionsArchiveData GetArchiveMissions() => _instance._services.GetService<MissionsArchiveData>();
        public static OpenedDialogs GetDialogs() => _instance._services.GetService<OpenedDialogs>();
        public static IReadOnlyCollection<GuildEmote> GetEmotes() => _instance._client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("AF_AFGuild"))).Emotes;
        public static IReadOnlyCollection<SocketGuildUser> GetUsers() => _instance._client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("AF_AFGuild"))).Users;
        public static SocketTextChannel GetChannel(ulong channelID) => _instance._client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("AF_AFGuild"))).GetTextChannel(channelID);
        public static SocketGuildUser GetGuildUser(ulong userID) => _instance._client.GetGuild(ulong.Parse(Environment.GetEnvironmentVariable("AF_AFGuild"))).GetUser(userID);
        public static DiscordSocketClient GetClient() => _instance._client;
        public static Config GetConfig() => _instance._config;

        public static bool IsUserSpamBanned(ulong userID)
        {
            bool isBanned = true;
            var signups = _instance._services.GetService<SignupsData>();

            signups.BanAccess.Wait(-1);
            try
            {
                isBanned = signups.SpamBans.ContainsKey(userID);
            }
            finally
            {
                signups.BanAccess.Release();
            }

            return isBanned;
        }

        public static bool ShowMissionToUser(ulong userID, ulong missionID)
        {
            bool showMission = false;
            var signups = _instance._services.GetService<SignupsData>();

            signups.BanAccess.Wait(-1);
            try
            {
                showMission = !(signups.SignupBans.ContainsKey(userID) && signups.Missions.Any(x => x.SignupChannel == missionID && x.Date < signups.SignupBans[userID]));
            }
            finally
            {
                signups.BanAccess.Release();
            }

            return showMission;
        }

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

            _client.GuildAvailable += Load;
            _client.GuildAvailable += WelcomeAsync;

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
            var signups = _services.GetService<SignupsData>();
            Game status;
            if (_statusCounter < signups.Missions.Where(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.NotEditing).Count())
            {
                var mission = signups.Missions.Where(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.NotEditing).ElementAt(_statusCounter);
                status = new Game($"Miejsc: {Helpers.MiscHelper.CountFreeSlots(mission)}/{Helpers.MiscHelper.CountAllSlots(mission)} - {mission.Title}");
            }
            else
            {
                status = new Game($"Prowadzone zapisy: {signups.Missions.Where(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.NotEditing).Count()}");
            }

            if (_statusCounter >= signups.Missions.Where(x => x.Editing == ArmaforcesMissionBotSharedClasses.Mission.EditEnum.NotEditing).Count())
                _statusCounter = 0;
            else
                _statusCounter++;

            _client.SetActivityAsync(status);
        }

        public IServiceProvider BuildServiceProvider() => new ServiceCollection()
        .AddSingleton(_client)
        .AddSingleton<SignupsData>()
        .AddSingleton(_config)
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

        private async Task WelcomeAsync(SocketGuild guild) {
            var signupsChannel = guild.GetChannel(_config.CreateMissionChannel) as SocketTextChannel;
            signupsChannel?.SendMessageAsync("Bot stoi! 🍆");
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
