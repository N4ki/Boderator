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
            var signups = _services.GetService<SignupsData>();

            if (signups.Missions.Count == 0)
                return;

            foreach (var mission in signups.Missions)
            {
                await mission.Access.WaitAsync(-1);
                try
                {
                    if(!mission.Editing && mission.Date.AddMinutes(-mission.CloseTime) < e.SignalTime)
                    {
                        var archive = _client.GetChannel(_config.SignupsArchive) as ITextChannel;
                        var archiveEmbed = new EmbedBuilder()
                            .WithColor(Color.Green)
                            .WithTitle(mission.Title)
                            .WithDescription(mission.Description)
                            .WithFooter(mission.Date.ToString())
                            .WithAuthor(_client.GetUser(mission.Owner).Username)
                            .AddField("Modlista:", mission.Modlist);

                        if (mission.Attachment != null)
                            archiveEmbed.WithImageUrl(mission.Attachment);

                        var channel = _client.GetChannel(mission.SignupChannel) as ITextChannel;

                        var messages = channel.GetMessagesAsync();
                        List<IMessage> messagesNormal = new List<IMessage>();
                        await messages.ForEachAsync(async x =>
                        {
                            foreach (var it in x)
                            {
                                messagesNormal.Add(it);
                            }
                        });

                        foreach (IMessage message in messagesNormal.AsEnumerable().Reverse())
                        {
                            if (message.Author.Id == _client.CurrentUser.Id && message.Embeds.Count() > 0)
                            {
                                var embed = message.Embeds.Single();
                                if (embed.Author == null)
                                {
                                    var title = embed.Title;
                                    foreach (var slot in mission.Teams.Single(x => x.TeamMsg == message.Id).Slots)
                                    {
                                        if (title.Contains(slot.Emoji))
                                            title = title.Remove(title.IndexOf(slot.Emoji));
                                    }
                                    archiveEmbed.AddField(title, embed.Description, true);
                                }
                            }
                        }

                        await channel.DeleteAsync();
                        signups.Missions.Remove(mission);

                        await archive.SendMessageAsync(embed: archiveEmbed.Build());
                        break;
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
