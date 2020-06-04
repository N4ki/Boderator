using ArmaforcesMissionBot.DataClasses;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using ArmaforcesMissionBot.DataClasses.SQL;

namespace ArmaforcesMissionBot.Handlers
{
    public class ArchiveHandler : IInstallable
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

            _timer.Elapsed += CheckMissionsToArchiive;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private async void CheckMissionsToArchiive(object source, ElapsedEventArgs e)
        {
            var runtimeData = _services.GetService<RuntimeData>();

            if (runtimeData.OpenedMissions.Count == 0)
                return;

            await using (var db = new DbBoderator())
            {
	            foreach (var missionID in runtimeData.OpenedMissions)
	            {
		            //await mission.Access.WaitAsync(-1);
		            if (db.Missions.Single(q => q.SignupChannel == missionID) is { } mission && mission.CloseDate < e.SignalTime)
		            {
			            var archive = _client.GetChannel(_config.SignupsArchive) as ITextChannel;
			            var archiveEmbed = new EmbedBuilder()
				            .WithColor(Color.Green)
				            .WithTitle(mission.Title)
				            .WithDescription(mission.Description)
				            .WithFooter(mission.Date.ToString())
				            .AddField("Data:", mission.Date.ToString(CultureInfo.InvariantCulture))
				            .AddField("Zamknięcie zapisów:", mission.CloseDate.ToString(CultureInfo.InvariantCulture))
				            .WithAuthor(_client.GetUser(mission.Owner).Username)
				            .AddField("Modlista:", mission.Modlist);

			            if (mission.Attachment != null)
				            archiveEmbed.WithImageUrl(mission.Attachment);

			            var channel = _client.GetChannel(mission.SignupChannel) as ITextChannel;

			            Helpers.MiscHelper.BuildTeamsEmbed(mission.SignupChannel, archiveEmbed);

			            await archive.SendMessageAsync(embed: archiveEmbed.Build());

			            await channel.DeleteAsync();
					}
	            }
            }
        }
    }
}
