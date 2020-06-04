using ArmaforcesMissionBot.DataClasses;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using ArmaforcesMissionBot.DataClasses.SQL;

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
            var runtimeData = _services.GetService<RuntimeData>();

            await runtimeData.BanAccess.WaitAsync(-1);
            try
            {
	            using (var db = new DbBoderator())
	            {
		            if (runtimeData.ActiveSignupBans.Count > 0)
		            {
			            List<ulong> toRemove = new List<ulong>();
			            foreach (var ban in runtimeData.ActiveSignupBans)
			            {
				            if (!db.SignupBans.Where(q => q.UserID == ban).Any(q => q.End > e.SignalTime))
				            {
					            toRemove.Add(ban);
				            }
			            }

			            foreach (var removeID in toRemove)
			            {
				            runtimeData.ActiveSignupBans.Remove(removeID);
			            }

			            if (toRemove.Any())
			            {
				            runtimeData.SignupBansMessage = await Helpers.BanHelper.MakeBanMessage<SignupBansTbl>(
					            _client.GetGuild(_config.AFGuild),
					            runtimeData.SignupBansMessage,
					            _config.HallOfShameChannel,
					            "Bany na zapisy:");
			            }
		            }

		            if (runtimeData.ActiveSpamBans.Count > 0)
		            {
			            List<ulong> toRemove = new List<ulong>();
			            var guild = _client.GetGuild(_config.AFGuild);
			            foreach (var ban in runtimeData.ActiveSpamBans)
			            {
				            if (!db.SpamBans.Where(q => q.UserID == ban).Any(q => q.End > e.SignalTime))
				            {
					            toRemove.Add(ban);
					            var user = _client.GetUser(ban);
					            foreach (var missionID in runtimeData.OpenedMissions)
					            {
						            var channel = guild.GetTextChannel(missionID);
						            await channel.RemovePermissionOverwriteAsync(user);
					            }
				            }
			            }

			            foreach (var removeID in toRemove)
			            {
				            runtimeData.ActiveSpamBans.Remove(removeID);
			            }

			            runtimeData.SpamBansMessage = await Helpers.BanHelper.MakeBanMessage<SpamBansTbl>(
				            guild,
				            runtimeData.SpamBansMessage,
				            _config.HallOfShameChannel,
				            "Bany za spam reakcjami:");
		            }
	            }
            }
            finally
            {
	            runtimeData.BanAccess.Release();
            }
        }
    }
}
