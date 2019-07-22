using ArmaforcesMissionBot.DataClasses;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ArmaforcesMissionBot.Handlers
{
    public class BanHandler : IInstallable
    {
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private Config _config;
        private Timer _timer;

        public async Task Install(IServiceProvider map)
        {
            _client = map.GetService<DiscordSocketClient>();
            _config = map.GetService<Config>();
            _services = map;
            // Hook the MessageReceived event into our command handler
            _timer = new Timer();
            _timer.Interval = 60000;

            _timer.Elapsed += CheckBans;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private async void CheckBans(object source, ElapsedEventArgs e)
        {
            var signups = _services.GetService<SignupsData>();

            await signups.BanAccess.WaitAsync();

            try
            {
                if (signups.SignupBans.Count > 0)
                {
                    List<ulong> toRemove = new List<ulong>();
                    foreach (var ban in signups.SignupBans)
                    {
                        if (ban.Value < e.SignalTime)
                        {
                            toRemove.Add(ban.Key);
                            signups.SignupBansMessage = await Helpers.BanHelper.MakeBanMessage(
                                _services, 
                                _client.GetGuild(_config.AFGuild), 
                                signups.SignupBans, 
                                signups.SignupBansMessage,
                                _config.BanAnnouncementChannel,
                                "Bany na zapisy:");
                        }
                    }
                    foreach(var removeID in toRemove)
                    {
                        signups.SignupBans.Remove(removeID);
                    }
                }
                if(signups.SpamBans.Count > 0)
                {
                    List<ulong> toRemove = new List<ulong>();
                    var guild = _client.GetGuild(_config.AFGuild);
                    foreach (var ban in signups.SpamBans)
                    {
                        if (ban.Value < e.SignalTime)
                        {
                            toRemove.Add(ban.Key);
                            var user = _client.GetUser(ban.Key);
                            if (signups.Missions.Count > 0)
                            {
                                foreach (var mission in signups.Missions)
                                {
                                    var channel = guild.GetTextChannel(mission.SignupChannel);
                                    await channel.RemovePermissionOverwriteAsync(user);
                                }
                            }
                        }
                    }
                    foreach (var removeID in toRemove)
                    {
                        signups.SpamBans.Remove(removeID);
                    }
                    signups.SpamBansMessage = await Helpers.BanHelper.MakeBanMessage(
                        _services, 
                        guild,
                        signups.SpamBans,
                        signups.SpamBansMessage,
                        _config.BanAnnouncementChannel,
                        "Bany za spam reakcjami:");
                }
            }
            finally
            {
                signups.BanAccess.Release();
            }
        }
    }
}
