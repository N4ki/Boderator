using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ArmaforcesMissionBot.Handlers 
{
    public class NotificationHandler : IInstallable
    {
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private Config _config;
        private Timer _timer;
        public Task Install(IServiceProvider map)
        {
            _client = map.GetService<DiscordSocketClient>();
            _config = map.GetService<Config>();
            _services = map;

            _timer = new Timer();
            _timer.Interval = 60 * 1000;

            _timer.Elapsed += CheckSendMissionNotification;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            return Task.CompletedTask;
        }

        private async void CheckSendMissionNotification(object sender, ElapsedEventArgs e)
        {
            var missions = _services.GetService<SignupsData>().Missions;

            if (missions.Count == 0)
                return;
            
            foreach(var mission in missions) 
            {
                await mission.Access.WaitAsync(-1);
                try 
                {
                    if (!mission.WasMentioned && (DateTime.Now - mission.Date) < new TimeSpan(1,0,0))
                    {
                        var channel = _client.GetChannel(mission.SignupChannel) as ITextChannel;
                        await channel.SendMessageAsync($"@{mission.Title} Misja rozpocznie się za ok. godzinę");
                        mission.WasMentioned = true;
                    }
                }
                finally
                {
                    mission.Access.Release();
                }
            }
        }
    }
}